namespace Sims1Pkg;

/// <summary>
/// Who made a package and what it's called: everything in a manifest except its list of files.
/// </summary>
public sealed record PackageDetails
{
    /// <summary>
    /// Names the package across its versions, so an installer can tell an update from a different package. When
    /// null, <see cref="GetEffectiveId"/> makes one from the author and name.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Name shown to players.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Who made the content.
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// Date the package was made.
    /// </summary>
    public required DateOnly Date { get; init; }

    /// <summary>
    /// The content's own version, chosen by its author. Not the format version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Longer description shown before installing.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// License the content is shared under.
    /// </summary>
    public string? License { get; init; }

    /// <summary>
    /// Where the content was published. Must be an absolute <c>http</c> or <c>https</c> address.
    /// </summary>
    public Uri? SourceUrl { get; init; }

    /// <summary>
    /// Gets the id the installer uses: <see cref="Id"/> in lowercase when it's set, otherwise one made from the author
    /// and name by <see cref="PackageIds.FromNameAndAuthor"/>.
    /// </summary>
    /// <returns>A valid, lowercase id.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Id"/> is set but isn't valid.</exception>
    public string GetEffectiveId()
    {
        if (Id is null)
        {
            return PackageIds.FromNameAndAuthor(Name, Author);
        }

        if (!PackageIds.IsValid(Id))
        {
            throw new InvalidOperationException($"'{Id}' is not a valid package id.");
        }

        return PackageIds.Normalize(Id);
    }

    /// <summary>
    /// Lists what's wrong with these details.
    /// </summary>
    /// <returns>An empty list when the details are valid.</returns>
    public IReadOnlyList<PackageDetailsProblem> GetProblems()
    {
        var problems = new List<PackageDetailsProblem>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            problems.Add(PackageDetailsProblem.NameMissing);
        }

        if (string.IsNullOrWhiteSpace(Author))
        {
            problems.Add(PackageDetailsProblem.AuthorMissing);
        }

        if (Id is not null && !PackageIds.IsValid(Id))
        {
            problems.Add(PackageDetailsProblem.IdNotValid);
        }

        if (SourceUrl is not null && !IsWebAddress(SourceUrl))
        {
            problems.Add(PackageDetailsProblem.SourceUrlNotWeb);
        }

        return problems;
    }

    internal static bool IsWebAddress(Uri url) =>
        url.IsAbsoluteUri && (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps);
}
