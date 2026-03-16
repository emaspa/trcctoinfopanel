using System.Xml.Serialization;

namespace TrccToInfoPanel;

/// <summary>
/// InfoPanel profile definition. Serialized to profiles.xml.
/// </summary>
public class Profile
{
    public Guid Guid { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 480;
    public bool ShowFps { get; set; }
    public bool Drag { get; set; }
    public string BackgroundColor { get; set; } = "#FF000000";
    public bool Active { get; set; } = true;
    public bool OpenGL { get; set; }
    public float FontScale { get; set; } = 1.33f;
    public bool Topmost { get; set; }
    public string Font { get; set; } = "Arial";
    public int FontSize { get; set; } = 20;
    public string Color { get; set; } = "#FFFFFFFF";
    public int WindowX { get; set; }
    public int WindowY { get; set; }
    public bool Resize { get; set; }
    public bool StrictWindowMatching { get; set; }
    public bool IsSelected { get; set; }
}

/// <summary>
/// Base class for all display items. Uses xsi:type for polymorphic XML serialization.
/// </summary>
[XmlInclude(typeof(TextDisplayItem))]
[XmlInclude(typeof(SensorDisplayItem))]
[XmlInclude(typeof(ClockDisplayItem))]
[XmlInclude(typeof(CalendarDisplayItem))]
[XmlInclude(typeof(ImageDisplayItem))]
[XmlInclude(typeof(BarDisplayItem))]
[XmlInclude(typeof(GraphDisplayItem))]
[XmlInclude(typeof(DonutDisplayItem))]
[XmlInclude(typeof(GaugeDisplayItem))]
[XmlInclude(typeof(ShapeDisplayItem))]
[XmlInclude(typeof(GroupDisplayItem))]
public class DisplayItem
{
    public string Name { get; set; } = "";
    public bool Hidden { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsLocked { get; set; }
    public int Rotation { get; set; }
}

public class TextDisplayItem : DisplayItem
{
    public string Font { get; set; } = "Arial";
    public int FontSize { get; set; } = 20;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public bool Strikeout { get; set; }
    public string Color { get; set; } = "#FFFFFFFF";
    public bool Uppercase { get; set; }
    public string FontStyle { get; set; } = "Regular";
    public bool RightAlign { get; set; }
    public bool CenterAlign { get; set; }
    public bool Wrap { get; set; } = true;
    public bool Ellipsis { get; set; } = true;
    public int Width { get; set; }
    public int Height { get; set; }
    public bool Marquee { get; set; }
    public int MarqueeSpeed { get; set; } = 50;
    public int MarqueeSpacing { get; set; } = 40;
}

public class SensorDisplayItem : TextDisplayItem
{
    [XmlElement("_valueType")]
    public string ValueTypeInternal { get; set; } = "NOW";
    public string SensorName { get; set; } = "";
    public string SensorType { get; set; } = "Libre";
    public int HwInfoRemoteIndex { get; set; } = -1;
    public uint Id { get; set; }
    public uint Instance { get; set; }
    public uint EntryId { get; set; }
    public string LibreSensorId { get; set; } = "";
    public string PluginSensorId { get; set; } = "";
    public string ValueType { get; set; } = "NOW";
    public int Threshold1 { get; set; }
    public string Threshold1Color { get; set; } = "#000000";
    public int Threshold2 { get; set; }
    public string Threshold2Color { get; set; } = "#000000";
    public bool ShowName { get; set; }
    public string Unit { get; set; } = "";
    public bool OverrideUnit { get; set; }
    public bool ShowUnit { get; set; } = true;
    public bool OverridePrecision { get; set; }
    public int Precision { get; set; }
    public float AdditionModifier { get; set; }
    public bool AbsoluteAddition { get; set; } = true;
    public float MultiplicationModifier { get; set; } = 1;
    public bool DivisionToggle { get; set; }
}

public class ClockDisplayItem : TextDisplayItem
{
    public string Format { get; set; } = "HH:mm:ss";
}

public class CalendarDisplayItem : TextDisplayItem
{
    public string Format { get; set; } = "yyyy/MM/dd";
}

public class ImageDisplayItem : DisplayItem
{
    public string Type { get; set; } = "FILE";
    public bool ReadOnly { get; set; }
    public string FilePath { get; set; } = "";
    public bool RelativePath { get; set; } = true;
    public string HttpUrl { get; set; } = "";
    public string RtspUrl { get; set; } = "";
    public bool Cache { get; set; } = true;
    public int Scale { get; set; } = 100;
    public bool Layer { get; set; }
    public string LayerColor { get; set; } = "#77FFFFFF";
    public bool ShowPanel { get; set; }
    public int Volume { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class BarDisplayItem : DisplayItem
{
    [XmlElement("_valueType")]
    public string ValueTypeInternal { get; set; } = "NOW";
    public string SensorName { get; set; } = "";
    public string SensorType { get; set; } = "Libre";
    public int HwInfoRemoteIndex { get; set; } = -1;
    public uint Id { get; set; }
    public uint Instance { get; set; }
    public uint EntryId { get; set; }
    public string LibreSensorId { get; set; } = "";
    public string PluginSensorId { get; set; } = "";
    public string ValueType { get; set; } = "NOW";
    public int MinValue { get; set; }
    public int MaxValue { get; set; } = 100;
    public bool AutoValue { get; set; }
    public int Width { get; set; } = 200;
    public int Height { get; set; } = 20;
    public bool FlipX { get; set; }
    public bool Frame { get; set; }
    public string FrameColor { get; set; } = "#FFFFFFFF";
    public bool Background { get; set; } = true;
    public string BackgroundColor { get; set; } = "#FF404040";
    public string Color { get; set; } = "#FF00FFFF";
    public bool Gradient { get; set; }
    public string GradientColor { get; set; } = "#FF00FF00";
    public int CornerRadius { get; set; } = 5;
}

public class GraphDisplayItem : DisplayItem
{
    [XmlElement("_valueType")]
    public string ValueTypeInternal { get; set; } = "NOW";
    public string SensorName { get; set; } = "";
    public string SensorType { get; set; } = "Libre";
    public int HwInfoRemoteIndex { get; set; } = -1;
    public uint Id { get; set; }
    public uint Instance { get; set; }
    public uint EntryId { get; set; }
    public string LibreSensorId { get; set; } = "";
    public string PluginSensorId { get; set; } = "";
    public string ValueType { get; set; } = "NOW";
    public int MinValue { get; set; }
    public int MaxValue { get; set; } = 100;
    public bool AutoValue { get; set; }
    public int Width { get; set; } = 200;
    public int Height { get; set; } = 100;
    public string Type { get; set; } = "LINE";
    public int Thickness { get; set; } = 2;
    public int Step { get; set; } = 4;
    public bool Fill { get; set; } = true;
    public string FillColor { get; set; } = "#FF6A5ACD";
    public string Color { get; set; } = "#FF00FFFF";
}

public class DonutDisplayItem : DisplayItem
{
    [XmlElement("_valueType")]
    public string ValueTypeInternal { get; set; } = "NOW";
    public string SensorName { get; set; } = "";
    public string SensorType { get; set; } = "Libre";
    public int HwInfoRemoteIndex { get; set; } = -1;
    public uint Id { get; set; }
    public uint Instance { get; set; }
    public uint EntryId { get; set; }
    public string LibreSensorId { get; set; } = "";
    public string PluginSensorId { get; set; } = "";
    public string ValueType { get; set; } = "NOW";
    public int MinValue { get; set; }
    public int MaxValue { get; set; } = 100;
    public bool AutoValue { get; set; }
    public int Width { get; set; } = 100;
    public int Height { get; set; } = 100;
    public int Thickness { get; set; } = 20;
    public int StartAngle { get; set; }
    public string CircleFillColor { get; set; } = "#FF000000";
    public string Color { get; set; } = "#FF00FFFF";
}

public class GaugeDisplayItem : DisplayItem
{
    [XmlElement("_valueType")]
    public string ValueTypeInternal { get; set; } = "NOW";
    public string SensorName { get; set; } = "";
    public string SensorType { get; set; } = "Libre";
    public int HwInfoRemoteIndex { get; set; } = -1;
    public uint Id { get; set; }
    public uint Instance { get; set; }
    public uint EntryId { get; set; }
    public string LibreSensorId { get; set; } = "";
    public string PluginSensorId { get; set; } = "";
    public string ValueType { get; set; } = "NOW";
    public int MinValue { get; set; }
    public int MaxValue { get; set; } = 100;
    public int Scale { get; set; } = 100;
    public int AnimationSpeed { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}

public class ShapeDisplayItem : DisplayItem
{
    public string Type { get; set; } = "Rectangle";
    public int Width { get; set; } = 100;
    public int Height { get; set; } = 100;
    public int CornerRadius { get; set; }
    public bool ShowFrame { get; set; } = true;
    public int FrameThickness { get; set; } = 2;
    public string FrameColor { get; set; } = "#FFFFFFFF";
    public bool ShowFill { get; set; } = true;
    public string FillColor { get; set; } = "#FF808080";
    public bool ShowGradient { get; set; }
    public string GradientColor { get; set; } = "#FF000000";
}

public class GroupDisplayItem : DisplayItem
{
    public List<DisplayItem> DisplayItems { get; set; } = [];
    public int DisplayItemsCount { get; set; }
    public bool IsExpanded { get; set; } = true;
}
