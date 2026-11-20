using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Sims1Pkg;

/// <summary>
/// Reads and writes <c>receipt.xml</c>.
/// </summary>
internal static partial class ReceiptXml
{
    public const int CurrentVersion = 1;
    private const string RootName = "S1Receipt";

    private static readonly XmlFieldReader Fields =
        new(Receipts.FileName, (message, inner) => new InstallException("The install record is damaged: " + message, inner));

    public static Receipt Read(Stream stream)
    {
        XElement root = Fields.Load(stream, RootName);
        Fields.CheckFormatVersion(root, CurrentVersion);

        string installedText = Fields.Required(root, "InstalledAt");
        if (!DateTimeOffset.TryParseExact(installedText, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset installedAt))
        {
            throw Damaged($"InstalledAt '{installedText}' isn't a date and time.");
        }

        string id = Fields.Required(root, "Id");
        if (!PackageIds.IsValid(id))
        {
            throw Damaged($"'{id}' isn't a valid package id.");
        }

        var files = new List<InstalledFile>();
        foreach (XElement file in Fields.Single(root, "Files")?.Elements("File") ?? [])
        {
            string path = CheckedPath(Fields.RequiredAttribute(file, "path", "A <File>"));
            string sha256 = Fields.RequiredAttribute(file, "sha256", $"<File path=\"{path}\">");
            if (!Sha256Pattern().IsMatch(sha256))
            {
                throw Damaged($"'{path}' has a SHA-256 that isn't 64 lowercase hex digits.");
            }

            files.Add(new InstalledFile(path, sha256));
        }

        var backups = new List<BackupFile>();
        foreach (XElement backup in Fields.Single(root, "Backups")?.Elements("Backup") ?? [])
        {
            string original = CheckedPath(Fields.RequiredAttribute(backup, "original", "A <Backup>"));
            string saved = CheckedPath(Fields.RequiredAttribute(backup, "saved", $"<Backup original=\"{original}\">"));
            if (!saved.StartsWith("backups/", StringComparison.Ordinal))
            {
                throw Damaged($"The backup of '{original}' isn't in the backups folder.");
            }

            backups.Add(new BackupFile(original, saved));
        }

        return new Receipt(
            PackageIds.Normalize(id),
            Fields.Required(root, "Name"),
            Fields.Required(root, "Author"),
            Fields.Optional(root, "Version"),
            installedAt,
            files,
            backups);
    }

    public static void Write(Receipt receipt, Stream stream)
    {
        var root = new XElement(
            RootName,
            new XAttribute("formatVersion", CurrentVersion.ToString(CultureInfo.InvariantCulture)),
            new XElement("Id", receipt.Id),
            new XElement("Name", receipt.Name),
            new XElement("Author", receipt.Author),
            XmlFieldReader.OptionalElement("Version", receipt.Version),
            new XElement("InstalledAt", receipt.InstalledAt.ToString("O", CultureInfo.InvariantCulture)),
            new XElement("Files", receipt.Files.Select(file => new XElement(
                "File",
                new XAttribute("path", file.Path),
                new XAttribute("sha256", file.Sha256)))),
            new XElement("Backups", receipt.Backups.Select(backup => new XElement(
                "Backup",
                new XAttribute("original", backup.OriginalPath),
                new XAttribute("saved", backup.SavedPath)))));

        XmlFieldReader.Save(root, stream);
    }

    private static string CheckedPath(string path) =>
        ArchivePath.GetProblem(path) is { } problem ? throw Damaged($"'{path}': {problem}") : path;

    private static InstallException Damaged(string message) => new("The install record is damaged: " + message);

    [GeneratedRegex("^[0-9a-f]{64}$")]
    private static partial Regex Sha256Pattern();
}
