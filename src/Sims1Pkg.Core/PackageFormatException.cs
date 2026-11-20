namespace Sims1Pkg;

/// <summary>
/// A package, or the manifest inside it, breaks the S1PK format.
/// </summary>
public sealed class PackageFormatException : IOException
{
    /// <summary>
    /// Creates the exception with a message that says what is wrong.
    /// </summary>
    /// <param name="message">What is wrong, written for the person who made the package.</param>
    public PackageFormatException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Creates the exception with a message and the error that caused it.
    /// </summary>
    /// <param name="message">What is wrong, written for the person who made the package.</param>
    /// <param name="innerException">The error that revealed the problem.</param>
    public PackageFormatException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
