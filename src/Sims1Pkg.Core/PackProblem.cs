namespace Sims1Pkg;

/// <summary>
/// Why a file in a source folder can't be packed, as a value an app can show in its own words.
/// </summary>
public enum PackProblem
{
    /// <summary>Neither the folder it's in nor its extension says what kind of content it is.</summary>
    NoKind,

    /// <summary>Its path has a name Windows would refuse or change. See <see cref="ArchivePath.IsValid"/>.</summary>
    PathNotValid,

    /// <summary>Another file would install to the same place, or has the same path when case is ignored.</summary>
    NameClash,
}
