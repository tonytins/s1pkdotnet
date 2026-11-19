namespace Sims1Pkg;

/// <summary>
/// Converts <see cref="ContentKind"/> values to and from the names used in a manifest.
/// </summary>
public static class ContentKinds
{
    /// <summary>
    /// Gets the name a manifest uses for a kind, such as <c>object</c>.
    /// </summary>
    /// <param name="kind">The kind to name.</param>
    /// <returns>The lowercase manifest name.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kind"/> is not a defined value.</exception>
    public static string GetManifestName(ContentKind kind) => kind switch
    {
        ContentKind.Object => "object",
        ContentKind.Skin => "skin",
        ContentKind.Wall => "wall",
        ContentKind.Floor => "floor",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown content kind."),
    };

    /// <summary>
    /// Reads a kind from its manifest name, ignoring case.
    /// </summary>
    /// <param name="name">The manifest name, such as <c>skin</c>.</param>
    /// <param name="kind">The matching kind, when found.</param>
    /// <returns>True when <paramref name="name"/> names a kind this library knows.</returns>
    public static bool TryParse(string? name, out ContentKind kind)
    {
        foreach (ContentKind candidate in Enum.GetValues<ContentKind>())
        {
            if (string.Equals(name, GetManifestName(candidate), StringComparison.OrdinalIgnoreCase))
            {
                kind = candidate;
                return true;
            }
        }

        kind = default;
        return false;
    }
}
