using System.Text.Json;

namespace Engine.Domain.Alarms;

/// <summary>
/// A declarative rule on one of an Asset's Data Points that raises an Alarm while it holds.
/// </summary>
/// <param name="Name">The name of the Alarm Condition, unique on its Asset.</param>
/// <param name="DataPoint">The name of the Asset Data Point it watches.</param>
/// <param name="Operator">One of <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&gt;=</c>, <c>==</c> and <c>!=</c>.</param>
/// <param name="Value">The constant to compare against, as JSON text.</param>
/// <param name="For">How long the comparison must hold before it raises an Alarm; none to raise at once.</param>
/// <param name="Deadband">The margin past the threshold the value must reach to clear; none for no margin.</param>
/// <param name="Priority">The Alarm Priority of the Alarms it raises.</param>
/// <param name="Message">The static text an Operator sees.</param>
public sealed record AlarmCondition(
    string Name,
    string DataPoint,
    string Operator,
    string Value,
    TimeSpan? For,
    double? Deadband,
    AlarmPriority Priority,
    string Message)
{
    /// <summary>
    /// Gets what makes this Alarm Condition raise differently from another: everything but its Priority and message.
    /// </summary>
    public string Trigger => $"{DataPoint}|{Operator}|{Value}|{For}|{Deadband}";

    /// <summary>
    /// Builds an Alarm Condition from a declaration the Registry has already validated.
    /// </summary>
    /// <param name="declaration">The declaration, as it appears in an <c>alarms</c> list.</param>
    /// <returns>The Alarm Condition.</returns>
    public static AlarmCondition Create(JsonElement declaration) =>
        new(
            declaration.GetProperty("name").GetString()!,
            declaration.GetProperty("dataPoint").GetString()!,
            declaration.GetProperty("operator").GetString()!,
            declaration.GetProperty("value").GetRawText(),
            declaration.TryGetProperty("for", out var duration) ? Duration(duration.GetString()!) : null,
            declaration.TryGetProperty("deadband", out var deadband) ? deadband.GetDouble() : null,
            Enum.Parse<AlarmPriority>(declaration.GetProperty("priority").GetString()!, true),
            declaration.GetProperty("message").GetString()!);

    /// <summary>
    /// Tells whether a value satisfies the comparison.
    /// </summary>
    /// <param name="reading">The value of the Data Point.</param>
    /// <returns><see langword="true"/> when the comparison holds.</returns>
    public bool Holds(JsonElement reading)
    {
        using var threshold = JsonDocument.Parse(Value);

        return Operator switch
        {
            "==" => Compare(reading, threshold.RootElement) == 0,
            "!=" => Compare(reading, threshold.RootElement) != 0,
            "<" => Compare(reading, threshold.RootElement) < 0,
            "<=" => Compare(reading, threshold.RootElement) <= 0,
            ">" => Compare(reading, threshold.RootElement) > 0,
            _ => Compare(reading, threshold.RootElement) >= 0
        };
    }

    /// <summary>
    /// Tells whether a value has moved far enough from the threshold to clear a live Alarm: out of the comparison
    /// and, with a deadband, past the threshold by that margin.
    /// </summary>
    /// <param name="reading">The value of the Data Point.</param>
    /// <returns><see langword="true"/> when the Alarm clears.</returns>
    public bool Clears(JsonElement reading)
    {
        if (Deadband is not { } margin)
        {
            return !Holds(reading);
        }

        using var threshold = JsonDocument.Parse(Value);
        var distance = reading.GetDouble() - threshold.RootElement.GetDouble();

        return Operator switch
        {
            "<" => distance >= margin,
            "<=" => distance > margin,
            ">" => distance <= -margin,
            _ => distance < -margin
        };
    }

    private static int Compare(JsonElement reading, JsonElement threshold) =>
        threshold.ValueKind switch
        {
            JsonValueKind.Number => reading.GetDouble().CompareTo(threshold.GetDouble()),
            JsonValueKind.String => string.CompareOrdinal(reading.GetString(), threshold.GetString()),
            _ => reading.GetBoolean() == threshold.GetBoolean() ? 0 : 1
        };

    private static TimeSpan Duration(string text)
    {
        var digits = text.TakeWhile(char.IsDigit).Count();
        var amount = double.Parse(text[..digits], System.Globalization.CultureInfo.InvariantCulture);

        return text[digits..] switch
        {
            "ms" => TimeSpan.FromMilliseconds(amount),
            "s" => TimeSpan.FromSeconds(amount),
            "m" => TimeSpan.FromMinutes(amount),
            _ => TimeSpan.FromHours(amount)
        };
    }
}
