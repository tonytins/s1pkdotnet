namespace Sims1Pkg;

/// <summary>
/// The installed packages in a game folder.
/// </summary>
/// <param name="Receipts">The receipts that could be read, by name.</param>
/// <param name="Problems">A message for each package folder whose receipt couldn't be read.</param>
public sealed record ReceiptListing(IReadOnlyList<Receipt> Receipts, IReadOnlyList<string> Problems);
