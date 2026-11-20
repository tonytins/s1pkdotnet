namespace Sims1Pkg;

/// <summary>
/// What an install wrote to the game folder, kept at <c>S1PK/&lt;id&gt;/receipt.xml</c> so uninstalling can remove
/// exactly that.
/// </summary>
/// <param name="Id">The package's id.</param>
/// <param name="Name">The package's name, for showing.</param>
/// <param name="Author">The package's author, for showing.</param>
/// <param name="Version">The package's version, or null when it has none.</param>
/// <param name="InstalledAt">When it was installed.</param>
/// <param name="Files">Every file written, relative to the game folder.</param>
/// <param name="Backups">Every file the install replaced, saved in the receipt's folder.</param>
public sealed record Receipt(
    string Id,
    string Name,
    string Author,
    string? Version,
    DateTimeOffset InstalledAt,
    IReadOnlyList<InstalledFile> Files,
    IReadOnlyList<BackupFile> Backups);
