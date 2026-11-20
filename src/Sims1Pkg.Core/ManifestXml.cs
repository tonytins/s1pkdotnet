using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Sims1Pkg;

/// <summary>
/// Reads and writes <c>manifest.xml</c>.
/// </summary>
/// <remarks>
/// <code language="xml"><![CDATA[
/// <S1Package formatVersion="1">
///   <Id>maya.retro-diner</Id>
///   <Name>Retro Diner Set</Name>
///   <Author>Maya</Author>
///   <Date>2001-03-14</Date>
///   <Files>
///     <File path="Downloads/DinerBooth.iff" kind="object" size="52114" sha256="…" />
///   </Files>
/// </S1Package>
/// ]]></code>
/// <c>Id</c>, <c>Version</c>, <c>Description</c>, <c>License</c>, and <c>SourceUrl</c> are optional. Elements and
/// attributes a reader doesn't know are ignored, so later versions can add optional ones. Anything that changes what
/// an older reader would install needs a new format version instead.
/// </remarks>
public static class ManifestXml
{
    private const string RootName = "S1Package";
    private const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// Reads a manifest.
    /// </summary>
    /// <param name="stream">The manifest's bytes. The stream is left open.</param>
    /// <returns>The manifest.</returns>
    /// <exception cref="PackageFormatException">
    /// The XML is malformed, uses a newer format version, lacks a required element, or holds an invalid value.
    /// </exception>
    public static PackageManifest Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        XElement root = Load(stream);
        if (root.Name != RootName)
        {
            throw new PackageFormatException($"manifest.xml starts with <{root.Name}>, not <{RootName}>.");
        }

        ReadFormatVersion(root);

        string? sourceUrlText = Optional(root, "SourceUrl");
        Uri? sourceUrl = null;
        if (sourceUrlText is not null &&
            (!Uri.TryCreate(sourceUrlText, UriKind.Absolute, out sourceUrl) || !PackageDetails.IsWebAddress(sourceUrl)))
        {
            throw new PackageFormatException($"SourceUrl '{sourceUrlText}' is not a web address.");
        }

        string dateText = Required(root, "Date");
        if (!DateOnly.TryParseExact(dateText, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            throw new PackageFormatException($"Date '{dateText}' isn't a date written as YYYY-MM-DD.");
        }

        var details = new PackageDetails
        {
            Id = Optional(root, "Id"),
            Name = Required(root, "Name"),
            Author = Required(root, "Author"),
            Date = date,
            Version = Optional(root, "Version"),
            Description = Optional(root, "Description"),
            License = Optional(root, "License"),
            SourceUrl = sourceUrl,
        };

        var manifest = new PackageManifest(details, ReadFiles(root));

        IReadOnlyList<string> problems = manifest.GetProblems();
        if (problems.Count > 0)
        {
            throw new PackageFormatException("manifest.xml isn't valid: " + string.Join(" ", problems));
        }

        return manifest;
    }

    /// <summary>
    /// Writes a manifest as UTF-8 XML, in the current format version.
    /// </summary>
    /// <param name="manifest">The manifest to write.</param>
    /// <param name="stream">Where to write it. The stream is left open.</param>
    /// <exception cref="ArgumentException">The manifest isn't valid. See <see cref="PackageManifest.GetProblems"/>.</exception>
    public static void Write(PackageManifest manifest, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(stream);

        IReadOnlyList<string> problems = manifest.GetProblems();
        if (problems.Count > 0)
        {
            throw new ArgumentException("The manifest isn't valid: " + string.Join(" ", problems), nameof(manifest));
        }

        PackageDetails details = manifest.Details;
        var root = new XElement(
            RootName,
            new XAttribute("formatVersion", PackageFormat.CurrentVersion.ToString(CultureInfo.InvariantCulture)),
            OptionalElement("Id", details.Id),
            new XElement("Name", details.Name),
            new XElement("Author", details.Author),
            new XElement("Date", details.Date.ToString(DateFormat, CultureInfo.InvariantCulture)),
            OptionalElement("Version", details.Version),
            OptionalElement("Description", details.Description),
            OptionalElement("License", details.License),
            OptionalElement("SourceUrl", details.SourceUrl?.AbsoluteUri),
            new XElement("Files", manifest.Files.Select(file => new XElement(
                "File",
                new XAttribute("path", file.Path),
                new XAttribute("kind", file.KindName),
                new XAttribute("size", file.Length.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("sha256", file.Sha256)))));

        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            CloseOutput = false,
        };

        using var writer = XmlWriter.Create(stream, settings);
        new XDocument(root).Save(writer);
    }

    private static XElement Load(Stream stream)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            CloseInput = false,
        };

        try
        {
            using var reader = XmlReader.Create(stream, settings);
            return XDocument.Load(reader).Root
                ?? throw new PackageFormatException("manifest.xml is empty.");
        }
        catch (XmlException exception)
        {
            throw new PackageFormatException("manifest.xml isn't valid XML: " + exception.Message, exception);
        }
    }

    private static void ReadFormatVersion(XElement root)
    {
        string? text = root.Attribute("formatVersion")?.Value;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int version) || version < 1)
        {
            throw new PackageFormatException("manifest.xml has no valid formatVersion.");
        }

        if (version > PackageFormat.CurrentVersion)
        {
            throw new PackageFormatException(
                $"This package uses format version {version}, and this version of Sims1Pkg reads up to " +
                $"{PackageFormat.CurrentVersion}. Install a newer Sims1Pkg to open it.");
        }
    }

    private static List<PackageFile> ReadFiles(XElement root)
    {
        XElement files = Single(root, "Files")
            ?? throw new PackageFormatException("manifest.xml has no <Files> element.");

        var result = new List<PackageFile>();
        foreach (XElement file in files.Elements("File"))
        {
            string path = RequiredAttribute(file, "path", "a <File>");
            string where = $"<File path=\"{path}\">";
            string sizeText = RequiredAttribute(file, "size", where);

            if (!long.TryParse(sizeText, NumberStyles.None, CultureInfo.InvariantCulture, out long size))
            {
                throw new PackageFormatException($"{where} has a size that isn't a whole number: '{sizeText}'.");
            }

            result.Add(new PackageFile(
                path,
                RequiredAttribute(file, "kind", where),
                size,
                RequiredAttribute(file, "sha256", where).ToLowerInvariant()));
        }

        return result;
    }

    private static XElement? Single(XElement parent, string name)
    {
        XElement? first = null;
        foreach (XElement element in parent.Elements(name))
        {
            if (first is not null)
            {
                throw new PackageFormatException($"manifest.xml has more than one <{name}>.");
            }

            first = element;
        }

        return first;
    }

    private static string Required(XElement parent, string name)
    {
        string? value = Optional(parent, name);
        return string.IsNullOrWhiteSpace(value)
            ? throw new PackageFormatException($"manifest.xml has no <{name}>.")
            : value;
    }

    private static string? Optional(XElement parent, string name)
    {
        string? value = Single(parent, name)?.Value;
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string RequiredAttribute(XElement element, string name, string where)
    {
        string? value = element.Attribute(name)?.Value;
        return string.IsNullOrEmpty(value)
            ? throw new PackageFormatException($"{where} has no {name}.")
            : value;
    }

    private static XElement? OptionalElement(string name, string? value) =>
        string.IsNullOrEmpty(value) ? null : new XElement(name, value);
}
