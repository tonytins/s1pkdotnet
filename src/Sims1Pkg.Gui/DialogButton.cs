namespace Sims1Pkg.Gui;

/// <summary>
/// A button on a <see cref="Dialog"/>.
/// </summary>
/// <param name="Id">What <see cref="Dialog.ShowAsync"/> returns when it's chosen.</param>
/// <param name="Text">Its label.</param>
/// <param name="IsDefault">Chosen by Enter.</param>
/// <param name="IsCancel">Chosen by Escape.</param>
internal sealed record DialogButton(string Id, string Text, bool IsDefault = false, bool IsCancel = false);
