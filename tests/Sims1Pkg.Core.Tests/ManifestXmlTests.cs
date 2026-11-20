namespace Sims1Pkg.Tests;

public sealed class ManifestXmlTests
{
    [Fact]
    public void RoundTripsEveryField()
    {
        var manifest = new PackageManifest(
            Samples.Details("maya.diner") with
            {
                Version = "1.2",
                Description = "Booths & a counter.\nFor <diners>.",
                License = "CC-BY-4.0",
                SourceUrl = new Uri("https://example.com/diner"),
            },
            [Samples.File("Downloads/Booth.iff", "booth"), Samples.File("Tile.wll", "tile", "wall")]);

        PackageManifest read = Samples.ReadManifest(Samples.ManifestText(manifest));

        Assert.Equal(manifest.Details, read.Details);
        Assert.Equal(manifest.Files, read.Files);
    }

    [Fact]
    public void RoundTripsNonAsciiText()
    {
        var manifest = Samples.Manifest() with { Details = Samples.Details() with { Name = "Café Crème", Author = "マヤ" } };

        Assert.Equal(manifest.Details, Samples.ReadManifest(Samples.ManifestText(manifest)).Details);
    }

    [Fact]
    public void WritesTheCurrentFormatVersionWithoutABom()
    {
        string text = Samples.ManifestText(Samples.Manifest());

        Assert.Contains("formatVersion=\"1\"", text);
        Assert.False(text.StartsWith('\uFEFF'));
    }

    [Fact]
    public void WriteRefusesAnInvalidManifest() =>
        Assert.Throws<ArgumentException>(() =>
            ManifestXml.Write(Samples.Manifest() with { Files = [] }, new MemoryStream()));

    [Fact]
    public void RefusesANewerFormatVersion()
    {
        string xml = Samples.ManifestText(Samples.Manifest()).Replace("formatVersion=\"1\"", "formatVersion=\"2\"");

        var error = Assert.Throws<PackageFormatException>(() => Samples.ReadManifest(xml));
        Assert.Contains("newer Sims1Pkg", error.Message);
    }

    [Theory]
    [InlineData("formatVersion=\"0\"")]
    [InlineData("formatVersion=\"one\"")]
    [InlineData("")]
    public void RefusesAMissingOrBadFormatVersion(string replacement)
    {
        string xml = Samples.ManifestText(Samples.Manifest()).Replace("formatVersion=\"1\"", replacement);

        Assert.Throws<PackageFormatException>(() => Samples.ReadManifest(xml));
    }

    [Theory]
    [InlineData("<Name>Retro Diner Set</Name>")]
    [InlineData("<Author>Maya</Author>")]
    [InlineData("<Date>2001-03-14</Date>")]
    public void RefusesAMissingRequiredElement(string element)
    {
        string xml = Samples.ManifestText(Samples.Manifest()).Replace(element, "");

        Assert.Throws<PackageFormatException>(() => Samples.ReadManifest(xml));
    }

    [Theory]
    [InlineData("<Date>2001-03-14</Date>", "<Date>14/03/2001</Date>")]
    [InlineData("<Date>2001-03-14</Date>", "<Date>2001-02-30</Date>")]
    [InlineData("<Name>Retro Diner Set</Name>", "<Name>A</Name><Name>B</Name>")]
    [InlineData("<Name>Retro Diner Set</Name>", "<Name>A</Name><SourceUrl>not a url</SourceUrl>")]
    [InlineData("<Name>Retro Diner Set</Name>", "<Name>A</Name><SourceUrl>file:///etc/passwd</SourceUrl>")]
    [InlineData("<Name>Retro Diner Set</Name>", "<Name>A</Name><Id>not valid</Id>")]
    [InlineData("kind=\"object\"", "kind=\"\"")]
    [InlineData("size=\"5\"", "size=\"-5\"")]
    [InlineData("size=\"5\"", "size=\"five\"")]
    [InlineData("path=\"Downloads/Booth.iff\"", "path=\"../Booth.iff\"")]
    [InlineData("path=\"Downloads/Booth.iff\"", "path=\"manifest.xml\"")]
    public void RefusesInvalidValues(string original, string replacement)
    {
        string xml = Samples.ManifestText(Samples.Manifest());
        Assert.Contains(original, xml);

        Assert.Throws<PackageFormatException>(() => Samples.ReadManifest(xml.Replace(original, replacement)));
    }

    [Fact]
    public void RefusesABadHash()
    {
        string xml = Samples.ManifestText(Samples.Manifest());
        string hash = Samples.Sha256("booth");

        Assert.Throws<PackageFormatException>(() => Samples.ReadManifest(xml.Replace(hash, hash[..10])));
    }

    [Fact]
    public void AcceptsAnUppercaseHashAndStoresItLowercase()
    {
        string hash = Samples.Sha256("booth");
        string xml = Samples.ManifestText(Samples.Manifest()).Replace(hash, hash.ToUpperInvariant());

        Assert.Equal(hash, Samples.ReadManifest(xml).Files[0].Sha256);
    }

    [Fact]
    public void RefusesPathsThatClashIgnoringCase()
    {
        var manifest = Samples.Manifest(Samples.File("Booth.iff", "a"), Samples.File("Booth.iff", "b"));
        string xml = Samples.ManifestText(Samples.Manifest(Samples.File("Booth.iff", "a"), Samples.File("Other.iff", "b")))
            .Replace("Other.iff", "BOOTH.IFF");

        Assert.Throws<ArgumentException>(() => Samples.ManifestText(manifest));
        Assert.Throws<PackageFormatException>(() => Samples.ReadManifest(xml));
    }

    [Fact]
    public void IgnoresUnknownElementsAndAttributes()
    {
        string xml = Samples.ManifestText(Samples.Manifest())
            .Replace("<Name>", "<Thumbnail src=\"x.png\" /><Name>")
            .Replace("<File ", "<File future=\"yes\" ")
            .Replace("</Files>", "<Folder path=\"later\" /></Files>");

        PackageManifest manifest = Samples.ReadManifest(xml);

        Assert.Equal("Retro Diner Set", manifest.Details.Name);
        Assert.Single(manifest.Files);
    }

    [Fact]
    public void KeepsUnknownKindsAsText()
    {
        PackageManifest manifest = Samples.ReadManifest(
            Samples.ManifestText(Samples.Manifest(Samples.File("song.mp3", "la", "music"))));

        Assert.Equal("music", manifest.Files[0].KindName);
        Assert.Null(manifest.Files[0].Kind);
    }

    [Theory]
    [InlineData("not xml at all")]
    [InlineData("<Other />")]
    [InlineData("<!DOCTYPE S1Package [<!ENTITY x SYSTEM \"file:///etc/passwd\">]><S1Package formatVersion=\"1\"><Name>&x;</Name></S1Package>")]
    public void RefusesDocumentsThatArentManifests(string xml) =>
        Assert.Throws<PackageFormatException>(() => Samples.ReadManifest(xml));
}
