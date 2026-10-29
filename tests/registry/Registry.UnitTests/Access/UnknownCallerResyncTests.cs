using Core.Application.Authorization;
using Xunit;

namespace Registry.UnitTests.Access;

public sealed class UnknownCallerResyncTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 29, 12, 0, 0, TimeSpan.Zero);
    private readonly IUserAccessResync _resync = Substitute.For<IUserAccessResync>();
    private readonly TimeProvider _time = Substitute.For<TimeProvider>();

    public UnknownCallerResyncTests()
    {
        _time.GetUtcNow().Returns(Now);
    }

    [Fact]
    public async Task RequestAsync_ShouldAskIdentityOnce_WhenTheSameCallerRetriesWithinTheInterval()
    {
        // Arrange
        var callerId = Guid.NewGuid();
        var unknown = new UnknownCallerResync(_resync, _time);
        await unknown.RequestAsync(callerId, Ct);
        _time.GetUtcNow().Returns(Now.AddSeconds(59));

        // Act
        await unknown.RequestAsync(callerId, Ct);

        // Assert
        await _resync.Received(1).RequestAsync(callerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestAsync_ShouldAskIdentityAgain_WhenTheIntervalHasPassed()
    {
        // Arrange
        var callerId = Guid.NewGuid();
        var unknown = new UnknownCallerResync(_resync, _time);
        await unknown.RequestAsync(callerId, Ct);
        _time.GetUtcNow().Returns(Now.AddMinutes(1));

        // Act
        await unknown.RequestAsync(callerId, Ct);

        // Assert
        await _resync.Received(2).RequestAsync(callerId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestAsync_ShouldAskIdentityForEachCaller_WhenDifferentCallersArrive()
    {
        // Arrange
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var unknown = new UnknownCallerResync(_resync, _time);

        // Act
        await unknown.RequestAsync(first, Ct);
        await unknown.RequestAsync(second, Ct);

        // Assert
        await _resync.Received(1).RequestAsync(first, Arg.Any<CancellationToken>());
        await _resync.Received(1).RequestAsync(second, Arg.Any<CancellationToken>());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
