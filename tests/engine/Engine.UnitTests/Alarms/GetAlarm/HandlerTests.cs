using Coderynx.Functional.Results.Errors;
using Engine.Application.Alarms;
using Engine.Application.Alarms.GetAlarm;
using Engine.Application.Authorization;
using Xunit;

namespace Engine.UnitTests.Alarms.GetAlarm;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldNotReadTheAlarm_WhenTheCallerIsNotAMember()
    {
        // Arrange
        var refusal = Error.NotFound("namespace.not_found", "No such Namespace.");
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireMemberAsync(Arg.Any<Guid>(), "acme", Ct).Returns(refusal);
        var alarms = Substitute.For<IAlarmRuntime>();
        var handler = new Handler(grants, alarms);

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid(), "acme", Guid.NewGuid()), Ct);

        // Assert
        result.Error.ShouldBe(refusal);
        await alarms.DidNotReceiveWithAnyArgs().GetAsync(default!, Guid.NewGuid(), Ct);
    }
}
