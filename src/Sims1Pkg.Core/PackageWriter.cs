using System.IO.Compression;

namespace Sims1Pkg;

/// <summary>
/// Makes <c>.s1pk</c> packages from a folder of content.
/// </summary>
public static class PackageWriter
{
    /// <summary>
    /// Works out what packing <paramref name="sourceFolder"/> would do, without writing anything. Each file's kind
    /// comes from <see cref="KindRules.GetKind"/>.
    /// </summary>
    /// <param name="sourceFolder">The folder of content.</param>
    /// <returns>Every file, with its kind and any problem, plus the files packing leaves out.</returns>
    /// <exception cref="DirectoryNotFoundException">The folder doesn't exist.</exception>
    public static PackPreview Preview(string sourceFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFolder);

        string root = Path.GetFullPath(sourceFolder);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException("The folder doesn't exist: " + sourceFolder);
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = 0,
            IgnoreInaccessible = false,
        };

        var found = new List<(string SourcePath, string ArchivePath)>();
        var leftOut = new List<string>();

        foreach (string file in Directory.EnumerateFiles(root, "*", options))
        {
            string archivePath = Path.GetRelativePath(root, file).Replace(Path.DirectorySeparatorChar, '/');
            if (archivePath.Split('/').Any(KindRules.IsLeftOut))
            {
                leftOut.Add(archivePath);
            }
            else
            {
                found.Add((file, archivePath));
            }
        }

        found.Sort((left, right) => StringComparer.Ordinal.Compare(left.ArchivePath, right.ArchivePath));
        leftOut.Sort(StringComparer.Ordinal);

        var items = found
            .Select(file => new PackItem(file.SourcePath, file.ArchivePath, KindRules.GetKind(file.ArchivePath), null))
            .Select(item => item with { Problem = GetProblem(item) })
            .ToList();

        MarkClashes(items);
        return new PackPreview(items, leftOut);
    }

    /// <summary>
    /// Packs a folder into an <c>.s1pk</c> file. The package is written to a temporary file next to
    /// <paramref name="packagePath"/> and moved into place only when complete, so a failure leaves nothing behind.
    /// </summary>
    /// <param name="sourceFolder">The folder of content.</param>
    /// <param name="packagePath">Where to write the package.</param>
    /// <param name="details">The package's details.</param>
    /// <param name="overwrite">Replace <paramref name="packagePath"/> if it exists.</param>
    /// <param name="cancellationToken">Stops packing.</param>
    /// <returns>The manifest written into the package.</returns>
    /// <exception cref="ArgumentException"><paramref name="details"/> has problems. See <see cref="PackageDetails.GetProblems"/>.</exception>
    /// <exception cref="DirectoryNotFoundException">The folder doesn't exist.</exception>
    /// <exception cref="PackageFormatException">The folder is empty, or a file in it can't be packed.</exception>
    /// <exception cref="IOException"><paramref name="packagePath"/> exists and <paramref name="overwrite"/> is false.</exception>
    public static async Task<PackageManifest> PackAsync(
        string sourceFolder,
        string packagePath,
        PackageDetails details,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packagePath);
        ArgumentNullException.ThrowIfNull(details);

        if (details.GetProblems() is { Count: > 0 } detailProblems)
        {
            throw new ArgumentException("The package details aren't valid: " + string.Join(", ", detailProblems), nameof(details));
        }

        PackPreview preview = Preview(sourceFolder);
        if (preview.Items.Count == 0)
        {
            throw new PackageFormatException("The folder has nothing to pack.");
        }

        if (!preview.IsReady)
        {
            IEnumerable<string> problems = preview.Items
                .Where(item => item.Problem is not null)
                .Select(item => $"'{item.ArchivePath}' ({item.Problem})");
            throw new PackageFormatException("Some files can't be packed: " + string.Join(", ", problems));
        }

        string fullPackagePath = Path.GetFullPath(packagePath);
        if (File.Exists(fullPackagePath) && !overwrite)
        {
            throw new IOException("The package already exists: " + packagePath);
        }

        string outputFolder = Path.GetDirectoryName(fullPackagePath) ?? Environment.CurrentDirectory;
        Directory.CreateDirectory(outputFolder);
        string tempPath = Path.Combine(outputFolder, $".{Path.GetFileName(fullPackagePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            PackageManifest manifest = await WriteArchiveAsync(tempPath, preview, details, cancellationToken).ConfigureAwait(false);
            File.Move(tempPath, fullPackagePath, overwrite);
            return manifest;
        }
        catch
        {
            TryDelete(tempPath);
            throw;
        }
    }

    // A failed cleanup must not hide the error that caused it.
    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static async Task<PackageManifest> WriteArchiveAsync(
        string path,
        PackPreview preview,
        PackageDetails details,
        CancellationToken cancellationToken)
    {
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 81920, useAsync: true);
        await using var archive = new ZipArchive(output, ZipArchiveMode.Create);

        var files = new List<PackageFile>(preview.Items.Count);
        foreach (PackItem item in preview.Items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ZipArchiveEntry entry = archive.CreateEntry(item.ArchivePath, CompressionLevel.Optimal);
            await using Stream entryStream = await entry.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var source = new FileStream(item.SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            await using var copy = new HashingStream(entryStream);

            await source.CopyToAsync(copy, cancellationToken).ConfigureAwait(false);
            (string sha256, long length) = copy.Finish();

            // Kind is never null here: the preview is ready.
            files.Add(new PackageFile(item.ArchivePath, ContentKinds.GetManifestName(item.Kind!.Value), length, sha256));
        }

        var manifest = new PackageManifest(details, files);
        ZipArchiveEntry manifestEntry = archive.CreateEntry(PackageFormat.ManifestFileName, CompressionLevel.Optimal);
        await using (Stream manifestStream = await manifestEntry.OpenAsync(cancellationToken).ConfigureAwait(false))
        {
            ManifestXml.Write(manifest, manifestStream);
        }

        return manifest;
    }

    private static PackProblem? GetProblem(PackItem item)
    {
        if (!ArchivePath.IsValid(item.ArchivePath))
        {
            return PackProblem.PathNotValid;
        }

        return item.Kind is null ? PackProblem.NoKind : null;
    }

    // Two files clash when their archive paths match ignoring case, or when they'd install to the same place.
    // Destinations are worked out with a stand-in id, since only objects use the id, and an object's destination
    // follows its archive path.
    private static void MarkClashes(List<PackItem> items)
    {
        var byDestination = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < items.Count; index++)
        {
            PackItem item = items[index];
            if (item.Problem is not null || item.Kind is not { } kind)
            {
                continue;
            }

            foreach (string key in new[] { "archive:" + item.ArchivePath, "game:" + GameLayout.GetDestination(item.ArchivePath, kind, "id") })
            {
                if (!byDestination.TryGetValue(key, out List<int>? indexes))
                {
                    byDestination[key] = indexes = [];
                }

                indexes.Add(index);
            }
        }

        foreach (List<int> indexes in byDestination.Values.Where(indexes => indexes.Count > 1))
        {
            foreach (int index in indexes)
            {
                items[index] = items[index] with { Problem = PackProblem.NameClash };
            }
        }
    }
}
