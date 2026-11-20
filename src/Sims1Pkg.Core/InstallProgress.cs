namespace Sims1Pkg;

/// <summary>
/// How far an install has got.
/// </summary>
/// <param name="Done">Files written so far.</param>
/// <param name="Total">Files to write in all.</param>
/// <param name="Destination">The file just written, relative to the game folder.</param>
public sealed record InstallProgress(int Done, int Total, string Destination);
