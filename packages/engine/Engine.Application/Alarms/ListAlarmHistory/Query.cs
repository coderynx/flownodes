using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;
using Engine.Domain.Alarms;

namespace Engine.Application.Alarms.ListAlarmHistory;

/// <summary>
/// Reads the history of the Alarms of a Namespace, newest first.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace.</param>
/// <param name="Asset">Keeps only Alarms of this Asset; no filter when <see langword="null"/>.</param>
/// <param name="From">Keeps only Alarms raised at or after this time.</param>
/// <param name="To">Keeps only Alarms raised before this time.</param>
/// <param name="Priority">
/// Keeps only Alarms of this Alarm Priority: <c>low</c>, <c>medium</c>, <c>high</c> or <c>critical</c>; none for all.
/// </param>
/// <param name="State">
/// Keeps only Alarms in this state: <c>active</c>, <c>cleared</c>, <c>ended</c> or <c>retired</c>; none for all.
/// </param>
/// <param name="Offset">How many Alarms to skip; negative values count as zero.</param>
/// <param name="Limit">How many Alarms to return; clamped to 1 through 200.</param>
public sealed record Query(
    Guid CallerId,
    string Namespace,
    string? Asset,
    DateTimeOffset? From,
    DateTimeOffset? To,
    string? Priority,
    string? State,
    int Offset,
    int Limit) : IRequest<Result<AlarmHistoryPage>>;

internal sealed class Handler(IRoleGrants grants, IAlarmRuntime alarms)
    : IRequestHandler<Query, Result<AlarmHistoryPage>>
{
    private const int MaxLimit = 200;

    private static readonly Dictionary<string, AlarmPriority> Priorities = Enum.GetValues<AlarmPriority>()
        .ToDictionary(priority => priority.ToString().ToLowerInvariant());

    private static readonly Dictionary<string, AlarmState> States =
        Enum.GetValues<AlarmState>().ToDictionary(state => state.ToString().ToLowerInvariant());

    public async Task<Result<AlarmHistoryPage>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireMemberAsync(request.CallerId, request.Namespace, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        AlarmPriority? priority = null;
        if (request.Priority is not null)
        {
            if (!Priorities.TryGetValue(request.Priority, out var parsed))
            {
                return Error.InvalidInput(
                    "alarm.unknown_priority",
                    $"Filter by one of {string.Join(", ", Priorities.Keys)}, not '{request.Priority}'.");
            }

            priority = parsed;
        }

        AlarmState? state = null;
        if (request.State is not null)
        {
            if (!States.TryGetValue(request.State, out var parsed))
            {
                return Error.InvalidInput(
                    "alarm.unknown_state",
                    $"Filter by one of {string.Join(", ", States.Keys)}, not '{request.State}'.");
            }

            state = parsed;
        }

        return Result.Found(
            await alarms.HistoryAsync(
                request.Namespace,
                request.Asset,
                request.From,
                request.To,
                priority,
                state,
                Math.Max(request.Offset, 0),
                Math.Clamp(request.Limit, 1, MaxLimit),
                cancellationToken));
    }
}
