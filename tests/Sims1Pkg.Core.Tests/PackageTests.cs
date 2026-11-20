namespace Sims1Pkg.Tests;

public sealed class PackageTests
{
    [Fact]
    public async Task OpensAndVerifiesAValidPackage()
    {
        using var folder = new TempFolder();
        string path = Samples.Package(folder, ("Downloads/Booth.iff", "booth"), ("Tile.wll", "tile"));

        using Package package = Package.Open(path);
        await package.VerifyAsync(TestContext.Current.CancellationToken);

        Assert.Equal("Retro Diner Set", package.Manifest.Details.Name);
        Assert.Equal(2, package.Manifest.Files.Count);
        using var reader = new StreamReader(package.OpenFile(package.Manifest.Files[0]));
        Assert.Equal("booth", await reader.ReadToEndAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void RefusesAMissingFile() =>
        Assert.Throws<FileNotFoundException>(() => Package.Open(Path.Combine(Path.GetTempPath(), "no-such.s1pk")));

    [Fact]
    public void RefusesAFileThatIsntAZip()
    {
        using var folder = new TempFolder();
        string path = folder.Write("fake.s1pk", "not a zip");

        Assert.Throws<PackageFormatException>(() => Package.Open(path));
    }

    [Fact]
    public void RefusesAZipWithoutAManifest()
    {
        using var folder = new TempFolder();
        string path = Samples.Zip(folder, [("Booth.iff", "booth")]);

        Assert.Throws<PackageFormatException>(() => Package.Open(path));
    }

    [Fact]
    public void RefusesAManifestThatIsntAtTheRoot()
    {
        using var folder = new TempFolder();
        string manifest = Samples.ManifestText(Samples.Manifest(Samples.File("Booth.iff", "booth")));
        string path = Samples.Zip(folder, [("Booth.iff", "booth"), ("inner/manifest.xml", manifest)]);

        Assert.Throws<PackageFormatException>(() => Package.Open(path));
    }

    [Fact]
    public void RefusesAListedFileThatIsMissing()
    {
        using var folder = new TempFolder();
        string manifest = Samples.ManifestText(Samples.Manifest(Samples.File("Booth.iff", "booth")));
        string path = Samples.Zip(folder, [(PackageFormat.ManifestFileName, manifest)]);

        Assert.Throws<PackageFormatException>(() => Package.Open(path));
    }

    [Fact]
    public void RefusesAnUnlistedFile()
    {
        using var folder = new TempFolder();
        string manifest = Samples.ManifestText(Samples.Manifest(Samples.File("Booth.iff", "booth")));
        string path = Samples.Zip(folder, [("Booth.iff", "booth"), ("../escape.iff", "x"), (PackageFormat.ManifestFileName, manifest)]);

        Assert.Throws<PackageFormatException>(() => Package.Open(path));
    }

    [Fact]
    public void RefusesAFileWhoseSizeDoesntMatch()
    {
        using var folder = new TempFolder();
        string manifest = Samples.ManifestText(Samples.Manifest(Samples.File("Booth.iff", "booth")));
        string path = Samples.Zip(folder, [("Booth.iff", "a longer booth"), (PackageFormat.ManifestFileName, manifest)]);

        Assert.Throws<PackageFormatException>(() => Package.Open(path));
    }

    [Fact]
    public async Task VerifyFindsAFileWhoseContentsDontMatch()
    {
        using var folder = new TempFolder();
        string manifest = Samples.ManifestText(Samples.Manifest(Samples.File("Booth.iff", "booth")));
        string path = Samples.Zip(folder, [("Booth.iff", "BOOTH"), (PackageFormat.ManifestFileName, manifest)]);

        using Package package = Package.Open(path);
        await Assert.ThrowsAsync<PackageFormatException>(() => package.VerifyAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public void IgnoresFolderEntries()
    {
        using var folder = new TempFolder();
        string manifest = Samples.ManifestText(Samples.Manifest(Samples.File("Downloads/Booth.iff", "booth")));
        string path = Samples.Zip(folder, [("Downloads/", ""), ("Downloads/Booth.iff", "booth"), (PackageFormat.ManifestFileName, manifest)]);

        using Package package = Package.Open(path);
        Assert.Single(package.Manifest.Files);
    }

    [Fact]
    public void OpenFileRefusesAFileFromElsewhere()
    {
        using var folder = new TempFolder();
        using Package package = Package.Open(Samples.Package(folder, ("Booth.iff", "booth")));

        Assert.Throws<ArgumentException>(() => package.OpenFile(Samples.File("Other.iff", "x")));
    }
}
