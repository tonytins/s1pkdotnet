namespace Sims1Pkg;

/// <summary>
/// What packing a folder would do, worked out without writing anything.
/// </summary>
/// <param name="Items">Every file that would be packed, in path order, with any problems.</param>
/// <param name="LeftOut">Paths of files packing leaves out, relative to the source folder. See <see cref="KindRules.IsLeftOut"/>.</param>
public sealed record PackPreview(IReadOnlyList<PackItem> Items, IReadOnlyList<string> LeftOut)
{
    /// <summary>
    /// True when there's at least one file to pack and none has a problem.
    /// </summary>
    public bool IsReady => Items.Count > 0 && Items.All(item => item.Problem is null);
}
