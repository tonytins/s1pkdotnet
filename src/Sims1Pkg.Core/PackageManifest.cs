using System.Text.RegularExpressions;

namespace Sims1Pkg;

/// <summary>
/// The contents of a package's <c>manifest.xml</c>: who made it and what it holds.
/// </summary>
/// <param name="Details">Who made the package and what it's called.</param>
/// <param name="Files">Every content file in the package. The manifest itself is not listed.</param>
public sealed partial record PackageManifest(PackageDetails Details, IReadOnlyList<PackageFile> Files)
{
    /// <summary>
    /// Lists what's wrong with this manifest, in English, for error messages.
    /// </summary>
    /// <returns>An empty list when the manifest is valid.</returns>
    public IReadOnlyList<string> GetProblems()
    {
        var problems = new List<string>();

        foreach (PackageDetailsProblem problem in Details.GetProblems())
        {
            problems.Add(problem switch
            {
                PackageDetailsProblem.NameMissing => "The package has no name.",
                PackageDetailsProblem.AuthorMissing => "The package has no author.",
                PackageDetailsProblem.IdNotValid => $"'{Details.Id}' is not a valid package id.",
                PackageDetailsProblem.SourceUrlNotWeb => $"'{Details.SourceUrl}' is not a web address.",
                _ => problem.ToString(),
            });
        }

        if (Files.Count == 0)
        {
            problems.Add("The package lists no files.");
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (PackageFile file in Files)
        {
            if (ArchivePath.GetProblem(file.Path) is { } pathProblem)
            {
                problems.Add($"'{file.Path}': {pathProblem}");
            }
            else if (file.Path.Equals(PackageFormat.ManifestFileName, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"'{file.Path}' can't be listed as a content file.");
            }
            else if (!seen.Add(file.Path))
            {
                problems.Add($"'{file.Path}' is listed more than once, ignoring case.");
            }

            if (string.IsNullOrWhiteSpace(file.KindName))
            {
                problems.Add($"'{file.Path}' has no kind.");
            }

            if (file.Length < 0)
            {
                problems.Add($"'{file.Path}' has a negative size.");
            }

            if (!Sha256Pattern().IsMatch(file.Sha256))
            {
                problems.Add($"'{file.Path}' has a SHA-256 that isn't 64 lowercase hex digits.");
            }
        }

        return problems;
    }

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Sha256Pattern();
}
