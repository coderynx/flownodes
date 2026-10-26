using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Engine.Application.Authorization;
using Engine.Application.Integrations;
using Engine.Application.Integrations.ListIntegrations;
using Engine.Domain.Integrations;
using Xunit;

namespace Engine.UnitTests.Integrations.ListIntegrations;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnLoadedAndFailedIntegrations_WhenTheSiloFoundBoth()
    {
        // Arrange
        IntegrationId.TryCreate("flownodes.mqtt", out var id);
        var loaded = InstalledIntegration.Loaded("mqtt", id!, "1.2.0", "0.1.0", ["Broker"]);
        var failed = InstalledIntegration.Failed(
            "broken",
            IntegrationLoadFailure.NotAnIntegration,
            "No entry assembly.");
        var installed = Substitute.For<IInstalledIntegrations>();
        installed.All.Returns([failed, loaded]);
        var handler = new Handler(Grants(Result.Updated()), installed);

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe([failed, loaded]);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNothing_WhenNoIntegrationIsInstalled()
    {
        // Arrange
        var installed = Substitute.For<IInstalledIntegrations>();
        installed.All.Returns([]);
        var handler = new Handler(Grants(Result.Updated()), installed);

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheRefusal_WhenTheCallerHoldsNoRole()
    {
        // Arrange
        var refusal = Error.Custom("access.forbidden", "No role.");
        var installed = Substitute.For<IInstalledIntegrations>();
        var handler = new Handler(Grants(refusal), installed);

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert
        result.Error.ShouldBe(refusal);
    }

    private static IRoleGrants Grants(Result outcome)
    {
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireAnyMembershipAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(outcome);

        return grants;
    }
}
