using System.Text.Json;
using System.Text.RegularExpressions;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal static partial class AlarmDeclarations
{
    private static readonly string[] Fields =
        ["name", "dataPoint", "operator", "value", "for", "deadband", "priority", "message"];

    private static readonly string[] Operators = ["<", "<=", ">", ">=", "==", "!="];
    private static readonly string[] Ordering = ["<", "<=", ">", ">="];
    private static readonly string[] Priorities = ["low", "medium", "high", "critical"];

    public static IEnumerable<SpecError> Validate(
        JsonElement spec,
        IReadOnlyList<Declaration> dataPoints,
        HashSet<string> inherited)
    {
        if (!spec.TryGetProperty("alarms", out var list))
        {
            yield break;
        }

        if (list.ValueKind != JsonValueKind.Array)
        {
            yield return new SpecError("spec.alarms", "Must be a list of Alarm Conditions.");

            yield break;
        }

        var seen = new HashSet<string>();
        var index = 0;
        foreach (var alarm in list.EnumerateArray())
        {
            var path = $"spec.alarms[{index++}]";
            if (alarm.ValueKind != JsonValueKind.Object)
            {
                yield return new SpecError(path, "Must be an object.");

                continue;
            }

            foreach (var error in ValidateOne(alarm, path, dataPoints, inherited, seen))
            {
                yield return error;
            }
        }
    }

    public static IEnumerable<string> Names(JsonElement spec) =>
        spec.TryGetProperty("alarms", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray()
                .Where(alarm => alarm.ValueKind == JsonValueKind.Object)
                .Select(alarm => Text(alarm, "name"))
                .OfType<string>()
            : [];

    private static IEnumerable<SpecError> ValidateOne(
        JsonElement alarm,
        string path,
        IReadOnlyList<Declaration> dataPoints,
        HashSet<string> inherited,
        HashSet<string> seen)
    {
        foreach (var field in alarm.EnumerateObject().Where(field => !Fields.Contains(field.Name)))
        {
            yield return new SpecError($"{path}.{field.Name}", "Unknown field.");
        }

        var name = Text(alarm, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            yield return new SpecError($"{path}.name", "Required, as the name of the Alarm Condition.");
        }
        else if (inherited.Contains(name))
        {
            yield return new SpecError(
                $"{path}.name",
                $"Alarm Condition '{name}' is already declared by the Asset Type, which an instance cannot redefine.");
        }
        else if (!seen.Add(name))
        {
            yield return new SpecError($"{path}.name", $"Alarm Condition '{name}' is declared twice.");
        }

        var dataPointName = Text(alarm, "dataPoint");
        var dataPoint = dataPoints.FirstOrDefault(candidate => candidate.Name == dataPointName);
        if (dataPoint is null)
        {
            yield return new SpecError(
                $"{path}.dataPoint",
                "Required, as the name of a Data Point this Asset declares.");
        }

        var op = Text(alarm, "operator");
        if (op is null || !Operators.Contains(op))
        {
            yield return new SpecError($"{path}.operator", $"Required, as one of {string.Join(", ", Operators)}.");
        }
        else if (Ordering.Contains(op) && dataPoint?.Type is "bool" or "string" or "enum")
        {
            yield return new SpecError(
                $"{path}.operator",
                $"Cannot order a {dataPoint.Type} Data Point; use == or !=.");
        }

        if (!alarm.TryGetProperty("value", out var value))
        {
            yield return new SpecError($"{path}.value", "Required.");
        }
        else if (dataPoint is not null && Mismatch(dataPoint, value) is { } problem)
        {
            yield return new SpecError($"{path}.value", problem);
        }

        if (alarm.TryGetProperty("for", out var duration) && !IsPositiveDuration(duration))
        {
            yield return new SpecError($"{path}.for", "Must be a positive duration such as 500ms, 10s, 5m or 1h.");
        }

        if (alarm.TryGetProperty("deadband", out var deadband))
        {
            if (op is "==" or "!=")
            {
                yield return new SpecError($"{path}.deadband", "Only <, <=, > and >= take a deadband.");
            }
            else if (deadband.ValueKind != JsonValueKind.Number || deadband.GetDouble() < 0)
            {
                yield return new SpecError($"{path}.deadband", "Must be a number that is not negative.");
            }
        }

        var priority = Text(alarm, "priority");
        if (priority is null || !Priorities.Contains(priority))
        {
            yield return new SpecError($"{path}.priority", $"Required, as one of {string.Join(", ", Priorities)}.");
        }

        if (string.IsNullOrWhiteSpace(Text(alarm, "message")))
        {
            yield return new SpecError($"{path}.message", "Required, as the text an Operator sees.");
        }
    }

    private static bool IsPositiveDuration(JsonElement duration) =>
        duration.ValueKind == JsonValueKind.String && Duration().IsMatch(duration.GetString()!) &&
        duration.GetString()!.Any(c => c is >= '1' and <= '9');

    private static string? Mismatch(Declaration dataPoint, JsonElement value) =>
        dataPoint.Type switch
        {
            "bool" when value.ValueKind is not (JsonValueKind.True or JsonValueKind.False) => "Must be true or false.",
            "integer" when value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out _) =>
                "Must be an integer.",
            "number" when value.ValueKind != JsonValueKind.Number => "Must be a number.",
            "string" when value.ValueKind != JsonValueKind.String => "Must be text.",
            "enum" when value.ValueKind != JsonValueKind.String || !dataPoint.Values.Contains(value.GetString()!) =>
                $"Must be one of {string.Join(", ", dataPoint.Values)}.",
            _ => null
        };

    private static string? Text(JsonElement spec, string name) =>
        spec.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    [GeneratedRegex(@"^\d+(ms|s|m|h)$")]
    private static partial Regex Duration();
}
