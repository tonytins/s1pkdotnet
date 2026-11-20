namespace Sims1Pkg.Tests;

/// <summary>
/// A folder under the system's temp folder, deleted on dispose.
/// </summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "s1pk-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    /// <summary>
    /// Writes a file at a path relative to the folder, with forward slashes, creating its folders.
    /// </summary>
    public string Write(string relativePath, string contents)
    {
        string full = System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllText(full, contents);
        return full;
    }

    public string[] ListFiles() =>
        Directory.GetFiles(Path, "*", SearchOption.AllDirectories)
            .Select(file => System.IO.Path.GetRelativePath(Path, file).Replace(System.IO.Path.DirectorySeparatorChar, '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
