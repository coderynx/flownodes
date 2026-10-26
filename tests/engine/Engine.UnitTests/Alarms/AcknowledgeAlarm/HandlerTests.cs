using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
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
    public async Task HandleAsync_ShouldNotAcknowledge_WhenTheCallerLacksTheRole()
    {
        // Arrange
        var refusal = Error.Custom("access.forbidden", "This needs the Operator or Engineer role.");
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireAnyRoleAsync(Arg.Any<Guid>(), "acme", Arg.Any<IReadOnlyCollection<NamespaceRole>>(), Ct)
            .Returns(refusal);
        var alarms = Substitute.For<IAlarmRuntime>();
        var handler = new Handler(grants, alarms);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), "acme", Guid.NewGuid(), null), Ct);

        // Assert
        result.Error.ShouldBe(refusal);
        await alarms.DidNotReceiveWithAnyArgs().AcknowledgeAsync(default!, Guid.NewGuid(), Guid.NewGuid(), default, Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldAcknowledgeAsTheCaller_WhenTheyAreAnOperatorOrEngineer()
    {
        // Arrange
        var caller = Guid.NewGuid();
        var id = Guid.NewGuid();
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireAnyRoleAsync(caller, "acme", Arg.Any<IReadOnlyCollection<NamespaceRole>>(), Ct)
            .Returns(Result.Updated());
        var alarms = Substitute.For<IAlarmRuntime>();
        alarms.AcknowledgeAsync("acme", id, caller, "seen", Ct).Returns(Result.Updated());
        var handler = new Handler(grants, alarms);

        // Act
        var result = await handler.HandleAsync(new Command(caller, "acme", id, "seen"), Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await grants.Received()
            .RequireAnyRoleAsync(
                caller,
                "acme",
                Arg.Is<IReadOnlyCollection<NamespaceRole>>(roles =>
                    roles.Contains(NamespaceRole.Operator) && roles.Contains(NamespaceRole.Engineer) &&
                    roles.Count == 2),
                Ct);
    }
}
