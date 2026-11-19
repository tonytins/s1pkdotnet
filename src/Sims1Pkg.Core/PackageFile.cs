namespace Sims1Pkg;

/// <summary>
/// One file listed in a package manifest.
/// </summary>
/// <param name="Path">Path of the file inside the package, using forward slashes.</param>
/// <param name="KindName">
/// The kind as written in the manifest. It is kept as text so a package from a newer
/// writer can still be inspected when it uses a kind this library doesn't know.
/// </param>
/// <param name="Length">Size of the file in bytes.</param>
/// <param name="Sha256">Lowercase hex SHA-256 hash of the file's contents.</param>
public sealed record PackageFile(string Path, string KindName, long Length, string Sha256)
{
    /// <summary>
    /// The known kind, or null when <see cref="KindName"/> is a kind this library doesn't know.
    /// A file with an unknown kind can be listed but must not be installed.
    /// </summary>
    public ContentKind? Kind => ContentKinds.TryParse(KindName, out ContentKind kind) ? kind : null;
}
