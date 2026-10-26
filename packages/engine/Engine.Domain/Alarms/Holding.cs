namespace Engine.Domain.Alarms;

/// <summary>
/// An Alarm Condition whose comparison has held since a moment but not yet for as long as it needs.
/// </summary>
/// <param name="Since">When the comparison started to hold.</param>
/// <param name="Trigger">The <see cref="AlarmCondition.Trigger"/> it has been holding for.</param>
public sealed record Holding(DateTimeOffset Since, string Trigger);
