namespace Engine.Domain.Alarms;

/// <summary>
/// Where an Alarm is in its lifecycle.
/// </summary>
public enum AlarmState
{
    /// <summary>
    /// Its cause holds.
    /// </summary>
    Active,

    /// <summary>
    /// Its cause has stopped holding, and it waits for an acknowledgement.
    /// </summary>
    Cleared,

    /// <summary>
    /// It was both cleared and acknowledged.
    /// </summary>
    Ended,

    /// <summary>
    /// The Alarm Condition that raised it changed or went away before an Operator acknowledged it.
    /// </summary>
    Retired
}
