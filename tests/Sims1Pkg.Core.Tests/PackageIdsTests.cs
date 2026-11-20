namespace Sims1Pkg.Tests;

public sealed class PackageIdsTests
{
    [Theory]
    [InlineData("maya.retro-diner")]
    [InlineData("A_b-9")]
    [InlineData("x")]
    public void AcceptsValidIds(string id) => Assert.True(PackageIds.IsValid(id));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(".hidden")]
    [InlineData("trailing.")]
    [InlineData("has space")]
    [InlineData("slash/id")]
    [InlineData("café")]
    [InlineData("con")]
    [InlineData("LPT1.diner")]
    public void RejectsInvalidIds(string? id) => Assert.False(PackageIds.IsValid(id));

    [Fact]
    public void RejectsIdsLongerThanTheLimit()
    {
        Assert.True(PackageIds.IsValid(new string('a', PackageIds.MaxLength)));
        Assert.False(PackageIds.IsValid(new string('a', PackageIds.MaxLength + 1)));
    }

    [Fact]
    public void NormalizeLowercasesSoCaseOnlyDifferencesMatch() =>
        Assert.Equal(PackageIds.Normalize("Maya.Diner"), PackageIds.Normalize("maya.DINER"));

    [Fact]
    public void NormalizeRejectsInvalidIds() =>
        Assert.Throws<ArgumentException>(() => PackageIds.Normalize("not valid"));

    [Theory]
    [InlineData("Retro Diner Set", "Maya", "maya.retro-diner-set")]
    [InlineData("Café Crème", "Zoé", "zoe.cafe-creme")]
    [InlineData("  Lots!!of   punctuation?? ", "A.B", "a-b.lots-of-punctuation")]
    [InlineData("Con", "", "con-pkg")]
    public void MakesIdsFromNameAndAuthor(string name, string author, string expected) =>
        Assert.Equal(expected, PackageIds.FromNameAndAuthor(name, author));

    [Fact]
    public void FallsBackToAHashWhenNothingUsableIsLeft()
    {
        string id = PackageIds.FromNameAndAuthor("ダイナー", "マヤ");

        Assert.StartsWith("pkg-", id);
        Assert.True(PackageIds.IsValid(id));
        Assert.Equal(id, PackageIds.FromNameAndAuthor("ダイナー", "マヤ"));
        Assert.NotEqual(id, PackageIds.FromNameAndAuthor("ダイナー", "ジョン"));
    }

    [Fact]
    public void TruncatesLongNamesToAValidId()
    {
        string id = PackageIds.FromNameAndAuthor(new string('n', 100), "maya");

        Assert.True(PackageIds.IsValid(id));
        Assert.Equal(PackageIds.MaxLength, id.Length);
    }
}
