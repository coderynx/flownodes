using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Identity.Domain.Users;

/// <summary>
/// The name a User signs in with: 1 to 64 letters, digits, dots, underscores, hyphens or at signs. Two usernames that
/// differ only in case are the same username.
/// </summary>
public sealed partial record Username
{
    private const int MaxLength = 64;

    private Username(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the username as its User wrote it.
    /// </summary>
    public string Value { get; }
    /// <summary>
    /// Gets the form two usernames are compared by: upper-case, so a case-only difference is no difference.
    /// </summary>
    public string Normalized => Value.ToUpperInvariant();

    /// <summary>
    /// Validates untrusted input as a username.
    /// </summary>
    /// <param name="raw">The text to validate.</param>
    /// <param name="username">
    /// The username when <paramref name="raw"/> is valid; otherwise <see langword="null"/>.
    /// </param>
    /// <returns><see langword="true"/> when <paramref name="raw"/> is a valid username.</returns>
    public static bool TryCreate(string? raw, [NotNullWhen(true)] out Username? username)
    {
        var valid = raw is { Length: > 0 and <= MaxLength } && Allowed().IsMatch(raw);
        username = valid ? new Username(raw!) : null;

        return valid;
    }

    /// <summary>
    /// Builds a username from already trusted text, such as a database row.
    /// </summary>
    /// <param name="raw">The text to wrap.</param>
    /// <returns>The username.</returns>
    /// <exception cref="ArgumentException"><paramref name="raw"/> is not a valid username.</exception>
    public static Username Create(string raw) =>
        TryCreate(raw, out var username)
            ? username
            : throw new ArgumentException($"'{raw}' is not a valid username.", nameof(raw));

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Za-z0-9._@-]+\z")]
    private static partial Regex Allowed();
}
