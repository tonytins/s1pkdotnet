namespace Sims1Pkg.Gui.Tests;

public sealed class PackFormTests
{
    private static readonly DateOnly Today = new(2026, 11, 19);

    private static PackForm Filled() => new(Today) { Name = " Retro Diner Set ", Author = "Maya" };

    [Fact]
    public void StartsWithTodaysDate() => Assert.Equal("2026-11-19", new PackForm(Today).Date);

    [Fact]
    public void AFilledFormMakesTrimmedDetails()
    {
        PackForm form = Filled();
        form.Version = "1.2";
        form.SourceUrl = " https://example.com/diner ";

        PackageDetails? details = form.ToDetails();

        Assert.NotNull(details);
        Assert.Equal("Retro Diner Set", details.Name);
        Assert.Equal(Today, details.Date);
        Assert.Equal("1.2", details.Version);
        Assert.Equal(new Uri("https://example.com/diner"), details.SourceUrl);
        Assert.Null(details.Id);
        Assert.Null(details.License);
    }

    [Fact]
    public void ListsEveryProblemInFieldOrder()
    {
        var form = new PackForm(Today) { Date = "19/11/2026", Id = "not valid", SourceUrl = "ftp://example.com" };

        Assert.Equal(
            [
                TextKeys.ProblemNameMissing,
                TextKeys.ProblemAuthorMissing,
                TextKeys.ProblemDate,
                TextKeys.ProblemIdNotValid,
                TextKeys.ProblemSourceUrl,
            ],
            form.GetProblems());
        Assert.Null(form.ToDetails());
    }

    [Theory]
    [InlineData("not a url")]
    [InlineData("/relative/path")]
    public void RefusesAWebPageThatIsntOne(string url)
    {
        PackForm form = Filled();
        form.SourceUrl = url;

        Assert.Equal([TextKeys.ProblemSourceUrl], form.GetProblems());
    }

    [Fact]
    public void EffectiveIdFollowsTheFields()
    {
        PackForm form = Filled();
        Assert.Equal("maya.retro-diner-set", form.GetEffectiveId());

        form.Id = "Maya.Diner";
        Assert.Equal("maya.diner", form.GetEffectiveId());

        form.Id = "not valid";
        Assert.Null(form.GetEffectiveId());

        form.Id = "";
        form.Author = "";
        Assert.Null(form.GetEffectiveId());
    }
}
