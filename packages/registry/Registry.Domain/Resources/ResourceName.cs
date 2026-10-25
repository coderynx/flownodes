using System.Diagnostics.CodeAnalysis;

namespace Registry.Domain.Resources;

/// <summary>
/// The name of a Resource, unique per Kind within its Namespace and never changed: a DNS label of lowercase letters,
/// digits and hyphens, at most 63 characters, that starts and ends with a letter or a digit.
/// </summary>
public sealed record ResourceName
{
    private ResourceName(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the name as written in a Manifest or a route.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates untrusted input as a Resource name.
    /// </summary>
    /// <param name="raw">The text to validate.</param>
    /// <param name="name">The name when <paramref name="raw"/> is valid; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="raw"/> is a valid Resource name.</returns>
    public static bool TryCreate(string? raw, [NotNullWhen(true)] out ResourceName? name)
    {
        var valid = DnsLabel.IsValid(raw);
        name = valid ? new ResourceName(raw!) : null;

        return valid;
    }

    /// <summary>
    /// Builds a Resource name from already trusted text, such as a database row.
    /// </summary>
    /// <param name="raw">The text to wrap.</param>
    /// <returns>The Resource name.</returns>
    /// <exception cref="ArgumentException"><paramref name="raw"/> is not a valid Resource name.</exception>
    public static ResourceName Create(string raw) =>
        TryCreate(raw, out var name)
            ? name
            : throw new ArgumentException(
                $"'{raw}' is not a DNS label of at most {DnsLabel.MaxLength} characters.",
                nameof(raw));

    /// <inheritdoc/>
    public override string ToString() => Value;
}
