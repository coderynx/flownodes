using System.Text.Json;
using Registry.Domain.Resources;

namespace Registry.Application.Resources;

internal static class DataPointDeclarations
{
    private static readonly string[] Types = ["bool", "integer", "number", "string", "enum"];
    private static readonly string[] Fields = ["name", "type", "writable", "unit", "min", "max", "values", "source"];

    public static IEnumerable<SpecError> Validate(JsonElement spec)
    {
        if (!spec.TryGetProperty("dataPoints", out var list))
        {
            yield break;
        }

        if (list.ValueKind != JsonValueKind.Array)
        {
            yield return new SpecError("spec.dataPoints", "Must be a list of Data Points.");

            yield break;
        }

        var seen = new HashSet<string>();
        var index = 0;
        foreach (var dataPoint in list.EnumerateArray())
        {
            var path = $"spec.dataPoints[{index++}]";
            if (dataPoint.ValueKind != JsonValueKind.Object)
            {
                yield return new SpecError(path, "Must be an object.");

                continue;
            }

            foreach (var error in ValidateOne(dataPoint, path, seen))
            {
                yield return error;
            }
        }
    }

    public static IEnumerable<string> Names(JsonElement spec) =>
        spec.TryGetProperty("dataPoints", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray()
                .Where(dataPoint => dataPoint.ValueKind == JsonValueKind.Object)
                .Select(dataPoint => Text(dataPoint, "name"))
                .OfType<string>()
            : [];

    public static IReadOnlyList<Declaration> Declarations(JsonElement spec)
    {
        if (!spec.TryGetProperty("dataPoints", out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var declarations = new List<Declaration>();
        var index = 0;
        foreach (var dataPoint in list.EnumerateArray())
        {
            var path = $"spec.dataPoints[{index++}]";
            if (dataPoint.ValueKind == JsonValueKind.Object && Text(dataPoint, "name") is { } name)
            {
                declarations.Add(
                    new Declaration(
                        name,
                        Text(dataPoint, "type"),
                        dataPoint.TryGetProperty("writable", out var writable) &&
                        writable.ValueKind == JsonValueKind.True,
                        dataPoint.TryGetProperty("source", out var source) ? source : null,
                        path));
            }
        }

        return declarations;
    }

    private static IEnumerable<SpecError> ValidateOne(JsonElement dataPoint, string path, HashSet<string> seen)
    {
        foreach (var field in dataPoint.EnumerateObject().Where(field => !Fields.Contains(field.Name)))
        {
            yield return new SpecError($"{path}.{field.Name}", "Unknown field.");
        }

        var name = Text(dataPoint, "name");
        if (!ResourceName.TryCreate(name, out _))
        {
            yield return new SpecError(
                $"{path}.name",
                "Required, as a DNS label of lowercase letters, digits and hyphens.");
        }
        else if (!seen.Add(name!))
        {
            yield return new SpecError($"{path}.name", $"Data Point '{name}' is declared twice.");
        }

        var type = Text(dataPoint, "type");
        if (type is null || !Types.Contains(type))
        {
            yield return new SpecError($"{path}.type", $"Required, as one of {string.Join(", ", Types)}.");
        }

        if (dataPoint.TryGetProperty("writable", out var writable) &&
            writable.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            yield return new SpecError($"{path}.writable", "Must be true or false.");
        }

        if (dataPoint.TryGetProperty("unit", out var unit) && unit.ValueKind != JsonValueKind.String)
        {
            yield return new SpecError($"{path}.unit", "Must be text.");
        }

        var numeric = type is "integer" or "number";
        foreach (var bound in new[] { "min", "max" }.Where(bound => dataPoint.TryGetProperty(bound, out _)))
        {
            if (!numeric)
            {
                yield return new SpecError($"{path}.{bound}", "Only an integer or number Data Point has bounds.");
            }
            else if (dataPoint.GetProperty(bound).ValueKind != JsonValueKind.Number)
            {
                yield return new SpecError($"{path}.{bound}", "Must be a number.");
            }
        }

        if (numeric && BoundsCross(dataPoint))
        {
            yield return new SpecError($"{path}.max", "Must not be smaller than min.");
        }

        foreach (var error in ValidateValues(dataPoint, path, type))
        {
            yield return error;
        }
    }

    private static bool BoundsCross(JsonElement dataPoint) =>
        dataPoint.TryGetProperty("min", out var min) && dataPoint.TryGetProperty("max", out var max) &&
        min.ValueKind == JsonValueKind.Number && max.ValueKind == JsonValueKind.Number &&
        min.GetDouble() > max.GetDouble();

    private static IEnumerable<SpecError> ValidateValues(JsonElement dataPoint, string path, string? type)
    {
        var hasValues = dataPoint.TryGetProperty("values", out var values);
        if (type == "enum")
        {
            var valid = hasValues && values.ValueKind == JsonValueKind.Array && values.GetArrayLength() > 0 &&
                        values.EnumerateArray().All(value => value.ValueKind == JsonValueKind.String);
            if (!valid)
            {
                yield return new SpecError($"{path}.values", "Required, as a list of the texts the Data Point allows.");
            }
        }
        else if (hasValues)
        {
            yield return new SpecError($"{path}.values", "Only an enum Data Point has values.");
        }
    }

    private static string? Text(JsonElement spec, string name) =>
        spec.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

internal sealed record Declaration(string Name, string? Type, bool Writable, JsonElement? Source, string Path);
