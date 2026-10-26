using System.Text.Json;

namespace Engine.Infrastructure.DataPoints;

internal static class ValueCheck
{
    public static string? Problem(
        string type,
        double? min,
        double? max,
        IReadOnlyList<string>? values,
        JsonElement json)
    {
        var fits = type switch
        {
            "bool" => json.ValueKind is JsonValueKind.True or JsonValueKind.False,
            "integer" => json.ValueKind == JsonValueKind.Number && json.TryGetInt64(out _),
            "number" => json.ValueKind == JsonValueKind.Number,
            "string" => json.ValueKind == JsonValueKind.String,
            _ => json.ValueKind == JsonValueKind.String && values?.Contains(json.GetString()!) == true
        };
        if (!fits)
        {
            return $"Must be a {type}.";
        }

        if (json.ValueKind == JsonValueKind.Number)
        {
            var number = json.GetDouble();
            if (number < min || number > max)
            {
                return $"Must be between {min} and {max}.";
            }
        }

        return null;
    }
}
