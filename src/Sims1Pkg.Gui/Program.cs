using Avalonia;

namespace Sims1Pkg.Gui;

/// <summary>
/// Starts Sims1Pkg.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the app. The first argument, if any, is a package to open.
    /// </summary>
    [STAThread]
    public static int Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Sims1Pkg's Avalonia app.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
