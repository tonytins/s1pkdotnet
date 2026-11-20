namespace Sims1Pkg.Tests;

public sealed class PackageDetailsTests
{
    [Fact]
    public void ValidDetailsHaveNoProblems() => Assert.Empty(Samples.Details().GetProblems());

    [Fact]
    public void ReportsEachProblem()
    {
        var details = Samples.Details("bad id") with
        {
            Name = " ",
            Author = "",
            SourceUrl = new Uri("ftp://example.com/diner"),
        };

        Assert.Equal(
            [
                PackageDetailsProblem.NameMissing,
                PackageDetailsProblem.AuthorMissing,
                PackageDetailsProblem.IdNotValid,
                PackageDetailsProblem.SourceUrlNotWeb,
            ],
            details.GetProblems());
    }

    [Fact]
    public void EffectiveIdIsTheLowercasedId() =>
        Assert.Equal("maya.diner", Samples.Details("Maya.Diner").GetEffectiveId());

    [Fact]
    public void EffectiveIdFallsBackToAuthorAndName() =>
        Assert.Equal("maya.retro-diner-set", Samples.Details().GetEffectiveId());

    [Fact]
    public void EffectiveIdRefusesAnInvalidId() =>
        Assert.Throws<InvalidOperationException>(() => Samples.Details("bad id").GetEffectiveId());
}
