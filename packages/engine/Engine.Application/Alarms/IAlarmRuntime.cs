using Coderynx.Functional.Results;
using Engine.Domain.Alarms;

namespace Engine.Application.Alarms;

/// <summary>
/// The live side of Alarms: those Assets have raised and the history the Engine keeps of every Alarm.
/// </summary>
public interface IAlarmRuntime
{
    /// <summary>
    /// Lists the live Alarms of a Namespace, critical first and then by when they were raised.
    /// </summary>
    /// <param name="namespace">The Namespace.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The live Alarms.</returns>
    Task<IReadOnlyList<Alarm>> ListLiveAsync(string @namespace, CancellationToken cancellationToken);

    /// <summary>
    /// Lists the live Alarms of one Asset, critical first and then by when they were raised.
    /// </summary>
    /// <param name="namespace">The Namespace of the Asset.</param>
    /// <param name="asset">The name of the Asset.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The live Alarms, or a not-found error when the Engine knows no such Asset.</returns>
    Task<Result<IReadOnlyList<Alarm>>> ListAssetAsync(
        string @namespace,
        string asset,
        CancellationToken cancellationToken);

    /// <summary>
    /// Reads one Alarm, live or ended, until its retention runs out.
    /// </summary>
    /// <param name="namespace">The Namespace of the Alarm.</param>
    /// <param name="id">The id of the Alarm.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The Alarm, or a not-found error when the Engine keeps none with that id.</returns>
    Task<Result<Alarm>> GetAsync(string @namespace, Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the history of Alarms of a Namespace, newest first.
    /// </summary>
    /// <param name="namespace">The Namespace.</param>
    /// <param name="asset">Keeps only Alarms of this Asset; no filter when <see langword="null"/>.</param>
    /// <param name="from">Keeps only Alarms raised at or after this time.</param>
    /// <param name="to">Keeps only Alarms raised before this time.</param>
    /// <param name="priority">Keeps only Alarms of this Alarm Priority; no filter when <see langword="null"/>.</param>
    /// <param name="state">Keeps only Alarms in this state; no filter when <see langword="null"/>.</param>
    /// <param name="offset">How many Alarms to skip.</param>
    /// <param name="limit">How many Alarms to return.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>One page of the history.</returns>
    Task<AlarmHistoryPage> HistoryAsync(
        string @namespace,
        string? asset,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AlarmPriority? priority,
        AlarmState? state,
        int offset,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>
    /// Acknowledges a live Alarm, active or cleared. One already acknowledged stays as it was, even once it has ended.
    /// </summary>
    /// <param name="namespace">The Namespace of the Alarm.</param>
    /// <param name="id">The id of the Alarm.</param>
    /// <param name="by">The id of the User who acknowledges.</param>
    /// <param name="byName">The username of the User who acknowledges.</param>
    /// <param name="comment">An optional comment.</param>
    /// <param name="cancellationToken">Cancels the acknowledgement.</param>
    /// <returns>
    /// Success when this User has acknowledged it, a conflict naming the first acknowledger when another User has, or a
    /// not-found error when it is neither live nor acknowledged.
    /// </returns>
    Task<Result> AcknowledgeAsync(
        string @namespace,
        Guid id,
        Guid by,
        string byName,
        string? comment,
        CancellationToken cancellationToken);
}

/// <summary>
/// One page of Alarm history.
/// </summary>
/// <param name="Items">The Alarms on this page.</param>
/// <param name="Total">How many Alarms match the filter, across all pages.</param>
public sealed record AlarmHistoryPage(IReadOnlyList<Alarm> Items, int Total);
