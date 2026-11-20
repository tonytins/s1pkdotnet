namespace Sims1Pkg.Tests;

public sealed class PackageWriterTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task PacksAFolderThatReadsBackAndVerifies()
    {
        using var source = new TempFolder();
        using var output = new TempFolder();
        source.Write("Downloads/Diner/Booth.iff", "booth");
        source.Write("GameData/Skins/face.bmp", "face");
        source.Write("walls/Tile.wll", "tile");
        source.Write("Checker.flr", "checker");
        string packagePath = Path.Combine(output.Path, "diner.s1pk");

        PackageManifest written = await PackageWriter.PackAsync(source.Path, packagePath, Samples.Details("maya.diner"), cancellationToken: Token);

        using Package package = Package.Open(packagePath);
        await package.VerifyAsync(Token);
        Assert.Equal(written.Files, package.Manifest.Files);
        Assert.Equal(
            [
                ("Checker.flr", "floor"),
                ("Downloads/Diner/Booth.iff", "object"),
                ("GameData/Skins/face.bmp", "skin"),
                ("walls/Tile.wll", "wall"),
            ],
            package.Manifest.Files.Select(file => (file.Path, file.KindName)));
        Assert.Equal(["diner.s1pk"], output.ListFiles());
    }

    [Fact]
    public async Task PacksLooseSkinTexturesAsSkins()
    {
        using var source = new TempFolder();
        using var output = new TempFolder();
        source.Write("B001FAFitlgt_01.cmx", "mesh");
        source.Write("textures/B001FAFitlgt_01.bmp", "texture");
        string packagePath = Path.Combine(output.Path, "outfit.s1pk");

        await PackageWriter.PackAsync(source.Path, packagePath, Samples.Details("maya.outfit"), cancellationToken: Token);

        using Package package = Package.Open(packagePath);
        await package.VerifyAsync(Token);
        Assert.Equal(
            [
                ("B001FAFitlgt_01.cmx", "skin", "GameData/Skins/B001FAFitlgt_01.cmx"),
                ("textures/B001FAFitlgt_01.bmp", "skin", "GameData/Skins/B001FAFitlgt_01.bmp"),
            ],
            package.Manifest.Files.Select(file =>
                (file.Path, file.KindName, GameLayout.GetDestination(file.Path, ContentKind.Skin, "maya.outfit"))));
    }

    [Fact]
    public void PreviewMarksFilesThatCantBePacked()
    {
        using var source = new TempFolder();
        source.Write("Booth.iff", "booth");
        source.Write("readme.txt", "read me");
        source.Write("Tile.wll", "a");
        source.Write("walls/Tile.wll", "b");

        PackPreview preview = PackageWriter.Preview(source.Path);

        Assert.False(preview.IsReady);
        Assert.Equal(
            [
                ("Booth.iff", (PackProblem?)null),
                ("Tile.wll", PackProblem.NameClash),
                ("readme.txt", PackProblem.NoKind),
                ("walls/Tile.wll", PackProblem.NameClash),
            ],
            preview.Items.Select(item => (item.ArchivePath, item.Problem)));
    }

    [Fact]
    public void PreviewLeavesOutClutter()
    {
        using var source = new TempFolder();
        source.Write("Booth.iff", "booth");
        source.Write(".DS_Store", "x");
        source.Write(".git/config", "x");
        source.Write("Thumbs.db", "x");
        source.Write("old.s1pk", "x");

        PackPreview preview = PackageWriter.Preview(source.Path);

        Assert.True(preview.IsReady);
        Assert.Equal(["Booth.iff"], preview.Items.Select(item => item.ArchivePath));
        Assert.Equal([".DS_Store", ".git/config", "Thumbs.db", "old.s1pk"], preview.LeftOut);
    }

    [Fact]
    public void PreviewMarksPathsWindowsWouldRefuse()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var source = new TempFolder();
        source.Write("Booth?.iff", "booth");

        Assert.Equal(PackProblem.PathNotValid, PackageWriter.Preview(source.Path).Items.Single().Problem);
    }

    [Fact]
    public void PreviewMarksPathsThatClashIgnoringCase()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        using var source = new TempFolder();
        source.Write("Downloads/Booth.iff", "a");
        source.Write("downloads/Booth.iff", "b");

        Assert.All(PackageWriter.Preview(source.Path).Items, item => Assert.Equal(PackProblem.NameClash, item.Problem));
    }

    [Fact]
    public async Task RefusesToPackWhenAFileHasAProblem()
    {
        using var source = new TempFolder();
        using var output = new TempFolder();
        source.Write("Booth.iff", "booth");
        source.Write("readme.txt", "read me");

        var error = await Assert.ThrowsAsync<PackageFormatException>(() =>
            PackageWriter.PackAsync(source.Path, Path.Combine(output.Path, "x.s1pk"), Samples.Details(), cancellationToken: Token));

        Assert.Contains("readme.txt", error.Message);
        Assert.Empty(output.ListFiles());
    }

    [Fact]
    public async Task RefusesAnEmptyFolder()
    {
        using var source = new TempFolder();
        using var output = new TempFolder();

        await Assert.ThrowsAsync<PackageFormatException>(() =>
            PackageWriter.PackAsync(source.Path, Path.Combine(output.Path, "x.s1pk"), Samples.Details(), cancellationToken: Token));
    }

    [Fact]
    public async Task RefusesInvalidDetails()
    {
        using var source = new TempFolder();
        source.Write("Booth.iff", "booth");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            PackageWriter.PackAsync(source.Path, Path.Combine(source.Path, "x.s1pk"), Samples.Details() with { Name = "" }, cancellationToken: Token));
    }

    [Fact]
    public async Task OverwritesOnlyWhenAsked()
    {
        using var source = new TempFolder();
        using var output = new TempFolder();
        source.Write("Booth.iff", "booth");
        string packagePath = output.Write("diner.s1pk", "old");

        await Assert.ThrowsAsync<IOException>(() =>
            PackageWriter.PackAsync(source.Path, packagePath, Samples.Details(), cancellationToken: Token));
        Assert.Equal("old", File.ReadAllText(packagePath));

        await PackageWriter.PackAsync(source.Path, packagePath, Samples.Details(), overwrite: true, cancellationToken: Token);
        using Package package = Package.Open(packagePath);
        Assert.Single(package.Manifest.Files);
    }

    [Fact]
    public async Task CanWriteThePackageInsideTheSourceFolder()
    {
        using var source = new TempFolder();
        source.Write("Booth.iff", "booth");
        string packagePath = Path.Combine(source.Path, "diner.s1pk");

        await PackageWriter.PackAsync(source.Path, packagePath, Samples.Details(), cancellationToken: Token);
        await PackageWriter.PackAsync(source.Path, packagePath, Samples.Details(), overwrite: true, cancellationToken: Token);

        using Package package = Package.Open(packagePath);
        Assert.Equal(["Booth.iff"], package.Manifest.Files.Select(file => file.Path));
    }

    [Fact]
    public async Task CancellingLeavesNothingBehind()
    {
        using var source = new TempFolder();
        using var output = new TempFolder();
        source.Write("Booth.iff", "booth");
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            PackageWriter.PackAsync(source.Path, Path.Combine(output.Path, "x.s1pk"), Samples.Details(), cancellationToken: cancelled.Token));

        Assert.Empty(output.ListFiles());
    }
}
