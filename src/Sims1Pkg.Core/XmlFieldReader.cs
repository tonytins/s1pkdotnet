using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace Sims1Pkg;

/// <summary>
/// Strict reading of the small XML documents Sims1Pkg writes: one of each known element, required values present,
/// and unknown elements left for later versions.
/// </summary>
/// <param name="document">The document's name in messages, such as <c>manifest.xml</c>.</param>
/// <param name="error">Makes the exception thrown for a problem, from its message and any error that caused it.</param>
internal sealed class XmlFieldReader(string document, Func<string, Exception?, Exception> error)
{
    /// <summary>
    /// Loads the document's root element, refusing DTDs so the XML can't reach outside itself.
    /// </summary>
    public XElement Load(Stream stream, string rootName)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            CloseInput = false,
        };

        XElement root;
        try
        {
            using var reader = XmlReader.Create(stream, settings);
            root = XDocument.Load(reader).Root ?? throw error($"{document} is empty.", null);
        }
        catch (XmlException exception)
        {
            throw error($"{document} isn't valid XML: {exception.Message}", exception);
        }

        if (root.Name != rootName)
        {
            throw error($"{document} starts with <{root.Name}>, not <{rootName}>.", null);
        }

        return root;
    }

    /// <summary>
    /// Checks the root's <c>formatVersion</c>, refusing one newer than <paramref name="newest"/>.
    /// </summary>
    public void CheckFormatVersion(XElement root, int newest)
    {
        string? text = root.Attribute("formatVersion")?.Value;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int version) || version < 1)
        {
            throw error($"{document} has no valid formatVersion.", null);
        }

        if (version > newest)
        {
            throw error(
                $"{document} uses format version {version}, and this version of Sims1Pkg reads up to {newest}. " +
                "Install a newer Sims1Pkg to open it.",
                null);
        }
    }

    public XElement? Single(XElement parent, string name)
    {
        XElement? first = null;
        foreach (XElement element in parent.Elements(name))
        {
            if (first is not null)
            {
                throw error($"{document} has more than one <{name}>.", null);
            }

            first = element;
        }

        return first;
    }

    public string Required(XElement parent, string name)
    {
        string? value = Optional(parent, name);
        return string.IsNullOrWhiteSpace(value) ? throw error($"{document} has no <{name}>.", null) : value;
    }

    public string? Optional(XElement parent, string name)
    {
        string? value = Single(parent, name)?.Value;
        return string.IsNullOrEmpty(value) ? null : value;
    }

    public string RequiredAttribute(XElement element, string name, string where)
    {
        string? value = element.Attribute(name)?.Value;
        return string.IsNullOrEmpty(value) ? throw error($"{where} in {document} has no {name}.", null) : value;
    }

    public static XElement? OptionalElement(string name, string? value) =>
        string.IsNullOrEmpty(value) ? null : new XElement(name, value);

    public static void Save(XElement root, Stream stream)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = true,
            CloseOutput = false,
        };

        using var writer = XmlWriter.Create(stream, settings);
        new XDocument(root).Save(writer);
    }
}
