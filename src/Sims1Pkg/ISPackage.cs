namespace Sims1Pkg;

public interface ISPackage
{
    /// <summary>
    /// Extracts files from a package archive.
    /// </summary>
    /// <param name="file">Path to the package file.</param>
    void Extract(string file);

    /// <summary>
    /// Compresses files into a package archive.
    /// </summary>
    /// <param name="sourceDirectory">Directory containing files to compress.</param>
    /// <param name="packagePath">Path for the output package file.</param>
    void Compress(string sourceDirectory, string packagePath);
}

public interface ISMetadata
{
    string Name { get; set; }
    string Author { get; set; }
    string Date { get; set; }
}