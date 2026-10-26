namespace Engine.Domain.Alarms;

/// <summary>
/// One occurrence of an abnormal situation on an Asset. It is active while its cause holds and cleared once it stops,
/// acknowledged or not independently of that, and ends once it is both cleared and acknowledged, or is retired
/// unacknowledged when its Alarm Condition changes or goes away.
/// </summary>
public sealed class Alarm
{
    private Alarm()
    {
    }

    /// <summary>
    /// Gets the id of the Alarm.
    /// </summary>
    public required Guid Id { get; init; }
    /// <summary>
    /// Gets the name of the Asset it was raised on.
    /// </summary>
    public required string Asset { get; init; }
    /// <summary>
    /// Gets the name of the Alarm Condition that raised it.
    /// </summary>
    public required string Condition { get; init; }
    /// <summary>
    /// Gets what made the Alarm Condition raise it, so a change of it retires the Alarm.
    /// </summary>
    public required string Trigger { get; init; }
    /// <summary>
    /// Gets when it was first raised.
    /// </summary>
    public required DateTimeOffset RaisedAt { get; init; }
    /// <summary>
    /// Gets its Alarm Priority.
    /// </summary>
    public AlarmPriority Priority { get; private set; }
    /// <summary>
    /// Gets the text an Operator sees.
    /// </summary>
    public string Message { get; private set; } = "";
    /// <summary>
    /// Gets how many times it has become active.
    /// </summary>
    public int ActiveCount { get; private set; }
    /// <summary>
    /// Gets when an Operator acknowledged it.
    /// </summary>
    public DateTimeOffset? AcknowledgedAt { get; private set; }
    /// <summary>
    /// Gets the id of the User who acknowledged it.
    /// </summary>
    public Guid? AcknowledgedBy { get; private set; }
    /// <summary>
    /// Gets the comment that came with the acknowledgement.
    /// </summary>
    public string? AcknowledgementComment { get; private set; }
    /// <summary>
    /// Gets when its cause last stopped holding; none while it is active.
    /// </summary>
    public DateTimeOffset? ClearedAt { get; private set; }
    /// <summary>
    /// Gets when it ended or was retired.
    /// </summary>
    public DateTimeOffset? EndedAt { get; private set; }
    /// <summary>
    /// Gets how it ended.
    /// </summary>
    public AlarmOutcome? Outcome { get; private set; }
    /// <summary>
    /// Gets a value indicating whether its cause has stopped holding.
    /// </summary>
    public bool IsCleared => ClearedAt is not null;
    /// <summary>
    /// Gets a value indicating whether an Operator has acknowledged it.
    /// </summary>
    public bool IsAcknowledged => AcknowledgedAt is not null;
    /// <summary>
    /// Gets a value indicating whether it has ended or been retired.
    /// </summary>
    public bool IsEnded => EndedAt is not null;

    /// <summary>
    /// Raises an Alarm, active and unacknowledged.
    /// </summary>
    /// <param name="asset">The name of the Asset.</param>
    /// <param name="condition">The Alarm Condition that holds.</param>
    /// <param name="now">The time it is raised.</param>
    /// <returns>The new Alarm.</returns>
    public static Alarm Raise(string asset, AlarmCondition condition, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            Asset = asset,
            Condition = condition.Name,
            Trigger = condition.Trigger,
            RaisedAt = now,
            Priority = condition.Priority,
            Message = condition.Message,
            ActiveCount = 1
        };

    /// <summary>
    /// Rebuilds an Alarm from its stored state.
    /// </summary>
    /// <param name="id">The id of the Alarm.</param>
    /// <param name="asset">The name of the Asset.</param>
    /// <param name="condition">The name of the Alarm Condition.</param>
    /// <param name="trigger">What made the Alarm Condition raise it.</param>
    /// <param name="raisedAt">When it was first raised.</param>
    /// <param name="priority">Its Alarm Priority.</param>
    /// <param name="message">The text an Operator sees.</param>
    /// <param name="activeCount">How many times it has become active.</param>
    /// <param name="acknowledgedAt">When it was acknowledged, if it was.</param>
    /// <param name="acknowledgedBy">Who acknowledged it.</param>
    /// <param name="acknowledgementComment">The comment on the acknowledgement.</param>
    /// <param name="clearedAt">When it cleared, if it has.</param>
    /// <param name="endedAt">When it ended, if it has.</param>
    /// <param name="outcome">How it ended.</param>
    /// <returns>The Alarm.</returns>
    public static Alarm Restore(
        Guid id,
        string asset,
        string condition,
        string trigger,
        DateTimeOffset raisedAt,
        AlarmPriority priority,
        string message,
        int activeCount,
        DateTimeOffset? acknowledgedAt,
        Guid? acknowledgedBy,
        string? acknowledgementComment,
        DateTimeOffset? clearedAt,
        DateTimeOffset? endedAt,
        AlarmOutcome? outcome) =>
        new()
        {
            Id = id,
            Asset = asset,
            Condition = condition,
            Trigger = trigger,
            RaisedAt = raisedAt,
            Priority = priority,
            Message = message,
            ActiveCount = activeCount,
            AcknowledgedAt = acknowledgedAt,
            AcknowledgedBy = acknowledgedBy,
            AcknowledgementComment = acknowledgementComment,
            ClearedAt = clearedAt,
            EndedAt = endedAt,
            Outcome = outcome
        };

    /// <summary>
    /// Records that the cause stopped holding. An acknowledged Alarm ends.
    /// </summary>
    /// <param name="now">The time it clears.</param>
    /// <exception cref="InvalidOperationException">The Alarm is already cleared.</exception>
    public void Clear(DateTimeOffset now)
    {
        if (IsCleared)
        {
            throw new InvalidOperationException("The Alarm is already cleared.");
        }

        ClearedAt = now;
        if (IsAcknowledged)
        {
            End(now, AlarmOutcome.Ended);
        }
    }

    /// <summary>
    /// Makes a cleared, unacknowledged Alarm active again and counts it.
    /// </summary>
    /// <exception cref="InvalidOperationException">The Alarm is not cleared, or has been acknowledged.</exception>
    public void Reactivate()
    {
        if (!IsCleared || IsAcknowledged)
        {
            throw new InvalidOperationException("Only a cleared, unacknowledged Alarm becomes active again.");
        }

        ClearedAt = null;
        ActiveCount++;
    }

    /// <summary>
    /// Records an Operator's acknowledgement. A cleared Alarm ends. Acknowledging again changes nothing.
    /// </summary>
    /// <param name="by">The id of the User who acknowledges.</param>
    /// <param name="comment">An optional comment.</param>
    /// <param name="now">The time of the acknowledgement.</param>
    /// <returns><see langword="true"/> when the Alarm was not acknowledged before.</returns>
    public bool Acknowledge(Guid by, string? comment, DateTimeOffset now)
    {
        if (IsAcknowledged)
        {
            return false;
        }

        AcknowledgedAt = now;
        AcknowledgedBy = by;
        AcknowledgementComment = comment;
        if (IsCleared)
        {
            End(now, AlarmOutcome.Ended);
        }

        return true;
    }

    /// <summary>
    /// Ends the Alarm without an acknowledgement, because its Alarm Condition changed or went away.
    /// </summary>
    /// <param name="now">The time it is retired.</param>
    public void Retire(DateTimeOffset now) => End(now, AlarmOutcome.Retired);

    /// <summary>
    /// Takes over the Alarm Priority and message of the Alarm Condition, which may change under a live Alarm.
    /// </summary>
    /// <param name="condition">The Alarm Condition that raised the Alarm.</param>
    /// <returns><see langword="true"/> when something changed.</returns>
    public bool Follow(AlarmCondition condition)
    {
        var changed = Priority != condition.Priority || Message != condition.Message;
        Priority = condition.Priority;
        Message = condition.Message;

        return changed;
    }

    private void End(DateTimeOffset now, AlarmOutcome outcome)
    {
        EndedAt = now;
        Outcome = outcome;
    }
}
