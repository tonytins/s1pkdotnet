namespace Sims1Pkg;

/// <summary>
/// Rules for the paths of files inside a package. A valid path is safe to install on Windows, macOS, and Linux.
/// </summary>
public static class ArchivePath
{
    // Characters Windows doesn't allow in file names.
    private static readonly char[] ForbiddenCharacters = ['\\', ':', '*', '?', '"', '<', '>', '|'];

    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>
    /// Checks whether <paramref name="path"/> is a valid path inside a package: relative, separated by forward
    /// slashes, with no empty, <c>.</c>, or <c>..</c> segments, and nothing Windows would refuse or quietly change.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True when the path is valid.</returns>
    public static bool IsValid(string? path) => GetProblem(path) is null;

    /// <summary>
    /// Checks whether <paramref name="name"/> is a name Windows reserves for a device, such as <c>CON</c>.
    /// </summary>
    internal static bool IsReservedName(string name) => ReservedNames.Contains(name);

    /// <summary>
    /// Says what's wrong with a path, in English, or returns null when it's valid.
    /// </summary>
    internal static string? GetProblem(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return "the path is empty.";
        }

        if (path[0] == '/')
        {
            return "the path starts at the root of the drive.";
        }

        foreach (string segment in path.Split('/'))
        {
            if (segment.Length == 0)
            {
                return "the path has an empty folder name.";
            }

            if (segment is "." or "..")
            {
                return "the path uses '.' or '..', which could reach outside its folder.";
            }

            if (segment.IndexOfAny(ForbiddenCharacters) >= 0 || segment.Any(char.IsControl))
            {
                return "the path has a character Windows doesn't allow in file names.";
            }

            if (segment[^1] is '.' or ' ')
            {
                return "a name in the path ends with a dot or a space, which Windows drops.";
            }

            if (IsReservedName(segment.Split('.')[0]))
            {
                return "a name in the path is one Windows reserves for devices.";
            }
        }

        return null;
    }
}
