namespace Sims1Pkg.Tests;

public sealed class ContentKindsTests
{
    public static TheoryData<ContentKind> AllKinds => new(Enum.GetValues<ContentKind>());

    [Theory]
    [MemberData(nameof(AllKinds))]
    public void ManifestNameRoundTrips(ContentKind kind)
    {
        string name = ContentKinds.GetManifestName(kind);

        Assert.True(ContentKinds.TryParse(name, out ContentKind parsed));
        Assert.Equal(kind, parsed);
    }

    [Theory]
    [InlineData("SKIN", ContentKind.Skin)]
    [InlineData("Wall", ContentKind.Wall)]
    public void TryParseIgnoresCase(string name, ContentKind expected)
    {
        Assert.True(ContentKinds.TryParse(name, out ContentKind parsed));
        Assert.Equal(expected, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("house")]
    public void TryParseRejectsUnknownNames(string? name)
    {
        Assert.False(ContentKinds.TryParse(name, out _));
    }

    [Fact]
    public void PackageFileKeepsUnknownKindAsText()
    {
        var file = new PackageFile("music/song.mp3", "music", 10, "00");

        Assert.Null(file.Kind);
        Assert.Equal("music", file.KindName);
    }
}
