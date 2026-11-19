namespace Sims1Pkg;

/// <summary>
/// What a packaged file is, which decides where it gets installed.
/// </summary>
public enum ContentKind
{
    /// <summary>
    /// A buy or build mode object (<c>.iff</c> or <c>.far</c>), installed to <c>Downloads</c>.
    /// </summary>
    Object,

    /// <summary>
    /// A skin, mesh, or texture for a Sim's head or body, installed to <c>GameData/Skins</c>.
    /// </summary>
    Skin,

    /// <summary>
    /// A wallpaper (<c>.wll</c>), installed to <c>GameData/Walls</c>.
    /// </summary>
    Wall,

    /// <summary>
    /// A floor covering (<c>.flr</c>), installed to <c>GameData/Floors</c>.
    /// </summary>
    Floor,
}
