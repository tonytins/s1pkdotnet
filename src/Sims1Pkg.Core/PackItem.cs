namespace Sims1Pkg;

/// <summary>
/// One file found while previewing a pack.
/// </summary>
/// <param name="SourcePath">Full path of the file on disk.</param>
/// <param name="ArchivePath">Its path in the package: relative to the source folder, with forward slashes.</param>
/// <param name="Kind">Its kind, or null when nothing places it.</param>
/// <param name="Problem">Why it can't be packed, or null when it can.</param>
public sealed record PackItem(string SourcePath, string ArchivePath, ContentKind? Kind, PackProblem? Problem);
