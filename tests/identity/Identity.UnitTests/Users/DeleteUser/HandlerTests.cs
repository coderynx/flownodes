using Coderynx.Functional.Results.Successes;
using Core.Contracts.Identity;
using Identity.Application;
using Identity.Application.Security;
using Identity.Application.Users.DeleteUser;
using Identity.Domain.Tokens;
using Xunit;

namespace Identity.UnitTests.Users.DeleteUser;

public sealed class HandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    private readonly IUserTokenRevoker _revoker = Substitute.For<IUserTokenRevoker>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestDoubles.Member("bob");
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([bob, alice]);
        var handler = new Handler(database, TestDoubles.Checks(database), _revoker);

        // Act
        var result = await handler.HandleAsync(new Command(bob.Id, alice.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
        database.Users.DidNotReceiveWithAnyArgs().Remove(alice);
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
        var handler = new Handler(database, TestDoubles.Checks(database), _revoker);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database, TestDoubles.Checks(database), _revoker);

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
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Disabled(TestDoubles.Administrator("other"));
        var database = TestDoubles.Database([administrator, other]);
        var handler = new Handler(database, TestDoubles.Checks(database), _revoker);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, administrator.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.LastAdministrator());
        database.Users.DidNotReceiveWithAnyArgs().Remove(administrator);
        TestDoubles.Published(database).ShouldBeEmpty();
        await _revoker.DidNotReceiveWithAnyArgs().RevokeAllAsync(Guid.Empty, Ct);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteTheAdministrator_WhenAnotherActiveAdministratorExists()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Administrator("other");
        var database = TestDoubles.Database([administrator, other]);
        var handler = new Handler(database, TestDoubles.Checks(database), _revoker);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        database.Users.Received(1).Remove(other);
        TestDoubles.Published(database).ShouldBe([new UserDeleted(other.Id, 2)]);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteTheUserAndTheirTokensAndPublishUserDeleted_WhenTheUserExists()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Disabled(TestDoubles.Member("alice"));
        var aliceToken = PersonalAccessToken.Create(alice.Id, "ci", "hash", Now, null);
        var administratorToken = PersonalAccessToken.Create(administrator.Id, "ci", "hash", Now, null);
        var database = TestDoubles.Database([administrator, alice], [aliceToken, administratorToken]);
        var handler = new Handler(database, TestDoubles.Checks(database), _revoker);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        database.PersonalAccessTokens.Received(1)
            .RemoveRange(Arg.Is<IEnumerable<PersonalAccessToken>>(tokens => tokens.Single() == aliceToken));
        database.Users.Received(1).Remove(alice);
        TestDoubles.Published(database).ShouldBe([new UserDeleted(alice.Id, 2)]);
        await _revoker.Received(1).RevokeAllAsync(alice.Id, Ct);
        await database.Received(1).SaveChangesAsync(Ct);
    }
}
