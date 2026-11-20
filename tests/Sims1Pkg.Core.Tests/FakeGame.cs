namespace Sims1Pkg.Tests;

/// <summary>
/// A game folder laid out like a Complete Collection install, under a temp folder.
/// </summary>
internal sealed class FakeGame : IDisposable
{
    private readonly TempFolder _folder = new();

    public FakeGame(string downloadsName = "Downloads")
    {
        foreach (string folder in new[] { "GameData/Skins", "GameData/Walls", "GameData/Floors", "UserData", downloadsName })
        {
            Directory.CreateDirectory(System.IO.Path.Combine(_folder.Path, folder));
        }

        Game = new GameInstall(_folder.Path);
    }

    public GameInstall Game { get; }

    public string Path => _folder.Path;

    public string Write(string relativePath, string contents) => _folder.Write(relativePath, contents);

    public string Read(string relativePath) =>
        File.ReadAllText(System.IO.Path.Combine(_folder.Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar)));

    public bool Exists(string relativePath) =>
        System.IO.Path.Exists(System.IO.Path.Combine(_folder.Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar)));

    /// <summary>
    /// Every file and its contents, plus every folder, so two snapshots are equal only when nothing changed.
    /// </summary>
    public SortedDictionary<string, string> Snapshot()
    {
        var snapshot = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (string entry in Directory.EnumerateFileSystemEntries(_folder.Path, "*", SearchOption.AllDirectories))
        {
            string relative = System.IO.Path.GetRelativePath(_folder.Path, entry).Replace(System.IO.Path.DirectorySeparatorChar, '/');
            snapshot[relative] = Directory.Exists(entry) ? "<folder>" : File.ReadAllText(entry);
        }

        return snapshot;
    }

    public void Dispose() => _folder.Dispose();
}
