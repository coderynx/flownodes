using Identity.Application;
using Identity.Application.Security;
using Identity.Domain.Tokens;
using Xunit;

namespace Identity.UnitTests;

public sealed class UserTokensTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    private readonly IUserTokenRevoker _revoker = Substitute.For<IUserTokenRevoker>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RevokeAllAsync_ShouldRevokeTheUsersTokensAndSessions_WhenTheUserHoldsTokens()
    {
        // Arrange
        var alice = TestDoubles.Member("alice");
        var bob = TestDoubles.Member("bob");
        var aliceToken = PersonalAccessToken.Create(alice.Id, "ci", "hash", Now.AddDays(-1), null);
        var bobToken = PersonalAccessToken.Create(bob.Id, "ci", "hash", Now.AddDays(-1), null);
        var database = TestDoubles.Database([alice, bob], [aliceToken, bobToken]);
        var tokens = new UserTokens(database, _revoker, TestDoubles.Clock(Now));

        // Act
        await tokens.RevokeAllAsync(alice.Id, Ct);

        // Assert
        aliceToken.RevokedAt.ShouldBe(Now);
        bobToken.RevokedAt.ShouldBeNull();
        await _revoker.Received(1).RevokeAllAsync(alice.Id, Ct);
    }

    [Fact]
    public async Task RevokeAllAsync_ShouldKeepTheRevocationMoment_WhenATokenIsAlreadyRevoked()
    {
        // Arrange
        var alice = TestDoubles.Member("alice");
        var token = PersonalAccessToken.Create(alice.Id, "ci", "hash", Now.AddDays(-2), null);
        token.Revoke(Now.AddDays(-1));
        var database = TestDoubles.Database([alice], [token]);
        var tokens = new UserTokens(database, _revoker, TestDoubles.Clock(Now));

        // Act
        await tokens.RevokeAllAsync(alice.Id, Ct);

        // Assert
        token.RevokedAt.ShouldBe(Now.AddDays(-1));
    }
}
