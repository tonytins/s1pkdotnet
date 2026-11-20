namespace Sims1Pkg.Gui.Tests;

public sealed class SettingsTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "s1pk-settings-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void RemembersTheGameFolder()
    {
        new Settings(_folder).SaveGameFolder("/games/The Sims");

        Assert.Equal("/games/The Sims", new Settings(_folder).LoadGameFolder());
    }

    [Fact]
    public void HasNothingAtFirst() => Assert.Null(new Settings(_folder).LoadGameFolder());

    [Fact]
    public void IgnoresABlankFile()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(Path.Combine(_folder, "game-folder.txt"), "  \n");

        Assert.Null(new Settings(_folder).LoadGameFolder());
    }
}
