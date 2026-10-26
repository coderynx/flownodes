namespace Engine.Domain.Alarms;

/// <summary>
/// How an Alarm came to its end.
/// </summary>
public enum AlarmOutcome
{
    /// <summary>
    /// The Alarm was both cleared and acknowledged.
    /// </summary>
    Ended,

    /// <summary>
    /// The Alarm Condition that raised it changed or went away before an Operator acknowledged it.
    /// </summary>
    Retired
}
