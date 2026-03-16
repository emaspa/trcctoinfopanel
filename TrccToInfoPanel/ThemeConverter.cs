namespace TrccToInfoPanel;

/// <summary>
/// Converts a parsed TRCC theme into InfoPanel profile + display items.
/// </summary>
public static class ThemeConverter
{
    // TRCC sensor category names
    private static readonly string[] SensorCategories = ["CPU", "GPU", "Memory", "HDD", "Network", "FAN"];

    // TRCC sensor fields per category: [mainCount][subCount-1]
    private static readonly string[,] SensorFields =
    {
        // CPU (mainCount=0)
        { "Temperature", "Usage", "Clock", "Power" },
        // GPU (mainCount=1)
        { "Temperature", "Usage", "Clock", "Power" },
        // Memory (mainCount=2)
        { "Temperature", "Usage", "Clock", "Available" },
        // HDD (mainCount=3)
        { "Temperature", "Activity", "Read Speed", "Write Speed" },
        // Network (mainCount=4)
        { "Upload Rate", "Download Rate", "Total Upload", "Total Download" },
        // FAN (mainCount=5)
        { "CPU Fan", "GPU Fan", "Fan 1", "Fan 2" },
    };

    private static readonly string[,] SensorUnits =
    {
        { "°C", "%", "MHz", "W" },       // CPU
        { "°C", "%", "MHz", "W" },       // GPU
        { "°C", "%", "MHz", "GB" },      // Memory
        { "°C", "%", "MB/s", "MB/s" },   // HDD
        { "KB/s", "KB/s", "MB", "MB" },  // Network
        { "RPM", "RPM", "RPM", "RPM" },  // FAN
    };

    // Placeholder LibreHardwareMonitor sensor IDs for common sensors
    private static readonly string[,] LibreSensorIds =
    {
        { "/intelcpu/0/temperature/6", "/intelcpu/0/load/0", "/intelcpu/0/clock/1", "/intelcpu/0/power/0" },
        { "/gpu-nvidia/0/temperature/0", "/gpu-nvidia/0/load/0", "/gpu-nvidia/0/clock/0", "/gpu-nvidia/0/power/0" },
        { "", "/ram/load/0", "", "/ram/data/1" },
        { "", "", "", "" },
        { "", "", "", "" },
        { "", "", "", "" },
    };

    public static (Profile profile, List<DisplayItem> items) Convert(TrccTheme theme)
    {
        var profile = new Profile
        {
            Name = $"TRCC - {theme.ThemeName}",
            Width = theme.CanvasWidth > 0 ? theme.CanvasWidth : 800,
            Height = theme.CanvasHeight > 0 ? theme.CanvasHeight : 480,
            BackgroundColor = "#FF000000",
            Active = true,
        };

        var items = new List<DisplayItem>();

        // Add background image if the theme has one
        var bgPath = FindBackgroundImage(theme.ThemeDirectory);
        if (bgPath != null)
        {
            items.Add(new ImageDisplayItem
            {
                Name = Path.GetFileName(bgPath),
                X = 0,
                Y = 0,
                FilePath = Path.GetFileName(bgPath),
                RelativePath = true,
                Width = profile.Width,
                Height = profile.Height,
            });
        }

        // Convert each TRCC display element
        foreach (var element in theme.Elements)
        {
            var item = ConvertElement(element);
            if (item != null)
                items.Add(item);
        }

        return (profile, items);
    }

    private static DisplayItem? ConvertElement(TrccDisplayElement element)
    {
        return element.MyMode switch
        {
            0 => ConvertSensorElement(element),
            1 => ConvertTimeElement(element),
            2 => ConvertDayOfWeekElement(element),
            3 => ConvertDateElement(element),
            4 => ConvertCustomTextElement(element),
            5 => ConvertImageElement(element),
            _ => ConvertCustomTextElement(element), // fallback
        };
    }

    private static DisplayItem ConvertSensorElement(TrccDisplayElement element)
    {
        var mainCount = element.MainCount;
        var subCount = element.SubCount;

        string sensorName;
        string unit;
        string libreSensorId;

        if (mainCount >= 0 && mainCount < SensorCategories.Length
            && subCount >= 1 && subCount <= 4)
        {
            var category = SensorCategories[mainCount];
            var field = SensorFields[mainCount, subCount - 1];
            sensorName = $"{category} {field}";
            unit = SensorUnits[mainCount, subCount - 1];
            libreSensorId = LibreSensorIds[mainCount, subCount - 1];
        }
        else if (mainCount == 10000)
        {
            sensorName = "Fan LCD RPM";
            unit = "RPM";
            libreSensorId = "";
        }
        else
        {
            sensorName = $"Sensor {mainCount}:{subCount}";
            unit = "";
            libreSensorId = "";
        }

        return new SensorDisplayItem
        {
            Name = sensorName,
            X = element.X,
            Y = element.Y,
            Font = MapFontName(element.FontName),
            FontSize = (int)element.FontSize,
            Bold = (element.FontStyle & 1) != 0,
            Italic = (element.FontStyle & 2) != 0,
            Underline = (element.FontStyle & 4) != 0,
            Strikeout = (element.FontStyle & 8) != 0,
            FontStyle = MapFontStyle(element.FontStyle),
            Color = ToHexColor(element.ColorA, element.ColorR, element.ColorG, element.ColorB),
            SensorName = sensorName,
            SensorType = "Libre",
            LibreSensorId = libreSensorId,
            Unit = unit,
            ShowUnit = true,
        };
    }

    private static DisplayItem ConvertTimeElement(TrccDisplayElement element)
    {
        var format = element.MyModeSub switch
        {
            1 => "hh:mm tt",
            _ => "HH:mm",
        };

        return new ClockDisplayItem
        {
            Name = "Clock",
            X = element.X,
            Y = element.Y,
            Font = MapFontName(element.FontName),
            FontSize = (int)element.FontSize,
            Bold = (element.FontStyle & 1) != 0,
            Italic = (element.FontStyle & 2) != 0,
            FontStyle = MapFontStyle(element.FontStyle),
            Color = ToHexColor(element.ColorA, element.ColorR, element.ColorG, element.ColorB),
            Format = format,
        };
    }

    private static DisplayItem ConvertDayOfWeekElement(TrccDisplayElement element)
    {
        return new CalendarDisplayItem
        {
            Name = "Day of Week",
            X = element.X,
            Y = element.Y,
            Font = MapFontName(element.FontName),
            FontSize = (int)element.FontSize,
            Bold = (element.FontStyle & 1) != 0,
            Italic = (element.FontStyle & 2) != 0,
            FontStyle = MapFontStyle(element.FontStyle),
            Color = ToHexColor(element.ColorA, element.ColorR, element.ColorG, element.ColorB),
            Format = "dddd",
        };
    }

    private static DisplayItem ConvertDateElement(TrccDisplayElement element)
    {
        var format = element.MyModeSub switch
        {
            0 or 1 => "yyyy/MM/dd",
            2 => "dd/MM/yyyy",
            3 => "MM/dd",
            4 => "dd/MM",
            _ => "yyyy/MM/dd",
        };

        return new CalendarDisplayItem
        {
            Name = "Date",
            X = element.X,
            Y = element.Y,
            Font = MapFontName(element.FontName),
            FontSize = (int)element.FontSize,
            Bold = (element.FontStyle & 1) != 0,
            Italic = (element.FontStyle & 2) != 0,
            FontStyle = MapFontStyle(element.FontStyle),
            Color = ToHexColor(element.ColorA, element.ColorR, element.ColorG, element.ColorB),
            Format = format,
        };
    }

    private static DisplayItem ConvertCustomTextElement(TrccDisplayElement element)
    {
        return new TextDisplayItem
        {
            Name = string.IsNullOrEmpty(element.Text) ? "Text" : element.Text,
            X = element.X,
            Y = element.Y,
            Font = MapFontName(element.FontName),
            FontSize = (int)element.FontSize,
            Bold = (element.FontStyle & 1) != 0,
            Italic = (element.FontStyle & 2) != 0,
            Underline = (element.FontStyle & 4) != 0,
            Strikeout = (element.FontStyle & 8) != 0,
            FontStyle = MapFontStyle(element.FontStyle),
            Color = ToHexColor(element.ColorA, element.ColorR, element.ColorG, element.ColorB),
        };
    }

    private static DisplayItem? ConvertImageElement(TrccDisplayElement element)
    {
        // Mode 5 = image from assembly resource, text contains the path
        if (string.IsNullOrEmpty(element.Text))
            return null;

        return new ImageDisplayItem
        {
            Name = Path.GetFileName(element.Text),
            X = element.X,
            Y = element.Y,
            FilePath = element.Text,
            RelativePath = false,
        };
    }

    private static string? FindBackgroundImage(string themeDir)
    {
        // TRCC themes store background images as 00.png or 01.png
        var candidates = new[] { "00.png", "01.png" };
        foreach (var name in candidates)
        {
            var path = Path.Combine(themeDir, name);
            if (File.Exists(path))
                return path;
        }
        return null;
    }

    private static string MapFontName(string trccFont)
    {
        // Map common Chinese font names to their English equivalents
        return trccFont switch
        {
            "微软雅黑" => "Microsoft YaHei",
            "宋体" => "SimSun",
            "黑体" => "SimHei",
            "楷体" => "KaiTi",
            "仿宋" => "FangSong",
            _ => string.IsNullOrEmpty(trccFont) ? "Arial" : trccFont,
        };
    }

    private static string MapFontStyle(byte style)
    {
        if (style == 0) return "Regular";
        var parts = new List<string>();
        if ((style & 1) != 0) parts.Add("Bold");
        if ((style & 2) != 0) parts.Add("Italic");
        return parts.Count > 0 ? string.Join(", ", parts) : "Regular";
    }

    private static string ToHexColor(byte a, byte r, byte g, byte b) =>
        $"#{a:X2}{r:X2}{g:X2}{b:X2}";
}
