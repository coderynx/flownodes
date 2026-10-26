using Coderynx.Functional.Results.Errors;
using Engine.Application.Authorization;
using Engine.Application.Devices;
using Engine.Application.Devices.ListDataPoints;
using Xunit;

namespace Engine.UnitTests.Devices.ListDataPoints;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldNotReadTheDevice_WhenTheCallerIsNotAMemberOfTheNamespace()
    {
        // Arrange
        var refusal = Error.NotFound("namespace.not_found", "No such Namespace.");
        var token = TestContext.Current.CancellationToken;
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireMemberAsync(Arg.Any<Guid>(), "acme", token).Returns(refusal);
        var devices = Substitute.For<IDeviceRuntime>();
        var handler = new Handler(grants, devices);

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid(), "acme", "t1"), token);

        // Assert
        result.Error.ShouldBe(refusal);
        await devices.DidNotReceiveWithAnyArgs().ListDataPointsAsync(default!, default!, token);
    }
}
