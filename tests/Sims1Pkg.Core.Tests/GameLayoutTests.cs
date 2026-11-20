namespace Sims1Pkg.Tests;

public sealed class GameLayoutTests
{
    [Theory]
    [InlineData("Downloads/Booth.iff", ContentKind.Object, "Downloads/maya.diner/Booth.iff")]
    [InlineData("downloads/Diner/Booth.iff", ContentKind.Object, "Downloads/maya.diner/Diner/Booth.iff")]
    [InlineData("objects/Booth.iff", ContentKind.Object, "Downloads/maya.diner/objects/Booth.iff")]
    [InlineData("Booth.iff", ContentKind.Object, "Downloads/maya.diner/Booth.iff")]
    [InlineData("GameData/Skins/B001.cmx", ContentKind.Skin, "GameData/Skins/B001.cmx")]
    [InlineData("skins/deep/B001.skn", ContentKind.Skin, "GameData/Skins/B001.skn")]
    [InlineData("Tile.wll", ContentKind.Wall, "GameData/Walls/Tile.wll")]
    [InlineData("floors/Checker.flr", ContentKind.Floor, "GameData/Floors/Checker.flr")]
    public void PlacesFilesByKind(string archivePath, ContentKind kind, string expected) =>
        Assert.Equal(expected, GameLayout.GetDestination(archivePath, kind, "maya.diner"));

    [Fact]
    public void UsesTheCanonicalId() =>
        Assert.Equal("Downloads/maya.diner/Booth.iff", GameLayout.GetDestination("Booth.iff", ContentKind.Object, "Maya.Diner"));

    [Theory]
    [InlineData("../Booth.iff", "maya.diner")]
    [InlineData("Booth.iff", "not valid")]
    public void RejectsBadInput(string archivePath, string id) =>
        Assert.Throws<ArgumentException>(() => GameLayout.GetDestination(archivePath, ContentKind.Object, id));
}
