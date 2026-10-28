using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Core.Domain.Authorization;
using Engine.Application.Authorization;
using Engine.Application.Flows;
using Engine.Application.Flows.CancelFlowExecutions;
using Xunit;

namespace Engine.UnitTests.Flows.CancelFlowExecutions;

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
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "acme", "cooling"), Ct);

        // Assert
        result.Error.ShouldBe(refusal);
        await flows.DidNotReceiveWithAnyArgs().CancelAllAsync(default!, default!, Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnHowManyWereCancelled_WhenTheCallerIsAnEngineer()
    {
        // Arrange
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireRoleAsync(Arg.Any<Guid>(), "acme", NamespaceRole.Engineer, Ct).Returns(Result.Updated());
        var flows = Substitute.For<IFlowRuntime>();
        flows.CancelAllAsync("acme", "cooling", Ct).Returns(Result.Found(3));
        var handler = new Handler(grants, flows);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "acme", "cooling"), Ct);

        // Assert
        result.Value.ShouldBe(3);
    }
}
