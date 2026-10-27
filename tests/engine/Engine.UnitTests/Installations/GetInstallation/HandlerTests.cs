using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Engine.Application.Authorization;
using Engine.Application.Installations;
using Engine.Application.Installations.GetInstallation;
using Engine.Domain.Installations;
using Xunit;

namespace Engine.UnitTests.Installations.GetInstallation;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnTheInstallation_WhenTheCallerHoldsARole()
    {
        // Arrange
        var installation = new Installation(Guid.NewGuid(), "0.1.0");
        var identity = Substitute.For<IInstallationIdentity>();
        identity.Current.Returns(installation);
        var handler = new Handler(Grants(Result.Updated()), identity);

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBe(installation);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheRefusal_WhenTheCallerHoldsNoRole()
    {
        // Arrange
        var refusal = Error.Custom("access.forbidden", "No role.");
        var handler = new Handler(Grants(refusal), Substitute.For<IInstallationIdentity>());

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
