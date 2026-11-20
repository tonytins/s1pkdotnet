namespace Sims1Pkg;

/// <summary>
/// Finds the game folder to install into.
/// </summary>
public static class GameLocator
{
    /// <summary>
    /// Environment variable that names the game folder, on any platform.
    /// </summary>
    public const string EnvironmentVariable = "TS1_GAME_DIR";

    /// <summary>
    /// Where the Complete Collection installs from CD on 64-bit Windows.
    /// </summary>
    public const string WindowsDefaultFolder = @"C:\Program Files (x86)\Maxis\The Sims Complete Collection";

    /// <summary>
    /// Finds the game folder: first <c>TS1_GAME_DIR</c>, then <paramref name="rememberedFolder"/>, then on Windows
    /// <see cref="WindowsDefaultFolder"/>. The first that <see cref="GameInstall.IsGameFolder"/> accepts wins.
    /// </summary>
    /// <param name="rememberedFolder">The folder the player chose last time, or null.</param>
    /// <returns>The game, or null when none was found and the player needs to pick one.</returns>
    public static GameInstall? Find(string? rememberedFolder) =>
        Find(
            Environment.GetEnvironmentVariable(EnvironmentVariable),
            rememberedFolder,
            OperatingSystem.IsWindows() ? WindowsDefaultFolder : null);

    internal static GameInstall? Find(params ReadOnlySpan<string?> candidates)
    {
        foreach (string? candidate in candidates)
        {
            if (GameInstall.IsGameFolder(candidate))
            {
                return new GameInstall(candidate!);
            }
        }

        return null;
    }
}
