namespace Sims1Pkg.Tests;

public sealed class KindRulesTests
{
    [Theory]
    [InlineData("Downloads/Diner/readme.txt", ContentKind.Object)]
    [InlineData("downloads/Booth.iff", ContentKind.Object)]
    [InlineData("GameData/Skins/face.bmp", ContentKind.Skin)]
    [InlineData("GAMEDATA/walls/Tile.bmp", ContentKind.Wall)]
    [InlineData("GameData/Floors/Checker.flr", ContentKind.Floor)]
    public void FolderLayoutWins(string path, ContentKind expected) => Assert.Equal(expected, KindRules.GetKind(path));

    [Theory]
    [InlineData("Booth.iff", ContentKind.Object)]
    [InlineData("stuff/Counter.FAR", ContentKind.Object)]
    [InlineData("Tile.wll", ContentKind.Wall)]
    [InlineData("Checker.flr", ContentKind.Floor)]
    [InlineData("B001.cmx", ContentKind.Skin)]
    [InlineData("B001.skn", ContentKind.Skin)]
    [InlineData("B001.bcf", ContentKind.Skin)]
    [InlineData("B001.bmf", ContentKind.Skin)]
    [InlineData("B001.cfp", ContentKind.Skin)]
    [InlineData("GameData/Objects/Booth.iff", ContentKind.Object)]
    public void ExtensionIsTheFallback(string path, ContentKind expected) => Assert.Equal(expected, KindRules.GetKind(path));

    [Theory]
    [InlineData("face.bmp")]
    [InlineData("readme.txt")]
    [InlineData("Downloads")]
    [InlineData("GameData/Skins")]
    public void UnplacedFilesHaveNoKind(string path) => Assert.Null(KindRules.GetKind(path));

    [Theory]
    [InlineData(".DS_Store", true)]
    [InlineData(".git", true)]
    [InlineData("thumbs.db", true)]
    [InlineData("Desktop.ini", true)]
    [InlineData("old.S1PK", true)]
    [InlineData("Booth.iff", false)]
    public void LeavesOutClutter(string name, bool expected) => Assert.Equal(expected, KindRules.IsLeftOut(name));
}
