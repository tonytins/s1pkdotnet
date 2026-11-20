using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Sims1Pkg.Gui;

/// <summary>
/// A small modal popup in Sims1Pkg's look: a title, a message, an optional list of files, and buttons.
/// </summary>
internal sealed class Dialog : Window
{
    private string? _choice;

    private Dialog(string title, string message, IReadOnlyList<string> lines, IReadOnlyList<DialogButton> buttons)
    {
        Title = title;
        Width = 480;
        SizeToContent = SizeToContent.Height;
        MaxHeight = 560;
        CanResize = false;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = (IBrush)Application.Current!.FindResource("MainBrush")!;
        Foreground = Brushes.White;

        var body = new StackPanel { Spacing = 10, Margin = new Thickness(20) };
        body.Children.Add(new TextBlock { Text = title, Classes = { "title" }, FontSize = 18 });
        body.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });

        if (lines.Count > 0)
        {
            var list = new StackPanel { Spacing = 2 };
            foreach (string line in lines)
            {
                list.Children.Add(new TextBlock { Text = line, TextWrapping = TextWrapping.Wrap, FontSize = 13 });
            }

            body.Children.Add(new Border
            {
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(2),
                Padding = new Thickness(8),
                Child = new ScrollViewer { MaxHeight = 220, Content = list },
            });
        }

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        foreach (DialogButton button in buttons)
        {
            var control = new Button { Content = button.Text, IsDefault = button.IsDefault, IsCancel = button.IsCancel };
            control.Click += (_, _) =>
            {
                _choice = button.Id;
                Close();
            };
            row.Children.Add(control);
        }

        body.Children.Add(row);
        // A white border, as the Maxis-era tools framed their panels, keeps the popup distinct from the window under it.
        Content = new Border { BorderBrush = Brushes.White, BorderThickness = new Thickness(2), Child = body };
    }

    /// <summary>
    /// Shows a popup over <paramref name="owner"/> and waits for a button.
    /// </summary>
    /// <returns>The chosen button's id, or null when the popup was closed another way.</returns>
    public static async Task<string?> ShowAsync(
        Window owner,
        string title,
        string message,
        IReadOnlyList<string> lines,
        params DialogButton[] buttons)
    {
        var dialog = new Dialog(title, message, lines, buttons);
        await dialog.ShowDialog(owner);
        return dialog._choice;
    }
}
