using System.Globalization;

namespace Sims1Pkg.Gui;

/// <summary>
/// The packing form's fields as typed, and what's wrong with them.
/// </summary>
/// <param name="today">The date the form starts with.</param>
internal sealed class PackForm(DateOnly today)
{
    public string Name { get; set; } = "";

    public string Author { get; set; } = "";

    public string Date { get; set; } = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public string Id { get; set; } = "";

    public string Version { get; set; } = "";

    public string Description { get; set; } = "";

    public string License { get; set; } = "";

    public string SourceUrl { get; set; } = "";

    /// <summary>
    /// Gets the text keys of everything wrong with the fields, in the order the fields appear.
    /// </summary>
    public IReadOnlyList<string> GetProblems()
    {
        (PackageDetails details, bool dateOk, bool urlOk) = Build();
        IReadOnlyList<PackageDetailsProblem> detailProblems = details.GetProblems();
        var problems = new List<string>();

        if (detailProblems.Contains(PackageDetailsProblem.NameMissing))
        {
            problems.Add(TextKeys.ProblemNameMissing);
        }

        if (detailProblems.Contains(PackageDetailsProblem.AuthorMissing))
        {
            problems.Add(TextKeys.ProblemAuthorMissing);
        }

        if (!dateOk)
        {
            problems.Add(TextKeys.ProblemDate);
        }

        if (detailProblems.Contains(PackageDetailsProblem.IdNotValid))
        {
            problems.Add(TextKeys.ProblemIdNotValid);
        }

        if (!urlOk || detailProblems.Contains(PackageDetailsProblem.SourceUrlNotWeb))
        {
            problems.Add(TextKeys.ProblemSourceUrl);
        }

        return problems;
    }

    /// <summary>
    /// Gets the package details the fields describe, or null when something's wrong. Blank optional fields are left
    /// out, and every field is trimmed.
    /// </summary>
    public PackageDetails? ToDetails() => GetProblems().Count == 0 ? Build().Details : null;

    /// <summary>
    /// Gets the id the package would install under, or null when the id field isn't valid yet, or it's blank and the
    /// name or author is too.
    /// </summary>
    public string? GetEffectiveId()
    {
        PackageDetails details = Build().Details;
        bool idOk = details.Id is null ? details.Name.Length > 0 && details.Author.Length > 0 : PackageIds.IsValid(details.Id);
        return idOk ? details.GetEffectiveId() : null;
    }

    private (PackageDetails Details, bool DateOk, bool UrlOk) Build()
    {
        bool dateOk = DateOnly.TryParseExact(Date.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date);

        Uri? url = null;
        string urlText = SourceUrl.Trim();
        bool urlOk = urlText.Length == 0 || Uri.TryCreate(urlText, UriKind.Absolute, out url);

        var details = new PackageDetails
        {
            Name = Name.Trim(),
            Author = Author.Trim(),
            Date = date,
            Id = Optional(Id),
            Version = Optional(Version),
            Description = Optional(Description),
            License = Optional(License),
            SourceUrl = url,
        };

        return (details, dateOk, urlOk);
    }

    private static string? Optional(string text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
