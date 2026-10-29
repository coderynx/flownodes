using System.Collections.Concurrent;

namespace Core.Application.Authorization;

/// <summary>
/// Asks Identity to publish a caller the module's replica has no record of, at most once per caller within
/// <see cref="Interval"/>, so a caller who retries cannot flood Identity (ADR-0026).
/// </summary>
/// <param name="resync">The module's way of asking Identity.</param>
/// <param name="time">The clock the interval is measured on.</param>
public sealed class UnknownCallerResync(IUserAccessResync resync, TimeProvider time)
{
    /// <summary>
    /// The interval within which a caller is asked for at most once.
    /// </summary>
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _requested = new();

    /// <summary>
    /// Asks Identity to publish the caller, unless it was asked to within the interval.
    /// </summary>
    /// <param name="callerId">The subject of the caller's access token.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A task that completes when the request is sent or skipped.</returns>
    public async Task RequestAsync(Guid callerId, CancellationToken cancellationToken = default)
    {
        var now = time.GetUtcNow();
        foreach (var requested in _requested.Where(requested => now - requested.Value >= Interval))
        {
            _requested.TryRemove(requested);
        }

        if (_requested.TryAdd(callerId, now))
        {
            await resync.RequestAsync(callerId, cancellationToken);
        }
    }
}
