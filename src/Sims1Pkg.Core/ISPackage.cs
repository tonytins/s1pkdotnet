namespace Sims1Pkg;

/// <summary>
/// Interface for package operations.
/// </summary>
public interface ISPackage
{
    /// <summary>
    /// Event raised when progress is made during package operations.
    /// </summary>
    event PackageProgressEventHandler? Progress;

    /// <summary>
    /// Extracts files from a package archive to the current directory.
    /// </summary>
    /// <param name="file">Path to the package file.</param>
    void Extract(string file);

    /// <summary>
    /// Extracts files from a package archive to the specified directory.
    /// </summary>
    /// <param name="packagePath">Path to the package file.</param>
    /// <param name="outputDirectory">Directory to extract files to.</param>
    void Extract(string packagePath, string? outputDirectory);

    /// <summary>
    /// Compresses files from a directory into a package archive.
    /// </summary>
    /// <param name="sourceDirectory">Directory containing files to compress.</param>
    /// <param name="packagePath">Path for the output package file.</param>
    void Compress(string sourceDirectory, string packagePath);
}

/// <summary>
/// Interface for package metadata.
/// </summary>
public interface ISMetadata
{
    /// <summary>
    /// Package name.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Package author.
    /// </summary>
    string Author { get; set; }

    /// <summary>
    /// Package date.
    /// </summary>
    string Date { get; set; }
}
