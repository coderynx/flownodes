using Coderynx.Functional.Results.Successes;
using Identity.Application;
using Identity.Application.Users.CreateUser;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Identity.UnitTests.Users.CreateUser;

public sealed class HandlerTests
{
    private const string Password = "alice has a long password";
    private readonly UserManager<User> _users = TestDoubles.UserManager();

    public HandlerTests()
    {
        _users.CreateAsync(Arg.Any<User>(), Arg.Any<string>()).Returns(IdentityResult.Success);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestDoubles.Member("bob");
        var database = TestDoubles.Database([bob]);
        var handler = new Handler(database, TestDoubles.Checks(database), _users);

        // Act
        var result = await handler.HandleAsync(new Command(bob.Id, "alice", null, Password), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
        await _users.DidNotReceiveWithAnyArgs().CreateAsync(null!, null!);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsDisabled()
    {
        // Arrange
        var administrator = TestDoubles.Disabled(TestDoubles.Administrator());
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database, TestDoubles.Checks(database), _users);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, "alice", null, Password), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
        await _users.DidNotReceiveWithAnyArgs().CreateAsync(null!, null!);
    }

    [Theory]
    [InlineData("")]
    [InlineData("alice smith")]
    public async Task HandleAsync_ShouldReturnBadRequest_WhenTheUsernameIsInvalid(string username)
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database, TestDoubles.Checks(database), _users);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, username, null, Password), Ct);

        // Assert
        result.Error.ShouldBe(Errors.InvalidUsername(username));
        await _users.DidNotReceiveWithAnyArgs().CreateAsync(null!, null!);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenTheUsernameIsTaken()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        _users.FindByNameAsync("alice").Returns(TestDoubles.Member("Alice"));
        var handler = new Handler(database, TestDoubles.Checks(database), _users);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, "alice", null, Password), Ct);

        // Assert
        result.Error.ShouldBe(Errors.UsernameTaken("alice"));
        await _users.DidNotReceiveWithAnyArgs().CreateAsync(null!, null!);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBadRequestWithEveryReason_WhenThePasswordIsRejected()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        var errors = new[]
            { new IdentityError { Description = "Too short." }, new IdentityError { Description = "Too weak." } };
        _users.CreateAsync(Arg.Any<User>(), "short").Returns(IdentityResult.Failed(errors));
        var handler = new Handler(database, TestDoubles.Checks(database), _users);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, "alice", null, "short"), Ct);

        // Assert
        result.Error.ShouldBe(Errors.InvalidPassword(["Too short.", "Too weak."]));
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateTheUserWithThePasswordAndNoRoles_WhenTheInputIsValid()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database, TestDoubles.Checks(database), _users);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, "Alice", " Alice A. ", Password), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Created);
        result.Value.Username.Value.ShouldBe("Alice");
        result.Value.DisplayName.ShouldBe("Alice A.");
        result.Value.IsAdministrator.ShouldBeFalse();
        result.Value.RoleGrants.ShouldBeEmpty();
        await _users.Received(1).CreateAsync(result.Value, Password);
        TestDoubles.Published(database).ShouldBeEmpty();
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenSavingHitsTheUniqueUsername()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        database.SaveChangesAsync(Ct).Returns(Task.FromException(new DbUpdateException("duplicate")));
        var handler = new Handler(database, TestDoubles.Checks(database), _users);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, "alice", null, Password), Ct);

        // Assert
        result.Error.ShouldBe(Errors.UsernameTaken("alice"));
    }
}
