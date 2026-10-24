using Coderynx.Functional.Results.Successes;
using Core.Contracts.Identity;
using Identity.Application.Database;
using Identity.Application.Security;
using Identity.Application.Setup.CompleteSetup;
using Identity.Domain.Namespaces;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Identity.UnitTests.Setup.CompleteSetup;

public sealed class HandlerTests
{
    private const string LiveToken = "live-setup-token";
    private const string Password = "a-long-enough-password";
    private readonly IIdentityDbContext _database = TestDoubles.Database([]);
    private readonly UserManager<User> _users = TestDoubles.UserManager();
    private readonly ISetupToken _setupToken = Substitute.For<ISetupToken>();

    public HandlerTests()
    {
        _setupToken.Matches(LiveToken).Returns(true);
        _users.CreateAsync(Arg.Any<User>(), Password).Returns(IdentityResult.Success);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateAnAdministratorWithBothRolesInDefault_WhenTheSetupTokenIsLive()
    {
        // Arrange
        var handler = new Handler(_database, _users, _setupToken);

        // Act
        var result = await handler.HandleAsync(
            new Command(LiveToken, "alice", " Alice ", Password),
            TestContext.Current.CancellationToken);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Created);
        var user = result.Value;
        user.Username.Value.ShouldBe("alice");
        user.DisplayName.ShouldBe("Alice");
        user.IsAdministrator.ShouldBeTrue();
        user.RoleGrants.Select(grant => (grant.Namespace, grant.Role))
            .ShouldBe([(KnownNamespace.Default, Role.Engineer), (KnownNamespace.Default, Role.Operator)]);
        user.Seq.ShouldBe(3);
        await _users.Received(1).CreateAsync(user, Password);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishTheAdministratorAndBothRoleGrantsWithTheirSeqs_WhenTheSetupSucceeds()
    {
        // Arrange
        var handler = new Handler(_database, _users, _setupToken);

        // Act
        var result = await handler.HandleAsync(
            new Command(LiveToken, "alice", null, Password),
            TestContext.Current.CancellationToken);

        // Assert
        var id = result.Value.Id;
        await _database.Received(1).PublishAsync(new AdministratorGranted(id, 1));
        await _database.Received(1).PublishAsync(new RoleGranted(id, KnownNamespace.Default, "Engineer", 2));
        await _database.Received(1).PublishAsync(new RoleGranted(id, KnownNamespace.Default, "Operator", 3));
        await _database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldInvalidateTheSetupToken_WhenTheSetupSucceeds()
    {
        // Arrange
        var handler = new Handler(_database, _users, _setupToken);

        // Act
        await handler.HandleAsync(
            new Command(LiveToken, "alice", null, Password),
            TestContext.Current.CancellationToken);

        // Assert
        _setupToken.Received(1).Invalidate();
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepTheSetupTokenLive_WhenTheCommitFails()
    {
        // Arrange
        _database.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("The database is down.")));
        var handler = new Handler(_database, _users, _setupToken);

        // Act
        var act = () => handler.HandleAsync(
            new Command(LiveToken, "alice", null, Password),
            TestContext.Current.CancellationToken);

        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
        _setupToken.DidNotReceive().Invalidate();
    }

    [Fact]
    public async Task HandleAsync_ShouldReportSetupDone_WhenAUserAlreadyExists()
    {
        // Arrange
        var database = TestDoubles.Database([TestDoubles.Member("alice")]);
        var handler = new Handler(database, _users, _setupToken);

        // Act
        var result = await handler.HandleAsync(
            new Command(LiveToken, "alice", null, Password),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("setup.done");
        await _users.DidNotReceiveWithAnyArgs().CreateAsync(null!, null!);
        _setupToken.DidNotReceive().Invalidate();
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseTheSetup_WhenTheSetupTokenIsWrong()
    {
        // Arrange
        var handler = new Handler(_database, _users, _setupToken);

        // Act
        var result = await handler.HandleAsync(
            new Command("wrong-token", "alice", null, Password),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("setup.invalid_token");
        await _users.DidNotReceiveWithAnyArgs().CreateAsync(null!, null!);
        await _database.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseTheUsername_WhenItIsInvalid()
    {
        // Arrange
        var handler = new Handler(_database, _users, _setupToken);

        // Act
        var result = await handler.HandleAsync(
            new Command(LiveToken, "not a username!", null, Password),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("user.invalid_username");
        await _users.DidNotReceiveWithAnyArgs().CreateAsync(null!, null!);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseThePasswordWithEveryReason_WhenTheUserManagerRejectsIt()
    {
        // Arrange
        _users.CreateAsync(Arg.Any<User>(), "short")
            .Returns(
                IdentityResult.Failed(
                    new IdentityError { Description = "Too short." },
                    new IdentityError { Description = "No digit." }));
        var handler = new Handler(_database, _users, _setupToken);

        // Act
        var result = await handler.HandleAsync(
            new Command(LiveToken, "alice", null, "short"),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("user.invalid_password");
        result.Error.Message.ShouldBe("Too short. No digit.");
        _database.ReceivedCalls().ShouldNotContain(call => call.GetMethodInfo().Name == "PublishAsync");
        await _database.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
        _setupToken.DidNotReceive().Invalidate();
    }
}
