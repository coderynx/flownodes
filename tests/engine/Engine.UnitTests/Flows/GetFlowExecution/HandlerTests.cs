using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Engine.Application.Authorization;
using Engine.Application.Flows;
using Engine.Application.Flows.GetFlowExecution;
using Engine.Domain.Flows;
using Xunit;

namespace Engine.UnitTests.Flows.GetFlowExecution;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReadNothing_WhenTheCallerHoldsNoRoleInTheNamespace()
    {
        // Arrange
        var refusal = Error.Custom("access.forbidden", "This needs a role in the Namespace.");
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireMemberAsync(Arg.Any<Guid>(), "acme", Ct).Returns(refusal);
        var flows = Substitute.For<IFlowRuntime>();
        var handler = new Handler(grants, flows);

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid(), "acme", "cooling", Guid.NewGuid()), Ct);

        // Assert
        result.Error.ShouldBe(refusal);
        await flows.DidNotReceiveWithAnyArgs().GetAsync(default!, default!, Guid.Empty, Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheExecution_WhenTheCallerHoldsARole()
    {
        // Arrange
        var execution = FlowExecution.Start(
            Guid.NewGuid(),
            "acme",
            "cooling",
            Guid.NewGuid(),
            1,
            """{"nodes": [{"id": "start", "type": "trigger"}]}""",
            "start",
            null,
            DateTimeOffset.UnixEpoch);
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireMemberAsync(Arg.Any<Guid>(), "acme", Ct).Returns(Result.Updated());
        var flows = Substitute.For<IFlowRuntime>();
        flows.GetAsync("acme", "cooling", execution.Id, Ct).Returns(Result.Found(execution));
        var handler = new Handler(grants, flows);

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid(), "acme", "cooling", execution.Id), Ct);

        // Assert
        result.Value.ShouldBe(execution);
    }
}
