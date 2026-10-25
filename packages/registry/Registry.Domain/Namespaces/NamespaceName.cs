using System.Diagnostics.CodeAnalysis;

namespace Registry.Domain.Namespaces;

/// <summary>
/// The name of a Namespace: a DNS label of lowercase letters, digits and hyphens, at most 63 characters, that starts
/// and ends with a letter or a digit.
/// </summary>
public sealed record NamespaceName
{
    /// <summary>
    /// The name of the Namespace every installation starts with.
    /// </summary>
    public const string Default = "default";

    private NamespaceName(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the name as written in a route or a manifest.
    /// </summary>
    public string Value { get; }
    /// <summary>
    /// Gets a value indicating whether this is the <c>default</c> Namespace, which cannot be deleted.
    /// </summary>
    public bool IsDefault => Value == Default;

    /// <summary>
    /// Validates untrusted input as a Namespace name.
    /// </summary>
    /// <param name="raw">The text to validate.</param>
    /// <param name="name">The name when <paramref name="raw"/> is valid; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="raw"/> is a valid Namespace name.</returns>
    public static bool TryCreate(string? raw, [NotNullWhen(true)] out NamespaceName? name)
    {
        var valid = DnsLabel.IsValid(raw);
        name = valid ? new NamespaceName(raw!) : null;

        return valid;
    }

    /// <summary>
    /// Builds a Namespace name from already trusted text, such as a database row.
    /// </summary>
    /// <param name="raw">The text to wrap.</param>
    /// <returns>The Namespace name.</returns>
    /// <exception cref="ArgumentException"><paramref name="raw"/> is not a valid Namespace name.</exception>
    public static NamespaceName Create(string raw) =>
        TryCreate(raw, out var name)
            ? name
            : throw new ArgumentException(
                $"'{raw}' is not a DNS label of at most {DnsLabel.MaxLength} characters.",
                nameof(raw));

    /// <inheritdoc/>
    public override string ToString() => Value;
}
