namespace Sims1Pkg.Gui.Tests;

public sealed class TitleBarsTests
{
    [Theory]
    [InlineData("GNOME", null, true)]
    [InlineData("ubuntu:GNOME", null, true)]
    [InlineData("KDE", null, true)]
    [InlineData("sway", null, false)]
    [InlineData("Hyprland", null, false)]
    [InlineData("GNOME", "Ubuntu", false)]
    [InlineData(null, null, false)]
    [InlineData("", null, false)]
    public void MergesOnlyOnDesktopsThatDrawFrames(string? desktop, string? wsl, bool expected) =>
        Assert.Equal(expected, TitleBars.Merges(desktop, wsl));
}
