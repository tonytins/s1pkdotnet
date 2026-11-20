namespace Sims1Pkg;

/// <summary>
/// Installs and uninstalls packages. Each runs in two steps: a plan that checks everything and writes nothing, then
/// the change itself, which puts the game folder back as it was if anything fails or is cancelled.
/// </summary>
/// <param name="timeProvider">The clock for receipts' install times. Defaults to the system clock.</param>
public sealed class Installer(TimeProvider? timeProvider = null)
{
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;

    /// <summary>
    /// Called after each change to the game folder with its number, from 1. Tests use it to fail at every step.
    /// </summary>
    internal Action<int>? AfterStep { get; set; }

    /// <summary>
    /// Works out what installing <paramref name="package"/> will do, and checks that it can: every file's kind is
    /// known, every file matches the manifest, nothing lands in the same place twice, no folder is in the way, and the
    /// game folder can be written to.
    /// </summary>
    /// <param name="package">The open package.</param>
    /// <param name="game">The game to install into.</param>
    /// <param name="cancellationToken">Stops the checks.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="PackageFormatException">The package has a file of an unknown kind or is damaged.</exception>
    /// <exception cref="GameFolderNotWritableException">The game folder can't be written to.</exception>
    /// <exception cref="InstallException">Something in the game folder is in the way, or a receipt is damaged.</exception>
    public async Task<InstallPlan> PlanInstallAsync(Package package, GameInstall game, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentNullException.ThrowIfNull(game);

        PackageManifest manifest = package.Manifest;
        List<string> unknown = [.. manifest.Files.Where(file => file.Kind is null).Select(file => $"'{file.Path}' ({file.KindName})")];
        if (unknown.Count > 0)
        {
            throw new PackageFormatException(
                "This package has files of kinds this version of Sims1Pkg doesn't know, so it can't install them: " +
                string.Join(", ", unknown) + ". Install a newer Sims1Pkg to install it.");
        }

        await package.VerifyAsync(cancellationToken).ConfigureAwait(false);

        string id = manifest.Details.GetEffectiveId();
        Receipt? previous = Receipts.Get(game, id);
        HashSet<string> owned = OwnedPaths(previous);

        var items = new List<InstallItem>(manifest.Files.Count);
        var destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (PackageFile file in manifest.Files)
        {
            string fullPath = game.ResolvePath(GameLayout.GetDestination(file.Path, file.Kind!.Value, id));
            string destination = game.GetRelativePath(fullPath);

            if (!destinations.Add(destination))
            {
                throw new PackageFormatException($"Two files in this package would install to {destination}.");
            }

            if (Directory.Exists(fullPath))
            {
                throw new InstallException($"A folder is in the way of {destination}.");
            }

            items.Add(new InstallItem(file, destination, fullPath, File.Exists(fullPath) && !owned.Contains(destination)));
        }

        CheckWritable(items.Select(item => Path.GetDirectoryName(item.FullPath)!).Append(game.ResolvePath(Receipts.FolderName)));

        InstallKind kind = previous is null ? InstallKind.Install
            : previous.Version == manifest.Details.Version ? InstallKind.Reinstall
            : InstallKind.Update;

        return new InstallPlan(package, game, id, kind, previous, items);
    }

    /// <summary>
    /// Installs a planned package. For an update or reinstall, the installed version's files are removed first, and
    /// any backups it made carry over. If anything fails or is cancelled, the game folder is put back as it was.
    /// </summary>
    /// <param name="plan">A plan from <see cref="PlanInstallAsync"/>.</param>
    /// <param name="replaceClashes">
    /// True when the player agreed to replace <see cref="InstallPlan.Clashes"/>. Must be true when there are any.
    /// </param>
    /// <param name="progress">Told after each file is written.</param>
    /// <param name="cancellationToken">Stops the install and undoes it.</param>
    /// <returns>The new receipt.</returns>
    /// <exception cref="InvalidOperationException">The plan has clashes and <paramref name="replaceClashes"/> is false.</exception>
    /// <exception cref="InstallException">
    /// The game folder changed since the plan, or the install failed and not everything could be put back.
    /// </exception>
    public async Task<Receipt> InstallAsync(
        InstallPlan plan,
        bool replaceClashes,
        IProgress<InstallProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(plan);

        if (plan.Clashes.Count > 0 && !replaceClashes)
        {
            throw new InvalidOperationException("The install would replace files already in the game, and that wasn't agreed to.");
        }

        GameInstall game = plan.Game;
        HashSet<string> owned = OwnedPaths(plan.Previous);
        if (plan.Items.FirstOrDefault(item => !item.Clashes && File.Exists(item.FullPath) && !owned.Contains(item.Destination)) is { } changed)
        {
            throw new InstallException($"{changed.Destination} appeared in the game folder after the install was checked. Try again.");
        }

        string receiptFolder = Receipts.GetFolder(game, plan.Id);
        string staging = Path.Combine(game.ResolvePath(Receipts.FolderName), $".staging-{Guid.NewGuid():N}");
        var journal = new Journal(AfterStep);
        var removed = new List<string>();

        try
        {
            journal.CreateFolder(Path.Combine(staging, "old"));
            StagePreviousVersion(plan, journal, staging, removed);
            List<BackupFile> backups = RestoreOrCarryBackups(plan, journal, receiptFolder);
            BackUpClashes(plan, journal, receiptFolder, backups);

            var files = new List<InstalledFile>(plan.Items.Count);
            foreach (InstallItem item in plan.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                journal.CreateFolder(Path.GetDirectoryName(item.FullPath)!);
                await WriteFileAsync(plan.Package, item, journal, cancellationToken).ConfigureAwait(false);
                files.Add(new InstalledFile(item.Destination, item.File.Sha256));
                progress?.Report(new InstallProgress(files.Count, plan.Items.Count, item.Destination));
            }

            PackageDetails details = plan.Package.Manifest.Details;
            var receipt = new Receipt(plan.Id, details.Name, details.Author, details.Version, _time.GetUtcNow(), files, backups);
            WriteReceipt(receipt, journal, receiptFolder, staging);

            journal.Commit();
            TryDeleteFolder(staging);
            PruneEmptyFolders(game, removed);
            return receipt;
        }
        catch (Exception exception)
        {
            throw Undo(journal, staging, exception, "install");
        }
    }

    /// <summary>
    /// Works out what uninstalling a package will do: which installed files were changed or removed since install.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="id">The package's id.</param>
    /// <param name="cancellationToken">Stops the checks.</param>
    /// <returns>The plan.</returns>
    /// <exception cref="InstallException">The package isn't installed, or its receipt is damaged.</exception>
    /// <exception cref="GameFolderNotWritableException">The game folder can't be written to.</exception>
    public async Task<UninstallPlan> PlanUninstallAsync(GameInstall game, string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Receipt receipt = Receipts.Get(game, id) ?? throw new InstallException($"'{id}' isn't installed.");
        var changed = new List<string>();
        var missing = new List<string>();

        foreach (InstalledFile file in receipt.Files)
        {
            string fullPath = game.ResolvePath(file.Path);
            if (!File.Exists(fullPath))
            {
                missing.Add(file.Path);
                continue;
            }

            await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            (string sha256, _) = await Package.HashAsync(stream, cancellationToken).ConfigureAwait(false);
            if (sha256 != file.Sha256)
            {
                changed.Add(file.Path);
            }
        }

        CheckWritable(receipt.Files.Select(file => Path.GetDirectoryName(game.ResolvePath(file.Path))!)
            .Append(Receipts.GetFolder(game, receipt.Id)));

        return new UninstallPlan(game, receipt, changed, missing);
    }

    /// <summary>
    /// Uninstalls a planned package: removes every file it installed, including changed ones, puts back the files it
    /// replaced, and removes its receipt. If anything fails, the game folder is put back as it was.
    /// </summary>
    /// <param name="plan">A plan from <see cref="PlanUninstallAsync"/>.</param>
    /// <exception cref="InstallException">The uninstall failed and not everything could be put back.</exception>
    public void Uninstall(UninstallPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        GameInstall game = plan.Game;
        Receipt receipt = plan.Receipt;
        string receiptFolder = Receipts.GetFolder(game, receipt.Id);
        string staging = Path.Combine(game.ResolvePath(Receipts.FolderName), $".staging-{Guid.NewGuid():N}");
        var journal = new Journal(AfterStep);
        var removed = new List<string>();

        try
        {
            journal.CreateFolder(Path.Combine(staging, "old"));
            for (int index = 0; index < receipt.Files.Count; index++)
            {
                string fullPath = game.ResolvePath(receipt.Files[index].Path);
                if (File.Exists(fullPath))
                {
                    journal.MoveFile(fullPath, Path.Combine(staging, "old", index.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    removed.Add(fullPath);
                }
            }

            foreach (BackupFile backup in receipt.Backups)
            {
                RestoreBackup(game, journal, receiptFolder, backup);
            }

            journal.MoveFolder(receiptFolder, Path.Combine(staging, "receipt"));
            journal.Commit();
            TryDeleteFolder(staging);
            PruneEmptyFolders(game, removed);
        }
        catch (Exception exception)
        {
            throw Undo(journal, staging, exception, "uninstall");
        }
    }

    private static HashSet<string> OwnedPaths(Receipt? receipt) =>
        new(receipt?.Files.Select(file => file.Path) ?? [], StringComparer.OrdinalIgnoreCase);

    private static void StagePreviousVersion(InstallPlan plan, Journal journal, string staging, List<string> removed)
    {
        if (plan.Previous is not { } previous)
        {
            return;
        }

        for (int index = 0; index < previous.Files.Count; index++)
        {
            string fullPath = plan.Game.ResolvePath(previous.Files[index].Path);
            if (File.Exists(fullPath))
            {
                journal.MoveFile(fullPath, Path.Combine(staging, "old", index.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                removed.Add(fullPath);
            }
        }
    }

    // A backup the new version still covers carries over; one it doesn't is put back now, since nothing replaces
    // that file any more. One whose place has been taken since stays a backup.
    private static List<BackupFile> RestoreOrCarryBackups(InstallPlan plan, Journal journal, string receiptFolder)
    {
        var kept = new List<BackupFile>();
        if (plan.Previous is not { } previous)
        {
            return kept;
        }

        var destinations = new HashSet<string>(plan.Items.Select(item => item.Destination), StringComparer.OrdinalIgnoreCase);
        foreach (BackupFile backup in previous.Backups)
        {
            if (destinations.Contains(backup.OriginalPath) || File.Exists(plan.Game.ResolvePath(backup.OriginalPath)))
            {
                kept.Add(backup);
            }
            else
            {
                RestoreBackup(plan.Game, journal, receiptFolder, backup);
            }
        }

        return kept;
    }

    private static void RestoreBackup(GameInstall game, Journal journal, string receiptFolder, BackupFile backup)
    {
        string saved = Path.Combine(receiptFolder, backup.SavedPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(saved))
        {
            return;
        }

        string original = game.ResolvePath(backup.OriginalPath);
        journal.CreateFolder(Path.GetDirectoryName(original)!);
        journal.MoveFile(saved, original);
    }

    private static void BackUpClashes(InstallPlan plan, Journal journal, string receiptFolder, List<BackupFile> backups)
    {
        var used = new HashSet<string>(backups.Select(backup => backup.SavedPath), StringComparer.OrdinalIgnoreCase);

        foreach (InstallItem item in plan.Items.Where(item => item.Clashes))
        {
            string savedPath = "backups/" + item.Destination;
            for (int copy = 2; used.Contains(savedPath) || File.Exists(Path.Combine(receiptFolder, savedPath)); copy++)
            {
                savedPath = $"backups/{copy}/{item.Destination}";
            }

            string savedFull = Path.Combine(receiptFolder, savedPath.Replace('/', Path.DirectorySeparatorChar));
            journal.CreateFolder(Path.GetDirectoryName(savedFull)!);
            journal.MoveFile(item.FullPath, savedFull);
            used.Add(savedPath);
            backups.Add(new BackupFile(item.Destination, savedPath));
        }
    }

    private static async Task WriteFileAsync(Package package, InstallItem item, Journal journal, CancellationToken cancellationToken)
    {
        string temp = Path.Combine(Path.GetDirectoryName(item.FullPath)!, $".s1pk-{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            await using (Stream source = package.OpenFile(item.File))
            await using (var hashing = new HashingStream(output))
            {
                await source.CopyToAsync(hashing, cancellationToken).ConfigureAwait(false);
                (string sha256, long length) = hashing.Finish();
                if (sha256 != item.File.Sha256 || length != item.File.Length)
                {
                    throw new PackageFormatException($"'{item.File.Path}' doesn't match the manifest. The package may be damaged.");
                }
            }

            journal.Do(() => File.Move(temp, item.FullPath), () => File.Delete(item.FullPath));
        }
        finally
        {
            TryDeleteFile(temp);
        }
    }

    private static void WriteReceipt(Receipt receipt, Journal journal, string receiptFolder, string staging)
    {
        journal.CreateFolder(receiptFolder);
        string path = Path.Combine(receiptFolder, Receipts.FileName);
        if (File.Exists(path))
        {
            journal.MoveFile(path, Path.Combine(staging, Receipts.FileName));
        }

        string temp = Path.Combine(receiptFolder, $".{Receipts.FileName}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write))
            {
                ReceiptXml.Write(receipt, stream);
            }

            journal.Do(() => File.Move(temp, path), () => File.Delete(path));
        }
        finally
        {
            TryDeleteFile(temp);
        }
    }

    private static Exception Undo(Journal journal, string staging, Exception exception, string what)
    {
        IReadOnlyList<Exception> failures = journal.Rollback();
        TryDeleteFolder(staging);

        if (failures.Count == 0)
        {
            return exception is OperationCanceledException or PackageFormatException or InstallException
                ? exception
                : new InstallException($"The {what} failed and was undone: {exception.Message}", exception);
        }

        return new InstallException(
            $"The {what} failed, and Sims1Pkg couldn't put everything back. Files waiting to be restored are in {staging}. " +
            $"The {what} failed because: {exception.Message}",
            new AggregateException([exception, .. failures]));
    }

    private static void CheckWritable(IEnumerable<string> folders)
    {
        foreach (string folder in folders.Distinct(StringComparer.Ordinal))
        {
            string? existing = folder;
            while (existing is not null && !Directory.Exists(existing))
            {
                existing = Path.GetDirectoryName(existing);
            }

            if (existing is null)
            {
                continue;
            }

            try
            {
                using var probe = new FileStream(
                    Path.Combine(existing, $".s1pk-probe-{Guid.NewGuid():N}"),
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.DeleteOnClose);
            }
            catch (UnauthorizedAccessException exception)
            {
                throw new GameFolderNotWritableException(existing, exception);
            }
        }
    }

    // After files are removed, their folders inside a package's own Downloads folder may be empty. The game's own
    // folders are never removed.
    private static void PruneEmptyFolders(GameInstall game, IEnumerable<string> removedFiles)
    {
        var keep = new HashSet<string>(
            Enum.GetValues<ContentKind>().Select(game.GetContentDirectory).Append(game.RootDirectory),
            StringComparer.Ordinal);

        foreach (string file in removedFiles)
        {
            for (string? folder = Path.GetDirectoryName(file);
                 folder is not null && !keep.Contains(folder) && folder.StartsWith(game.RootDirectory, StringComparison.Ordinal);
                 folder = Path.GetDirectoryName(folder))
            {
                try
                {
                    if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder).Any())
                    {
                        Directory.Delete(folder);
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    break;
                }
            }
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static void TryDeleteFolder(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
