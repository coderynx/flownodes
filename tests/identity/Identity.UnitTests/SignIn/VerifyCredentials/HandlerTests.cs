using Identity.Application.Database;
using Identity.Application.SignIn.VerifyCredentials;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Identity.UnitTests.SignIn.VerifyCredentials;

public sealed class HandlerTests
{
    private const string Password = "the-right-password";
    private readonly IIdentityDbContext _database = TestDoubles.Database([]);
    private readonly UserManager<User> _users = TestDoubles.UserManager();

    [Fact]
    public async Task HandleAsync_ShouldFindTheUserAndForgetFailedAttempts_WhenThePasswordIsRight()
    {
        // Arrange
        var user = Known(TestDoubles.Member("alice"));
        _users.CheckPasswordAsync(user, Password).Returns(true);
        var handler = new Handler(_database, _users);

        // Act
        var result = await handler.HandleAsync(new Command("alice", Password), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeSameAs(user);
        await _users.Received(1).ResetAccessFailedCountAsync(user);
        await _users.DidNotReceiveWithAnyArgs().AccessFailedAsync(null!);
        await _database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldCountTheFailure_WhenThePasswordIsWrong()
    {
        // Arrange
        var user = Known(TestDoubles.Member("alice"));
        var handler = new Handler(_database, _users);

        // Act
        var result = await handler.HandleAsync(new Command("alice", "wrong"), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("signin.invalid_credentials");
        await _users.Received(1).AccessFailedAsync(user);
        await _users.DidNotReceiveWithAnyArgs().ResetAccessFailedCountAsync(null!);
        await _database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldReportTheLockout_WhenAWrongPasswordLocksTheUserOut()
    {
        // Arrange
        var user = Known(TestDoubles.Member("alice"));
        _users.IsLockedOutAsync(user).Returns(false, true);
        var handler = new Handler(_database, _users);

        // Act
        var result = await handler.HandleAsync(new Command("alice", "wrong"), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("signin.locked_out");
        await _users.Received(1).AccessFailedAsync(user);
        await _database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseWithoutCheckingThePassword_WhenTheUserIsLockedOut()
    {
        // Arrange
        var user = Known(TestDoubles.Member("alice"));
        _users.IsLockedOutAsync(user).Returns(true);
        _users.CheckPasswordAsync(user, Password).Returns(true);
        var handler = new Handler(_database, _users);

        // Act
        var result = await handler.HandleAsync(new Command("alice", Password), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("signin.locked_out");
        await _users.DidNotReceiveWithAnyArgs().CheckPasswordAsync(null!, null!);
        await _database.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseWithoutCheckingThePassword_WhenTheUserIsDisabled()
    {
        // Arrange
        var user = Known(TestDoubles.Disabled(TestDoubles.Member("bob")));
        _users.CheckPasswordAsync(user, Password).Returns(true);
        var handler = new Handler(_database, _users);

        // Act
        var result = await handler.HandleAsync(new Command("bob", Password), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("signin.invalid_credentials");
        await _users.DidNotReceiveWithAnyArgs().CheckPasswordAsync(null!, null!);
        await _users.DidNotReceiveWithAnyArgs().AccessFailedAsync(null!);
        await _database.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuse_WhenTheUsernameIsUnknown()
    {
        // Arrange
        var handler = new Handler(_database, _users);

        // Act
        var result = await handler.HandleAsync(new Command("nobody", Password), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("signin.invalid_credentials");
        await _database.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private User Known(User user)
    {
        _users.FindByNameAsync(user.Username.Value).Returns(user);

        return user;
    }
}
