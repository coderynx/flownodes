using System.Text.Json.Serialization;
using Engine.Domain.Alarms;

namespace Engine.Presentation.Alarms.Contracts;

internal sealed record AlarmDto(
    Guid Id,
    string Asset,
    string Condition,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? DataPoint,
    string Priority,
    string Message,
    string State,
    DateTimeOffset RaisedAt,
    int ActiveCount,
    DateTimeOffset? AcknowledgedAt,
    Guid? AcknowledgedBy,
    string? AcknowledgedByName,
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
            alarm.DataPoint,
            alarm.Priority.ToString().ToLowerInvariant(),
            alarm.Message,
            alarm.State.ToString().ToLowerInvariant(),
            alarm.RaisedAt,
            alarm.ActiveCount,
            alarm.AcknowledgedAt,
            alarm.AcknowledgedBy,
            alarm.AcknowledgedByName,
            alarm.AcknowledgementComment,
            alarm.ClearedAt,
            alarm.EndedAt);
}
