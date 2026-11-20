namespace Sims1Pkg;

/// <summary>
/// Sims1Pkg can't write to the game folder. On Windows this usually means the game is under <c>Program Files</c> and
/// the installer needs administrator rights.
/// </summary>
/// <param name="folder">The folder that couldn't be written to.</param>
/// <param name="innerException">The error writing to it.</param>
public sealed class GameFolderNotWritableException(string folder, Exception innerException)
    : InstallException($"Sims1Pkg can't write to {folder}.", innerException)
{
    /// <summary>
    /// The folder that couldn't be written to.
    /// </summary>
    public string Folder { get; } = folder;
}
