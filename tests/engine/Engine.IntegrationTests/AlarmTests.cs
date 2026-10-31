using Coderynx.Functional.Results.Errors;
using Engine.Domain.Alarms;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class AlarmTests(PostgresFixture postgres)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
    private static readonly Guid Operator = Guid.NewGuid();

    [Fact]
    public async Task Asset_ShouldRaiseNothing_WhenTheValueRecoversWithinTheDuration()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low("for", "\"2s\"")), 2);

        // Act
        await WriteAsync(engine, 2.5);
        await Task.Delay(500, Ct);
        await WriteAsync(engine, 3.5);
        await Task.Delay(2500, Ct);

        // Assert
        (await LiveAsync(engine)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Asset_ShouldRaiseAnActiveUnacknowledgedAlarm_WhenTheValueHoldsForTheDuration()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low("for", "\"1s\"")), 2);

        // Act
        await WriteAsync(engine, 2.5);

        // Assert
        (await LiveAsync(engine)).ShouldBeEmpty();
        var alarm = await RaisedAsync(engine);
        alarm.IsCleared.ShouldBeFalse();
        alarm.IsAcknowledged.ShouldBeFalse();
        alarm.Priority.ShouldBe(AlarmPriority.High);
        alarm.Message.ShouldBe("Brake pressure below 3 bar");
    }

    [Fact]
    public async Task Asset_ShouldClearOnlyPastTheDeadband_WhenTheConditionHasOne()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low("deadband", "0.2")), 2);
        await WriteAsync(engine, 2.5);
        await RaisedAsync(engine);

        // Act
        await WriteAsync(engine, 3.1);
        var inside = (await LiveAsync(engine)).Single();
        await WriteAsync(engine, 3.2);

        // Assert
        inside.IsCleared.ShouldBeFalse();
        (await Eventually(engine, alarm => alarm.IsCleared)).ActiveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Asset_ShouldReactivateTheSameAlarm_WhenAClearedUnacknowledgedAlarmHoldsAgain()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low()), 2);
        await WriteAsync(engine, 2.5);
        var raised = await RaisedAsync(engine);
        await WriteAsync(engine, 4);
        await Eventually(engine, alarm => alarm.IsCleared);

        // Act
        await WriteAsync(engine, 2);

        // Assert
        var again = await Eventually(engine, alarm => !alarm.IsCleared && alarm.ActiveCount == 2);
        again.Id.ShouldBe(raised.Id);
        (await LiveAsync(engine)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task AcknowledgeAsync_ShouldEndTheAlarm_WhenItIsAcknowledgedAndThenClears()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low()), 2);
        await WriteAsync(engine, 2.5);
        var raised = await RaisedAsync(engine);

        // Act
        var result = await engine.Alarms.AcknowledgeAsync("acme", raised.Id, Operator, "ada", "looking", Ct);
        await WriteAsync(engine, 4);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await Until(async () => (await LiveAsync(engine)).Count == 0);
        var row = (await engine.Alarms.HistoryAsync("acme", "train", null, null, null, null, 0, 10, Ct)).Items.Single();
        row.IsEnded.ShouldBeTrue();
        row.Outcome.ShouldBe(AlarmOutcome.Ended);
        row.AcknowledgedBy.ShouldBe(Operator);
        row.AcknowledgementComment.ShouldBe("looking");
        row.AcknowledgedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task AcknowledgeAsync_ShouldEndTheAlarmAndAllowANewOne_WhenItIsClearedFirst()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low()), 2);
        await WriteAsync(engine, 2.5);
        var first = await RaisedAsync(engine);
        await WriteAsync(engine, 4);
        await Eventually(engine, alarm => alarm.IsCleared);

        // Act
        await engine.Alarms.AcknowledgeAsync("acme", first.Id, Operator, "ada", null, Ct);
        await WriteAsync(engine, 2);

        // Assert
        var second = await Eventually(engine, alarm => alarm.Id != first.Id);
        second.ActiveCount.ShouldBe(1);
        var page = await engine.Alarms.HistoryAsync("acme", "train", null, null, null, null, 0, 10, Ct);
        page.Total.ShouldBe(2);
    }

    [Fact]
    public async Task AcknowledgeAsync_ShouldConflictAndChangeNothing_WhenAnotherUserAlreadyAcknowledged()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low()), 2);
        await WriteAsync(engine, 2.5);
        var raised = await RaisedAsync(engine);
        await engine.Alarms.AcknowledgeAsync("acme", raised.Id, Operator, "ada", "first", Ct);

        // Act
        var result = await engine.Alarms.AcknowledgeAsync("acme", raised.Id, Guid.NewGuid(), "grace", "second", Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
        result.Error.Message.ShouldContain("ada");
        var alarm = (await LiveAsync(engine)).Single();
        alarm.AcknowledgedBy.ShouldBe(Operator);
        alarm.AcknowledgementComment.ShouldBe("first");
    }

    [Fact]
    public async Task AcknowledgeAsync_ShouldReturnNotFound_WhenNoLiveAlarmHasTheId()
    {
        // Arrange
        await using var engine = await StartAsync();

        // Act
        var result = await engine.Alarms.AcknowledgeAsync("acme", Guid.NewGuid(), Operator, "ada", null, Ct);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Asset_ShouldRetireTheAlarm_WhenTheThresholdChanges()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low()), 2);
        await WriteAsync(engine, 2.5);
        await RaisedAsync(engine);

        // Act
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low("value", "2")), 3, 2);

        // Assert
        await Until(async () => (await LiveAsync(engine)).Count == 0);
        var row = (await engine.Alarms.HistoryAsync("acme", "train", null, null, null, null, 0, 10, Ct)).Items.Single();
        row.Outcome.ShouldBe(AlarmOutcome.Retired);
        row.IsAcknowledged.ShouldBeFalse();
    }

    [Fact]
    public async Task Asset_ShouldUpdateTheAlarmInPlace_WhenOnlyThePriorityChanges()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low()), 2);
        await WriteAsync(engine, 2.5);
        var raised = await RaisedAsync(engine);

        // Act
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low("priority", "\"critical\"")), 3, 2);

        // Assert
        var alarm = await Eventually(engine, alarm => alarm.Priority == AlarmPriority.Critical);
        alarm.Id.ShouldBe(raised.Id);
    }

    [Fact]
    public async Task Asset_ShouldRetireItsAlarms_WhenItIsDeleted()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low()), 2);
        await WriteAsync(engine, 2.5);
        await RaisedAsync(engine);

        // Act
        await engine.DeleteAsync("acme", "Asset", "train", 3);

        // Assert
        await Until(async () => (await engine.Alarms.ListLiveAsync("acme", Ct)).Count == 0);
        var row = (await engine.Alarms.HistoryAsync("acme", "train", null, null, null, null, 0, 10, Ct)).Items.Single();
        row.Outcome.ShouldBe(AlarmOutcome.Retired);
    }

    [Fact]
    public async Task Asset_ShouldInheritTheAlarmConditions_WhenItIsAnInstanceOfAnAssetType()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync(
            "acme",
            "AssetType",
            "train",
            $$"""{"dataPoints": [{"name": "pressure", "type": "number"}], "alarms": [{{Low()}}]}""",
            2);

        // Act
        await engine.ApplyAsync("acme", "Asset", "train-1", """{"assetType": "train"}""", 3);

        // Assert
        await engine.Assets.WriteDataPointAsync(
            "acme",
            "train-1",
            "pressure",
            System.Text.Json.JsonDocument.Parse("2.5").RootElement,
            Ct);
        var alarm = await Wait.UntilAsync(
            async () =>
            {
                var live = (await engine.Alarms.ListAssetAsync("acme", "train-1", Ct)).Value;

                return live.Count == 1 ? live[0] : null;
            },
            Ct);
        alarm.Condition.ShouldBe("low");
    }

    [Fact]
    public async Task Asset_ShouldKeepItsAlarms_WhenItsAssetTypeIsGone()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync(
            "acme",
            "AssetType",
            "train",
            $$"""{"dataPoints": [{"name": "pressure", "type": "number"}], "alarms": [{{Low()}}]}""",
            2);
        await engine.ApplyAsync("acme", "Asset", "train-1", """{"assetType": "train"}""", 3);
        await engine.Assets.WriteDataPointAsync(
            "acme",
            "train-1",
            "pressure",
            System.Text.Json.JsonDocument.Parse("2.5").RootElement,
            Ct);
        await Wait.UntilAsync(
            async () => (await engine.Alarms.ListAssetAsync("acme", "train-1", Ct)).Value.FirstOrDefault(),
            Ct);

        // Act
        await engine.DeleteAsync("acme", "AssetType", "train", 4);
        await Task.Delay(500, Ct);

        // Assert
        (await engine.Alarms.ListAssetAsync("acme", "train-1", Ct)).Value.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Asset_ShouldKeepItsAlarms_WhenItIsReactivated()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low()), 2);
        await WriteAsync(engine, 2.5);
        var raised = await RaisedAsync(engine);

        // Act
        await engine.DeactivateAllAsync();

        // Assert
        var alarm = await Eventually(engine, alarm => alarm.Id == raised.Id);
        alarm.ActiveCount.ShouldBe(1);
        (await LiveAsync(engine)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Asset_ShouldRaiseTheAlarmAfterFailover_WhenAForWaitWasRunning()
    {
        // Arrange
        await using var engine = await StartAsync();
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low("for", "\"2s\"")), 2);
        await WriteAsync(engine, 2.5);

        // Act
        await engine.DeactivateAllAsync();

        // Assert
        var alarm = await RaisedAsync(engine);
        alarm.IsCleared.ShouldBeFalse();
    }

    [Fact]
    public async Task Asset_ShouldReportLiveAlarmCountsPerPriority_WhenAlarmsAreLive()
    {
        // Arrange
        await using var engine = await StartAsync();

        // Act
        await engine.ApplyAsync("acme", "Asset", "train", Train(Low()), 2);
        await WriteAsync(engine, 2.5);
        await RaisedAsync(engine);

        // Assert
        var status = await engine.Status.WaitForAsync(
            "acme",
            "Asset",
            "train",
            status => status.GetProperty("alarms").GetProperty("high").GetInt32() == 1,
            Ct);
        status.GetProperty("alarms").GetProperty("low").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task ListLiveAsync_ShouldSortByPriorityThenTimeRaised_WhenAlarmsDiffer()
    {
        // Arrange
        await using var engine = await StartAsync();
        var spec = Train(
            Low("name", "\"first-low\"", "priority", "\"low\""),
            Low("name", "\"second-critical\"", "priority", "\"critical\""),
            Low("name", "\"third-low\"", "priority", "\"low\""));
        await engine.ApplyAsync("acme", "Asset", "train", spec, 2);

        // Act
        await WriteAsync(engine, 2.5);
        await Until(async () => (await engine.Alarms.ListLiveAsync("acme", Ct)).Count == 3);

        // Assert
        var live = await engine.Alarms.ListLiveAsync("acme", Ct);
        live[0].Condition.ShouldBe("second-critical");
        live.Skip(1).Select(alarm => alarm.Priority).ShouldAllBe(priority => priority == AlarmPriority.Low);
    }

    private async Task<EngineCluster> StartAsync() =>
        await EngineCluster.StartAsync(alarmDatabase: await postgres.CreateDatabaseAsync());

    private static string Train(params string[] alarms) =>
        $$"""{"dataPoints": [{"name": "pressure", "type": "number"}], "alarms": [{{string.Join(", ", alarms)}}]}""";

    /// <summary>
    /// A condition on pressure below 3 bar, with fields replaced from pairs of name and JSON value.
    /// </summary>
    private static string Low(params string[] overrides)
    {
        var fields = new Dictionary<string, string>
        {
            ["name"] = "\"low\"",
            ["dataPoint"] = "\"pressure\"",
            ["operator"] = "\"<\"",
            ["value"] = "3",
            ["priority"] = "\"high\"",
            ["message"] = "\"Brake pressure below 3 bar\""
        };
        for (var index = 0; index < overrides.Length; index += 2)
        {
            fields[overrides[index]] = overrides[index + 1];
        }

        return "{" + string.Join(", ", fields.Select(field => $"\"{field.Key}\": {field.Value}")) + "}";
    }

    private static async Task WriteAsync(EngineCluster engine, double value)
    {
        var result = await engine.Assets.WriteDataPointAsync(
            "acme",
            "train",
            "pressure",
            System.Text.Json.JsonDocument.Parse(value.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .RootElement,
            Ct);
        result.IsSuccess.ShouldBeTrue();
    }

    private static async Task<IReadOnlyList<Alarm>> LiveAsync(EngineCluster engine) =>
        (await engine.Alarms.ListAssetAsync("acme", "train", Ct)).Value;

    private static Task<Alarm> RaisedAsync(EngineCluster engine) => Eventually(engine, _ => true);

    private static Task<Alarm> Eventually(EngineCluster engine, Func<Alarm, bool> condition) =>
        Wait.UntilAsync(async () => (await LiveAsync(engine)).FirstOrDefault(condition), Ct);

    private static async Task Until(Func<Task<bool>> condition) =>
        await Wait.UntilAsync(async () => await condition() ? new object() : null, Ct);
}
