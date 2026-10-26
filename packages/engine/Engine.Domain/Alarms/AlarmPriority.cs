namespace Engine.Domain.Alarms;

/// <summary>
/// How urgently an Alarm needs an Operator's attention.
/// </summary>
public enum AlarmPriority
{
    /// <summary>
    /// The Alarm can wait.
    /// </summary>
    Low,

    /// <summary>
    /// The Alarm needs attention soon.
    /// </summary>
    Medium,

    /// <summary>
    /// The Alarm needs attention promptly.
    /// </summary>
    High,

    /// <summary>
    /// The Alarm needs attention at once.
    /// </summary>
    Critical
}
