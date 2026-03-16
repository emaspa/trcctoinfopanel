using System.Text.RegularExpressions;

namespace TrccToInfoPanel;

/// <summary>
/// Reads TRCC config1.dc binary theme files.
/// Supports format version 221 (0xDD) - the current TRCC format.
/// </summary>
public static partial class TrccThemeReader
{
    public static TrccTheme Read(string configPath)
    {
        var themeDir = Path.GetDirectoryName(configPath)
            ?? throw new ArgumentException("Invalid config path");

        var theme = new TrccTheme
        {
            ThemeDirectory = themeDir,
            ThemeName = Path.GetFileName(themeDir)
        };

        // Parse resolution from parent/grandparent directory name
        // Handles: Theme800480/Theme1, zt1600720/000a, etc.
        var parentDir = Path.GetFileName(Path.GetDirectoryName(themeDir)) ?? "";
        if (!ParseResolution(parentDir, theme))
        {
            // Try grandparent (e.g., Web/zt1600720/000a -> zt1600720)
            var grandparentDir = Path.GetFileName(
                Path.GetDirectoryName(Path.GetDirectoryName(themeDir))) ?? "";
            ParseResolution(grandparentDir, theme);
        }

        using var stream = File.OpenRead(configPath);
        using var reader = new BinaryReader(stream);

        var formatByte = reader.ReadByte();
        if (formatByte != 221 && formatByte != 220)
            throw new InvalidDataException($"Unknown format byte: {formatByte} (expected 220 or 221)");

        if (formatByte == 220)
            throw new NotSupportedException("Legacy format (0xDC/220) is not supported. Only format 221 (0xDD) themes can be converted.");

        ReadFormat221(reader, theme);
        return theme;
    }

    private static void ReadFormat221(BinaryReader reader, TrccTheme theme)
    {
        theme.SystemInfoEnabled = reader.ReadBoolean();
        var count = reader.ReadInt32();

        for (int i = 0; i < count; i++)
        {
            var element = new TrccDisplayElement
            {
                MyMode = reader.ReadInt32(),
                MyModeSub = reader.ReadInt32(),
                X = reader.ReadInt32(),
                Y = reader.ReadInt32(),
                MainCount = reader.ReadInt32(),
                SubCount = reader.ReadInt32(),
                FontName = reader.ReadString(),
                FontSize = reader.ReadSingle(),
                FontStyle = reader.ReadByte(),
                FontUnit = reader.ReadByte(),
                FontGdiCharSet = reader.ReadByte(),
                ColorA = reader.ReadByte(),
                ColorR = reader.ReadByte(),
                ColorG = reader.ReadByte(),
                ColorB = reader.ReadByte(),
                Text = reader.ReadString()
            };

            theme.Elements.Add(element);
        }

        // Display settings after elements
        theme.BackgroundEnabled = reader.ReadBoolean();
        theme.TransparencyEnabled = reader.ReadBoolean();
        theme.Direction = reader.ReadInt32();
        theme.UIMode = reader.ReadInt32();
        theme.Mode = reader.ReadInt32();
        theme.TransparentBackground = reader.ReadBoolean();
        theme.JpX = reader.ReadInt32();
        theme.JpY = reader.ReadInt32();
        theme.JpW = reader.ReadInt32();
        theme.JpH = reader.ReadInt32();
        theme.MaskEnabled = reader.ReadBoolean();
        theme.MaskX = reader.ReadInt32();
        theme.MaskY = reader.ReadInt32();
    }

    private static bool ParseResolution(string dirName, TrccTheme theme)
    {
        // Match "Theme800480", "Theme1600720u", "zt1600720", etc.
        var match = ResolutionRegex().Match(dirName);
        if (!match.Success) return false;

        var digits = match.Groups[1].Value;

        // Known resolution patterns (width x height)
        var knownResolutions = new Dictionary<string, (int w, int h)>
        {
            ["240240"] = (240, 240), ["240320"] = (240, 320), ["320240"] = (320, 240),
            ["320320"] = (320, 320), ["360360"] = (360, 360), ["480480"] = (480, 480),
            ["480640"] = (480, 640), ["480800"] = (480, 800), ["480854"] = (480, 854),
            ["640172"] = (640, 172), ["640480"] = (640, 480), ["800480"] = (800, 480),
            ["854480"] = (854, 480), ["960320"] = (960, 320), ["960540"] = (960, 540),
            ["540960"] = (540, 960), ["1280480"] = (1280, 480), ["1600720"] = (1600, 720),
            ["1920440"] = (1920, 440), ["1920462"] = (1920, 462),
        };

        if (knownResolutions.TryGetValue(digits, out var res))
        {
            theme.CanvasWidth = res.w;
            theme.CanvasHeight = res.h;
            return true;
        }
        return false;
    }

    [GeneratedRegex(@"(?:Theme|zt)(\d+)")]
    private static partial Regex ResolutionRegex();
}
