namespace Sims1Pkg;

/// <summary>
/// Finds the receipts of installed packages in a game folder.
/// </summary>
public static class Receipts
{
    /// <summary>
    /// The folder in the game folder that holds one folder per installed package.
    /// </summary>
    public const string FolderName = "S1PK";

    /// <summary>
    /// The receipt's file name inside a package's folder.
    /// </summary>
    public const string FileName = "receipt.xml";

    /// <summary>
    /// Gets the folder that holds a package's receipt and backups: <c>S1PK/&lt;id&gt;</c>.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="id">The package's id.</param>
    /// <returns>A full path.</returns>
    public static string GetFolder(GameInstall game, string id)
    {
        ArgumentNullException.ThrowIfNull(game);
        return game.ResolvePath(FolderName + "/" + PackageIds.Normalize(id));
    }

    /// <summary>
    /// Reads the receipt of an installed package.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <param name="id">The package's id.</param>
    /// <returns>The receipt, or null when the package isn't installed.</returns>
    /// <exception cref="InstallException">The receipt is damaged.</exception>
    public static Receipt? Get(GameInstall game, string id)
    {
        string path = Path.Combine(GetFolder(game, id), FileName);
        if (!File.Exists(path))
        {
            return null;
        }

        using FileStream stream = File.OpenRead(path);
        return ReceiptXml.Read(stream);
    }

    /// <summary>
    /// Lists every installed package. A damaged receipt is reported in the listing rather than stopping it.
    /// </summary>
    /// <param name="game">The game.</param>
    /// <returns>The receipts, by name, and a message for each receipt that couldn't be read.</returns>
    public static ReceiptListing List(GameInstall game)
    {
        ArgumentNullException.ThrowIfNull(game);

        string root = game.ResolvePath(FolderName);
        var receipts = new List<Receipt>();
        var problems = new List<string>();

        if (!Directory.Exists(root))
        {
            return new ReceiptListing(receipts, problems);
        }

        foreach (string folder in Directory.EnumerateDirectories(root).Order(StringComparer.Ordinal))
        {
            string name = Path.GetFileName(folder);
            if (name.StartsWith('.'))
            {
                continue;
            }

            try
            {
                Receipt? receipt = PackageIds.IsValid(name) ? Get(game, name) : null;
                if (receipt is null)
                {
                    problems.Add($"{FolderName}/{name} has no install record.");
                }
                else if (receipt.Id != PackageIds.Normalize(name))
                {
                    problems.Add($"{FolderName}/{name} holds the record for '{receipt.Id}'.");
                }
                else
                {
                    receipts.Add(receipt);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                problems.Add($"{FolderName}/{name}: {exception.Message}");
            }
        }

        receipts.Sort((left, right) => StringComparer.CurrentCultureIgnoreCase.Compare(left.Name, right.Name));
        return new ReceiptListing(receipts, problems);
    }
}

