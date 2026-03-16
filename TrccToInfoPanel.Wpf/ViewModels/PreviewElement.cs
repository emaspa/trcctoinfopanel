using System.Windows.Media;

namespace TrccToInfoPanel.Wpf.ViewModels;

public class PreviewElement
{
    public string DisplayText { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public string FontFamily { get; set; } = "Arial";
    public double FontSize { get; set; } = 16;
    public System.Windows.FontWeight FontWeight { get; set; } = System.Windows.FontWeights.Normal;
    public System.Windows.FontStyle FontStyleWpf { get; set; } = System.Windows.FontStyles.Normal;
    public SolidColorBrush ColorBrush { get; set; } = Brushes.White;
    public string TypeLabel { get; set; } = "";

    private static readonly string[] SensorCategories = ["CPU", "GPU", "Memory", "HDD", "Network", "FAN"];
    private static readonly string[,] SensorFields =
    {
        { "Temp", "Usage", "Clock", "Power" },
        { "Temp", "Usage", "Clock", "Power" },
        { "Temp", "Usage", "Clock", "Avail" },
        { "Temp", "Activity", "Read", "Write" },
        { "UP", "DL", "Total UP", "Total DL" },
        { "CPUFAN", "GPUFAN", "FAN1", "FAN2" },
    };
    private static readonly string[,] SensorPlaceholders =
    {
        { "65°C", "42%", "4200MHz", "95W" },
        { "71°C", "87%", "1890MHz", "220W" },
        { "45°C", "63%", "3200MHz", "12.4GB" },
        { "38°C", "15%", "520MB/s", "310MB/s" },
        { "1.2KB/s", "45MB/s", "2.1GB", "48GB" },
        { "980RPM", "1450RPM", "1200RPM", "850RPM" },
    };

    public static PreviewElement FromTrccElement(TrccDisplayElement el)
    {
        var preview = new PreviewElement
        {
            X = el.X,
            Y = el.Y,
            FontFamily = MapFontName(el.FontName),
            FontSize = el.FontSize,
            FontWeight = (el.FontStyle & 1) != 0 ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal,
            FontStyleWpf = (el.FontStyle & 2) != 0 ? System.Windows.FontStyles.Italic : System.Windows.FontStyles.Normal,
            ColorBrush = new SolidColorBrush(Color.FromArgb(el.ColorA, el.ColorR, el.ColorG, el.ColorB)),
        };

        switch (el.MyMode)
        {
            case 0: // Sensor
                if (el.MainCount >= 0 && el.MainCount < SensorCategories.Length
                    && el.SubCount >= 1 && el.SubCount <= 4)
                {
                    preview.DisplayText = SensorPlaceholders[el.MainCount, el.SubCount - 1];
                    preview.TypeLabel = $"{SensorCategories[el.MainCount]} {SensorFields[el.MainCount, el.SubCount - 1]}";
                }
                else
                {
                    preview.DisplayText = "N/A";
                    preview.TypeLabel = $"Sensor {el.MainCount}:{el.SubCount}";
                }
                break;
            case 1: // Time
                preview.DisplayText = el.MyModeSub == 1 ? "10:45 PM" : "22:45";
                preview.TypeLabel = "Clock";
                break;
            case 2: // Day of week
                preview.DisplayText = "Monday";
                preview.TypeLabel = "Day of Week";
                break;
            case 3: // Date
                preview.DisplayText = el.MyModeSub switch
                {
                    2 => "16/03/2026",
                    3 => "03/16",
                    4 => "16/03",
                    _ => "2026/03/16",
                };
                preview.TypeLabel = "Date";
                break;
            case 4: // Custom text
                preview.DisplayText = string.IsNullOrEmpty(el.Text) ? "(empty)" : el.Text;
                preview.TypeLabel = "Text";
                break;
            case 5: // Image
                preview.DisplayText = "[IMG]";
                preview.TypeLabel = "Image";
                break;
            default:
                preview.DisplayText = "?";
                preview.TypeLabel = $"Mode {el.MyMode}";
                break;
        }

        return preview;
    }

    private static string MapFontName(string trccFont) => trccFont switch
    {
        "微软雅黑" => "Microsoft YaHei",
        "宋体" => "SimSun",
        "黑体" => "SimHei",
        "楷体" => "KaiTi",
        "仿宋" => "FangSong",
        _ => string.IsNullOrEmpty(trccFont) ? "Arial" : trccFont,
    };
}
