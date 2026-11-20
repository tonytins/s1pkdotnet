using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Sims1Pkg;

/// <summary>
/// Rules for package ids, which name a package across its versions and name its folders in the game.
/// </summary>
public static class PackageIds
{
    /// <summary>
    /// The longest id allowed, in characters.
    /// </summary>
    public const int MaxLength = 64;

    /// <summary>
    /// Checks whether <paramref name="id"/> is a valid package id: 1 to 64 characters from <c>A-Z</c>, <c>a-z</c>,
    /// <c>0-9</c>, <c>.</c>, <c>_</c>, and <c>-</c>, not starting or ending with <c>.</c>, and not a name Windows
    /// reserves.
    /// </summary>
    /// <param name="id">The id to check.</param>
    /// <returns>True when the id is valid.</returns>
    public static bool IsValid(string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length > MaxLength || id[0] == '.' || id[^1] == '.')
        {
            return false;
        }

        foreach (char character in id)
        {
            if (!IsAllowed(character))
            {
                return false;
            }
        }

        return !ArchivePath.IsReservedName(id.Split('.')[0]);
    }

    /// <summary>
    /// Gets the canonical form of a valid id. Ids that differ only in case are the same package, so the canonical form
    /// is lowercase.
    /// </summary>
    /// <param name="id">A valid id.</param>
    /// <returns>The id in lowercase.</returns>
    /// <exception cref="ArgumentException"><paramref name="id"/> is not a valid id.</exception>
    public static string Normalize(string id)
    {
        if (!IsValid(id))
        {
            throw new ArgumentException($"'{id}' is not a valid package id.", nameof(id));
        }

        return id.ToLowerInvariant();
    }

    /// <summary>
    /// Makes an id from a package's author and name, for packages that don't set one, such as
    /// <c>maya.retro-diner-set</c>. Accents are dropped, and other characters become dashes. When nothing usable is
    /// left, as with a name written only in Japanese, the id is made from a hash of the author and name instead.
    /// </summary>
    /// <param name="name">The package name.</param>
    /// <param name="author">The package author.</param>
    /// <returns>A valid, lowercase id.</returns>
    public static string FromNameAndAuthor(string name, string author)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(author);

        string authorPart = Slug(author);
        string namePart = Slug(name);
        string id = (authorPart, namePart) switch
        {
            ("", "") => string.Empty,
            ("", _) => namePart,
            (_, "") => authorPart,
            _ => authorPart + "." + namePart,
        };

        if (id.Length > MaxLength)
        {
            id = id[..MaxLength].TrimEnd('.', '-');
        }

        if (IsValid(id))
        {
            return id;
        }

        if (id.Length > 0 && IsValid(id + "-pkg"))
        {
            return id + "-pkg";
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(author + "\n" + name));
        return "pkg-" + Convert.ToHexStringLower(hash)[..12];
    }

    private static string Slug(string text)
    {
        var builder = new StringBuilder(text.Length);
        bool lastWasDash = true;

        foreach (char character in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            char lower = char.ToLowerInvariant(character);
            if (IsAllowed(lower) && lower != '.')
            {
                builder.Append(lower);
                lastWasDash = false;
            }
            else if (!lastWasDash)
            {
                builder.Append('-');
                lastWasDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static bool IsAllowed(char character) =>
        char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-';
}
