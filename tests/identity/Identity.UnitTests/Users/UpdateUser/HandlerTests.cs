using Coderynx.Functional.Results.Successes;
using Core.Contracts.Identity;
using Identity.Application;
using Identity.Application.Users.UpdateUser;
using Xunit;

namespace Identity.UnitTests.Users.UpdateUser;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestDoubles.Member("bob");
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([bob, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(bob.Id, alice.Id, "alicia", "Alicia"), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
        alice.Username.Value.ShouldBe("alice");
        alice.DisplayName.ShouldBeNull();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsDisabled()
    {
        // Arrange
        var administrator = TestDoubles.Disabled(TestDoubles.Administrator());
        var alice = TestDoubles.Member("alice");
        var handler = new Handler(TestDoubles.Database([administrator, alice]));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "alicia", null), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, Guid.NewGuid(), "alicia", null), Ct);

        // Assert
        result.Error.ShouldBe(Errors.UserNotFound());
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBadRequest_WhenTheNewUsernameIsInvalid()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "alice smith", null), Ct);

        // Assert
        result.Error.ShouldBe(Errors.InvalidUsername("alice smith"));
        alice.Username.Value.ShouldBe("alice");
        TestDoubles.Published(database).ShouldBeEmpty();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenAnotherUserHasTheUsername()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice, TestDoubles.Member("bob")]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "bob", null), Ct);

        // Assert
        result.Error.ShouldBe(Errors.UsernameTaken("bob"));
        alice.Username.Value.ShouldBe("alice");
        TestDoubles.Published(database).ShouldBeEmpty();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRenameTheUserAndPublishUserRenamed_WhenTheUsernameIsFree()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "alicia", null), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        result.Value.ShouldBeSameAs(alice);
        alice.Username.Value.ShouldBe("alicia");
        TestDoubles.Published(database).ShouldBe([new UserRenamed(alice.Id, "alicia", 1)]);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceedWithoutPublishing_WhenTheUsernameIsUnchanged()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "alice", null), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        alice.Seq.ShouldBe(0);
        TestDoubles.Published(database).ShouldBeEmpty();
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldChangeOnlyTheDisplayNameWithoutPublishing_WhenNoUsernameIsGiven()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, null, " Alice A. "), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        alice.DisplayName.ShouldBe("Alice A.");
        alice.Username.Value.ShouldBe("alice");
        TestDoubles.Published(database).ShouldBeEmpty();
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldClearTheDisplayName_WhenItIsEmpty()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice", "Alice A.");
        var handler = new Handler(TestDoubles.Database([administrator, alice]));

        // Act
        await handler.HandleAsync(new Command(administrator.Id, alice.Id, null, string.Empty), Ct);

        // Assert
        alice.DisplayName.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepTheDisplayName_WhenNoneIsGiven()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice", "Alice A.");
        var handler = new Handler(TestDoubles.Database([administrator, alice]));

        // Act
        await handler.HandleAsync(new Command(administrator.Id, alice.Id, "alicia", null), Ct);

        // Assert
        alice.DisplayName.ShouldBe("Alice A.");
    }
}
