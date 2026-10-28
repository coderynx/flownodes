using System.Text.Json;

namespace Engine.Domain;

/// <summary>
/// A comparison of a value against a constant, as Alarm Conditions and Flows write it.
/// </summary>
/// <param name="Operator">One of <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&gt;=</c>, <c>==</c> and <c>!=</c>.</param>
/// <param name="Value">The constant to compare against, as JSON text.</param>
public sealed record Comparison(string Operator, string Value)
{
    /// <summary>
    /// Tells whether a value satisfies the comparison. A value of another JSON type than the constant is unequal to
    /// it and never ordered against it.
    /// </summary>
    /// <param name="reading">The value to compare.</param>
    /// <returns><see langword="true"/> when the comparison holds.</returns>
    public bool Holds(JsonElement reading)
    {
        using var threshold = JsonDocument.Parse(Value);
        var order = Compare(reading, threshold.RootElement);

        return Operator switch
        {
            "==" => order == 0,
            "!=" => order != 0,
            "<" => order < 0,
            "<=" => order <= 0,
            ">" => order > 0,
            _ => order >= 0
        };
    }

    private static int? Compare(JsonElement reading, JsonElement threshold) =>
        (reading.ValueKind, threshold.ValueKind) switch
        {
            (JsonValueKind.Number, JsonValueKind.Number) => reading.GetDouble().CompareTo(threshold.GetDouble()),
            (JsonValueKind.String, JsonValueKind.String) =>
                string.CompareOrdinal(reading.GetString(), threshold.GetString()),
            (JsonValueKind.True or JsonValueKind.False, JsonValueKind.True or JsonValueKind.False) =>
                reading.GetBoolean() == threshold.GetBoolean() ? 0 : null,
            _ => null
        };
}
