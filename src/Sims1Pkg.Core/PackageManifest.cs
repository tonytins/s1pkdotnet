namespace Sims1Pkg;

/// <summary>
/// The contents of a package's <c>manifest.xml</c>: who made it and what it holds.
/// </summary>
public sealed record PackageManifest
{
    /// <summary>
    /// Format version of the package. Readers refuse versions newer than they support.
    /// </summary>
    public int FormatVersion { get; init; } = PackageFormat.CurrentVersion;

    /// <summary>
    /// Name shown to players.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Who made the content.
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// Date the package was made.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// The content's own version, chosen by its author. Not the format version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Longer description shown before installing.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// License the content is shared under.
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// Where the content was published.
    /// </summary>
    public Uri? SourceUrl { get; init; }

    /// <summary>
    /// Every content file in the package. The manifest itself is not listed.
    /// </summary>
    public IReadOnlyList<PackageFile> Files { get; init; } = [];
}
