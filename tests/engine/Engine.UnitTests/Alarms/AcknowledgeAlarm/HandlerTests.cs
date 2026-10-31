using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Core.Application.Authorization;
using Core.Domain.Authorization;
using Engine.Application.Alarms;
using Engine.Application.Alarms.AcknowledgeAlarm;
using Engine.Application.Authorization;
using Xunit;

namespace Engine.UnitTests.Alarms.AcknowledgeAlarm;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldNotAcknowledge_WhenTheCallerIsNotAnOperator()
    {
        // Arrange
        var refusal = Error.Custom(AccessErrors.ForbiddenCode, "This needs the Operator role.");
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireRoleAsync(Arg.Any<Guid>(), "acme", NamespaceRole.Operator, Ct).Returns(refusal);
        var alarms = Substitute.For<IAlarmRuntime>();
        var handler = new Handler(grants, alarms);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "ada", "acme", Guid.NewGuid(), null), Ct);

        // Assert
        result.Error.ShouldBe(refusal);
        await alarms.DidNotReceiveWithAnyArgs()
            .AcknowledgeAsync(default!, Guid.NewGuid(), Guid.NewGuid(), default!, default, Ct);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task HandleAsync_ShouldRefuseTheCaller_WhenTheTokenCarriesNoName(string? name)
    {
        // Arrange
        var caller = Guid.NewGuid();
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireRoleAsync(caller, "acme", NamespaceRole.Operator, Ct).Returns(Result.Updated());
        var alarms = Substitute.For<IAlarmRuntime>();
        var handler = new Handler(grants, alarms);

        // Act
        var result = await handler.HandleAsync(new Command(caller, name, "acme", Guid.NewGuid(), null), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
        await alarms.DidNotReceiveWithAnyArgs()
            .AcknowledgeAsync(default!, Guid.NewGuid(), Guid.NewGuid(), default!, default, Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldAcknowledgeAsTheCaller_WhenTheyAreAnOperator()
    {
        // Arrange
        var caller = Guid.NewGuid();
        var id = Guid.NewGuid();
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireRoleAsync(caller, "acme", NamespaceRole.Operator, Ct).Returns(Result.Updated());
        var alarms = Substitute.For<IAlarmRuntime>();
        alarms.AcknowledgeAsync("acme", id, caller, "ada", "seen", Ct).Returns(Result.Updated());
        var handler = new Handler(grants, alarms);

        // Act
        var result = await handler.HandleAsync(new Command(caller, "ada", "acme", id, "seen"), Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
