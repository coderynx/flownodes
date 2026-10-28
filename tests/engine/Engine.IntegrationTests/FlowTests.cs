using System.Text.Json;
using Engine.Application.Flows;
using Engine.Domain.Flows;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class FlowTests(PostgresFixture postgres)
{
    private const string Simulator = """{"integration": "flownodes.simulator", "config": {"units": 2}}""";
    private const string Manual = """{"id": "start", "type": "trigger", "next": "first"}""";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Flow_ShouldRunItsNodes_WhenAnAssetDataPointChanges()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(
            engine,
            """{"id": "start", "type": "trigger", "dataPoint": {"asset": "room", "name": "target"}, "next": "first"}""",
            WriteLamp("first", "true"));

        // Act
        await WriteTargetAsync(engine, 5);

        // Assert
        await Until(async () => await LampAsync(engine) == true);
        var execution = (await FinishedAsync(engine, 1)).Single();
        execution.State.ShouldBe(FlowExecutionState.Succeeded);
        execution.Steps.ShouldBe(2);
        execution.Variable("trigger.value")!.GetValue<double>().ShouldBe(5);
    }

    [Fact]
    public async Task Flow_ShouldTriggerOnlyOnTheTurnToTrue_WhenTheTriggerHasACondition()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(
            engine,
            """
            {"id": "start", "type": "trigger", "dataPoint": {"asset": "room", "name": "target"},
             "when": {"operator": ">", "value": 25}}
            """);

        // Act
        foreach (var value in new[] { 10, 30, 31, 10, 30 })
        {
            await WriteTargetAsync(engine, value);
            await Task.Delay(200, Ct);
        }

        // Assert
        var executions = await FinishedAsync(engine, 2);
        await Task.Delay(500, Ct);
        (await ListAsync(engine)).Count.ShouldBe(2);
        executions.ShouldAllBe(execution => execution.Variable("trigger.value")!.GetValue<int>() == 30);
    }

    [Fact]
    public async Task Flow_ShouldRunWithTheEventPayload_WhenADeviceReportsTheEvent()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(
            engine,
            """{"id": "start", "type": "trigger", "event": {"device": "lamp", "name": "toggled"}, "next": "first"}""",
            WriteTarget("first", "7"));

        // Act
        await engine.Devices.InvokeActionAsync("acme", "lamp", "toggle", Json("{}"), Ct);

        // Assert
        await Until(async () => await TargetAsync(engine) == 7);
        var execution = (await FinishedAsync(engine, 1)).Single();
        execution.Variable("trigger.payload.on")!.GetValue<bool>().ShouldBeTrue();
    }

    [Fact]
    public async Task Flow_ShouldRunOnEveryFire_WhenTheTriggerIsASchedule()
    {
        // Arrange
        await using var engine = await StartAsync();

        // Act
        await ApplyFlowAsync(engine, """{"id": "start", "type": "trigger", "schedule": {"cron": "* * * * * *"}}""");

        // Assert
        var executions = await FinishedAsync(engine, 2);
        executions.ShouldAllBe(execution => execution.Variable("trigger.scheduledAt") != null);
    }

    [Fact]
    public async Task Flow_ShouldBranchOnTheActionResult_WhenItInvokesAnAction()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(
            engine,
            Manual,
            """
            {"id": "first", "type": "invoke", "action": {"device": "t1", "name": "set"},
             "parameters": {"value": 33}, "into": "result", "next": "check"}
            """,
            """
            {"id": "check", "type": "branch", "if": {"from": "result.temperature", "operator": ">", "value": 30},
             "then": "hot", "else": "cold"}
            """,
            WriteTarget("hot", "1"),
            WriteTarget("cold", "0"));

        // Act
        var run = await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Assert
        run.IsSuccess.ShouldBeTrue();
        await Until(async () => await TargetAsync(engine) == 1);
        var execution = (await FinishedAsync(engine, 1)).Single();
        execution.Variable("result.temperature")!.GetValue<double>().ShouldBe(33);
        execution.Visits.ShouldContainKey("hot");
    }

    [Fact]
    public async Task Flow_ShouldWriteTheResultToAnAsset_WhenItInvokesAnActionOfAnExternalService()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(
            engine,
            Manual,
            """
            {"id": "first", "type": "invoke", "action": {"connection": "sim", "name": "forecast"},
             "parameters": {"city": "Rome"}, "into": "weather", "next": "copy"}
            """,
            """
            {"id": "copy", "type": "write", "dataPoint": {"asset": "room", "name": "target"},
             "from": "weather.temperature"}
            """);

        // Act
        await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Assert
        await Until(async () => await TargetAsync(engine) == 18);
        var execution = (await FinishedAsync(engine, 1)).Single();
        execution.State.ShouldBe(FlowExecutionState.Succeeded);
        execution.Variable("weather.city")!.GetValue<string>().ShouldBe("Rome");
    }

    [Fact]
    public async Task Flow_ShouldRunWithTheEventPayload_WhenAnExternalServiceReportsTheEvent()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(
            engine,
            6,
            1,
            "parallel",
            Announce("kick"),
            """
            {"id": "heard", "type": "trigger", "event": {"connection": "sim", "name": "announced"}, "next": "copy"}
            """,
            """{"id": "copy", "type": "write", "dataPoint": {"asset": "room", "name": "target"}, "from": "trigger.payload.value"}""");

        // Act
        await engine.Flows.StartAsync("acme", "cooling", "kick", Json("{}"), Ct);

        // Assert
        await Until(async () => await TargetAsync(engine) == 4);
        var executions = await FinishedAsync(engine, 2);
        executions.ShouldAllBe(execution => execution.State == FlowExecutionState.Succeeded);
        executions.Single(execution => execution.Trigger == "heard").Variable("trigger.event")!.GetValue<string>()
            .ShouldBe("announced");
    }

    [Fact]
    public async Task Flow_ShouldContinueWithThePayload_WhenTheExternalServiceEventItWaitsForArrives()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(
            engine,
            6,
            1,
            "parallel",
            """{"id": "start", "type": "trigger", "next": "first"}""",
            """
            {"id": "first", "type": "wait", "timeout": "30s", "into": "heard", "next": "copy",
             "event": {"connection": "sim", "name": "announced"}}
            """,
            """{"id": "copy", "type": "write", "dataPoint": {"asset": "room", "name": "target"}, "from": "heard.payload.value"}""",
            Announce("kick"));
        var run = await engine.Flows.StartAsync("acme", "cooling", "start", Json("{}"), Ct);
        await Until(async () => await StateAsync(engine, run.Value.Execution!.Value) == FlowExecutionState.Waiting);

        // Act
        await Until(async () =>
        {
            await engine.Flows.StartAsync("acme", "cooling", "kick", Json("{}"), Ct);
            await Task.Delay(200, Ct);

            return await TargetAsync(engine) == 4;
        });

        // Assert
        var execution = await GetAsync(engine, run.Value.Execution!.Value);
        execution.State.ShouldBe(FlowExecutionState.Succeeded);
        execution.Variable("heard.event")!.GetValue<string>().ShouldBe("announced");
    }

    [Fact]
    public async Task Flow_ShouldResumeItsWait_WhenEveryGrainIsDeactivated()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(engine, Manual, Pause("first", "\"3s\"", "then"), WriteTarget("then", "9"));
        var run = await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);
        await Until(async () => await StateAsync(engine, run.Value.Execution!.Value) == FlowExecutionState.Waiting);

        // Act
        await engine.DeactivateAllAsync();

        // Assert
        await Until(async () => await TargetAsync(engine) == 9);
        var execution = (await FinishedAsync(engine, 1)).Single();
        execution.State.ShouldBe(FlowExecutionState.Succeeded);
        execution.Steps.ShouldBe(3);
    }

    [Fact]
    public async Task Flow_ShouldKeepRunningItsGeneration_WhenTheFlowChangesMidway()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(engine, Manual, Pause("first", "\"1s\"", "then"), WriteTarget("then", "1"));
        await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Act
        await ApplyFlowAsync(engine, 7, 2, null, Manual, Pause("first", "\"1s\"", "then"), WriteTarget("then", "2"));

        // Assert
        var execution = (await FinishedAsync(engine, 1)).Single();
        execution.Generation.ShouldBe(1);
        (await TargetAsync(engine)).ShouldBe(1);
    }

    [Fact]
    public async Task Flow_ShouldContinue_WhenTheConditionItWaitsForHolds()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(
            engine,
            Manual,
            """
            {"id": "first", "type": "wait", "timeout": "30s", "next": "then",
             "until": {"dataPoint": {"asset": "room", "name": "target"}, "operator": ">", "value": 100}}
            """,
            WriteLamp("then", "true"));
        var run = await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);
        await Until(async () => await StateAsync(engine, run.Value.Execution!.Value) == FlowExecutionState.Waiting);

        // Act
        await WriteTargetAsync(engine, 150);

        // Assert
        await Until(async () => await LampAsync(engine) == true);
        (await FinishedAsync(engine, 1)).Single().State.ShouldBe(FlowExecutionState.Succeeded);
    }

    [Fact]
    public async Task Flow_ShouldTakeTheTimedOutEdge_WhenTheConditionItWaitsForNeverHolds()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(
            engine,
            Manual,
            """
            {"id": "first", "type": "wait", "timeout": "1s", "timedOut": "late",
             "until": {"dataPoint": {"asset": "room", "name": "target"}, "operator": ">", "value": 100}}
            """,
            WriteTarget("late", "2"));

        // Act
        await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Assert
        await Until(async () => await TargetAsync(engine) == 2);
        (await FinishedAsync(engine, 1)).Single().Visits.ShouldContainKey("late");
    }

    [Fact]
    public async Task Flow_ShouldFailWithReferenceMissing_WhenANodeNamesADeviceThatIsNotThere()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(
            engine,
            Manual,
            """{"id": "first", "type": "write", "dataPoint": {"device": "ghost", "name": "on"}, "value": true}""");

        // Act
        await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Assert
        var execution = (await FinishedAsync(engine, 1)).Single();
        execution.State.ShouldBe(FlowExecutionState.Failed);
        execution.Error!.Code.ShouldBe("ReferenceMissing");
        execution.Error.Node.ShouldBe("first");
    }

    [Fact]
    public async Task RunAsync_ShouldDropAndRecordTheTrigger_WhenASingleModeFlowIsBusy()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(engine, Manual, Pause("first", "\"30s\""));
        await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Act
        var second = await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Assert
        second.Error.Code.ShouldBe("flow.busy");
        var executions = await ListAsync(engine);
        executions.ShouldContain(execution =>
            execution.State == FlowExecutionState.Dropped && execution.Error!.Code == "Busy");
        executions.ShouldContain(execution => execution.State == FlowExecutionState.Waiting);
    }

    [Fact]
    public async Task RunAsync_ShouldRunTheTriggerLater_WhenAQueueModeFlowIsBusy()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(engine, 6, 1, "queue", Manual, Pause("first", "\"1s\""));
        await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Act
        var second = await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Assert
        second.Value.Queued.ShouldBeTrue();
        var executions = await FinishedAsync(engine, 2);
        executions.ShouldAllBe(execution => execution.State == FlowExecutionState.Succeeded);
    }

    [Fact]
    public async Task RunAsync_ShouldCancelTheRunningExecution_WhenARestartModeFlowIsTriggeredAgain()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(engine, 6, 1, "restart", Manual, Pause("first", "\"30s\""));
        var first = await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Act
        var second = await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Assert
        second.Value.Execution.ShouldNotBe(first.Value.Execution);
        await Until(async () =>
            (await GetAsync(engine, first.Value.Execution!.Value)).State == FlowExecutionState.Cancelled);
        (await GetAsync(engine, second.Value.Execution!.Value)).State.ShouldBe(FlowExecutionState.Waiting);
    }

    [Fact]
    public async Task CancelAsync_ShouldCancelTheExecution_WhenItIsWaiting()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(engine, Manual, Pause("first", "\"30s\""));
        var run = await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);
        var id = run.Value.Execution!.Value;
        await Until(async () => (await GetAsync(engine, id)).State == FlowExecutionState.Waiting);

        // Act
        var result = await engine.Flows.CancelAsync("acme", "cooling", id, Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await Until(async () => (await GetAsync(engine, id)).State == FlowExecutionState.Cancelled);
        (await engine.Flows.CancelAsync("acme", "cooling", id, Ct)).Error.Code.ShouldBe("flow_execution.finished");
    }

    [Fact]
    public async Task DeleteAsync_ShouldCancelTheRunningExecutions_WhenTheFlowIsDeleted()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(engine, Manual, Pause("first", "\"30s\""));
        var run = await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Act
        await engine.DeleteAsync("acme", "Flow", "cooling", 7);

        // Assert
        await Until(async () =>
            (await GetAsync(engine, run.Value.Execution!.Value)).State == FlowExecutionState.Cancelled);
        (await engine.Flows.CancelAllAsync("acme", "cooling", Ct)).Error.Code.ShouldBe("flow.not_found");
    }

    [Fact]
    public async Task Flow_ShouldReportTheActiveCountAndTheLastResult_WhenExecutionsRunAndFinish()
    {
        // Arrange
        await using var engine = await StartAsync();
        await ApplyFlowAsync(engine, 6, 1, "parallel", Manual, Pause("first", "\"1s\""));

        // Act
        await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);
        await engine.Flows.StartAsync("acme", "cooling", null, Json("{}"), Ct);

        // Assert
        await engine.Status.WaitForAsync(
            "acme",
            "Flow",
            "cooling",
            status => status.GetProperty("activeExecutions").GetInt32() == 2,
            Ct);
        var settled = await engine.Status.WaitForAsync(
            "acme",
            "Flow",
            "cooling",
            status => status.GetProperty("activeExecutions").GetInt32() == 0,
            Ct);
        settled.GetProperty("lastResult").GetProperty("state").GetString().ShouldBe("succeeded");
        RecordingStatus.Reason(settled).ShouldBe("Resolved");
    }

    [Fact]
    public async Task PurgeAsync_ShouldKeepTheNewestAndTheRecent_WhenExecutionsAreOld()
    {
        // Arrange
        await using var engine = await StartAsync();
        var now = DateTimeOffset.UtcNow;
        var ages = new[] { 30, 20, 10, 1 };
        foreach (var days in ages)
        {
            await engine.FlowExecutions.SaveAsync(Dropped(now.AddDays(-days)), Ct);
        }

        // Act
        await engine.FlowExecutions.PurgeAsync(now.AddDays(-7), 2, Ct);

        // Assert
        var kept = (await ListAsync(engine)).Select(execution => (int)Math.Round((now - execution.StartedAt).TotalDays))
            .ToList();
        kept.ShouldBe([1, 10], true);
    }

    [Fact]
    public async Task ListAsync_ShouldFilterAndOrderTheExecutions_WhenAskedByStateAndFinish()
    {
        // Arrange
        await using var engine = await StartAsync();
        var now = DateTimeOffset.UtcNow;
        var older = Dropped(now.AddMinutes(-2));
        var newer = Dropped(now.AddMinutes(-1));
        var waiting = FlowExecution.Start(
            Guid.NewGuid(),
            "acme",
            "cooling",
            Guid.NewGuid(),
            1,
            """{"nodes": [{"id": "start", "type": "trigger", "next": "hold"}, {"id": "hold", "type": "wait", "for": "1h"}]}""",
            "start",
            null,
            now);
        foreach (var execution in new[] { older, waiting, newer })
        {
            await engine.FlowExecutions.SaveAsync(execution, Ct);
        }

        // Act
        var dropped = await engine.Flows.ListAsync(
            "acme",
            "cooling",
            "Dropped",
            FlowExecutionOrder.StartedAt,
            false,
            0,
            10,
            Ct);
        var byFinish = await engine.Flows.ListAsync(
            "acme",
            "cooling",
            null,
            FlowExecutionOrder.FinishedAt,
            true,
            0,
            10,
            Ct);
        var atNode = await engine.Flows.ListAsync(
            "acme",
            "cooling",
            "hol",
            FlowExecutionOrder.StartedAt,
            false,
            0,
            10,
            Ct);

        // Assert
        dropped.Items.Select(execution => execution.Id).ShouldBe([older.Id, newer.Id]);
        dropped.Total.ShouldBe(2);
        byFinish.Items.Select(execution => execution.Id).ShouldBe([newer.Id, older.Id, waiting.Id]);
        atNode.Items.ShouldHaveSingleItem().Id.ShouldBe(waiting.Id);
    }

    private static FlowExecution Dropped(DateTimeOffset at) =>
        FlowExecution.Drop(
            Guid.NewGuid(),
            "acme",
            "cooling",
            Guid.NewGuid(),
            1,
            """{"nodes": [{"id": "start", "type": "trigger"}]}""",
            "start",
            "Busy",
            "Dropped.",
            at);

    private async Task<EngineCluster> StartAsync()
    {
        var engine = await EngineCluster.StartAsync(flowDatabase: await postgres.CreateDatabaseAsync());
        await engine.ApplyAsync("acme", "Connection", "sim", Simulator, 2);
        await engine.ApplyAsync("acme", "Device", "lamp", Device("Switch", 1), 3);
        await engine.ApplyAsync("acme", "Device", "t1", Device("Thermometer", 1), 4);
        await engine.ApplyAsync(
            "acme",
            "Asset",
            "room",
            """{"dataPoints": [{"name": "target", "type": "number"}]}""",
            5);
        await Until(async () =>
            (await engine.Devices.ListDataPointsAsync("acme", "lamp", Ct)).Value.Any(point => point.Value is not null));

        return engine;
    }

    private static Task ApplyFlowAsync(EngineCluster engine, params string[] nodes) =>
        ApplyFlowAsync(engine, 6, 1, null, nodes);

    private static Task ApplyFlowAsync(
        EngineCluster engine,
        long seq,
        long generation,
        string? mode,
        params string[] nodes)
    {
        var executionMode = mode is null ? "" : $"\"executionMode\": \"{mode}\", ";

        return engine.ApplyAsync(
            "acme",
            "Flow",
            "cooling",
            $$"""{{{executionMode}}"nodes": [{{string.Join(", ", nodes)}}]}""",
            seq,
            generation);
    }

    private static string Announce(string trigger) =>
        $$$"""
           {"id": "{{{trigger}}}", "type": "trigger", "next": "{{{trigger}}}-call"},
           {"id": "{{{trigger}}}-call", "type": "invoke", "action": {"connection": "sim", "name": "announce"},
            "parameters": {"value": 4}}
           """;

    private static string WriteLamp(string id, string value) =>
        $$"""{"id": "{{id}}", "type": "write", "dataPoint": {"device": "lamp", "name": "on"}, "value": {{value}}}""";

    private static string WriteTarget(string id, string value) =>
        $$"""{"id": "{{id}}", "type": "write", "dataPoint": {"asset": "room", "name": "target"}, "value": {{value}}}""";

    private static string Pause(string id, string duration, string? next = null) =>
        next is null
            ? $$"""{"id": "{{id}}", "type": "wait", "for": {{duration}}}"""
            : $$"""{"id": "{{id}}", "type": "wait", "for": {{duration}}, "next": "{{next}}"}""";

    private static string Device(string type, int id) =>
        $$$"""{"connection": "sim", "type": "{{{type}}}", "address": {"id": {{{id}}}}}""";

    private static async Task WriteTargetAsync(EngineCluster engine, double value)
    {
        var result = await engine.Assets.WriteDataPointAsync(
            "acme",
            "room",
            "target",
            Json(value.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            Ct);
        result.IsSuccess.ShouldBeTrue();
    }

    private static async Task<int?> TargetAsync(EngineCluster engine) =>
        (await engine.Assets.ListDataPointsAsync("acme", "room", Ct)).Value.Single(point => point.Name == "target")
        .Value?.GetInt32();

    private static async Task<bool?> LampAsync(EngineCluster engine) =>
        (await engine.Devices.ListDataPointsAsync("acme", "lamp", Ct)).Value.Single(point => point.Name == "on")
        .Value?.GetBoolean();

    private static async Task<IReadOnlyList<FlowExecution>> ListAsync(EngineCluster engine) =>
        (await engine.Flows.ListAsync("acme", "cooling", null, FlowExecutionOrder.StartedAt, true, 0, 200, Ct)).Items;

    private static async Task<FlowExecution> GetAsync(EngineCluster engine, Guid id) =>
        (await engine.Flows.GetAsync("acme", "cooling", id, Ct)).Value;

    private static async Task<FlowExecutionState> StateAsync(EngineCluster engine, Guid id) =>
        (await GetAsync(engine, id)).State;

    private static Task<IReadOnlyList<FlowExecution>> FinishedAsync(EngineCluster engine, int count) =>
        Wait.UntilAsync<IReadOnlyList<FlowExecution>>(
            async () =>
            {
                var finished = (await ListAsync(engine)).Where(execution => execution.IsFinished).ToList();

                return finished.Count >= count ? finished : null;
            },
            Ct);

    private static Task Until(Func<Task<bool>> condition) =>
        Wait.UntilAsync(async () => await condition() ? new object() : null, Ct);

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
