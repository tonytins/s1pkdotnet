using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Sims1Pkg.Tests;

internal static class Samples
{
    public static PackageDetails Details(string? id = null) => new()
    {
        Id = id,
        Name = "Retro Diner Set",
        Author = "Maya",
        Date = new DateOnly(2001, 3, 14),
    };

    public static string Sha256(string contents) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(contents)));

    public static PackageFile File(string path, string contents, string kind = "object") =>
        new(path, kind, Encoding.UTF8.GetByteCount(contents), Sha256(contents));

    public static PackageManifest Manifest(params PackageFile[] files) =>
        new(Details(), files.Length == 0 ? [File("Downloads/Booth.iff", "booth")] : files);

    public static string ManifestText(PackageManifest manifest)
    {
        using var stream = new MemoryStream();
        ManifestXml.Write(manifest, stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static PackageManifest ReadManifest(string xml) =>
        ManifestXml.Read(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

    /// <summary>
    /// Writes a ZIP holding <paramref name="entries"/>, by entry name.
    /// </summary>
    public static string Zip(TempFolder folder, IEnumerable<(string Name, string Contents)> entries, string name = "test.s1pk")
    {
        string path = System.IO.Path.Combine(folder.Path, name);
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach ((string entryName, string contents) in entries)
        {
            using var writer = new StreamWriter(archive.CreateEntry(entryName).Open());
            writer.Write(contents);
        }

        return path;
    }

    /// <summary>
    /// Writes a valid package holding <paramref name="files"/>, by path and contents.
    /// </summary>
    public static string Package(TempFolder folder, params (string Path, string Contents)[] files)
    {
        PackageManifest manifest = new(Details(), files.Select(file => File(file.Path, file.Contents)).ToList());
        return Zip(folder, files.Append((PackageFormat.ManifestFileName, ManifestText(manifest))));
    }

    /// <summary>
    /// Packs <paramref name="files"/>, by path and contents, into a package in <paramref name="output"/> and opens it.
    /// </summary>
    public static async Task<Package> PackAsync(TempFolder output, PackageDetails details, params (string Path, string Contents)[] files)
    {
        using var source = new TempFolder();
        foreach ((string path, string contents) in files)
        {
            source.Write(path, contents);
        }

        string packagePath = System.IO.Path.Combine(output.Path, Guid.NewGuid().ToString("N") + ".s1pk");
        await PackageWriter.PackAsync(source.Path, packagePath, details);
        return Sims1Pkg.Package.Open(packagePath);
    }
}
