using System.Globalization;

namespace Sims1Pkg.Gui.Tests;

public sealed class TextKeysTests
{
    private static readonly InterfaceText English = new(Path.Combine(AppContext.BaseDirectory, "uitext"), CultureInfo.InvariantCulture);

    [Fact]
    public void EveryKindHasText() =>
        Assert.All(Enum.GetValues<ContentKind>(), kind => Assert.True(English.Has(TextKeys.ForKind(kind))));

    [Fact]
    public void EveryPackProblemHasText() =>
        Assert.All(Enum.GetValues<PackProblem>(), problem => Assert.True(English.Has(TextKeys.ForProblem(problem))));

    [Fact]
    public void EveryDetailsProblemHasText() =>
        Assert.All(Enum.GetValues<PackageDetailsProblem>(), problem => Assert.True(English.Has(TextKeys.ForProblem(problem))));
}
