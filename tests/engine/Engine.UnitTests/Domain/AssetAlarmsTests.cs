using System.Text.Json;
using Engine.Domain.Alarms;
using Xunit;

namespace Engine.UnitTests.Domain;

public sealed class AssetAlarmsTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Operator = Guid.NewGuid();

    [Fact]
    public void Evaluate_ShouldRaiseNothing_WhenTheValueHoldsForLessThanTheDuration()
    {
        // Arrange
        var alarms = Watching(Low(forTime: "10s"));

        // Act
        alarms.Evaluate(Reads(2.5), T0);
        var changed = alarms.Evaluate(Reads(2.5), T0.AddSeconds(9));

        // Assert
        changed.ShouldBeEmpty();
        alarms.Live.ShouldBeEmpty();
    }

    [Fact]
    public void Evaluate_ShouldRaiseAnActiveUnacknowledgedAlarm_WhenTheValueHoldsForTheDuration()
    {
        // Arrange
        var alarms = Watching(Low(forTime: "10s"));
        alarms.Evaluate(Reads(2.5), T0);

        // Act
        var changed = alarms.Evaluate(Reads(2.5), T0.AddSeconds(10));

        // Assert
        var alarm = changed.ShouldHaveSingleItem();
        alarm.IsCleared.ShouldBeFalse();
        alarm.IsAcknowledged.ShouldBeFalse();
        alarm.Priority.ShouldBe(AlarmPriority.High);
        alarm.Message.ShouldBe("Brake pressure below 3 bar");
        alarm.ActiveCount.ShouldBe(1);
    }

    [Fact]
    public void Evaluate_ShouldRaiseAtOnce_WhenTheConditionHasNoDuration()
    {
        // Arrange
        var alarms = Watching(Low());

        // Act
        var changed = alarms.Evaluate(Reads(2.5), T0);

        // Assert
        changed.ShouldHaveSingleItem();
    }

    [Fact]
    public void Evaluate_ShouldRaiseNothing_WhenTheValueRecoversWithinTheDuration()
    {
        // Arrange
        var alarms = Watching(Low(forTime: "10s"));
        alarms.Evaluate(Reads(2.5), T0);
        alarms.Evaluate(Reads(3.5), T0.AddSeconds(5));

        // Act
        var changed = alarms.Evaluate(Reads(2.5), T0.AddSeconds(12));

        // Assert
        changed.ShouldBeEmpty();
        alarms.Pending.ShouldContainKey("low");
    }

    [Fact]
    public void Evaluate_ShouldResetTheWait_WhenTheValueIsBad()
    {
        // Arrange
        var alarms = Watching(Low(forTime: "10s"));
        alarms.Evaluate(Reads(2.5), T0);
        alarms.Evaluate(_ => null, T0.AddSeconds(5));

        // Act
        var changed = alarms.Evaluate(Reads(2.5), T0.AddSeconds(11));

        // Assert
        changed.ShouldBeEmpty();
        alarms.Pending["low"].Since.ShouldBe(T0.AddSeconds(11));
    }

    [Fact]
    public void Evaluate_ShouldNotClearTheAlarm_WhenTheValueIsBad()
    {
        // Arrange
        var alarms = Watching(Low());
        alarms.Evaluate(Reads(2.5), T0);

        // Act
        var changed = alarms.Evaluate(_ => null, T0.AddSeconds(1));

        // Assert
        changed.ShouldBeEmpty();
        alarms.Live.ShouldHaveSingleItem().IsCleared.ShouldBeFalse();
    }

    [Fact]
    public void Evaluate_ShouldClearOnlyPastTheDeadband_WhenTheConditionHasOne()
    {
        // Arrange
        var alarms = Watching(Low(deadband: 0.2));
        alarms.Evaluate(Reads(2.5), T0);

        // Act
        var inside = alarms.Evaluate(Reads(3.1), T0.AddSeconds(1));
        var past = alarms.Evaluate(Reads(3.2), T0.AddSeconds(2));

        // Assert
        inside.ShouldBeEmpty();
        past.ShouldHaveSingleItem().IsCleared.ShouldBeTrue();
    }

    [Fact]
    public void Evaluate_ShouldReactivateTheSameAlarm_WhenAClearedUnacknowledgedAlarmHoldsAgain()
    {
        // Arrange
        var alarms = Watching(Low());
        var raised = alarms.Evaluate(Reads(2.5), T0).Single();
        alarms.Evaluate(Reads(4), T0.AddSeconds(1));

        // Act
        var changed = alarms.Evaluate(Reads(2), T0.AddSeconds(2));

        // Assert
        changed.ShouldHaveSingleItem().ShouldBeSameAs(raised);
        raised.IsCleared.ShouldBeFalse();
        raised.ActiveCount.ShouldBe(2);
        alarms.Live.Count.ShouldBe(1);
    }

    [Fact]
    public void Evaluate_ShouldEndTheAlarm_WhenItIsAcknowledgedAndThenClears()
    {
        // Arrange
        var alarms = Watching(Low());
        var raised = alarms.Evaluate(Reads(2.5), T0).Single();
        alarms.Acknowledge(raised.Id, Operator, "on it", T0.AddSeconds(1));

        // Act
        alarms.Evaluate(Reads(4), T0.AddSeconds(2));

        // Assert
        raised.IsEnded.ShouldBeTrue();
        raised.Outcome.ShouldBe(AlarmOutcome.Ended);
        alarms.Live.ShouldBeEmpty();
    }

    [Fact]
    public void Acknowledge_ShouldEndTheAlarm_WhenItIsAlreadyCleared()
    {
        // Arrange
        var alarms = Watching(Low());
        var raised = alarms.Evaluate(Reads(2.5), T0).Single();
        alarms.Evaluate(Reads(4), T0.AddSeconds(1));

        // Act
        var acknowledged = alarms.Acknowledge(raised.Id, Operator, "gone", T0.AddSeconds(2));

        // Assert
        acknowledged.ShouldBeSameAs(raised);
        raised.IsEnded.ShouldBeTrue();
        raised.AcknowledgedBy.ShouldBe(Operator);
        raised.AcknowledgementComment.ShouldBe("gone");
        raised.AcknowledgedAt.ShouldBe(T0.AddSeconds(2));
        alarms.Live.ShouldBeEmpty();
    }

    [Fact]
    public void Evaluate_ShouldRaiseANewAlarm_WhenTheConditionHoldsAfterTheOldOneEnded()
    {
        // Arrange
        var alarms = Watching(Low());
        var first = alarms.Evaluate(Reads(2.5), T0).Single();
        alarms.Evaluate(Reads(4), T0.AddSeconds(1));
        alarms.Acknowledge(first.Id, Operator, null, T0.AddSeconds(2));

        // Act
        var changed = alarms.Evaluate(Reads(2), T0.AddSeconds(3));

        // Assert
        var second = changed.ShouldHaveSingleItem();
        second.Id.ShouldNotBe(first.Id);
        second.ActiveCount.ShouldBe(1);
    }

    [Fact]
    public void Acknowledge_ShouldChangeNothing_WhenTheAlarmIsAlreadyAcknowledged()
    {
        // Arrange
        var alarms = Watching(Low());
        var raised = alarms.Evaluate(Reads(2.5), T0).Single();
        alarms.Acknowledge(raised.Id, Operator, "first", T0.AddSeconds(1));

        // Act
        alarms.Acknowledge(raised.Id, Guid.NewGuid(), "second", T0.AddSeconds(2));

        // Assert
        raised.AcknowledgedBy.ShouldBe(Operator);
        raised.AcknowledgementComment.ShouldBe("first");
        raised.AcknowledgedAt.ShouldBe(T0.AddSeconds(1));
    }

    [Fact]
    public void Acknowledge_ShouldFindNothing_WhenNoLiveAlarmHasTheId()
    {
        // Arrange
        var alarms = Watching(Low());

        // Act
        var found = alarms.Acknowledge(Guid.NewGuid(), Operator, null, T0);

        // Assert
        found.ShouldBeNull();
    }

    [Fact]
    public void Reconcile_ShouldRetireTheAlarm_WhenTheThresholdChanges()
    {
        // Arrange
        var alarms = Watching(Low());
        var raised = alarms.Evaluate(Reads(2.5), T0).Single();

        // Act
        var changed = alarms.Reconcile([Low(value: "4")], T0.AddSeconds(1));

        // Assert
        changed.ShouldHaveSingleItem().ShouldBeSameAs(raised);
        raised.Outcome.ShouldBe(AlarmOutcome.Retired);
        raised.IsAcknowledged.ShouldBeFalse();
        alarms.Live.ShouldBeEmpty();
    }

    [Fact]
    public void Reconcile_ShouldUpdateTheAlarmInPlace_WhenOnlyThePriorityChanges()
    {
        // Arrange
        var alarms = Watching(Low());
        var raised = alarms.Evaluate(Reads(2.5), T0).Single();

        // Act
        alarms.Reconcile([Low(priority: AlarmPriority.Critical)], T0.AddSeconds(1));

        // Assert
        raised.IsEnded.ShouldBeFalse();
        raised.Priority.ShouldBe(AlarmPriority.Critical);
        alarms.Live.ShouldHaveSingleItem();
    }

    [Fact]
    public void Reconcile_ShouldRetireTheAlarm_WhenTheConditionIsRemoved()
    {
        // Arrange
        var alarms = Watching(Low());
        alarms.Evaluate(Reads(2.5), T0);

        // Act
        var changed = alarms.Reconcile([], T0.AddSeconds(1));

        // Assert
        changed.ShouldHaveSingleItem().Outcome.ShouldBe(AlarmOutcome.Retired);
    }

    [Fact]
    public void RetireAll_ShouldRetireEveryLiveAlarm_WhenTheAssetIsDeleted()
    {
        // Arrange
        var alarms = Watching(Low(), Low(name: "other"));
        alarms.Evaluate(Reads(2.5), T0);

        // Act
        var retired = alarms.RetireAll(T0.AddSeconds(1));

        // Assert
        retired.Count.ShouldBe(2);
        retired.ShouldAllBe(alarm => alarm.Outcome == AlarmOutcome.Retired);
        alarms.Live.ShouldBeEmpty();
    }

    [Fact]
    public void Evaluate_ShouldRaiseAtOnce_WhenTheDeadlinePassedWhileTheAssetWasAway()
    {
        // Arrange
        var first = Watching(Low(forTime: "10s"));
        first.Evaluate(Reads(2.5), T0);
        var restored = AssetAlarms.Restore("train-1", first.Live, first.Pending);
        restored.Reconcile([Low(forTime: "10s")], T0.AddSeconds(30));

        // Act
        var changed = restored.Evaluate(Reads(2.5), T0.AddSeconds(30));

        // Assert
        changed.ShouldHaveSingleItem();
    }

    [Fact]
    public void NextDeadline_ShouldBeWhenThePendingConditionHasHeldLongEnough()
    {
        // Arrange
        var alarms = Watching(Low(forTime: "10s"));

        // Act
        alarms.Evaluate(Reads(2.5), T0);

        // Assert
        alarms.NextDeadline.ShouldBe(T0.AddSeconds(10));
    }

    [Theory]
    [InlineData(">", "5", 5.1, true)]
    [InlineData(">", "5", 5, false)]
    [InlineData(">=", "5", 5, true)]
    [InlineData("<=", "5", 5, true)]
    [InlineData("==", "5", 5, true)]
    [InlineData("!=", "5", 5, false)]
    public void Holds_ShouldCompareNumbers_WhenTheOperatorIs(string op, string threshold, double value, bool expected)
    {
        // Arrange
        var condition = Low(op: op, value: threshold);

        // Act
        var holds = condition.Holds(Json(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        // Assert
        holds.ShouldBe(expected);
    }

    [Theory]
    [InlineData("\"fault\"", "\"fault\"", "==", true)]
    [InlineData("\"fault\"", "\"ok\"", "==", false)]
    [InlineData("\"ok\"", "\"fault\"", "!=", true)]
    [InlineData("true", "true", "==", true)]
    [InlineData("false", "true", "==", false)]
    [InlineData("false", "true", "!=", true)]
    public void Holds_ShouldCompareTextAndBooleans_WhenTheOperatorIsEquality(
        string reading,
        string threshold,
        string op,
        bool expected)
    {
        // Arrange
        var condition = Low(op: op, value: threshold);

        // Act
        var holds = condition.Holds(Json(reading));

        // Assert
        holds.ShouldBe(expected);
    }

    private static AssetAlarms Watching(params AlarmCondition[] conditions)
    {
        var alarms = AssetAlarms.Restore("train-1", [], new Dictionary<string, Holding>());
        alarms.Reconcile(conditions, T0);

        return alarms;
    }

    private static Func<string, JsonElement?> Reads(double value) =>
        _ => Json(value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();

    private static AlarmCondition Low(
        string name = "low",
        string op = "<",
        string value = "3",
        string? forTime = null,
        double? deadband = null,
        AlarmPriority priority = AlarmPriority.High)
    {
        var declaration = $$"""
                            {"name": "{{name}}", "dataPoint": "pressure", "operator": "{{op}}", "value": {{value}},
                             "priority": "{{priority.ToString().ToLowerInvariant()}}", "message": "Brake pressure below 3 bar"
                             {{(forTime is null ? "" : $", \"for\": \"{forTime}\"")}}
                             {{(deadband is null ? "" : $", \"deadband\": {deadband.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}")}}}
                            """;

        return AlarmCondition.Create(Json(declaration));
    }
}
