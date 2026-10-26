using Coderynx.Functional.Results.Successes;
using Core.Contracts.Identity;
using Identity.Application;
using Identity.Application.Users.DisableUser;
using Xunit;

namespace Identity.UnitTests.Users.DisableUser;

public sealed class HandlerTests
{
    private readonly IUserTokens _tokens = Substitute.For<IUserTokens>();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsUnknown()
    {
        // Arrange
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([alice]);
        var handler = new Handler(database, TestDoubles.Checks(database), _tokens);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), alice.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
        await _tokens.DidNotReceiveWithAnyArgs().RevokeAllAsync(Guid.Empty, Ct);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsDisabled()
    {
        // Arrange
        var administrator = TestDoubles.Disabled(TestDoubles.Administrator());
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database, TestDoubles.Checks(database), _tokens);

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
        var bob = TestDoubles.Member("bob");
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([bob, alice]);
        var handler = new Handler(database, TestDoubles.Checks(database), _tokens);

        // Act
        var result = await handler.HandleAsync(new Command(bob.Id, alice.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
        alice.IsDisabled.ShouldBeFalse();
        await _tokens.DidNotReceiveWithAnyArgs().RevokeAllAsync(Guid.Empty, Ct);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database, TestDoubles.Checks(database), _tokens);

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
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database, TestDoubles.Checks(database), _tokens);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, administrator.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.LastAdministrator());
        administrator.IsDisabled.ShouldBeFalse();
        TestDoubles.Published(database).ShouldBeEmpty();
        await _tokens.DidNotReceiveWithAnyArgs().RevokeAllAsync(Guid.Empty, Ct);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenEveryOtherAdministratorIsDisabled()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Disabled(TestDoubles.Administrator("other"));
        var database = TestDoubles.Database([administrator, other]);
        var handler = new Handler(database, TestDoubles.Checks(database), _tokens);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, administrator.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.LastAdministrator());
    }

    [Fact]
    public async Task HandleAsync_ShouldDisableTheAdministrator_WhenAnotherActiveAdministratorExists()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Administrator("other");
        var database = TestDoubles.Database([administrator, other]);
        var handler = new Handler(database, TestDoubles.Checks(database), _tokens);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        other.IsDisabled.ShouldBeTrue();
        TestDoubles.Published(database).ShouldBe([new UserDisabled(other.Id, 2)]);
    }

    [Fact]
    public async Task HandleAsync_ShouldDisableTheUserRevokeTheirTokensAndPublishUserDisabled_WhenTheUserIsEnabled()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database, TestDoubles.Checks(database), _tokens);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        alice.IsDisabled.ShouldBeTrue();
        TestDoubles.Published(database).ShouldBe([new UserDisabled(alice.Id, 1)]);
        await _tokens.Received(1).RevokeAllAsync(alice.Id, Ct);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRevokeTheTokensWithoutPublishing_WhenTheUserIsAlreadyDisabled()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Disabled(TestDoubles.Member("alice"));
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database, TestDoubles.Checks(database), _tokens);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        alice.Seq.ShouldBe(1);
        TestDoubles.Published(database).ShouldBeEmpty();
        await _tokens.Received(1).RevokeAllAsync(alice.Id, Ct);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceedWithoutPublishing_WhenTheUserIsADisabledAdministrator()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Disabled(TestDoubles.Administrator("other"));
        var database = TestDoubles.Database([administrator, other]);
        var handler = new Handler(database, TestDoubles.Checks(database), _tokens);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        TestDoubles.Published(database).ShouldBeEmpty();
    }
}
