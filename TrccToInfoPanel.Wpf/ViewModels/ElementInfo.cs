namespace TrccToInfoPanel.Wpf.ViewModels;

public class ElementInfo
{
    public int Index { get; set; }
    public string Type { get; set; } = "";
    public string Description { get; set; } = "";
    public string Position { get; set; } = "";
    public string Font { get; set; } = "";
    public string Color { get; set; } = "";

    private static readonly string[] SensorCategories = ["CPU", "GPU", "Memory", "HDD", "Network", "FAN"];
    private static readonly string[,] SensorFields =
    {
        { "Temperature", "Usage", "Clock", "Power" },
        { "Temperature", "Usage", "Clock", "Power" },
        { "Temperature", "Usage", "Clock", "Available" },
        { "Temperature", "Activity", "Read Speed", "Write Speed" },
        { "Upload Rate", "Download Rate", "Total Upload", "Total Download" },
        { "CPU Fan", "GPU Fan", "Fan 1", "Fan 2" },
    };

    public static ElementInfo FromTrccElement(TrccDisplayElement el, int index)
    {
        var (type, desc) = el.MyMode switch
        {
            0 when el.MainCount >= 0 && el.MainCount < SensorCategories.Length && el.SubCount >= 1 && el.SubCount <= 4
                => ("Sensor", $"{SensorCategories[el.MainCount]} {SensorFields[el.MainCount, el.SubCount - 1]}"),
            0 => ("Sensor", $"Sensor {el.MainCount}:{el.SubCount}"),
            1 => ("Clock", el.MyModeSub == 1 ? "12-hour" : "24-hour"),
            2 => ("Day", "Day of Week"),
            3 => ("Date", el.MyModeSub switch { 2 => "dd/MM/yyyy", 3 => "MM/dd", 4 => "dd/MM", _ => "yyyy/MM/dd" }),
            4 => ("Text", string.IsNullOrEmpty(el.Text) ? "(empty)" : $"\"{el.Text}\""),
            5 => ("Image", string.IsNullOrEmpty(el.Text) ? "(none)" : System.IO.Path.GetFileName(el.Text)),
            _ => ($"Mode {el.MyMode}", ""),
        };

        return new ElementInfo
        {
            Index = index,
            Type = type,
            Description = desc,
            Position = $"({el.X}, {el.Y})",
            Font = $"{el.FontName} {el.FontSize}pt",
            Color = $"#{el.ColorA:X2}{el.ColorR:X2}{el.ColorG:X2}{el.ColorB:X2}",
        };
    }
}
