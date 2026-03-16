namespace TrccToInfoPanel;

/// <summary>
/// Represents a parsed TRCC theme from a config1.dc file.
/// </summary>
public class TrccTheme
{
    public bool SystemInfoEnabled { get; set; }
    public List<TrccDisplayElement> Elements { get; set; } = [];

    // Display settings
    public bool BackgroundEnabled { get; set; }
    public bool TransparencyEnabled { get; set; }
    public int Direction { get; set; }
    public int UIMode { get; set; }
    public int Mode { get; set; }
    public bool TransparentBackground { get; set; }
    public int JpX { get; set; }
    public int JpY { get; set; }
    public int JpW { get; set; }
    public int JpH { get; set; }
    public bool MaskEnabled { get; set; }
    public int MaskX { get; set; }
    public int MaskY { get; set; }

    // Metadata from directory structure
    public int CanvasWidth { get; set; }
    public int CanvasHeight { get; set; }
    public string ThemeName { get; set; } = "";
    public string ThemeDirectory { get; set; } = "";
}

public class TrccDisplayElement
{
    public int MyMode { get; set; }
    public int MyModeSub { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int MainCount { get; set; }
    public int SubCount { get; set; }
    public string FontName { get; set; } = "";
    public float FontSize { get; set; }
    public byte FontStyle { get; set; }
    public byte FontUnit { get; set; }
    public byte FontGdiCharSet { get; set; }
    public byte ColorA { get; set; }
    public byte ColorR { get; set; }
    public byte ColorG { get; set; }
    public byte ColorB { get; set; }
    public string Text { get; set; } = "";
}
