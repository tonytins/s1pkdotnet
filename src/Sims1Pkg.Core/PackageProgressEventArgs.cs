using System;

namespace Sims1Pkg;

/// <summary>
/// Event arguments for package operation progress reporting.
/// </summary>
public class PackageProgressEventArgs : EventArgs
{
    /// <summary>
    /// Current progress percentage (0-100).
    /// </summary>
    public int PercentComplete { get; }

    /// <summary>
    /// Human-readable progress message.
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Optional: The file currently being processed.
    /// </summary>
    public string? CurrentFile { get; }

    public PackageProgressEventArgs(int percentComplete, string message, string? currentFile = null)
    {
        PercentComplete = percentComplete;
        Message = message;
        CurrentFile = currentFile;
    }
}

/// <summary>
/// Delegate for package progress events.
/// </summary>
public delegate void PackageProgressEventHandler(object sender, PackageProgressEventArgs e);
