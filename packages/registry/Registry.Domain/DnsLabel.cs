using System.Text.RegularExpressions;

namespace Registry.Domain;

internal static partial class DnsLabel
{
    public const int MaxLength = 63;
    public static bool IsValid(string? raw) => raw is { Length: > 0 and <= MaxLength } && Pattern().IsMatch(raw);

    [GeneratedRegex("^[a-z0-9]([a-z0-9-]*[a-z0-9])?$")]
    private static partial Regex Pattern();
}
