namespace Sims1Pkg;

/// <summary>
/// Where each kind of content goes in a Complete Collection install, as paths relative to the game folder with
/// forward slashes. Checked against a Complete Collection CD install, which has <c>Downloads</c>,
/// <c>GameData/Skins</c>, <c>GameData/Walls</c>, and <c>GameData/Floors</c> with this casing.
/// </summary>
public static class GameLayout
{
    /// <summary>
    /// Gets the game folder that content of <paramref name="kind"/> is installed to, such as <c>GameData/Skins</c>.
    /// </summary>
    /// <param name="kind">The kind of content.</param>
    /// <returns>A path relative to the game folder, with forward slashes.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined value.</exception>
    public static string GetFolder(ContentKind kind) => kind switch
    {
        ContentKind.Object => "Downloads",
        ContentKind.Skin => "GameData/Skins",
        ContentKind.Wall => "GameData/Walls",
        ContentKind.Floor => "GameData/Floors",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown content kind."),
    };

    /// <summary>
    /// Gets where a packaged file is installed, relative to the game folder.
    /// <list type="bullet">
    /// <item>Objects go in the package's own folder, <c>Downloads/&lt;id&gt;/</c>, keeping their subfolders. A
    /// leading <c>Downloads/</c> in the archive path is dropped, so <c>Downloads/Booth.iff</c> installs to
    /// <c>Downloads/&lt;id&gt;/Booth.iff</c>.</item>
    /// <item>Skins, walls, and floors go straight into their <c>GameData</c> folder, by file name alone.</item>
    /// </list>
    /// </summary>
    /// <param name="archivePath">The file's path in the package.</param>
    /// <param name="kind">The file's kind.</param>
    /// <param name="packageId">The package's id.</param>
    /// <returns>A path relative to the game folder, with forward slashes.</returns>
    /// <exception cref="ArgumentException">The archive path or the id isn't valid.</exception>
    public static string GetDestination(string archivePath, ContentKind kind, string packageId)
    {
        if (ArchivePath.GetProblem(archivePath) is { } problem)
        {
            throw new ArgumentException($"'{archivePath}': {problem}", nameof(archivePath));
        }

        string id = PackageIds.Normalize(packageId);
        string folder = GetFolder(kind);

        if (kind != ContentKind.Object)
        {
            return folder + "/" + archivePath[(archivePath.LastIndexOf('/') + 1)..];
        }

        string prefix = folder + "/";
        string rest = archivePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? archivePath[prefix.Length..]
            : archivePath;

        return $"{folder}/{id}/{rest}";
    }
}
