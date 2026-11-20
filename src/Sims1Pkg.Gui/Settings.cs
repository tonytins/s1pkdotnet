namespace Sims1Pkg.Gui;

/// <summary>
/// What Sims1Pkg remembers between runs: the game folder the player chose, as one line of text.
/// </summary>
/// <param name="folder">Where the settings live.</param>
internal sealed class Settings(string folder)
{
    private const string GameFolderFile = "game-folder.txt";

    /// <summary>
    /// The settings in the user's application data folder, such as <c>~/.config/Sims1Pkg</c> on Linux.
    /// </summary>
    public static Settings ForUser() =>
        new(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Sims1Pkg"));

    /// <summary>
    /// Gets the game folder the player chose last, or null when there's none or it can't be read.
    /// </summary>
    public string? LoadGameFolder()
    {
        try
        {
            string path = Path.Combine(folder, GameFolderFile);
            string? line = File.Exists(path) ? File.ReadLines(path).FirstOrDefault()?.Trim() : null;
            return string.IsNullOrEmpty(line) ? null : line;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Remembers the game folder the player chose. A failure to save is ignored: the player is asked again next time.
    /// </summary>
    public void SaveGameFolder(string gameFolder)
    {
        try
        {
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, GameFolderFile), gameFolder + Environment.NewLine);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
