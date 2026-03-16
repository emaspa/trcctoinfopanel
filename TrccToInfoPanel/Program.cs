using TrccToInfoPanel;

if (args.Length < 1)
{
    Console.WriteLine("TRCC to InfoPanel Theme Converter");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  TrccToInfoPanel <config1.dc>              Convert a single theme");
    Console.WriteLine("  TrccToInfoPanel <ThemeDirectory>           Convert a single theme directory");
    Console.WriteLine("  TrccToInfoPanel <ThemeResolutionDir> --all Convert all themes in a resolution folder");
    Console.WriteLine("  TrccToInfoPanel --batch <USBLCD_Dir>      Convert all resolutions and themes");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  -o, --output <dir>   Output directory (default: ./output)");
    Console.WriteLine("  --all                Convert all Theme1-5 in a resolution folder");
    Console.WriteLine("  --batch              Convert everything under a USBLCD directory");
    Console.WriteLine("  --dump               Dump parsed TRCC theme info without converting");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine(@"  TrccToInfoPanel C:\TRCCCAP\Data\USBLCD\Theme800480\Theme1\config1.dc");
    Console.WriteLine(@"  TrccToInfoPanel C:\TRCCCAP\Data\USBLCD\Theme800480\Theme1");
    Console.WriteLine(@"  TrccToInfoPanel C:\TRCCCAP\Data\USBLCD\Theme800480 --all");
    Console.WriteLine(@"  TrccToInfoPanel --batch C:\TRCCCAP\Data\USBLCD -o converted");
    return 1;
}

var outputDir = "./output";
var dump = false;
var batchMode = false;
var convertAll = false;
string? inputPath = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-o" or "--output":
            if (i + 1 < args.Length) outputDir = args[++i];
            break;
        case "--dump":
            dump = true;
            break;
        case "--batch":
            batchMode = true;
            break;
        case "--all":
            convertAll = true;
            break;
        default:
            inputPath ??= args[i];
            break;
    }
}

if (inputPath == null)
{
    Console.Error.WriteLine("Error: No input path specified.");
    return 1;
}

try
{
    if (batchMode)
        return BatchConvert(inputPath, outputDir, dump);
    if (convertAll)
        return ConvertAllInResolution(inputPath, outputDir, dump);
    return ConvertSingle(inputPath, outputDir, dump);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}

static int ConvertSingle(string path, string outputDir, bool dump)
{
    var configPath = path;

    // If path is a directory, look for config1.dc inside it
    if (Directory.Exists(path))
    {
        configPath = Path.Combine(path, "config1.dc");
        if (!File.Exists(configPath))
        {
            Console.Error.WriteLine($"Error: config1.dc not found in {path}");
            return 1;
        }
    }

    if (!File.Exists(configPath))
    {
        Console.Error.WriteLine($"Error: File not found: {configPath}");
        return 1;
    }

    Console.WriteLine($"Reading: {configPath}");
    var theme = TrccThemeReader.Read(configPath);

    if (dump)
    {
        DumpTheme(theme);
        return 0;
    }

    Console.WriteLine($"Converting: {theme.CanvasWidth}x{theme.CanvasHeight} theme with {theme.Elements.Count} elements");
    var (profile, items) = ThemeConverter.Convert(theme);
    InfoPanelWriter.WriteProfile(outputDir, profile, items, theme.ThemeDirectory);
    Console.WriteLine("Done!");
    return 0;
}

static int ConvertAllInResolution(string resolutionDir, string outputDir, bool dump)
{
    if (!Directory.Exists(resolutionDir))
    {
        Console.Error.WriteLine($"Error: Directory not found: {resolutionDir}");
        return 1;
    }

    var themeDirs = Directory.GetDirectories(resolutionDir, "Theme*")
        .Where(d => File.Exists(Path.Combine(d, "config1.dc")))
        .OrderBy(d => d)
        .ToList();

    if (themeDirs.Count == 0)
    {
        Console.Error.WriteLine($"No themes with config1.dc found in {resolutionDir}");
        return 1;
    }

    Console.WriteLine($"Found {themeDirs.Count} themes in {Path.GetFileName(resolutionDir)}");
    var errors = 0;

    foreach (var dir in themeDirs)
    {
        try
        {
            ConvertSingle(dir, outputDir, dump);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  Error converting {dir}: {ex.Message}");
            errors++;
        }
    }

    return errors > 0 ? 1 : 0;
}

static int BatchConvert(string usblcdDir, string outputDir, bool dump)
{
    if (!Directory.Exists(usblcdDir))
    {
        Console.Error.WriteLine($"Error: Directory not found: {usblcdDir}");
        return 1;
    }

    var resolutionDirs = Directory.GetDirectories(usblcdDir, "Theme*")
        .OrderBy(d => d)
        .ToList();

    if (resolutionDirs.Count == 0)
    {
        Console.Error.WriteLine($"No Theme* directories found in {usblcdDir}");
        return 1;
    }

    Console.WriteLine($"Batch converting {resolutionDirs.Count} resolution folders...");
    Console.WriteLine();
    var total = 0;
    var errors = 0;

    foreach (var resDir in resolutionDirs)
    {
        var themeDirs = Directory.GetDirectories(resDir, "Theme*")
            .Where(d => File.Exists(Path.Combine(d, "config1.dc")))
            .OrderBy(d => d)
            .ToList();

        foreach (var dir in themeDirs)
        {
            try
            {
                ConvertSingle(dir, outputDir, dump);
                total++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  Error: {dir}: {ex.Message}");
                errors++;
            }
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Converted {total} themes ({errors} errors)");
    return errors > 0 ? 1 : 0;
}

static void DumpTheme(TrccTheme theme)
{
    Console.WriteLine($"  Canvas:     {theme.CanvasWidth}x{theme.CanvasHeight}");
    Console.WriteLine($"  SysInfo:    {theme.SystemInfoEnabled}");
    Console.WriteLine($"  Background: {theme.BackgroundEnabled}");
    Console.WriteLine($"  Transparent:{theme.TransparencyEnabled}");
    Console.WriteLine($"  Direction:  {theme.Direction}°");
    Console.WriteLine($"  UIMode:     {theme.UIMode}");
    Console.WriteLine($"  Mode:       {theme.Mode}");
    Console.WriteLine($"  Mask:       {theme.MaskEnabled} ({theme.MaskX}, {theme.MaskY})");
    Console.WriteLine($"  Crop:       ({theme.JpX}, {theme.JpY}) {theme.JpW}x{theme.JpH}");
    Console.WriteLine($"  Elements:   {theme.Elements.Count}");
    Console.WriteLine();

    foreach (var (el, i) in theme.Elements.Select((e, i) => (e, i)))
    {
        var modeStr = el.MyMode switch
        {
            0 => $"Sensor (main={el.MainCount}, sub={el.SubCount})",
            1 => $"Time (sub={el.MyModeSub})",
            2 => "Day of Week",
            3 => $"Date (sub={el.MyModeSub})",
            4 => $"Text: \"{el.Text}\"",
            5 => $"Image: \"{el.Text}\"",
            _ => $"Unknown({el.MyMode})",
        };
        var color = $"#{el.ColorA:X2}{el.ColorR:X2}{el.ColorG:X2}{el.ColorB:X2}";
        Console.WriteLine($"  [{i}] Mode={el.MyMode}: {modeStr}");
        Console.WriteLine($"      Pos=({el.X},{el.Y}) Font=\"{el.FontName}\" Size={el.FontSize} Color={color}");
    }
}
