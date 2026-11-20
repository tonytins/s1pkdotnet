namespace Sims1Pkg;

/// <summary>
/// Something wrong with a package's details, as a value an app can show in its own words.
/// </summary>
public enum PackageDetailsProblem
{
    /// <summary>The name is empty.</summary>
    NameMissing,

    /// <summary>The author is empty.</summary>
    AuthorMissing,

    /// <summary>The id is set but isn't valid. See <see cref="PackageIds.IsValid"/>.</summary>
    IdNotValid,

    /// <summary>The source URL isn't an absolute <c>http</c> or <c>https</c> address.</summary>
    SourceUrlNotWeb,
}
