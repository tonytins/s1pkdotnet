namespace Sims1Pkg;

/// <summary>
/// One file an install will write.
/// </summary>
/// <param name="File">The file in the package.</param>
/// <param name="Destination">Where it goes, relative to the game folder, in its casing on disk.</param>
/// <param name="FullPath">Where it goes, as a full path.</param>
/// <param name="Clashes">True when a file the package doesn't own is already there and would be replaced.</param>
public sealed record InstallItem(PackageFile File, string Destination, string FullPath, bool Clashes);
