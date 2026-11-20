namespace Sims1Pkg;

/// <summary>
/// What installing a package will do, worked out without writing anything. Show it to the player, ask about
/// <see cref="Clashes"/> and updates, then pass it to <see cref="Installer.InstallAsync"/>.
/// </summary>
/// <param name="Package">The package. Keep it open until the install finishes.</param>
/// <param name="Game">The game it installs into.</param>
/// <param name="Id">The package's id.</param>
/// <param name="Kind">Whether this is a new install, an update, or a reinstall.</param>
/// <param name="Previous">The receipt of the installed version, for an update or reinstall.</param>
/// <param name="Items">Every file the install will write.</param>
public sealed record InstallPlan(
    Package Package,
    GameInstall Game,
    string Id,
    InstallKind Kind,
    Receipt? Previous,
    IReadOnlyList<InstallItem> Items)
{
    /// <summary>
    /// Files already in the game that the install would replace. Each is backed up first, so uninstalling puts it
    /// back.
    /// </summary>
    public IReadOnlyList<InstallItem> Clashes => [.. Items.Where(item => item.Clashes)];
}
