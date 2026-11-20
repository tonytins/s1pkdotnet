namespace Sims1Pkg;

/// <summary>
/// A file that was already in the game, saved before an install replaced it.
/// </summary>
/// <param name="OriginalPath">Where it was, relative to the game folder.</param>
/// <param name="SavedPath">Where it's saved, relative to the receipt's folder, under <c>backups/</c>.</param>
public sealed record BackupFile(string OriginalPath, string SavedPath);
