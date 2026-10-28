using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Core.Domain.Authorization;
using Engine.Application.Authorization;
using Engine.Application.Flows;
using Engine.Application.Flows.CancelFlowExecution;
using Xunit;

namespace Engine.UnitTests.Flows.CancelFlowExecution;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldNotCancel_WhenTheCallerIsNoEngineer()
    {
        // Arrange
        var refusal = Error.Custom("access.forbidden", "This needs the Engineer role.");
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireRoleAsync(Arg.Any<Guid>(), "acme", NamespaceRole.Engineer, Ct).Returns(refusal);
        var flows = Substitute.For<IFlowRuntime>();
        var handler = new Handler(grants, flows);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "acme", "cooling", Guid.NewGuid()), Ct);

        // Assert
        result.Error.ShouldBe(refusal);
        await flows.DidNotReceiveWithAnyArgs().CancelAsync(default!, default!, Guid.Empty, Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldCancelTheExecution_WhenTheCallerIsAnEngineer()
    {
        // Arrange
        var id = Guid.NewGuid();
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireRoleAsync(Arg.Any<Guid>(), "acme", NamespaceRole.Engineer, Ct).Returns(Result.Updated());
        var flows = Substitute.For<IFlowRuntime>();
        flows.CancelAsync("acme", "cooling", id, Ct).Returns(Result.Updated());
        var handler = new Handler(grants, flows);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "acme", "cooling", id), Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await flows.Received().CancelAsync("acme", "cooling", id, Ct);
    }
}
