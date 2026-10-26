using Coderynx.Functional.Results.Errors;
using Engine.Application.Alarms;
using Engine.Application.Alarms.ListAlarms;
using Engine.Application.Authorization;
using Xunit;

namespace Engine.UnitTests.Alarms.ListAlarms;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldNotListAlarms_WhenTheCallerIsNotAMember()
    {
        // Arrange
        var refusal = Error.NotFound("namespace.not_found", "No such Namespace.");
        var token = TestContext.Current.CancellationToken;
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireMemberAsync(Arg.Any<Guid>(), "acme", token).Returns(refusal);
        var alarms = Substitute.For<IAlarmRuntime>();
        var handler = new Handler(grants, alarms);

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid(), "acme"), token);

        // Assert
        result.Error.ShouldBe(refusal);
        await alarms.DidNotReceiveWithAnyArgs().ListLiveAsync(default!, token);
    }
}
