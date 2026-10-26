using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Engine.Application.Alarms;
using Engine.Application.Alarms.ListAlarmHistory;
using Engine.Application.Authorization;
using Xunit;

namespace Engine.UnitTests.Alarms.ListAlarmHistory;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldNotReadHistory_WhenTheCallerIsNotAMember()
    {
        // Arrange
        var refusal = Error.NotFound("namespace.not_found", "No such Namespace.");
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireMemberAsync(Arg.Any<Guid>(), "acme", Ct).Returns(refusal);
        var alarms = Substitute.For<IAlarmRuntime>();
        var handler = new Handler(grants, alarms);

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid(), "acme", null, null, null, 0, 50), Ct);

        // Assert
        result.Error.ShouldBe(refusal);
        await alarms.DidNotReceiveWithAnyArgs().HistoryAsync(default!, default, default, default, 0, 0, Ct);
    }

    [Theory]
    [InlineData(-5, 0, 0, 1)]
    [InlineData(10, 1000, 10, 200)]
    public async Task HandleAsync_ShouldClampThePage_WhenOffsetOrLimitIsOutOfRange(
        int offset,
        int limit,
        int expectedOffset,
        int expectedLimit)
    {
        // Arrange
        var caller = Guid.NewGuid();
        var grants = Substitute.For<IRoleGrants>();
        grants.RequireMemberAsync(caller, "acme", Ct).Returns(Result.Updated());
        var alarms = Substitute.For<IAlarmRuntime>();
        alarms.HistoryAsync("acme", "train", null, null, expectedOffset, expectedLimit, Ct)
            .Returns(new AlarmHistoryPage([], 0));
        var handler = new Handler(grants, alarms);

        // Act
        var result = await handler.HandleAsync(new Query(caller, "acme", "train", null, null, offset, limit), Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }
}
