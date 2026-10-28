using System.Text.Json;
using Engine.Domain.Flows;

namespace Engine.Presentation.Flows.Contracts;

internal sealed record FlowExecutionDto(
    Guid Id,
    string Flow,
    long Generation,
    string Trigger,
    string State,
    string? CurrentNode,
    int Steps,
    DateTimeOffset? Deadline,
    long? LatenessMs,
    FlowExecutionErrorDto? Error,
    JsonElement Variables,
    DateTimeOffset StartedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? FinishedAt);

internal sealed record FlowExecutionErrorDto(string Code, string Message, string Node, int Attempts);

internal static class FlowExecutionMappings
{
    public static FlowExecutionDto ToDto(this FlowExecution execution) =>
        new(
            execution.Id,
            execution.Flow,
            execution.Generation,
            execution.Trigger,
            execution.State.ToString().ToLowerInvariant(),
            execution.CurrentNode,
            execution.Steps,
            execution.Deadline,
            (long?)execution.Lateness?.TotalMilliseconds,
            execution.Error is { } error
                ? new FlowExecutionErrorDto(error.Code, error.Message, error.Node, error.Attempts)
                : null,
            JsonDocument.Parse(execution.Variables).RootElement.Clone(),
            execution.StartedAt,
            execution.UpdatedAt,
            execution.FinishedAt);
}
