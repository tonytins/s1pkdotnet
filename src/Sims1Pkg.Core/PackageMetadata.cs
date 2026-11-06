using System.Xml.Serialization;

namespace Sims1Pkg;

/// <summary>
/// Data Transfer Object for package metadata.
/// Used for XML serialization in metadata.xml files.
/// </summary>
[XmlRoot("PackageMetadata")]
public class PackageMetadata : ISMetadata
{
    [XmlElement("Name")]
    public string Name { get; set; } = string.Empty;

    [XmlElement("Author")]
    public string Author { get; set; } = string.Empty;

    [XmlElement("Date")]
    public string Date { get; set; } = string.Empty;

    [XmlElement("License")]
    public string License { get; set; } = string.Empty;

    [XmlElement("SourceURL")]
    public string SourceURL { get; set; } = string.Empty;

    // Optional fields for future expansion
    [XmlElement("Version")]
    public string? Version { get; set; }

    [XmlElement("Description")]
    public string? Description { get; set; }

    /// <summary>
    /// Serializes the metadata to XML string.
    /// </summary>
    public string ToXml()
    {
        var serializer = new XmlSerializer(typeof(PackageMetadata));
        using var writer = new System.IO.StringWriter();
        serializer.Serialize(writer, this);
        return writer.ToString();
    }

    /// <summary>
    /// Deserializes metadata from XML string.
    /// </summary>
    public static PackageMetadata? FromXml(string xml)
    {
        var serializer = new XmlSerializer(typeof(PackageMetadata));
        using var reader = new System.IO.StringReader(xml);
        return serializer.Deserialize(reader) as PackageMetadata;
    }
}
