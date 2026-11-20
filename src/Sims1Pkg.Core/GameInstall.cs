namespace Sims1Pkg;

/// <summary>
/// A Sims 1 installation on disk, laid out like the Complete Collection CD install.
/// </summary>
public sealed class GameInstall
{
    /// <summary>
    /// Creates a view of the installation rooted at <paramref name="rootDirectory"/>.
    /// The directory is not checked or created here. See <see cref="IsGameFolder"/>.
    /// </summary>
    /// <param name="rootDirectory">The game folder, the one that holds <c>GameData</c>.</param>
    /// <exception cref="ArgumentException"><paramref name="rootDirectory"/> is empty or whitespace.</exception>
    public GameInstall(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        RootDirectory = Path.GetFullPath(rootDirectory);
    }

    /// <summary>
    /// Full path of the game folder.
    /// </summary>
    public string RootDirectory { get; }

    /// <summary>
    /// Checks whether <paramref name="folder"/> looks like a Sims 1 game folder: it holds <c>GameData</c> and
    /// <c>UserData</c> folders, in any case. Checked against a Complete Collection CD install, which has both but no
    /// <c>Sims.exe</c> at its root under every install method.
    /// </summary>
    /// <param name="folder">The folder to check.</param>
    /// <returns>True when the folder looks like a game folder.</returns>
    public static bool IsGameFolder(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
        {
            return false;
        }

        var game = new GameInstall(folder);
        return Directory.Exists(game.ResolvePath("GameData")) && Directory.Exists(game.ResolvePath("UserData"));
    }

    /// <summary>
    /// Gets the folder that files of <paramref name="kind"/> are installed to, using the folder's real casing on disk
    /// when it exists.
    /// </summary>
    /// <param name="kind">The kind of content.</param>
    /// <returns>A full path inside <see cref="RootDirectory"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined value.</exception>
    public string GetContentDirectory(ContentKind kind) => ResolvePath(GameLayout.GetFolder(kind));

    /// <summary>
    /// Turns a path relative to the game folder into a full path, matching each name to one already on disk ignoring
    /// case. A Wine install may write <c>downloads</c> where the game's own files say <c>Downloads</c>, and on Linux
    /// those are different folders. Names not on disk keep the casing given.
    /// </summary>
    /// <param name="relativePath">A path relative to the game folder, with forward slashes.</param>
    /// <returns>A full path inside <see cref="RootDirectory"/>.</returns>
    /// <exception cref="ArgumentException">The path isn't a valid relative path. See <see cref="ArchivePath.IsValid"/>.</exception>
    public string ResolvePath(string relativePath)
    {
        if (ArchivePath.GetProblem(relativePath) is { } problem)
        {
            throw new ArgumentException($"'{relativePath}': {problem}", nameof(relativePath));
        }

        string current = RootDirectory;
        foreach (string name in relativePath.Split('/'))
        {
            string exact = Path.Combine(current, name);
            if (Path.Exists(exact) || !Directory.Exists(current))
            {
                current = exact;
                continue;
            }

            string? match = Directory.EnumerateFileSystemEntries(current)
                .Select(Path.GetFileName)
                .Where(entry => string.Equals(entry, name, StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.Ordinal)
                .FirstOrDefault();

            current = Path.Combine(current, match ?? name);
        }

        return current;
    }

    /// <summary>
    /// Gets a full path inside the game folder as a path relative to it, with forward slashes.
    /// </summary>
    /// <param name="fullPath">A full path inside <see cref="RootDirectory"/>.</param>
    /// <returns>The relative path.</returns>
    public string GetRelativePath(string fullPath) =>
        Path.GetRelativePath(RootDirectory, fullPath).Replace(Path.DirectorySeparatorChar, '/');
}
