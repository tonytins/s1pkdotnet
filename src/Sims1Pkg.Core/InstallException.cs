namespace Sims1Pkg;

/// <summary>
/// An install or uninstall can't go ahead, or failed and couldn't put everything back.
/// </summary>
public class InstallException : IOException
{
    /// <summary>
    /// Creates the exception with a message that says what is wrong.
    /// </summary>
    /// <param name="message">What is wrong, written for the player.</param>
    public InstallException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates the exception with a message and the error that caused it.
    /// </summary>
    /// <param name="message">What is wrong, written for the player.</param>
    /// <param name="innerException">The error that caused it.</param>
    public InstallException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
