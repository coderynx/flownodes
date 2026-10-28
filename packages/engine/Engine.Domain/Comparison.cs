using System.Text.Json;

namespace Engine.Domain;

/// <summary>
/// A comparison of a value against a constant, as Alarm Conditions and Flows write it.
/// </summary>
public sealed record Comparison
{
    private static readonly string[] Operators = ["<", "<=", ">", ">=", "==", "!="];

    private Comparison(string op, string value)
    {
        Operator = op;
        Value = value;
    }

    /// <summary>
    /// Gets the operator: one of <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&gt;=</c>, <c>==</c> and <c>!=</c>.
    /// </summary>
    public string Operator { get; }
    /// <summary>
    /// Gets the constant to compare against, as JSON text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Builds a comparison from an operator and a constant that have already been validated.
    /// </summary>
    /// <param name="op">The operator.</param>
    /// <param name="value">The constant, as JSON text.</param>
    /// <returns>The comparison.</returns>
    /// <exception cref="ArgumentException">The operator is not one of the six.</exception>
    public static Comparison Create(string op, string value) =>
        Operators.Contains(op)
            ? new Comparison(op, value)
            : throw new ArgumentException($"'{op}' is not a comparison operator.", nameof(op));

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
            ">=" => order >= 0,
            _ => throw new InvalidOperationException($"'{Operator}' is not a comparison operator.")
        };
    }

    private static int? Compare(JsonElement reading, JsonElement threshold) =>
        (reading.ValueKind, threshold.ValueKind) switch
        {
            (JsonValueKind.Number, JsonValueKind.Number) => reading.GetDouble().CompareTo(threshold.GetDouble()),
            (JsonValueKind.String, JsonValueKind.String) => string.CompareOrdinal(
                reading.GetString(),
                threshold.GetString()),
            (JsonValueKind.True or JsonValueKind.False, JsonValueKind.True or JsonValueKind.False) =>
                reading.GetBoolean() == threshold.GetBoolean() ? 0 : null,
            _ => null
        };
}
