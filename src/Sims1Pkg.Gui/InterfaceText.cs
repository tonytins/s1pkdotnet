using System.Globalization;
using CSTNet;

namespace Sims1Pkg.Gui;

/// <summary>
/// The interface text, from <c>.cst</c> tables. English is the base, and the player's language is laid over it, so a
/// string a translation lacks shows in English.
/// </summary>
internal sealed class InterfaceText
{
    /// <summary>
    /// The table that holds Sims1Pkg's text, <c>_100_sims1pkg.cst</c>.
    /// </summary>
    public const int TableId = 100;

    private const string BaseLanguage = "english";

    private readonly CSTLayers _layers;

    /// <summary>
    /// Reads the tables in <paramref name="uitextFolder"/> for <paramref name="culture"/>.
    /// </summary>
    /// <param name="uitextFolder">The folder holding one <c>&lt;language&gt;.dir</c> folder per language.</param>
    /// <param name="culture">The player's interface culture.</param>
    public InterfaceText(string uitextFolder, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(uitextFolder);
        ArgumentNullException.ThrowIfNull(culture);

        Language = GetLanguageName(culture);
        CSTDirectory english = Read(Path.Combine(uitextFolder, BaseLanguage + ".dir"));

        string translationFolder = Path.Combine(uitextFolder, Language + ".dir");
        _layers = Language != BaseLanguage && Directory.Exists(translationFolder)
            ? new CSTLayers(Read(translationFolder), english)
            : new CSTLayers(english);
    }

    /// <summary>
    /// The language asked for, such as <c>french</c>, whether or not a translation exists.
    /// </summary>
    public string Language { get; }

    /// <summary>
    /// Reads the text shipped beside the app, for the player's interface culture.
    /// </summary>
    public static InterfaceText Load() =>
        new(Path.Combine(AppContext.BaseDirectory, "uitext"), CultureInfo.CurrentUICulture);

    /// <summary>
    /// Gets a string, filling its placeholders, such as <c>%s</c>, with <paramref name="variables"/>. A string missing
    /// from every table shows its key, so the gap is visible rather than blank.
    /// </summary>
    /// <param name="key">A key from <see cref="TextKeys"/>.</param>
    /// <param name="variables">Values for the string's placeholders, in order.</param>
    /// <returns>The text.</returns>
    public string Get(string key, params object?[] variables)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!TryGet(key, out string? value))
        {
            return key;
        }

        return variables.Length == 0 ? value : CST.Format(value, variables);
    }

    /// <summary>
    /// Checks whether any table has a non-empty string for <paramref name="key"/>.
    /// </summary>
    public bool Has(string key) => TryGet(key, out _);

    /// <summary>
    /// Gets the folder name a culture's tables use: the language's English name in lowercase, such as <c>french</c>.
    /// </summary>
    internal static string GetLanguageName(CultureInfo culture)
    {
        if (string.IsNullOrEmpty(culture.Name))
        {
            return BaseLanguage;
        }

        CultureInfo language = culture.IsNeutralCulture ? culture : culture.Parent;
        return language.EnglishName.ToLowerInvariant();
    }

    private bool TryGet(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value) =>
        _layers.TryGetValue(TableId, key, out value, treatEmptyAsMissing: true);

    private static CSTDirectory Read(string folder) =>
        Directory.Exists(folder) ? CSTDirectory.Read(folder) : new CSTDirectory(new Dictionary<int, CSTTable>());
}
