namespace Sims1Pkg;

/// <summary>
/// A Sims 1 installation on disk, laid out like the Complete Collection CD install.
/// </summary>
public sealed class GameInstall
{
    /// <summary>
    /// Creates a view of the installation rooted at <paramref name="rootDirectory"/>.
    /// The directory is not checked or created here.
    /// </summary>
    /// <param name="rootDirectory">The game folder, the one that holds <c>Sims.exe</c>.</param>
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
    /// Gets the folder that files of <paramref name="kind"/> are installed to.
    /// </summary>
    /// <param name="kind">The kind of content.</param>
    /// <returns>A full path inside <see cref="RootDirectory"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined value.</exception>
    public string GetContentDirectory(ContentKind kind) =>
        Path.Combine(RootDirectory, GameLayout.GetFolder(kind).Replace('/', Path.DirectorySeparatorChar));
}
