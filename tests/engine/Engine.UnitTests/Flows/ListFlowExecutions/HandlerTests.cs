using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Engine.Application.Authorization;
using Engine.Application.Flows;
using Engine.Application.Flows.ListFlowExecutions;
using Xunit;

namespace Engine.UnitTests.Flows.ListFlowExecutions;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldListNothing_WhenTheCallerHoldsNoRoleInTheNamespace()
    {
        // Arrange
        var refusal = Error.Custom("access.forbidden", "This needs a role in the Namespace.");
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireMemberAsync(Arg.Any<Guid>(), "acme", Ct).Returns(refusal);
        var flows = Substitute.For<IFlowRuntime>();
        var handler = new Handler(grants, flows);

        // Act
        var result = await handler.HandleAsync(
            new Query(Guid.NewGuid(), "acme", "cooling", null, null, false, 0, 50),
            Ct);

        // Assert
        result.Error.ShouldBe(refusal);
        await flows.DidNotReceiveWithAnyArgs().ListAsync(default!, default!, null, default, false, 0, 0, Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldPassTheOrderAndClampTheWindow_WhenTheCallerAsksForTooMuch()
    {
        // Arrange
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireMemberAsync(Arg.Any<Guid>(), "acme", Ct).Returns(Result.Updated());
        var page = new FlowExecutionPage([], 0);
        var flows = Substitute.For<IFlowRuntime>();
        flows.ListAsync("acme", "cooling", "waiting", FlowExecutionOrder.FinishedAt, true, 0, 200, Ct).Returns(page);
        var handler = new Handler(grants, flows);

        // Act
        var result = await handler.HandleAsync(
            new Query(Guid.NewGuid(), "acme", "cooling", "waiting", "finishedAt", true, -5, 1000),
            Ct);

        // Assert
        result.Value.ShouldBe(page);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseTheSort_WhenTheKeyIsUnknown()
    {
        // Arrange
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireMemberAsync(Arg.Any<Guid>(), "acme", Ct).Returns(Result.Updated());
        var flows = Substitute.For<IFlowRuntime>();
        var handler = new Handler(grants, flows);

        // Act
        var result = await handler.HandleAsync(
            new Query(Guid.NewGuid(), "acme", "cooling", null, "colour", false, 0, 50),
            Ct);

        // Assert
        result.Error.Code.ShouldBe("flow_execution.unknown_sort");
        await flows.DidNotReceiveWithAnyArgs().ListAsync(default!, default!, null, default, false, 0, 0, Ct);
    }
}
