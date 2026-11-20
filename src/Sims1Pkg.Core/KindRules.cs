namespace Sims1Pkg;

/// <summary>
/// Works out a file's kind while packing, from where it sits in the source folder.
/// </summary>
public static class KindRules
{
    private static readonly Dictionary<string, ContentKind> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".iff"] = ContentKind.Object,
        [".far"] = ContentKind.Object,
        [".wll"] = ContentKind.Wall,
        [".flr"] = ContentKind.Floor,
        [".cmx"] = ContentKind.Skin,
        [".skn"] = ContentKind.Skin,
        [".bcf"] = ContentKind.Skin,
        [".bmf"] = ContentKind.Skin,
        [".cfp"] = ContentKind.Skin,
    };

    /// <summary>
    /// Gets a file's kind from its path in the source folder.
    /// <list type="number">
    /// <item>A file under one of the game's own folders takes that folder's kind: <c>Downloads/</c> gives
    /// <see cref="ContentKind.Object"/>, and <c>GameData/Skins/</c>, <c>GameData/Walls/</c>, and
    /// <c>GameData/Floors/</c> give <see cref="ContentKind.Skin"/>, <see cref="ContentKind.Wall"/>, and
    /// <see cref="ContentKind.Floor"/>. Folder names are matched ignoring case.</item>
    /// <item>Any other file takes its kind from its extension: <c>.iff</c> and <c>.far</c> are objects,
    /// <c>.wll</c> walls, <c>.flr</c> floors, and <c>.cmx</c>, <c>.skn</c>, <c>.bcf</c>, <c>.bmf</c>, and
    /// <c>.cfp</c> skins.</item>
    /// </list>
    /// </summary>
    /// <param name="archivePath">The path relative to the source folder, with forward slashes.</param>
    /// <returns>The kind, or null when neither rule places the file.</returns>
    public static ContentKind? GetKind(string archivePath)
    {
        ArgumentNullException.ThrowIfNull(archivePath);

        foreach (ContentKind kind in Enum.GetValues<ContentKind>())
        {
            string folder = GameLayout.GetFolder(kind) + "/";
            if (archivePath.Length > folder.Length && archivePath.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
            {
                return kind;
            }
        }

        return Extensions.TryGetValue(Path.GetExtension(archivePath), out ContentKind byExtension)
            ? byExtension
            : null;
    }

    /// <summary>
    /// Checks whether packing leaves a file out: hidden files whose names start with a dot, the folder files Windows
    /// makes (<c>Thumbs.db</c> and <c>desktop.ini</c>), and other packages.
    /// </summary>
    /// <param name="fileName">The file's name, without its folder.</param>
    /// <returns>True when packing leaves the file out.</returns>
    public static bool IsLeftOut(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);

        return fileName.StartsWith('.')
            || fileName.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(PackageFormat.FileExtension, StringComparison.OrdinalIgnoreCase);
    }
}
