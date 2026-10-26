using Engine.Domain.Alarms;

namespace Engine.Presentation.Alarms.Contracts;

internal sealed record AlarmDto(
    Guid Id,
    string Asset,
    string Condition,
    string Priority,
    string Message,
    string State,
    DateTimeOffset RaisedAt,
    int ActiveCount,
    DateTimeOffset? AcknowledgedAt,
    Guid? AcknowledgedBy,
    string? AcknowledgementComment,
    DateTimeOffset? ClearedAt,
    DateTimeOffset? EndedAt);

internal static class AlarmMappings
{
    public static AlarmDto ToDto(this Alarm alarm) =>
        new(
            alarm.Id,
            alarm.Asset,
            alarm.Condition,
            alarm.Priority.ToString().ToLowerInvariant(),
            alarm.Message,
            State(alarm),
            alarm.RaisedAt,
            alarm.ActiveCount,
            alarm.AcknowledgedAt,
            alarm.AcknowledgedBy,
            alarm.AcknowledgementComment,
            alarm.ClearedAt,
            alarm.EndedAt);

    private static string State(Alarm alarm) =>
        alarm.Outcome?.ToString().ToLowerInvariant() ?? (alarm.IsCleared ? "cleared" : "active");
}
