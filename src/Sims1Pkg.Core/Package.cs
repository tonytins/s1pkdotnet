using System.IO.Compression;
using System.Security.Cryptography;

namespace Sims1Pkg;

/// <summary>
/// An open <c>.s1pk</c> file. Opening checks the archive against its manifest. <see cref="VerifyAsync"/> also checks
/// every file's contents.
/// </summary>
public sealed class Package : IDisposable
{
    // A manifest this big is either broken or hostile.
    private const long MaxManifestLength = 1024 * 1024;

    private readonly ZipArchive _archive;
    private readonly Dictionary<string, ZipArchiveEntry> _entries;

    private Package(string path, ZipArchive archive, PackageManifest manifest, Dictionary<string, ZipArchiveEntry> entries)
    {
        FilePath = path;
        _archive = archive;
        Manifest = manifest;
        _entries = entries;
    }

    /// <summary>
    /// Full path of the package file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// The package's manifest.
    /// </summary>
    public PackageManifest Manifest { get; }

    /// <summary>
    /// Opens a package and checks that the archive matches its manifest: the manifest is at the root, every listed
    /// file is in the archive with the listed size, and the archive holds nothing the manifest doesn't list.
    /// </summary>
    /// <param name="path">Path of the <c>.s1pk</c> file.</param>
    /// <returns>The open package. Dispose it to close the file.</returns>
    /// <exception cref="FileNotFoundException">The file doesn't exist.</exception>
    /// <exception cref="PackageFormatException">The file isn't a valid package.</exception>
    public static Package Open(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The package doesn't exist: " + path, fullPath);
        }

        ZipArchive archive;
        try
        {
            archive = ZipFile.OpenRead(fullPath);
        }
        catch (InvalidDataException exception)
        {
            throw new PackageFormatException($"'{Path.GetFileName(path)}' isn't a ZIP archive, so it isn't a package.", exception);
        }

        try
        {
            Dictionary<string, ZipArchiveEntry> entries = IndexEntries(archive);
            PackageManifest manifest = ReadManifest(entries);
            CheckEntries(manifest, entries);
            return new Package(fullPath, archive, manifest, entries);
        }
        catch
        {
            archive.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Opens one of the package's files for reading.
    /// </summary>
    /// <param name="file">A file from <see cref="Manifest"/>.</param>
    /// <returns>The file's contents. Dispose the stream when done.</returns>
    /// <exception cref="ArgumentException"><paramref name="file"/> isn't in this package.</exception>
    public Stream OpenFile(PackageFile file)
    {
        ArgumentNullException.ThrowIfNull(file);

        return _entries.TryGetValue(file.Path, out ZipArchiveEntry? entry)
            ? entry.Open()
            : throw new ArgumentException($"'{file.Path}' isn't in this package.", nameof(file));
    }

    /// <summary>
    /// Checks every file's contents against the size and SHA-256 in the manifest.
    /// </summary>
    /// <param name="cancellationToken">Stops the check.</param>
    /// <exception cref="PackageFormatException">A file doesn't match the manifest.</exception>
    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        foreach (PackageFile file in Manifest.Files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using Stream stream = OpenFile(file);
            (string sha256, long length) = await HashAsync(stream, cancellationToken).ConfigureAwait(false);

            if (length != file.Length || sha256 != file.Sha256)
            {
                throw new PackageFormatException(
                    $"'{file.Path}' doesn't match the manifest. The package may be damaged.");
            }
        }
    }

    /// <inheritdoc/>
    public void Dispose() => _archive.Dispose();

    internal static async Task<(string Sha256, long Length)> HashAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        byte[] buffer = new byte[81920];
        long length = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            hash.AppendData(buffer, 0, read);
            length += read;
        }

        return (Convert.ToHexStringLower(hash.GetHashAndReset()), length);
    }

    private static Dictionary<string, ZipArchiveEntry> IndexEntries(ZipArchive archive)
    {
        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/') && entry.Length == 0)
            {
                continue;
            }

            if (!entries.TryAdd(entry.FullName, entry))
            {
                throw new PackageFormatException($"The archive holds '{entry.FullName}' more than once.");
            }
        }

        return entries;
    }

    private static PackageManifest ReadManifest(Dictionary<string, ZipArchiveEntry> entries)
    {
        if (!entries.TryGetValue(PackageFormat.ManifestFileName, out ZipArchiveEntry? entry))
        {
            throw new PackageFormatException("The archive has no manifest.xml at its root, so it isn't a package.");
        }

        if (entry.Length > MaxManifestLength)
        {
            throw new PackageFormatException("manifest.xml is too large to be a real manifest.");
        }

        using Stream stream = entry.Open();
        return ManifestXml.Read(stream);
    }

    private static void CheckEntries(PackageManifest manifest, Dictionary<string, ZipArchiveEntry> entries)
    {
        foreach (PackageFile file in manifest.Files)
        {
            if (!entries.TryGetValue(file.Path, out ZipArchiveEntry? entry))
            {
                throw new PackageFormatException($"The manifest lists '{file.Path}', but the archive doesn't hold it.");
            }

            if (entry.Length != file.Length)
            {
                throw new PackageFormatException(
                    $"'{file.Path}' is {entry.Length} bytes in the archive, but the manifest says {file.Length}.");
            }
        }

        var listed = new HashSet<string>(manifest.Files.Select(file => file.Path), StringComparer.Ordinal);
        foreach (string name in entries.Keys)
        {
            if (name != PackageFormat.ManifestFileName && !listed.Contains(name))
            {
                throw new PackageFormatException($"The archive holds '{name}', which the manifest doesn't list.");
            }
        }
    }
}
