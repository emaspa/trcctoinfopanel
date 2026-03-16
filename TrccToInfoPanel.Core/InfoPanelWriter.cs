using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace TrccToInfoPanel;

/// <summary>
/// Writes InfoPanel-compatible profile XML files.
/// </summary>
public static class InfoPanelWriter
{
    public static void WriteProfile(string outputDir, Profile profile, List<DisplayItem> items,
        string? sourceThemeDir = null, Action<string>? log = null)
    {
        log ??= _ => { };

        Directory.CreateDirectory(outputDir);

        var profilesDir = Path.Combine(outputDir, "profiles");
        Directory.CreateDirectory(profilesDir);

        var assetsDir = Path.Combine(outputDir, "assets", profile.Guid.ToString());
        Directory.CreateDirectory(assetsDir);

        // Write profiles.xml
        WriteProfilesXml(outputDir, profile);

        // Write display items XML
        WriteDisplayItemsXml(profilesDir, profile.Guid, items);

        // Copy image assets from source theme directory
        if (sourceThemeDir != null)
            CopyAssets(sourceThemeDir, assetsDir);

        log($"  Profile:  {Path.Combine(outputDir, "profiles.xml")}");
        log($"  Items:    {Path.Combine(profilesDir, $"{profile.Guid}.xml")}");
        log($"  Assets:   {assetsDir}");
    }

    private static void WriteProfilesXml(string outputDir, Profile profile)
    {
        var path = Path.Combine(outputDir, "profiles.xml");
        var profiles = new List<Profile>();

        // If profiles.xml already exists, load existing profiles
        if (File.Exists(path))
        {
            try
            {
                var xs = new XmlSerializer(typeof(List<Profile>));
                using var reader = File.OpenRead(path);
                if (xs.Deserialize(reader) is List<Profile> existing)
                    profiles = existing;
            }
            catch
            {
                // Start fresh if existing file is corrupt
            }
        }

        // Remove any existing profile with same GUID, then add the new one
        profiles.RemoveAll(p => p.Guid == profile.Guid);
        profiles.Add(profile);

        var serializer = new XmlSerializer(typeof(List<Profile>));
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),
        };

        using var writer = XmlWriter.Create(path, settings);
        serializer.Serialize(writer, profiles);
    }

    private static void WriteDisplayItemsXml(string profilesDir, Guid profileGuid, List<DisplayItem> items)
    {
        var path = Path.Combine(profilesDir, $"{profileGuid}.xml");

        var serializer = new XmlSerializer(
            typeof(List<DisplayItem>),
            [
                typeof(TextDisplayItem),
                typeof(SensorDisplayItem),
                typeof(ClockDisplayItem),
                typeof(CalendarDisplayItem),
                typeof(ImageDisplayItem),
                typeof(BarDisplayItem),
                typeof(GraphDisplayItem),
                typeof(DonutDisplayItem),
                typeof(GaugeDisplayItem),
                typeof(ShapeDisplayItem),
                typeof(GroupDisplayItem),
            ]);

        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = new UTF8Encoding(false),
        };

        using var writer = XmlWriter.Create(path, settings);
        serializer.Serialize(writer, items);
    }

    private static void CopyAssets(string sourceThemeDir, string assetsDir)
    {
        var imageFiles = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif" };
        foreach (var pattern in imageFiles)
        {
            foreach (var file in Directory.GetFiles(sourceThemeDir, pattern))
            {
                var destFile = Path.Combine(assetsDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }
        }
    }
}
