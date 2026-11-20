namespace Sims1Pkg.Tests;

public sealed class InstallerTests : IDisposable
{
    private static readonly DateTimeOffset Now = new(2026, 11, 19, 20, 30, 0, TimeSpan.Zero);

    private readonly FakeGame _game = new();
    private readonly TempFolder _packages = new();
    private readonly Installer _installer = new(new FixedTime(Now));

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    private static PackageDetails Version(string version) => Samples.Details("maya.diner") with { Version = version };

    public void Dispose()
    {
        _game.Dispose();
        _packages.Dispose();
    }

    private Task<Package> PackAsync(string version, params (string Path, string Contents)[] files) =>
        Samples.PackAsync(_packages, Version(version), files);

    private async Task<Receipt> InstallAsync(Package package, bool replace = false)
    {
        InstallPlan plan = await _installer.PlanInstallAsync(package, _game.Game, Token);
        return await _installer.InstallAsync(plan, replace, cancellationToken: Token);
    }

    private async Task UninstallAsync(string id = "maya.diner") =>
        _installer.Uninstall(await _installer.PlanUninstallAsync(_game.Game, id, Token));

    [Fact]
    public async Task InstallsEachKindWhereTheGameReadsIt()
    {
        using Package package = await PackAsync("1.0",
            ("Downloads/Diner/Booth.iff", "booth"), ("Checker.flr", "checker"), ("Tile.wll", "tile"), ("GameData/Skins/face.bmp", "face"));

        InstallPlan plan = await _installer.PlanInstallAsync(package, _game.Game, Token);
        Receipt receipt = await _installer.InstallAsync(plan, replaceClashes: false, cancellationToken: Token);

        Assert.Equal(InstallKind.Install, plan.Kind);
        Assert.Empty(plan.Clashes);
        Assert.Equal("booth", _game.Read("Downloads/maya.diner/Diner/Booth.iff"));
        Assert.Equal("checker", _game.Read("GameData/Floors/Checker.flr"));
        Assert.Equal("tile", _game.Read("GameData/Walls/Tile.wll"));
        Assert.Equal("face", _game.Read("GameData/Skins/face.bmp"));
        Assert.Equal(Now, receipt.InstalledAt);
        Assert.Equal(receipt, Receipts.Get(_game.Game, "maya.diner") is { } read ? read with { Files = receipt.Files, Backups = receipt.Backups } : null);
        Assert.Equal(4, Receipts.Get(_game.Game, "maya.diner")!.Files.Count);
        Assert.Equal(["maya.diner"], Directory.GetFileSystemEntries(Path.Combine(_game.Path, "S1PK")).Select(Path.GetFileName));
    }

    [Fact]
    public async Task UninstallPutsTheGameBackAsItWas()
    {
        var before = _game.Snapshot();
        using Package package = await PackAsync("1.0", ("Downloads/Diner/Booth.iff", "booth"), ("Tile.wll", "tile"));

        await InstallAsync(package);
        await UninstallAsync();

        Assert.Equal(before, WithoutEmptyReceiptsFolder(_game.Snapshot()));
    }

    [Fact]
    public async Task ReportsClashesAndRefusesToReplaceThemUnlessAgreed()
    {
        _game.Write("GameData/Walls/Tile.wll", "the player's tile");
        var before = _game.Snapshot();
        using Package package = await PackAsync("1.0", ("Tile.wll", "tile"), ("Booth.iff", "booth"));

        InstallPlan plan = await _installer.PlanInstallAsync(package, _game.Game, Token);

        Assert.Equal(["GameData/Walls/Tile.wll"], plan.Clashes.Select(item => item.Destination));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _installer.InstallAsync(plan, replaceClashes: false, cancellationToken: Token));
        Assert.Equal(before, _game.Snapshot());
    }

    [Fact]
    public async Task ReplacedFilesAreBackedUpAndPutBackOnUninstall()
    {
        _game.Write("GameData/Walls/Tile.wll", "the player's tile");
        var before = _game.Snapshot();
        using Package package = await PackAsync("1.0", ("Tile.wll", "tile"));

        Receipt receipt = await InstallAsync(package, replace: true);

        Assert.Equal("tile", _game.Read("GameData/Walls/Tile.wll"));
        Assert.Equal([new BackupFile("GameData/Walls/Tile.wll", "backups/GameData/Walls/Tile.wll")], receipt.Backups);
        Assert.Equal("the player's tile", _game.Read("S1PK/maya.diner/backups/GameData/Walls/Tile.wll"));

        await UninstallAsync();

        Assert.Equal(before, WithoutEmptyReceiptsFolder(_game.Snapshot()));
    }

    [Fact]
    public async Task UpdatesInPlaceAndCarriesBackupsOver()
    {
        _game.Write("GameData/Walls/Tile.wll", "the player's tile");
        var before = _game.Snapshot();
        using Package first = await PackAsync("1.0", ("Tile.wll", "tile 1"), ("Booth.iff", "booth 1"), ("Old.iff", "old"));
        await InstallAsync(first, replace: true);
        using Package second = await PackAsync("2.0", ("Tile.wll", "tile 2"), ("Booth.iff", "booth 2"), ("New.iff", "new"));

        InstallPlan plan = await _installer.PlanInstallAsync(second, _game.Game, Token);
        Receipt receipt = await _installer.InstallAsync(plan, replaceClashes: false, cancellationToken: Token);

        Assert.Equal(InstallKind.Update, plan.Kind);
        Assert.Empty(plan.Clashes);
        Assert.Equal("tile 2", _game.Read("GameData/Walls/Tile.wll"));
        Assert.Equal("booth 2", _game.Read("Downloads/maya.diner/Booth.iff"));
        Assert.Equal("new", _game.Read("Downloads/maya.diner/New.iff"));
        Assert.False(_game.Exists("Downloads/maya.diner/Old.iff"));
        Assert.Single(receipt.Backups);

        await UninstallAsync();

        Assert.Equal(before, WithoutEmptyReceiptsFolder(_game.Snapshot()));
    }

    [Fact]
    public async Task AnUpdateThatDropsAReplacedFilePutsTheOriginalBack()
    {
        _game.Write("GameData/Walls/Tile.wll", "the player's tile");
        using Package first = await PackAsync("1.0", ("Tile.wll", "tile"), ("Booth.iff", "booth"));
        await InstallAsync(first, replace: true);
        using Package second = await PackAsync("2.0", ("Booth.iff", "booth 2"));

        Receipt receipt = await InstallAsync(second);

        Assert.Equal("the player's tile", _game.Read("GameData/Walls/Tile.wll"));
        Assert.Empty(receipt.Backups);
    }

    [Fact]
    public async Task InstallingTheSameVersionIsAReinstall()
    {
        using Package package = await PackAsync("1.0", ("Booth.iff", "booth"));
        await InstallAsync(package);

        InstallPlan plan = await _installer.PlanInstallAsync(package, _game.Game, Token);

        Assert.Equal(InstallKind.Reinstall, plan.Kind);
        Assert.Empty(plan.Clashes);
        await _installer.InstallAsync(plan, replaceClashes: false, cancellationToken: Token);
        Assert.Equal("booth", _game.Read("Downloads/maya.diner/Booth.iff"));
    }

    [Fact]
    public async Task UsesTheInstallsOwnFolderCasing()
    {
        using var game = new FakeGame(downloadsName: "downloads");
        using Package package = await PackAsync("1.0", ("Booth.iff", "booth"));

        InstallPlan plan = await _installer.PlanInstallAsync(package, game.Game, Token);
        await _installer.InstallAsync(plan, replaceClashes: false, cancellationToken: Token);

        Assert.Equal("booth", game.Read("downloads/maya.diner/Booth.iff"));
        Assert.Equal(["downloads"], Directory.GetDirectories(game.Path, "?ownloads").Select(Path.GetFileName));
    }

    [Fact]
    public async Task RefusesPackagesWithUnknownKinds()
    {
        string manifest = Samples.ManifestText(Samples.Manifest(Samples.File("song.mp3", "la", "music")));
        using Package package = Package.Open(Samples.Zip(_packages, [("song.mp3", "la"), (PackageFormat.ManifestFileName, manifest)]));

        await Assert.ThrowsAsync<PackageFormatException>(() => _installer.PlanInstallAsync(package, _game.Game, Token));
    }

    [Fact]
    public async Task RefusesWhenAFolderIsInTheWay()
    {
        Directory.CreateDirectory(Path.Combine(_game.Path, "GameData", "Walls", "Tile.wll"));
        using Package package = await PackAsync("1.0", ("Tile.wll", "tile"));

        await Assert.ThrowsAsync<InstallException>(() => _installer.PlanInstallAsync(package, _game.Game, Token));
    }

    [Fact]
    public async Task RefusesWhenAFileAppearsAfterThePlan()
    {
        using Package package = await PackAsync("1.0", ("Tile.wll", "tile"));
        InstallPlan plan = await _installer.PlanInstallAsync(package, _game.Game, Token);
        _game.Write("GameData/Walls/Tile.wll", "sneaky");

        await Assert.ThrowsAsync<InstallException>(() => _installer.InstallAsync(plan, replaceClashes: false, cancellationToken: Token));
        Assert.Equal("sneaky", _game.Read("GameData/Walls/Tile.wll"));
    }

    [Fact]
    public async Task UninstallPlanReportsChangedAndMissingFiles()
    {
        using Package package = await PackAsync("1.0", ("Booth.iff", "booth"), ("Tile.wll", "tile"), ("Checker.flr", "checker"));
        await InstallAsync(package);
        _game.Write("GameData/Walls/Tile.wll", "edited by the player");
        File.Delete(Path.Combine(_game.Path, "GameData", "Floors", "Checker.flr"));

        UninstallPlan plan = await _installer.PlanUninstallAsync(_game.Game, "maya.diner", Token);
        _installer.Uninstall(plan);

        Assert.Equal(["GameData/Walls/Tile.wll"], plan.Changed);
        Assert.Equal(["GameData/Floors/Checker.flr"], plan.Missing);
        Assert.False(_game.Exists("GameData/Walls/Tile.wll"));
        Assert.Null(Receipts.Get(_game.Game, "maya.diner"));
    }

    [Fact]
    public async Task UninstallRefusesAPackageThatIsntInstalled() =>
        await Assert.ThrowsAsync<InstallException>(() => _installer.PlanUninstallAsync(_game.Game, "maya.diner", Token));

    [Fact]
    public async Task CancellingPutsTheGameBack()
    {
        var before = _game.Snapshot();
        using Package package = await PackAsync("1.0", ("Booth.iff", "booth"), ("Tile.wll", "tile"));
        InstallPlan plan = await _installer.PlanInstallAsync(package, _game.Game, Token);
        using var cancel = new CancellationTokenSource();
        var progress = new SyncProgress<InstallProgress>(_ => cancel.Cancel());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _installer.InstallAsync(plan, false, progress, cancel.Token));

        Assert.Equal(before, _game.Snapshot());
    }

    [Fact]
    public async Task AFailedUpdateAtAnyStepLeavesTheOldVersionInstalled()
    {
        _game.Write("GameData/Walls/Tile.wll", "the player's tile");
        _game.Write("GameData/Walls/Other.wll", "the player's other tile");
        using Package first = await PackAsync("1.0", ("Tile.wll", "tile 1"), ("Downloads/Diner/Booth.iff", "booth 1"), ("Old.iff", "old"));
        await InstallAsync(first, replace: true);
        using Package second = await PackAsync("2.0", ("Other.wll", "other 2"), ("Downloads/Diner/Booth.iff", "booth 2"), ("Sub/New.iff", "new"));

        int failedRuns = await FailAtEveryStepAsync(async installer =>
        {
            InstallPlan plan = await installer.PlanInstallAsync(second, _game.Game, Token);
            await installer.InstallAsync(plan, replaceClashes: true, cancellationToken: Token);
        });

        Assert.True(failedRuns > 10, $"Only {failedRuns} steps were tried.");
        Assert.Equal("booth 2", _game.Read("Downloads/maya.diner/Diner/Booth.iff"));
        Assert.Equal("other 2", _game.Read("GameData/Walls/Other.wll"));
        Assert.Equal("the player's tile", _game.Read("GameData/Walls/Tile.wll"));
        Assert.False(_game.Exists("Downloads/maya.diner/Old.iff"));
    }

    [Fact]
    public async Task AFailedUninstallAtAnyStepLeavesThePackageInstalled()
    {
        _game.Write("GameData/Walls/Tile.wll", "the player's tile");
        using Package package = await PackAsync("1.0", ("Tile.wll", "tile"), ("Downloads/Diner/Booth.iff", "booth"));
        await InstallAsync(package, replace: true);

        int failedRuns = await FailAtEveryStepAsync(async installer =>
            installer.Uninstall(await installer.PlanUninstallAsync(_game.Game, "maya.diner", Token)));

        Assert.True(failedRuns > 3, $"Only {failedRuns} steps were tried.");
        Assert.Null(Receipts.Get(_game.Game, "maya.diner"));
        Assert.Equal("the player's tile", _game.Read("GameData/Walls/Tile.wll"));
        Assert.False(_game.Exists("Downloads/maya.diner"));
    }

    [Fact]
    public async Task RefusesAGameFolderItCantWriteTo()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        string walls = Path.Combine(_game.Path, "GameData", "Walls");
        File.SetUnixFileMode(walls, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            if (CanWrite(walls))
            {
                return; // Running as root, which ignores the mode.
            }

            using Package package = await PackAsync("1.0", ("Tile.wll", "tile"));

            var error = await Assert.ThrowsAsync<GameFolderNotWritableException>(() => _installer.PlanInstallAsync(package, _game.Game, Token));
            Assert.Equal(walls, error.Folder);
        }
        finally
        {
            File.SetUnixFileMode(walls, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    /// <summary>
    /// Runs <paramref name="operation"/> once per step, failing at that step, until a run gets through every step.
    /// Every failed run must leave the game folder exactly as it was.
    /// </summary>
    private async Task<int> FailAtEveryStepAsync(Func<Installer, Task> operation)
    {
        var before = _game.Snapshot();
        for (int failAt = 1; failAt < 500; failAt++)
        {
            var installer = new Installer(new FixedTime(Now))
            {
                AfterStep = step =>
                {
                    if (step == failAt)
                    {
                        throw new IOException($"Failing at step {step}.");
                    }
                },
            };

            try
            {
                await operation(installer);
                return failAt - 1;
            }
            catch (InstallException)
            {
                Assert.True(before.SequenceEqual(_game.Snapshot()), $"The game folder changed after failing at step {failAt}.");
            }
        }

        throw new InvalidOperationException("The operation never finished.");
    }

    private static bool CanWrite(string folder)
    {
        try
        {
            File.Delete(File.Create(Path.Combine(folder, "probe")).Name);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    // Installing makes the S1PK folder, and uninstalling the last package leaves it empty.
    private static SortedDictionary<string, string> WithoutEmptyReceiptsFolder(SortedDictionary<string, string> snapshot)
    {
        if (snapshot.Keys.Count(key => key.StartsWith("S1PK", StringComparison.Ordinal)) == 1)
        {
            snapshot.Remove("S1PK");
        }

        return snapshot;
    }

    private sealed class SyncProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
