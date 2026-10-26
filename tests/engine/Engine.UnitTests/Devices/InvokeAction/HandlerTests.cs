using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Core.Domain.Authorization;
using Engine.Application.Authorization;
using Engine.Application.Devices;
using Engine.Application.Devices.InvokeAction;
using Xunit;

namespace Engine.UnitTests.Devices.InvokeAction;

public sealed class HandlerTests
{
    private static readonly JsonElement Parameters = JsonSerializer.SerializeToElement(new { });

    [Fact]
    public async Task HandleAsync_ShouldCallTheDevice_WhenTheCallerIsAnOperator()
    {
        // Arrange
        var callerId = Guid.NewGuid();
        var token = TestContext.Current.CancellationToken;
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireRoleAsync(callerId, "acme", NamespaceRole.Operator, token).Returns(Result.Updated());
        var devices = Substitute.For<IDeviceRuntime>();
        devices.InvokeActionAsync("acme", "t1", "set", Parameters, token)
            .Returns(Result.Found(JsonSerializer.SerializeToElement(1)));
        var handler = new Handler(grants, devices);

        // Act
        var result = await handler.HandleAsync(new Command(callerId, "acme", "t1", "set", Parameters), token);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldNotCallTheDevice_WhenTheCallerLacksTheOperatorRole()
    {
        // Arrange
        var refusal = Error.Custom("access.forbidden", "Needs Operator.");
        var token = TestContext.Current.CancellationToken;
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireRoleAsync(Arg.Any<Guid>(), "acme", NamespaceRole.Operator, token).Returns(refusal);
        var devices = Substitute.For<IDeviceRuntime>();
        var handler = new Handler(grants, devices);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "acme", "t1", "set", Parameters), token);

        // Assert
        result.Error.ShouldBe(refusal);
        await devices.DidNotReceiveWithAnyArgs().InvokeActionAsync(default!, default!, default!, default, token);
    }
}
