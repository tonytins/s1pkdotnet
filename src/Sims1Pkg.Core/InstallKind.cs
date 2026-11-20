namespace Sims1Pkg;

/// <summary>
/// Whether an install adds a new package or replaces an installed version of the same one.
/// </summary>
public enum InstallKind
{
    /// <summary>No package with this id is installed.</summary>
    Install,

    /// <summary>Another version of this package is installed and will be replaced.</summary>
    Update,

    /// <summary>The same version of this package is installed and will be installed again.</summary>
    Reinstall,
}
