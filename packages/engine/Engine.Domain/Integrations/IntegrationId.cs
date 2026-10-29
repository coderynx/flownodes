using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Engine.Domain.Integrations;

/// <summary>
/// The vendor-qualified id of an Integration, such as <c>flownodes.mqtt</c>: at least two dot-separated segments of
/// lowercase letters, digits and inner hyphens.
/// </summary>
public sealed partial record IntegrationId
{
    private IntegrationId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Gets the id as the Integration declares it.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Validates untrusted input as an Integration id.
    /// </summary>
    /// <param name="raw">The text to validate.</param>
    /// <param name="id">The id when <paramref name="raw"/> is valid; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when <paramref name="raw"/> is a valid Integration id.</returns>
    public static bool TryCreate(string? raw, [NotNullWhen(true)] out IntegrationId? id)
    {
        var valid = raw is not null && Pattern().IsMatch(raw);
        id = valid ? new IntegrationId(raw!) : null;

        return valid;
    }

    /// <inheritdoc/>
    public override string ToString() => Value;

    [GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*(\.[a-z0-9]+(-[a-z0-9]+)*)+\z")]
    private static partial Regex Pattern();
}
