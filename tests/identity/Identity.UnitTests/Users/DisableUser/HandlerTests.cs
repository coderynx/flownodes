using Coderynx.Functional.Results.Successes;
using Core.Contracts.Identity;
using Identity.Application;
using Identity.Application.Security;
using Identity.Application.Users.DisableUser;
using Identity.Domain.Tokens;
using Xunit;

namespace Identity.UnitTests.Users.DisableUser;

public sealed class HandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    private readonly IUserTokenRevoker _revoker = Substitute.For<IUserTokenRevoker>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsUnknown()
    {
        // Arrange
        var alice = TestUsers.Member("alice");
        var database = TestUsers.Database([alice]);
        var handler = new Handler(database, _revoker, TestUsers.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), alice.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
        await _revoker.DidNotReceiveWithAnyArgs().RevokeAllAsync(Guid.Empty, Ct);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsDisabled()
    {
        // Arrange
        var administrator = TestUsers.Disabled(TestUsers.Administrator());
        var alice = TestUsers.Member("alice");
        var database = TestUsers.Database([administrator, alice]);
        var handler = new Handler(database, _revoker, TestUsers.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
        alice.IsDisabled.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestUsers.Member("bob");
        var alice = TestUsers.Member("alice");
        var database = TestUsers.Database([bob, alice]);
        var handler = new Handler(database, _revoker, TestUsers.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(bob.Id, alice.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
        alice.IsDisabled.ShouldBeFalse();
        await _revoker.DidNotReceiveWithAnyArgs().RevokeAllAsync(Guid.Empty, Ct);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var database = TestUsers.Database([administrator]);
        var handler = new Handler(database, _revoker, TestUsers.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, Guid.NewGuid()), Ct);

        // Assert
        result.Error.ShouldBe(Errors.UserNotFound());
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenTheUserIsTheLastActiveAdministrator()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var database = TestUsers.Database([administrator]);
        var handler = new Handler(database, _revoker, TestUsers.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, administrator.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.LastAdministrator());
        administrator.IsDisabled.ShouldBeFalse();
        TestUsers.Published(database).ShouldBeEmpty();
        await _revoker.DidNotReceiveWithAnyArgs().RevokeAllAsync(Guid.Empty, Ct);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenEveryOtherAdministratorIsDisabled()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var other = TestUsers.Disabled(TestUsers.Administrator("other"));
        var database = TestUsers.Database([administrator, other]);
        var handler = new Handler(database, _revoker, TestUsers.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, administrator.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.LastAdministrator());
    }

    [Fact]
    public async Task HandleAsync_ShouldDisableTheAdministrator_WhenAnotherActiveAdministratorExists()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var other = TestUsers.Administrator("other");
        var database = TestUsers.Database([administrator, other]);
        var handler = new Handler(database, _revoker, TestUsers.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        other.IsDisabled.ShouldBeTrue();
        TestUsers.Published(database).ShouldBe([new UserDisabled(other.Id, 2)]);
    }

    [Fact]
    public async Task HandleAsync_ShouldDisableTheUserRevokeTheirTokensAndPublishUserDisabled_WhenTheUserIsEnabled()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var alice = TestUsers.Member("alice");
        var aliceToken = PersonalAccessToken.Create(alice.Id, "ci", "hash", Now.AddDays(-1), null);
        var administratorToken = PersonalAccessToken.Create(administrator.Id, "ci", "hash", Now.AddDays(-1), null);
        var database = TestUsers.Database([administrator, alice], [aliceToken, administratorToken]);
        var handler = new Handler(database, _revoker, TestUsers.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        alice.IsDisabled.ShouldBeTrue();
        aliceToken.RevokedAt.ShouldBe(Now);
        administratorToken.RevokedAt.ShouldBeNull();
        TestUsers.Published(database).ShouldBe([new UserDisabled(alice.Id, 1)]);
        await _revoker.Received(1).RevokeAllAsync(alice.Id, Ct);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRevokeTheTokensWithoutPublishing_WhenTheUserIsAlreadyDisabled()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var alice = TestUsers.Disabled(TestUsers.Member("alice"));
        var database = TestUsers.Database([administrator, alice]);
        var handler = new Handler(database, _revoker, TestUsers.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        alice.Seq.ShouldBe(1);
        TestUsers.Published(database).ShouldBeEmpty();
        await _revoker.Received(1).RevokeAllAsync(alice.Id, Ct);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceedWithoutPublishing_WhenTheUserIsADisabledAdministrator()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var other = TestUsers.Disabled(TestUsers.Administrator("other"));
        var database = TestUsers.Database([administrator, other]);
        var handler = new Handler(database, _revoker, TestUsers.Clock(Now));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        TestUsers.Published(database).ShouldBeEmpty();
    }
}
