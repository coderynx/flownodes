using System.Text.Json;
using System.Text.RegularExpressions;

namespace Registry.Application.Resources;

internal static partial class Durations
{
    public const string Hint = "Must be a positive duration such as 500ms, 10s, 5m or 1h.";

    public static bool IsPositive(JsonElement duration) =>
        duration.ValueKind == JsonValueKind.String && Pattern().IsMatch(duration.GetString()!) &&
        duration.GetString()!.Any(c => c is >= '1' and <= '9');

    [GeneratedRegex(@"^\d+(ms|s|m|h)$")]
    private static partial Regex Pattern();
}
