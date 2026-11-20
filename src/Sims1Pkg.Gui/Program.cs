using Avalonia;
using Avalonia.Media;

namespace Sims1Pkg.Gui;

/// <summary>
/// Starts Sims1Pkg.
/// </summary>
public static class Program
{
    /// <summary>
    /// The version, from GitInfo, as major.minor.patch.
    /// </summary>
    public static string Version =>
        $"{ThisAssembly.Git.SemVer.Major}.{ThisAssembly.Git.SemVer.Minor}.{ThisAssembly.Git.SemVer.Patch}";

    /// <summary>
    /// Runs the app. The first argument, if any, is a package to open. <c>--version</c> prints the version and checks
    /// the app's own files are in place, with no window, which the packaging scripts use as a smoke test.
    /// </summary>
    [STAThread]
    public static int Main(string[] args) =>
        args is ["--version"] ? PrintVersion() : BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    private static int PrintVersion()
    {
        Console.WriteLine($"Sims1Pkg {Version} (format {PackageFormat.CurrentVersion})");

        string[] missing =
        [
            .. new[] { "uitext/english.dir/_100_sims1pkg.cst", "licenses/ComicNeue-OFL.txt" }
                .Where(file => !File.Exists(Path.Combine(AppContext.BaseDirectory, file))),
        ];

        if (!InterfaceText.Load().Has(TextKeys.AppTitle))
        {
            missing = [.. missing, "the interface text's app title"];
        }

        foreach (string file in missing)
        {
            Console.Error.WriteLine($"Sims1Pkg: {file} is missing.");
        }

        return missing.Length == 0 ? 0 : 1;
    }

    /// <summary>
    /// Comic Neue, bundled with the app, as the font for everything.
    /// </summary>
    public const string FontFamilyName = "avares://Sims1Pkg/Assets/Fonts#Comic Neue";

    /// <summary>
    /// Sims1Pkg's Avalonia app.
    /// </summary>
    /// <remarks>
    /// On X11, Avalonia leaves the window manager to draw the title bar unless it's asked to draw the frame itself,
    /// which the navy bar needs to be the title bar, as Roundhouse and Passenger ask. The option is marked experimental
    /// in Avalonia 12, so it's on only where <see cref="TitleBars.Merges()"/> would merge the bar.
    /// </remarks>
    public static AppBuilder BuildAvaloniaApp()
    {
#pragma warning disable AVALONIA_X11_CSD // Experimental; Roundhouse draws its merged title bar on X11 the same way.
        var x11 = new X11PlatformOptions { EnableDrawnDecorations = TitleBars.Merges() };
#pragma warning restore AVALONIA_X11_CSD
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(x11)
            .With(new FontManagerOptions { DefaultFamilyName = FontFamilyName })
            .LogToTrace();
    }
}
