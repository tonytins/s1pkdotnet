using System.Globalization;
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

    private static readonly XmlFieldReader Fields =
        new(PackageFormat.ManifestFileName, (message, inner) => inner is null
            ? new PackageFormatException(message)
            : new PackageFormatException(message, inner));

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

        XElement root = Fields.Load(stream, RootName);
        Fields.CheckFormatVersion(root, PackageFormat.CurrentVersion);

        string? sourceUrlText = Fields.Optional(root, "SourceUrl");
        Uri? sourceUrl = null;
        if (sourceUrlText is not null &&
            (!Uri.TryCreate(sourceUrlText, UriKind.Absolute, out sourceUrl) || !PackageDetails.IsWebAddress(sourceUrl)))
        {
            throw new PackageFormatException($"SourceUrl '{sourceUrlText}' is not a web address.");
        }

        string dateText = Fields.Required(root, "Date");
        if (!DateOnly.TryParseExact(dateText, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
        {
            throw new PackageFormatException($"Date '{dateText}' isn't a date written as YYYY-MM-DD.");
        }

        var details = new PackageDetails
        {
            Id = Fields.Optional(root, "Id"),
            Name = Fields.Required(root, "Name"),
            Author = Fields.Required(root, "Author"),
            Date = date,
            Version = Fields.Optional(root, "Version"),
            Description = Fields.Optional(root, "Description"),
            License = Fields.Optional(root, "License"),
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
            XmlFieldReader.OptionalElement("Id", details.Id),
            new XElement("Name", details.Name),
            new XElement("Author", details.Author),
            new XElement("Date", details.Date.ToString(DateFormat, CultureInfo.InvariantCulture)),
            XmlFieldReader.OptionalElement("Version", details.Version),
            XmlFieldReader.OptionalElement("Description", details.Description),
            XmlFieldReader.OptionalElement("License", details.License),
            XmlFieldReader.OptionalElement("SourceUrl", details.SourceUrl?.AbsoluteUri),
            new XElement("Files", manifest.Files.Select(file => new XElement(
                "File",
                new XAttribute("path", file.Path),
                new XAttribute("kind", file.KindName),
                new XAttribute("size", file.Length.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("sha256", file.Sha256)))));

        XmlFieldReader.Save(root, stream);
    }

    private static List<PackageFile> ReadFiles(XElement root)
    {
        XElement files = Fields.Single(root, "Files")
            ?? throw new PackageFormatException("manifest.xml has no <Files> element.");

        var result = new List<PackageFile>();
        foreach (XElement file in files.Elements("File"))
        {
            string path = Fields.RequiredAttribute(file, "path", "A <File>");
            string where = $"<File path=\"{path}\">";
            string sizeText = Fields.RequiredAttribute(file, "size", where);

            if (!long.TryParse(sizeText, NumberStyles.None, CultureInfo.InvariantCulture, out long size))
            {
                throw new PackageFormatException($"{where} has a size that isn't a whole number: '{sizeText}'.");
            }

            result.Add(new PackageFile(
                path,
                Fields.RequiredAttribute(file, "kind", where),
                size,
                Fields.RequiredAttribute(file, "sha256", where).ToLowerInvariant()));
        }

        return result;
    }
}
