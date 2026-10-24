using Coderynx.Functional.Results.Successes;
using Identity.Application;
using Identity.Application.Security;
using Identity.Application.Users.ResetPassword;
using Identity.Domain.Tokens;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Identity.UnitTests.Users.ResetPassword;

public sealed class HandlerTests
{
    private const string Password = "a brand new password";
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    private readonly IPasswordHasher<User> _hasher = Substitute.For<IPasswordHasher<User>>();
    private readonly IUserTokenRevoker _revoker = Substitute.For<IUserTokenRevoker>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestDoubles.Member("bob");
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([bob, alice]);
        var handler = new Handler(database, TestDoubles.UserManager(_hasher), _revoker, TestDoubles.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(bob.Id, alice.Id, Password), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
        alice.PasswordHash.ShouldBeNull();
        await _revoker.DidNotReceiveWithAnyArgs().RevokeAllAsync(Guid.Empty, Ct);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsDisabled()
    {
        // Arrange
        var administrator = TestDoubles.Disabled(TestDoubles.Administrator());
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database, TestDoubles.UserManager(_hasher), _revoker, TestDoubles.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, Password), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database, TestDoubles.UserManager(_hasher), _revoker, TestDoubles.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, Guid.NewGuid(), Password), Ct);

        // Assert
        result.Error.ShouldBe(Errors.UserNotFound());
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBadRequestWithEveryReason_WhenThePolicyRejectsThePassword()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice]);
        var users = TestDoubles.UserManager(_hasher);
        users.PasswordValidators.Add(TestDoubles.AcceptingValidator());
        users.PasswordValidators.Add(TestDoubles.RejectingValidator("Too short.", "Too common."));
        var handler = new Handler(database, users, _revoker, TestDoubles.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "short"), Ct);

        // Assert
        result.Error.ShouldBe(Errors.InvalidPassword(["Too short.", "Too common."]));
        result.Error.Message.ShouldBe("Too short. Too common.");
        alice.PasswordHash.ShouldBeNull();
        await _revoker.DidNotReceiveWithAnyArgs().RevokeAllAsync(Guid.Empty, Ct);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReplaceTheHashClearTheLockoutAndRevokeTheTokens_WhenThePasswordIsValid()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        alice.SetPasswordHash("old hash");
        alice.RecordFailedAccess();
        alice.LockOutUntil(Now.AddMinutes(5));
        var token = PersonalAccessToken.Create(alice.Id, "ci", "hash", Now.AddDays(-1), null);
        var database = TestDoubles.Database([administrator, alice], [token]);
        var users = TestDoubles.UserManager(_hasher);
        users.PasswordValidators.Add(TestDoubles.AcceptingValidator());
        _hasher.HashPassword(alice, Password).Returns("new hash");
        var handler = new Handler(database, users, _revoker, TestDoubles.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, Password), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        alice.PasswordHash.ShouldBe("new hash");
        alice.AccessFailedCount.ShouldBe(0);
        alice.LockoutEnd.ShouldBeNull();
        token.RevokedAt.ShouldBe(Now);
        TestDoubles.Published(database).ShouldBeEmpty();
        await _revoker.Received(1).RevokeAllAsync(alice.Id, Ct);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepTheRevocationMoment_WhenATokenIsAlreadyRevoked()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var token = PersonalAccessToken.Create(alice.Id, "ci", "hash", Now.AddDays(-2), null);
        token.Revoke(Now.AddDays(-1));
        var database = TestDoubles.Database([administrator, alice], [token]);
        var handler = new Handler(database, TestDoubles.UserManager(_hasher), _revoker, TestDoubles.Clock(Now));

        // Act
        await handler.HandleAsync(new Command(administrator.Id, alice.Id, Password), Ct);

        // Assert
        token.RevokedAt.ShouldBe(Now.AddDays(-1));
    }
}
