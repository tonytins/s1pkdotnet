using System.Globalization;
using System.Reflection;

namespace Sims1Pkg.Gui.Tests;

public sealed class InterfaceTextTests
{
    private static string ShippedFolder => Path.Combine(AppContext.BaseDirectory, "uitext");

    public static TheoryData<string> AllKeys => new(
        typeof(TextKeys).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(constant => constant.IsLiteral)
            .Select(constant => (string)constant.GetRawConstantValue()!));

    [Theory]
    [MemberData(nameof(AllKeys))]
    public void EnglishHasEveryKey(string key) =>
        Assert.True(new InterfaceText(ShippedFolder, CultureInfo.InvariantCulture).Has(key), $"English lacks '{key}'.");

    [Fact]
    public void EnglishTableReadsWithoutProblems()
    {
        var directory = CSTNet.CSTDirectory.Read(Path.Combine(ShippedFolder, "english.dir"));

        Assert.Empty(directory.Problems);
        Assert.True(directory.TryGetTable(InterfaceText.TableId, out CSTNet.CSTTable? table));
        Assert.Empty(table.Problems);
    }

    [Theory]
    [InlineData("fr-FR", "french")]
    [InlineData("fr", "french")]
    [InlineData("en-US", "english")]
    [InlineData("pt-BR", "portuguese")]
    [InlineData("", "english")]
    public void NamesLanguagesAsTheirFoldersDo(string culture, string expected) =>
        Assert.Equal(expected, InterfaceText.GetLanguageName(new CultureInfo(culture)));

    [Fact]
    public void TranslationsFallBackToEnglish()
    {
        using var folder = new TempTables();
        folder.Write("english", "greeting ^Hello, %s!^\nfarewell ^Goodbye^\nempty ^Not empty in English^");
        folder.Write("french", "greeting ^Bonjour, %s !^\nempty ^^");

        var text = new InterfaceText(folder.Path, new CultureInfo("fr-FR"));

        Assert.Equal("Bonjour, Maya !", text.Get("greeting", "Maya"));
        Assert.Equal("Goodbye", text.Get("farewell"));
        Assert.Equal("Not empty in English", text.Get("empty"));
    }

    [Fact]
    public void AMissingStringShowsItsKey()
    {
        using var folder = new TempTables();
        folder.Write("english", "greeting ^Hello^");

        var text = new InterfaceText(folder.Path, CultureInfo.InvariantCulture);

        Assert.Equal("no such key", text.Get("no such key"));
        Assert.False(text.Has("no such key"));
    }

    [Fact]
    public void WorksWithNoTablesAtAll()
    {
        var text = new InterfaceText(Path.Combine(Path.GetTempPath(), "no-such-uitext"), new CultureInfo("de-DE"));

        Assert.Equal("app title", text.Get("app title"));
        Assert.Equal("german", text.Language);
    }

    private sealed class TempTables : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "s1pk-uitext-" + Guid.NewGuid().ToString("N"));

        public void Write(string language, string table)
        {
            string folder = System.IO.Path.Combine(Path, language + ".dir");
            Directory.CreateDirectory(folder);
            File.WriteAllText(System.IO.Path.Combine(folder, "_100_sims1pkg.cst"), table);
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
