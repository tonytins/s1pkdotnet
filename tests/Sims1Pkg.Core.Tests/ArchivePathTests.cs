namespace Sims1Pkg.Tests;

public sealed class ArchivePathTests
{
    [Theory]
    [InlineData("Booth.iff")]
    [InlineData("Downloads/Diner/Booth.iff")]
    [InlineData("GameData/Skins/B001FAfit_01.cmx")]
    [InlineData("names with spaces/and.dots.iff")]
    public void AcceptsSafePaths(string path) => Assert.True(ArchivePath.IsValid(path));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/etc/passwd")]
    [InlineData("../outside.iff")]
    [InlineData("Downloads/../../outside.iff")]
    [InlineData("./Booth.iff")]
    [InlineData("Downloads//Booth.iff")]
    [InlineData("Downloads/")]
    [InlineData(@"Downloads\Booth.iff")]
    [InlineData("C:/Windows/win.ini")]
    [InlineData("C:Booth.iff")]
    [InlineData("Booth?.iff")]
    [InlineData("Booth.iff.")]
    [InlineData("Booth.iff ")]
    [InlineData("Downloads/CON.iff")]
    [InlineData("nul")]
    [InlineData("Booth\u0001.iff")]
    public void RejectsUnsafePaths(string? path) => Assert.False(ArchivePath.IsValid(path));
}
