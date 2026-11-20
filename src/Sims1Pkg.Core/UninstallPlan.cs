namespace Sims1Pkg;

/// <summary>
/// What uninstalling a package will do, worked out without writing anything.
/// </summary>
/// <param name="Game">The game the package is installed in.</param>
/// <param name="Receipt">The package's receipt.</param>
/// <param name="Changed">Installed files that were changed since install. Uninstalling removes them too.</param>
/// <param name="Missing">Installed files that are gone already.</param>
public sealed record UninstallPlan(
    GameInstall Game,
    Receipt Receipt,
    IReadOnlyList<string> Changed,
    IReadOnlyList<string> Missing);
