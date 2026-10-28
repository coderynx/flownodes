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
            declaration.TryGetProperty("for", out var duration) ? Durations.Parse(duration.GetString()!) : null,
            declaration.TryGetProperty("deadband", out var deadband) ? deadband.GetDouble() : null,
            Enum.Parse<AlarmPriority>(declaration.GetProperty("priority").GetString()!, true),
            declaration.GetProperty("message").GetString()!);

    /// <summary>
    /// Tells whether a value satisfies the comparison.
    /// </summary>
    /// <param name="reading">The value of the Data Point.</param>
    /// <returns><see langword="true"/> when the comparison holds.</returns>
    public bool Holds(JsonElement reading) => Comparison.Create(Operator, Value).Holds(reading);

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
            "<" or "<=" => distance >= margin,
            _ => distance <= -margin
        };
    }
}
