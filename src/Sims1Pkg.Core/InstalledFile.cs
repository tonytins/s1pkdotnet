namespace Sims1Pkg;

/// <summary>
/// A file an install wrote.
/// </summary>
/// <param name="Path">Its path relative to the game folder, with forward slashes, in its casing on disk.</param>
/// <param name="Sha256">Lowercase hex SHA-256 of what was written.</param>
public sealed record InstalledFile(string Path, string Sha256);
