using System.Text.Json;

namespace Engine.Domain.Alarms;

/// <summary>
/// The Alarm Conditions of one Asset and the Alarms they have raised: at most one live Alarm per Alarm Condition.
/// </summary>
public sealed class AssetAlarms
{
    private readonly string _asset;
    private readonly Dictionary<string, Alarm> _live;
    private readonly Dictionary<string, Holding> _holding;
    private IReadOnlyList<AlarmCondition> _conditions = [];

    private AssetAlarms(string asset, IEnumerable<Alarm> live, IReadOnlyDictionary<string, Holding> holding)
    {
        _asset = asset;
        _live = live.ToDictionary(alarm => alarm.Condition);
        _holding = new Dictionary<string, Holding>(holding);
    }

    /// <summary>
    /// Gets the live Alarms: those not yet ended.
    /// </summary>
    public IReadOnlyList<Alarm> Live => [.. _live.Values];
    /// <summary>
    /// Gets the Alarm Conditions that hold but have not held long enough, by name.
    /// </summary>
    public IReadOnlyDictionary<string, Holding> Pending => _holding;
    /// <summary>
    /// Gets the moment the first pending Alarm Condition has held long enough, if any is pending.
    /// </summary>
    public DateTimeOffset? NextDeadline =>
        _holding.Select(entry => Deadline(entry.Key, entry.Value)).Where(deadline => deadline is not null).Min();

    /// <summary>
    /// Starts the Alarms of an Asset, or restores them from stored state.
    /// </summary>
    /// <param name="asset">The name of the Asset.</param>
    /// <param name="live">The live Alarms stored for it.</param>
    /// <param name="pending">The pending Alarm Conditions stored for it.</param>
    /// <returns>The Alarms of the Asset.</returns>
    public static AssetAlarms Restore(
        string asset,
        IEnumerable<Alarm> live,
        IReadOnlyDictionary<string, Holding> pending) => new(asset, live, pending);

    /// <summary>
    /// Adopts the Alarm Conditions the Asset now declares. A live Alarm whose Alarm Condition is gone or raises
    /// differently is retired; one whose Alarm Priority or message changed follows it.
    /// </summary>
    /// <param name="conditions">The Alarm Conditions of the Asset, its Asset Type's included.</param>
    /// <param name="now">The time of the change.</param>
    /// <returns>The Alarms that changed.</returns>
    public IReadOnlyList<Alarm> Reconcile(IReadOnlyList<AlarmCondition> conditions, DateTimeOffset now)
    {
        _conditions = conditions;
        var changed = new List<Alarm>();
        foreach (var alarm in _live.Values.ToList())
        {
            var condition = conditions.FirstOrDefault(candidate => candidate.Name == alarm.Condition);
            if (condition is null || condition.Trigger != alarm.Trigger)
            {
                alarm.Retire(now);
                _live.Remove(alarm.Condition);
                changed.Add(alarm);
            }
            else if (alarm.Follow(condition))
            {
                changed.Add(alarm);
            }
        }

        foreach (var name in _holding.Keys.ToList())
        {
            var condition = conditions.FirstOrDefault(candidate => candidate.Name == name);
            if (condition is null || condition.Trigger != _holding[name].Trigger)
            {
                _holding.Remove(name);
            }
        }

        return changed;
    }

    /// <summary>
    /// Retires every live Alarm, as when its Asset is deleted.
    /// </summary>
    /// <param name="now">The time of the retirement.</param>
    /// <returns>The Alarms that were retired.</returns>
    public IReadOnlyList<Alarm> RetireAll(DateTimeOffset now)
    {
        var retired = _live.Values.ToList();
        foreach (var alarm in retired)
        {
            alarm.Retire(now);
        }

        _live.Clear();
        _holding.Clear();

        return retired;
    }

    /// <summary>
    /// Evaluates every Alarm Condition against the current values. A missing or bad value neither raises nor clears
    /// an Alarm, and starts any <c>for</c> wait over.
    /// </summary>
    /// <param name="read">Reads a Data Point: its value, or <see langword="null"/> when it has none to trust.</param>
    /// <param name="now">The time of the evaluation.</param>
    /// <returns>The Alarms that changed.</returns>
    public IReadOnlyList<Alarm> Evaluate(Func<string, JsonElement?> read, DateTimeOffset now)
    {
        var changed = new List<Alarm>();
        foreach (var condition in _conditions)
        {
            if (read(condition.DataPoint) is not { } reading)
            {
                _holding.Remove(condition.Name);

                continue;
            }

            _live.TryGetValue(condition.Name, out var alarm);
            if (alarm is { IsCleared: false })
            {
                if (condition.Clears(reading))
                {
                    alarm.Clear(now);
                    if (alarm.IsEnded)
                    {
                        _live.Remove(condition.Name);
                    }

                    changed.Add(alarm);
                }

                continue;
            }

            if (!condition.Holds(reading))
            {
                _holding.Remove(condition.Name);

                continue;
            }

            if (!_holding.TryGetValue(condition.Name, out var holding))
            {
                holding = new Holding(now, condition.Trigger);
                _holding[condition.Name] = holding;
            }

            if (now < holding.Since + (condition.For ?? TimeSpan.Zero))
            {
                continue;
            }

            _holding.Remove(condition.Name);
            if (alarm is null)
            {
                alarm = Alarm.Raise(_asset, condition, now);
                _live[condition.Name] = alarm;
            }
            else
            {
                alarm.Reactivate();
            }

            changed.Add(alarm);
        }

        return changed;
    }

    /// <summary>
    /// Records an Operator's acknowledgement of a live Alarm.
    /// </summary>
    /// <param name="id">The id of the Alarm.</param>
    /// <param name="by">The id of the User who acknowledges.</param>
    /// <param name="byName">The username of the User who acknowledges.</param>
    /// <param name="comment">An optional comment.</param>
    /// <param name="now">The time of the acknowledgement.</param>
    /// <returns>
    /// The Alarm, or <see langword="null"/> when this Asset has no live Alarm with that id. An Alarm already
    /// acknowledged is returned unchanged.
    /// </returns>
    public Alarm? Acknowledge(Guid id, Guid by, string byName, string? comment, DateTimeOffset now)
    {
        var alarm = _live.Values.FirstOrDefault(candidate => candidate.Id == id);
        if (alarm?.Acknowledge(by, byName, comment, now) == true && alarm.IsEnded)
        {
            _live.Remove(alarm.Condition);
        }

        return alarm;
    }

    private DateTimeOffset? Deadline(string name, Holding holding) =>
        _conditions.FirstOrDefault(condition => condition.Name == name) is { } condition
            ? holding.Since + (condition.For ?? TimeSpan.Zero)
            : null;
}
