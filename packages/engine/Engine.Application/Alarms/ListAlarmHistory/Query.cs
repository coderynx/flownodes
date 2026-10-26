using Coderynx.Functional.Results;
using Coderynx.MediatorKit.Abstractions;
using Engine.Application.Authorization;

namespace Engine.Application.Alarms.ListAlarmHistory;

/// <summary>
/// Reads the history of the Alarms of a Namespace, newest first.
/// </summary>
/// <param name="CallerId">The subject of the caller's access token.</param>
/// <param name="Namespace">The Namespace.</param>
/// <param name="Asset">Keeps only Alarms of this Asset; no filter when <see langword="null"/>.</param>
/// <param name="From">Keeps only Alarms raised at or after this time.</param>
/// <param name="To">Keeps only Alarms raised before this time.</param>
/// <param name="Offset">How many Alarms to skip; negative values count as zero.</param>
/// <param name="Limit">How many Alarms to return; clamped to 1 through 200.</param>
public sealed record Query(
    Guid CallerId,
    string Namespace,
    string? Asset,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Offset,
    int Limit) : IRequest<Result<AlarmHistoryPage>>;

internal sealed class Handler(IRoleGrants grants, IAlarmRuntime alarms)
    : IRequestHandler<Query, Result<AlarmHistoryPage>>
{
    private const int MaxLimit = 200;

    public async Task<Result<AlarmHistoryPage>> HandleAsync(
        Query request,
        CancellationToken cancellationToken = default)
    {
        var access = await grants.RequireMemberAsync(request.CallerId, request.Namespace, cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        return Result.Found(
            await alarms.HistoryAsync(
                request.Namespace,
                request.Asset,
                request.From,
                request.To,
                Math.Max(request.Offset, 0),
                Math.Clamp(request.Limit, 1, MaxLimit),
                cancellationToken));
    }
}
