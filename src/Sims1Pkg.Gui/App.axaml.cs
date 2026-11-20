using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Sims1Pkg.Gui;

/// <summary>
/// Sims1Pkg's application: its theme and its window.
/// </summary>
public sealed class App : Application
{
    /// <inheritdoc/>
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    /// <inheritdoc/>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow(desktop.Args ?? []);
            desktop.MainWindow = window;

            // macOS opens a document by sending an event rather than an argument, both at launch and while running.
            if (TryGetFeature(typeof(IActivatableLifetime)) is IActivatableLifetime activatable)
            {
                activatable.Activated += async (_, e) =>
                {
                    if (e is FileActivatedEventArgs { Files: [{ } file, ..] } && file.TryGetLocalPath() is { } path)
                    {
                        await window.OpenPackageAsync(path);
                    }
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
