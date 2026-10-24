using Identity.Domain.Tokens;
using Xunit;

namespace Identity.UnitTests.Domain.Tokens;

public sealed class PersonalAccessTokenTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ShouldRecordEveryValue_WhenGivenThem()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiresAt = Now.AddDays(30);

        // Act
        var token = PersonalAccessToken.Create(userId, "laptop", "hash", Now, expiresAt);

        // Assert
        token.Id.ShouldNotBe(Guid.Empty);
        token.UserId.ShouldBe(userId);
        token.Name.ShouldBe("laptop");
        token.SecretHash.ShouldBe("hash");
        token.CreatedAt.ShouldBe(Now);
        token.ExpiresAt.ShouldBe(expiresAt);
        token.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_ShouldGiveEachTokenItsOwnId_WhenCalledTwice()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var first = PersonalAccessToken.Create(userId, "laptop", "hash", Now, null);
        var second = PersonalAccessToken.Create(userId, "laptop", "hash", Now, null);

        // Assert
        first.Id.ShouldNotBe(second.Id);
    }

    [Fact]
    public void IsActive_ShouldBeTrue_WhenTheTokenNeverExpires()
    {
        // Arrange
        var token = PersonalAccessToken.Create(Guid.NewGuid(), "laptop", "hash", Now, null);

        // Act
        var active = token.IsActive(Now.AddYears(100));

        // Assert
        active.ShouldBeTrue();
    }

    [Fact]
    public void IsActive_ShouldBeTrue_WhenTheExpiryIsStillAhead()
    {
        // Arrange
        var token = PersonalAccessToken.Create(Guid.NewGuid(), "laptop", "hash", Now, Now.AddDays(1));

        // Act
        var active = token.IsActive(Now.AddDays(1).AddTicks(-1));

        // Assert
        active.ShouldBeTrue();
    }

    [Fact]
    public void IsActive_ShouldBeFalse_WhenTheExpiryIsReached()
    {
        // Arrange
        var token = PersonalAccessToken.Create(Guid.NewGuid(), "laptop", "hash", Now, Now.AddDays(1));

        // Act
        var active = token.IsActive(Now.AddDays(1));

        // Assert
        active.ShouldBeFalse();
    }

    [Fact]
    public void IsActive_ShouldBeFalse_WhenTheExpiryHasPassed()
    {
        // Arrange
        var token = PersonalAccessToken.Create(Guid.NewGuid(), "laptop", "hash", Now, Now.AddDays(1));

        // Act
        var active = token.IsActive(Now.AddDays(2));

        // Assert
        active.ShouldBeFalse();
    }

    [Fact]
    public void IsActive_ShouldBeFalse_WhenTheTokenIsRevoked()
    {
        // Arrange
        var token = PersonalAccessToken.Create(Guid.NewGuid(), "laptop", "hash", Now, null);
        token.Revoke(Now);

        // Act
        var active = token.IsActive(Now);

        // Assert
        active.ShouldBeFalse();
    }

    [Fact]
    public void Revoke_ShouldRecordTheMoment_WhenTheTokenIsActive()
    {
        // Arrange
        var token = PersonalAccessToken.Create(Guid.NewGuid(), "laptop", "hash", Now, null);

        // Act
        token.Revoke(Now.AddHours(1));

        // Assert
        token.RevokedAt.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void Revoke_ShouldKeepTheFirstMoment_WhenTheTokenIsAlreadyRevoked()
    {
        // Arrange
        var token = PersonalAccessToken.Create(Guid.NewGuid(), "laptop", "hash", Now, null);
        token.Revoke(Now.AddHours(1));

        // Act
        token.Revoke(Now.AddHours(2));

        // Assert
        token.RevokedAt.ShouldBe(Now.AddHours(1));
    }
}
