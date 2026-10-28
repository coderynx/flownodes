using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Core.Domain.Authorization;
using Engine.Application.Authorization;
using Engine.Application.Flows;
using Engine.Application.Flows.RunFlow;
using Xunit;

namespace Engine.UnitTests.Flows.RunFlow;

public sealed class HandlerTests
{
    private static readonly JsonElement Payload = JsonDocument.Parse("""{"value": 1}""").RootElement;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldNotRun_WhenTheCallerIsNoEngineer()
    {
        // Arrange
        var refusal = Error.Custom("access.forbidden", "This needs the Engineer role.");
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireRoleAsync(Arg.Any<Guid>(), "acme", NamespaceRole.Engineer, Ct).Returns(refusal);
        var flows = Substitute.For<IFlowRuntime>();
        var handler = new Handler(grants, flows);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "acme", "cooling", null, Payload), Ct);

        // Assert
        result.Error.ShouldBe(refusal);
        await flows.DidNotReceiveWithAnyArgs().RunAsync(default!, default!, default, default, Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRunTheTriggerWithThePayload_WhenTheCallerIsAnEngineer()
    {
        // Arrange
        var run = new FlowRun(Guid.NewGuid(), false);
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireRoleAsync(Arg.Any<Guid>(), "acme", NamespaceRole.Engineer, Ct).Returns(Result.Updated());
        var flows = Substitute.For<IFlowRuntime>();
        flows.RunAsync("acme", "cooling", "manual", Payload, Ct).Returns(Result.Found(run));
        var handler = new Handler(grants, flows);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "acme", "cooling", "manual", Payload), Ct);

        // Assert
        result.Value.ShouldBe(run);
    }
}
