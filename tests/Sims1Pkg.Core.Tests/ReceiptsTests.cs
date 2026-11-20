using System.Text;

namespace Sims1Pkg.Tests;

public sealed class ReceiptsTests
{
    private static readonly Receipt Sample = new(
        "maya.diner",
        "Retro Diner Set",
        "Maya",
        "1.2",
        new DateTimeOffset(2026, 11, 19, 20, 30, 0, TimeSpan.Zero),
        [new InstalledFile("Downloads/maya.diner/Booth.iff", Samples.Sha256("booth"))],
        [new BackupFile("GameData/Walls/Tile.wll", "backups/GameData/Walls/Tile.wll")]);

    private static string Text(Receipt receipt)
    {
        using var stream = new MemoryStream();
        ReceiptXml.Write(receipt, stream);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static Receipt Read(string xml) => ReceiptXml.Read(new MemoryStream(Encoding.UTF8.GetBytes(xml)));

    [Fact]
    public void RoundTrips()
    {
        Receipt read = Read(Text(Sample));

        Assert.Equal(Sample with { Files = [], Backups = [] }, read with { Files = [], Backups = [] });
        Assert.Equal(Sample.Files, read.Files);
        Assert.Equal(Sample.Backups, read.Backups);
    }

    [Theory]
    [InlineData("formatVersion=\"1\"", "formatVersion=\"9\"")]
    [InlineData("<Id>maya.diner</Id>", "<Id>bad id</Id>")]
    [InlineData("<Id>maya.diner</Id>", "")]
    [InlineData("Downloads/maya.diner/Booth.iff", "../Booth.iff")]
    [InlineData("saved=\"backups/", "saved=\"elsewhere/")]
    [InlineData("<InstalledAt>2026", "<InstalledAt>yesterday")]
    public void RefusesDamagedReceipts(string original, string replacement)
    {
        string xml = Text(Sample);
        Assert.Contains(original, xml);

        Assert.Throws<InstallException>(() => Read(xml.Replace(original, replacement)));
    }

    [Fact]
    public void ListReportsDamagedReceiptsAndKeepsGoing()
    {
        using var game = new FakeGame();
        using (FileStream stream = File.Create(game.Write("S1PK/maya.diner/receipt.xml", "")))
        {
            ReceiptXml.Write(Sample, stream);
        }

        game.Write("S1PK/broken/receipt.xml", "<not a receipt");
        game.Write("S1PK/empty/readme.txt", "no receipt here");
        game.Write("S1PK/.staging-123/old/0", "ignored");

        ReceiptListing listing = Receipts.List(game.Game);

        Assert.Equal(["maya.diner"], listing.Receipts.Select(receipt => receipt.Id));
        Assert.Equal(2, listing.Problems.Count);
    }

    [Fact]
    public void ListIsEmptyWhenNothingIsInstalled()
    {
        using var game = new FakeGame();

        ReceiptListing listing = Receipts.List(game.Game);

        Assert.Empty(listing.Receipts);
        Assert.Empty(listing.Problems);
        Assert.Null(Receipts.Get(game.Game, "maya.diner"));
    }
}
