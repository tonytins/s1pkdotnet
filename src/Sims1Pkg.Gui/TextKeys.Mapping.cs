namespace Sims1Pkg.Gui;

/// <summary>
/// Which text key says each of Core's values in the player's language.
/// </summary>
internal static partial class TextKeys
{
    public static string ForKind(ContentKind kind) => kind switch
    {
        ContentKind.Object => KindObject,
        ContentKind.Skin => KindSkin,
        ContentKind.Wall => KindWall,
        ContentKind.Floor => KindFloor,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static string ForProblem(PackProblem problem) => problem switch
    {
        PackProblem.NoKind => ProblemNoKind,
        PackProblem.PathNotValid => ProblemPathNotValid,
        PackProblem.NameClash => ProblemNameClash,
        _ => throw new ArgumentOutOfRangeException(nameof(problem), problem, null),
    };

    public static string ForProblem(PackageDetailsProblem problem) => problem switch
    {
        PackageDetailsProblem.NameMissing => ProblemNameMissing,
        PackageDetailsProblem.AuthorMissing => ProblemAuthorMissing,
        PackageDetailsProblem.IdNotValid => ProblemIdNotValid,
        PackageDetailsProblem.SourceUrlNotWeb => ProblemSourceUrl,
        _ => throw new ArgumentOutOfRangeException(nameof(problem), problem, null),
    };
}
