namespace Sims1Pkg;

/// <summary>
/// Fixed names and numbers that define the S1PK container.
/// </summary>
public static class PackageFormat
{
    /// <summary>
    /// File extension for S1PK packages.
    /// </summary>
    public const string FileExtension = ".s1pk";

    /// <summary>
    /// Name of the manifest stored at the root of every package.
    /// </summary>
    public const string ManifestFileName = "manifest.xml";

    /// <summary>
    /// Format version this library reads and writes.
    /// </summary>
    public const int CurrentVersion = 1;
}
