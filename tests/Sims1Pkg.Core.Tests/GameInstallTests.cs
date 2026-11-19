namespace Sims1Pkg.Tests;

public sealed class GameInstallTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "The Sims");

    [Theory]
    [InlineData(ContentKind.Object, new[] { "Downloads" })]
    [InlineData(ContentKind.Skin, new[] { "GameData", "Skins" })]
    [InlineData(ContentKind.Wall, new[] { "GameData", "Walls" })]
    [InlineData(ContentKind.Floor, new[] { "GameData", "Floors" })]
    public void MapsEachKindToItsGameFolder(ContentKind kind, string[] segments)
    {
        var game = new GameInstall(Root);

        Assert.Equal(Path.Combine([Root, .. segments]), game.GetContentDirectory(kind));
    }

    [Fact]
    public void EveryKindHasAFolder()
    {
        var game = new GameInstall(Root);

        foreach (ContentKind kind in Enum.GetValues<ContentKind>())
        {
            Assert.StartsWith(Root, game.GetContentDirectory(kind));
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsBlankRoot(string root)
    {
        Assert.Throws<ArgumentException>(() => new GameInstall(root));
    }
}
