using System.Text.RegularExpressions;

namespace TrccToInfoPanel;

/// <summary>
/// Reads TRCC config1.dc binary theme files.
/// Supports format 221 (0xDD) and legacy format 220 (0xDC).
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

        if (formatByte == 221)
            ReadFormat221(reader, theme);
        else
            ReadFormat220(reader, theme);

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

    /// <summary>
    /// Format 220 has 14 hardcoded font+color blocks with fixed sensor assignments,
    /// followed by 14 position pairs, then display settings and advanced elements.
    /// </summary>
    private static void ReadFormat220(BinaryReader reader, TrccTheme theme)
    {
        // Header
        reader.ReadInt32(); // skip
        reader.ReadInt32(); // skip
        bool flagHeader = reader.ReadBoolean();    // header text enabled
        theme.SystemInfoEnabled = reader.ReadBoolean(); // myXtxx
        bool flagCpuTemp = reader.ReadBoolean();   // CPU temp sensor
        bool flagCpuClock = reader.ReadBoolean();  // CPU clock sensor
        bool flagMemory = reader.ReadBoolean();    // Memory sensor
        bool flagGpuTemp = reader.ReadBoolean();   // GPU temp sensor
        bool flagGpuClock = reader.ReadBoolean();  // GPU clock sensor
        bool flagStorageMem = reader.ReadBoolean();// Storage/Memory2 sensor
        reader.ReadInt32(); // skip

        // Each element slot: font+color block always read, but only used if flag is set.
        // ArrayList index layout: [0]=myMode, [1]=myModeSub, [2]=x, [3]=y, [4]=mainCount, [5]=subCount

        // Slot 0: Header text (mode=4 custom text) - has an extra text string prefix
        var headerText = reader.ReadString();
        var headerFc = ReadFontColor(reader);

        // Slot 1: CPU temp value (mode=0, sensor main=0, sub=1)
        var cpuTempValueFc = ReadFontColor(reader);

        // Slot 2: CPU temp label (mode=4, text="CPU")
        var cpuTempLabelFc = ReadFontColor(reader);

        // Slot 3: CPU clock value (mode=0, sensor main=0, sub=3)
        var cpuClockValueFc = ReadFontColor(reader);

        // Slot 4: CPU clock label (mode=4, text="CPU")
        var cpuClockLabelFc = ReadFontColor(reader);

        // Slot 5: Memory value (mode=0, sensor main=0, sub=2)
        // Actually from code: main=0, sub=2 => CPU Usage. But flag5 maps to Memory.
        // Let me re-check the arrayList mappings from decompiled code:
        // arrayList5 (flag3/cpuTemp): mode=0, modeSub=1, main=0, sub=1 -> CPU Temp
        // arrayList6 (flag4/cpuClock): mode=0, modeSub=1, main=0, sub=3 -> CPU Clock
        // arrayList7 (flag5/memory): mode=0, modeSub=1, main=0, sub=2 -> CPU Usage (labeled as Memory)
        // arrayList8 (flag6/gpuTemp): mode=0, modeSub=1, main=1, sub=1 -> GPU Temp
        // arrayList9 (flag7/gpuClock): mode=0, modeSub=1, main=1, sub=3 -> GPU Clock
        // arrayList10 (flag8/storageMem): mode=0, modeSub=1, main=1, sub=2 -> GPU Usage
        var memValueFc = ReadFontColor(reader);

        // Slot 6: Memory label (mode=4, text="CPU")
        var memLabelFc = ReadFontColor(reader);

        // Slot 7: GPU temp value (mode=0, sensor main=1, sub=1)
        var gpuTempValueFc = ReadFontColor(reader);

        // Slot 8: GPU temp label (mode=4, text="GPU")
        var gpuTempLabelFc = ReadFontColor(reader);

        // Slot 9: GPU clock value (mode=0, sensor main=1, sub=3)
        var gpuClockValueFc = ReadFontColor(reader);

        // Slot 10: GPU clock label (mode=4, text="GPU")
        var gpuClockLabelFc = ReadFontColor(reader);

        // Slot 11: Storage/Mem2 value (mode=0, sensor main=1, sub=2)
        var stoMemValueFc = ReadFontColor(reader);

        // Slot 12: Storage/Mem2 label
        var stoMemLabelFc = ReadFontColor(reader);

        // --- Display settings ---
        theme.BackgroundEnabled = reader.ReadBoolean();  // myBjxs
        theme.TransparencyEnabled = reader.ReadBoolean(); // myTpxs
        theme.Direction = reader.ReadInt32();  // directionB
        theme.UIMode = reader.ReadInt32();     // myUIMode

        // --- Positions: 14 pairs (x,y) for slots 0-13 ---
        // Order matches the arrayList assignment order in the decompiled code:
        // arrayList4(header), arrayList5(cpuTempVal), arrayList11(cpuTempLabel),
        // arrayList6(cpuClockVal), arrayList12(cpuClockLabel),
        // arrayList7(memVal), arrayList13(memLabel),
        // arrayList8(gpuTempVal), arrayList14(gpuTempLabel),
        // arrayList9(gpuClockVal), arrayList15(gpuClockLabel),
        // arrayList10(stoMemVal), arrayList16(stoMemLabel)
        int headerX = reader.ReadInt32(), headerY = reader.ReadInt32();
        int cpuTempValX = reader.ReadInt32(), cpuTempValY = reader.ReadInt32();
        int cpuTempLblX = reader.ReadInt32(), cpuTempLblY = reader.ReadInt32();
        int cpuClockValX = reader.ReadInt32(), cpuClockValY = reader.ReadInt32();
        int cpuClockLblX = reader.ReadInt32(), cpuClockLblY = reader.ReadInt32();
        int memValX = reader.ReadInt32(), memValY = reader.ReadInt32();
        int memLblX = reader.ReadInt32(), memLblY = reader.ReadInt32();
        int gpuTempValX = reader.ReadInt32(), gpuTempValY = reader.ReadInt32();
        int gpuTempLblX = reader.ReadInt32(), gpuTempLblY = reader.ReadInt32();
        int gpuClockValX = reader.ReadInt32(), gpuClockValY = reader.ReadInt32();
        int gpuClockLblX = reader.ReadInt32(), gpuClockLblY = reader.ReadInt32();
        int stoMemValX = reader.ReadInt32(), stoMemValY = reader.ReadInt32();
        int stoMemLblX = reader.ReadInt32(), stoMemLblY = reader.ReadInt32();

        // Theme name string (unused)
        reader.ReadString();

        // modeSub override from boolean
        bool modeSubFlag = reader.ReadBoolean();
        int modeSub = modeSubFlag ? 1 : 0;

        // Mode
        theme.Mode = reader.ReadInt32();

        // Background/crop settings
        theme.TransparentBackground = reader.ReadBoolean();
        theme.JpX = reader.ReadInt32();
        theme.JpY = reader.ReadInt32();
        theme.JpW = reader.ReadInt32();
        theme.JpH = reader.ReadInt32();

        // Mask settings
        theme.MaskEnabled = reader.ReadBoolean();
        theme.MaskX = reader.ReadInt32();
        theme.MaskY = reader.ReadInt32();

        // Advanced elements
        bool advancedEnabled = reader.ReadBoolean();  // flag10
        bool iconEnabled = reader.ReadBoolean();       // flag11 (date display)
        bool clockEnabled = reader.ReadBoolean();      // flag12 (time display)
        int advSub1 = reader.ReadInt32();  // num7 - date modeSub
        int advSub2 = reader.ReadInt32();  // num8 - time modeSub
        int advX1 = reader.ReadInt32();    // num9 - date X
        int advY1 = reader.ReadInt32();    // num10 - date Y
        int advX2 = reader.ReadInt32();    // num11 - time X
        int advY2 = reader.ReadInt32();    // num12 - time Y

        // Font+color for date display
        var dateFc = ReadFontColor(reader);
        // Font+color for time display
        var timeFc = ReadFontColor(reader);

        // Custom text element
        bool customTextEnabled = reader.ReadBoolean(); // flag13
        int customTextX = reader.ReadInt32();
        int customTextY = reader.ReadInt32();
        var customTextFc = ReadFontColor(reader);

        // --- Build elements list ---
        // Add advanced elements first (they get inserted at index 0 in the original code)
        if (advancedEnabled && customTextEnabled)
        {
            theme.Elements.Add(MakeElement(2, 0, customTextX, customTextY, 0, 0, customTextFc, ""));
        }
        if (advancedEnabled && clockEnabled)
        {
            theme.Elements.Add(MakeElement(1, advSub2, advX2, advY2, 0, 0, timeFc, ""));
        }
        if (advancedEnabled && iconEnabled)
        {
            theme.Elements.Add(MakeElement(3, advSub1, advX1, advY1, 0, 0, dateFc, ""));
        }

        // Header text
        if (flagHeader && headerText.Length > 0)
        {
            theme.Elements.Add(MakeElement(4, 0, headerX, headerY, 0, 0, headerFc, headerText));
        }

        // CPU temp value + label
        if (flagCpuTemp)
        {
            theme.Elements.Add(MakeElement(0, modeSub, cpuTempValX, cpuTempValY, 0, 1, cpuTempValueFc, ""));
            theme.Elements.Add(MakeElement(4, 0, cpuTempLblX, cpuTempLblY, 0, 0, cpuTempLabelFc, "CPU"));
        }

        // CPU clock value + label
        if (flagCpuClock)
        {
            theme.Elements.Add(MakeElement(0, modeSub, cpuClockValX, cpuClockValY, 0, 3, cpuClockValueFc, ""));
            theme.Elements.Add(MakeElement(4, 0, cpuClockLblX, cpuClockLblY, 0, 0, cpuClockLabelFc, "CPU"));
        }

        // Memory/CPU usage value + label
        if (flagMemory)
        {
            theme.Elements.Add(MakeElement(0, modeSub, memValX, memValY, 0, 2, memValueFc, ""));
            theme.Elements.Add(MakeElement(4, 0, memLblX, memLblY, 0, 0, memLabelFc, "CPU"));
        }

        // GPU temp value + label
        if (flagGpuTemp)
        {
            theme.Elements.Add(MakeElement(0, modeSub, gpuTempValX, gpuTempValY, 1, 1, gpuTempValueFc, ""));
            theme.Elements.Add(MakeElement(4, 0, gpuTempLblX, gpuTempLblY, 0, 0, gpuTempLabelFc, "GPU"));
        }

        // GPU clock value + label
        if (flagGpuClock)
        {
            theme.Elements.Add(MakeElement(0, modeSub, gpuClockValX, gpuClockValY, 1, 3, gpuClockValueFc, ""));
            theme.Elements.Add(MakeElement(4, 0, gpuClockLblX, gpuClockLblY, 0, 0, gpuClockLabelFc, "GPU"));
        }

        // Storage/GPU usage value + label
        if (flagStorageMem)
        {
            theme.Elements.Add(MakeElement(0, modeSub, stoMemValX, stoMemValY, 1, 2, stoMemValueFc, ""));
            theme.Elements.Add(MakeElement(4, 0, stoMemLblX, stoMemLblY, 0, 0, stoMemLabelFc, "GPU"));
        }
    }

    private static (string fontName, float fontSize, byte fontStyle, byte fontUnit, byte gdiCharSet,
        byte colorA, byte colorR, byte colorG, byte colorB) ReadFontColor(BinaryReader reader)
    {
        return (
            reader.ReadString(),
            reader.ReadSingle(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte(),
            reader.ReadByte()
        );
    }

    private static TrccDisplayElement MakeElement(int mode, int modeSub, int x, int y, int mainCount, int subCount,
        (string fontName, float fontSize, byte fontStyle, byte fontUnit, byte gdiCharSet,
         byte colorA, byte colorR, byte colorG, byte colorB) fc, string text)
    {
        return new TrccDisplayElement
        {
            MyMode = mode,
            MyModeSub = modeSub,
            X = x,
            Y = y,
            MainCount = mainCount,
            SubCount = subCount,
            FontName = fc.fontName,
            FontSize = fc.fontSize,
            FontStyle = fc.fontStyle,
            FontUnit = fc.fontUnit,
            FontGdiCharSet = fc.gdiCharSet,
            ColorA = fc.colorA,
            ColorR = fc.colorR,
            ColorG = fc.colorG,
            ColorB = fc.colorB,
            Text = text,
        };
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
            ["172640"] = (172, 640), ["240240"] = (240, 240), ["240320"] = (240, 320),
            ["320240"] = (320, 240), ["320320"] = (320, 320), ["320960"] = (320, 960),
            ["360360"] = (360, 360), ["480480"] = (480, 480), ["480640"] = (480, 640),
            ["480800"] = (480, 800), ["480854"] = (480, 854), ["4801280"] = (480, 1280),
            ["540960"] = (540, 960), ["640172"] = (640, 172), ["640480"] = (640, 480),
            ["7201600"] = (720, 1600), ["800480"] = (800, 480), ["854480"] = (854, 480),
            ["960320"] = (960, 320), ["960540"] = (960, 540), ["4401920"] = (440, 1920),
            ["4621920"] = (462, 1920), ["1280480"] = (1280, 480), ["1600720"] = (1600, 720),
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
