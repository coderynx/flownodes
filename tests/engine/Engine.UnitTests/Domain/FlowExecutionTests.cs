using System.Text.Json;
using System.Text.Json.Nodes;
using Engine.Domain.Flows;
using Xunit;

namespace Engine.UnitTests.Domain;

public sealed class FlowExecutionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_ShouldKeepThePayloadAndMoveToTheNextNode_WhenTheTriggerLeadsSomewhere()
    {
        // Act
        var execution = Start(Flow(Read("read", "check"), Branch("check", "big", "small")));

        // Assert
        execution.State.ShouldBe(FlowExecutionState.Running);
        execution.CurrentNode.ShouldBe("read");
        execution.Variable("trigger.value")!.GetValue<double>().ShouldBe(7);
        execution.Steps.ShouldBe(1);
    }

    [Fact]
    public void Start_ShouldSucceedAtOnce_WhenTheTriggerLeadsNowhere()
    {
        // Act
        var execution = Start("""{"nodes": [{"id": "start", "type": "trigger"}]}""");

        // Assert
        execution.State.ShouldBe(FlowExecutionState.Succeeded);
        execution.FinishedAt.ShouldBe(T0);
    }

    [Fact]
    public void Complete_ShouldStoreTheOutputAndAdvance_WhenTheNodeKeepsAValue()
    {
        // Arrange
        var execution = Start(Flow(Read("read", "check"), Branch("check", "big", "small")));

        // Act
        execution.Complete(JsonNode.Parse("""{"value": 42}"""), T0.AddSeconds(1));

        // Assert
        execution.CurrentNode.ShouldBe("check");
        execution.Variable("reading.value")!.GetValue<double>().ShouldBe(42);
        execution.Steps.ShouldBe(2);
    }

    [Theory]
    [InlineData(42, "big")]
    [InlineData(3, "small")]
    public void Branch_ShouldTakeTheEdgeOfTheOutcome_WhenTheComparisonIsEvaluated(double value, string expected)
    {
        // Arrange
        var execution = Start(Flow(Read("read", "check"), Branch("check", "big", "small"), Wait("big"), Wait("small")));
        execution.Complete(JsonNode.Parse($$"""{"value": {{value}}}"""), T0);

        // Act
        execution.Branch(T0);

        // Assert
        execution.CurrentNode.ShouldBe(expected);
    }

    [Fact]
    public void Branch_ShouldTakeTheElseEdge_WhenTheVariableIsMissing()
    {
        // Arrange
        var execution = Start(Flow(Branch("read", "big", "small"), Wait("big"), Wait("small")));

        // Act
        execution.Branch(T0);

        // Assert
        execution.CurrentNode.ShouldBe("small");
    }

    [Fact]
    public void Fail_ShouldFollowTheErrorEdgeWithTheError_WhenTheNodeWiresOne()
    {
        // Arrange
        var execution = Start(
            Flow(
                """{"id": "read", "type": "read", "dataPoint": {"asset": "a", "name": "b"}, "error": "recover"}""",
                Wait("recover")));

        // Act
        execution.Fail("ReferenceMissing", "Asset 'a' is not there.", 1, T0);

        // Assert
        execution.CurrentNode.ShouldBe("recover");
        execution.Variable("error.code")!.GetValue<string>().ShouldBe("ReferenceMissing");
        execution.Variable("error.node")!.GetValue<string>().ShouldBe("read");
    }

    [Fact]
    public void Fail_ShouldEndTheExecutionFailed_WhenTheNodeWiresNoErrorEdge()
    {
        // Arrange
        var execution = Start(Flow(Read("read", null)));

        // Act
        execution.Fail("Timeout", "The read timed out.", 3, T0.AddSeconds(30));

        // Assert
        execution.State.ShouldBe(FlowExecutionState.Failed);
        execution.Error.ShouldBe(new FlowExecutionError("Timeout", "The read timed out.", "read", 3));
        execution.FinishedAt.ShouldBe(T0.AddSeconds(30));
    }

    [Fact]
    public void InvocationId_ShouldStayTheSame_WhenTheNodeHasNotCompleted()
    {
        // Arrange
        var execution = Start(
            Flow(
                """{"id": "read", "type": "invoke", "action": {"device": "d", "name": "x"}, "next": "b"}""",
                """{"id": "b", "type": "branch", "if": {"from": "trigger.value", "operator": ">", "value": 0}, "then": "read"}"""));
        var first = execution.InvocationId;

        // Act
        execution.Complete(null, T0);
        execution.Branch(T0);

        // Assert
        first.ShouldBe($"{execution.Id}/read/1");
        execution.InvocationId.ShouldBe($"{execution.Id}/read/2");
    }

    [Fact]
    public void Wait_ShouldWaitUntilTheDeadline_WhenTheNodeWaitsForADuration()
    {
        // Arrange
        var execution = Start(Flow(Wait("read")));

        // Act
        execution.Wait(T0);

        // Assert
        execution.State.ShouldBe(FlowExecutionState.Waiting);
        execution.Deadline.ShouldBe(T0.AddMinutes(5));
    }

    [Fact]
    public void Elapse_ShouldContinueAndRecordTheLateness_WhenTheWaitIsOverdueWithinItsMaxLateness()
    {
        // Arrange
        var execution = Start(
            Flow("""{"id": "read", "type": "wait", "for": "5m", "maxLateness": "1m", "next": "end"}""", Wait("end")));
        execution.Wait(T0);

        // Act
        execution.Elapse(T0.AddMinutes(5).AddSeconds(20));

        // Assert
        execution.CurrentNode.ShouldBe("end");
        execution.Lateness.ShouldBe(TimeSpan.FromSeconds(20));
    }

    [Fact]
    public void Elapse_ShouldTakeTheTimedOutEdge_WhenTheWaitIsPastItsMaxLateness()
    {
        // Arrange
        var execution = Start(
            Flow(
                """{"id": "read", "type": "wait", "for": "5m", "maxLateness": "1m", "next": "end", "timedOut": "late"}""",
                Wait("end"),
                Wait("late")));
        execution.Wait(T0);

        // Act
        execution.Elapse(T0.AddMinutes(7));

        // Assert
        execution.CurrentNode.ShouldBe("late");
    }

    [Fact]
    public void Elapse_ShouldFailWithTimeout_WhenAConditionWaitTimesOutWithNoEdge()
    {
        // Arrange
        var execution = Start(
            Flow(
                """
                {"id": "read", "type": "wait", "timeout": "1m",
                 "until": {"dataPoint": {"asset": "a", "name": "b"}, "operator": ">", "value": 1}}
                """));
        execution.Wait(T0);

        // Act
        execution.Elapse(T0.AddMinutes(1));

        // Assert
        execution.State.ShouldBe(FlowExecutionState.Failed);
        execution.Error!.Code.ShouldBe("Timeout");
    }

    [Fact]
    public void Complete_ShouldKeepTheEventAndContinue_WhenAWaitedEventArrivesBeforeTheTimeout()
    {
        // Arrange
        var execution = Start(
            Flow(
                """{"id": "read", "type": "wait", "timeout": "1m", "event": {"device": "d", "name": "e"}, "into": "press", "next": "end"}""",
                Wait("end")));
        execution.Wait(T0);

        // Act
        execution.Complete(JsonNode.Parse("""{"count": 2}"""), T0.AddSeconds(10));

        // Assert
        execution.CurrentNode.ShouldBe("end");
        execution.Deadline.ShouldBeNull();
        execution.Variable("press.count")!.GetValue<int>().ShouldBe(2);
    }

    [Fact]
    public void Branch_ShouldFailTheExecution_WhenACycleReachesTheStepLimit()
    {
        // Arrange
        var execution = Start(
            Flow("""{"id": "read", "type": "branch", "if": {"from": "trigger.value", "operator": ">", "value": 0}, "then": "read"}"""));

        // Act
        while (!execution.IsFinished)
        {
            execution.Branch(T0);
        }

        // Assert
        execution.Error!.Code.ShouldBe("StepLimit");
        execution.Steps.ShouldBe(1000);
    }

    [Fact]
    public void Branch_ShouldFailTheExecution_WhenACycleReachesTheFlowsOwnStepLimit()
    {
        // Arrange
        var execution = Start(
            """
            {"limits": {"steps": 5}, "nodes": [{"id": "start", "type": "trigger", "next": "read"},
              {"id": "read", "type": "branch", "if": {"from": "trigger.value", "operator": ">", "value": 0}, "then": "read"}]}
            """);

        // Act
        while (!execution.IsFinished)
        {
            execution.Branch(T0);
        }

        // Assert
        execution.Error!.Code.ShouldBe("StepLimit");
        execution.Steps.ShouldBe(5);
    }

    [Fact]
    public void Complete_ShouldFailTheExecution_WhenTheVariablesOutgrowTheLimit()
    {
        // Arrange
        var execution = Start(Flow(Read("read", null)));

        // Act
        execution.Complete(JsonValue.Create(new string('x', 64 * 1024)), T0);

        // Assert
        execution.State.ShouldBe(FlowExecutionState.Failed);
        execution.Error!.Code.ShouldBe("VariablesTooLarge");
    }

    [Fact]
    public void Cancel_ShouldEndTheExecutionCancelled_WhenItIsWaiting()
    {
        // Arrange
        var execution = Start(Flow(Wait("read")));
        execution.Wait(T0);

        // Act
        execution.Cancel(T0.AddSeconds(1));

        // Assert
        execution.State.ShouldBe(FlowExecutionState.Cancelled);
        execution.Deadline.ShouldBeNull();
        execution.IsFinished.ShouldBeTrue();
    }

    [Theory]
    [InlineData(ExecutionMode.Single, 0, 0, FlowAdmission.Start)]
    [InlineData(ExecutionMode.Single, 1, 0, FlowAdmission.Drop)]
    [InlineData(ExecutionMode.Restart, 1, 0, FlowAdmission.Restart)]
    [InlineData(ExecutionMode.Queue, 0, 0, FlowAdmission.Start)]
    [InlineData(ExecutionMode.Queue, 1, 3, FlowAdmission.Queue)]
    [InlineData(ExecutionMode.Queue, 1, 10, FlowAdmission.Drop)]
    [InlineData(ExecutionMode.Parallel, 9, 0, FlowAdmission.Start)]
    [InlineData(ExecutionMode.Parallel, 10, 0, FlowAdmission.Drop)]
    public void Admit_ShouldDecideByTheExecutionMode_WhenTriggeredWhileExecutionsRun(
        ExecutionMode mode,
        int running,
        int queued,
        FlowAdmission expected)
    {
        // Arrange
        var graph = FlowGraph.Create(
            Json($$"""{"executionMode": "{{mode.ToString().ToLowerInvariant()}}", "nodes": [{"id": "t", "type": "trigger"}]}"""));

        // Act
        var admission = graph.Admit(running, queued);

        // Assert
        admission.ShouldBe(expected);
    }

    [Fact]
    public void Create_ShouldReadTheExecutionModeAndTriggers_WhenTheSpecIsValid()
    {
        // Act
        var definition = FlowGraph.Create(
            Json(
                """
                {"executionMode": "parallel", "nodes": [
                  {"id": "t", "type": "trigger", "schedule": {"cron": "0 * * * *", "maxLateness": "5m"}, "next": "w"},
                  {"id": "w", "type": "write", "dataPoint": {"device": "d", "name": "on"}, "value": true, "timeout": "2s",
                   "retry": {"times": 2, "delay": "100ms"}}]}
                """));

        // Assert
        definition.Mode.ShouldBe(ExecutionMode.Parallel);
        var trigger = definition.Triggers.ShouldHaveSingleItem();
        trigger.Schedule!.MaxLateness.ShouldBe(TimeSpan.FromMinutes(5));
        var write = definition.Node("w").ShouldBeOfType<WriteNode>();
        write.Policy.ShouldBe(new CallPolicy(null, TimeSpan.FromSeconds(2), 2, TimeSpan.FromMilliseconds(100)));
        write.DataPoint.ShouldBe(new DataPointRef(DataPointOwner.Device, "d", "on"));
    }

    private static FlowExecution Start(string spec) =>
        FlowExecution.Start(
            Guid.NewGuid(),
            "acme",
            "cooling",
            Guid.NewGuid(),
            1,
            spec,
            "start",
            JsonNode.Parse("""{"value": 7}"""),
            T0);

    private static string Flow(params string[] nodes) =>
        $$"""{"nodes": [{"id": "start", "type": "trigger", "next": "read"}, {{string.Join(", ", nodes)}}]}""";

    private static string Read(string id, string? next) =>
        $$"""{"id": "{{id}}", "type": "read", "dataPoint": {"asset": "room", "name": "t"}, "into": "reading"{{Edge("next", next)}}}""";

    private static string Branch(string id, string then, string otherwise) =>
        $$"""
          {"id": "{{id}}", "type": "branch", "if": {"from": "reading.value", "operator": ">", "value": 10},
           "then": "{{then}}", "else": "{{otherwise}}"}
          """;

    private static string Wait(string id) => $$"""{"id": "{{id}}", "type": "wait", "for": "5m"}""";
    private static string Edge(string name, string? target) => target is null ? "" : $", \"{name}\": \"{target}\"";
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement.Clone();
}
