namespace Sims1Pkg.Tests;

public sealed class GameFolderTests
{
    [Fact]
    public void RecognizesAGameFolder()
    {
        using var game = new FakeGame();

        Assert.True(GameInstall.IsGameFolder(game.Path));
    }

    [Fact]
    public void RecognizesAGameFolderWhoseNamesAreLowercase()
    {
        using var folder = new TempFolder();
        Directory.CreateDirectory(Path.Combine(folder.Path, "gamedata"));
        Directory.CreateDirectory(Path.Combine(folder.Path, "userdata"));

        Assert.True(GameInstall.IsGameFolder(folder.Path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/no/such/folder")]
    public void RejectsWhatIsntAFolder(string? folder) => Assert.False(GameInstall.IsGameFolder(folder));

    [Fact]
    public void RejectsAFolderWithoutGameData()
    {
        using var folder = new TempFolder();
        Directory.CreateDirectory(Path.Combine(folder.Path, "UserData"));

        Assert.False(GameInstall.IsGameFolder(folder.Path));
    }

    [Fact]
    public void ResolvesNamesToTheirCasingOnDisk()
    {
        using var game = new FakeGame(downloadsName: "downloads");

        Assert.Equal(Path.Combine(game.Path, "downloads", "maya.diner", "Booth.iff"), game.Game.ResolvePath("Downloads/maya.diner/Booth.iff"));
        Assert.Equal(Path.Combine(game.Path, "downloads"), game.Game.GetContentDirectory(ContentKind.Object));
    }

    [Fact]
    public void KeepsTheGivenCasingForNamesNotOnDisk()
    {
        using var game = new FakeGame();

        Assert.Equal(Path.Combine(game.Path, "S1PK", "maya.diner"), game.Game.ResolvePath("S1PK/maya.diner"));
    }

    [Fact]
    public void ResolveRefusesPathsThatLeaveTheGameFolder()
    {
        using var game = new FakeGame();

        Assert.Throws<ArgumentException>(() => game.Game.ResolvePath("../outside"));
    }

    [Fact]
    public void LocatorTakesTheFirstGameFolder()
    {
        using var first = new FakeGame();
        using var second = new FakeGame();
        using var notAGame = new TempFolder();

        Assert.Equal(second.Path, GameLocator.Find(null, notAGame.Path, second.Path, first.Path)?.RootDirectory);
        Assert.Null(GameLocator.Find(null, notAGame.Path));
    }
}
