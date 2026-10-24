using Coderynx.Functional.Results.Successes;
using Identity.Application;
using Identity.Application.Self.ChangePassword;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Identity.UnitTests.Self.ChangePassword;

public sealed class HandlerTests
{
    private const string Current = "alice has a long password";
    private const string New = "alice has a newer password";
    private readonly IPasswordHasher<User> _hasher = Substitute.For<IPasswordHasher<User>>();
    private readonly UserManager<User> _users;

    public HandlerTests()
    {
        _users = TestDoubles.UserManager(_hasher);
        _users.CheckPasswordAsync(Arg.Any<User>(), Current).Returns(true);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsUnknown()
    {
        // Arrange
        var database = TestDoubles.Database([TestDoubles.Member("alice")]);
        var handler = new Handler(database, _users);

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), Current, New), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsDisabled()
    {
        // Arrange
        var alice = TestDoubles.Disabled(TestDoubles.Member("alice"));
        var database = TestDoubles.Database([alice]);
        var handler = new Handler(database, _users);

        // Act
        var result = await handler.HandleAsync(new Command(alice.Id, Current, New), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
        alice.PasswordHash.ShouldBeNull();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBadRequest_WhenTheCurrentPasswordIsWrong()
    {
        // Arrange
        var alice = TestDoubles.Member("alice");
        alice.SetPasswordHash("old hash");
        var database = TestDoubles.Database([alice]);
        var handler = new Handler(database, _users);

        // Act
        var result = await handler.HandleAsync(new Command(alice.Id, "not the password", New), Ct);

        // Assert
        result.Error.ShouldBe(Errors.WrongCurrentPassword());
        alice.PasswordHash.ShouldBe("old hash");
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBadRequest_WhenThePolicyRejectsTheNewPassword()
    {
        // Arrange
        var alice = TestDoubles.Member("alice");
        alice.SetPasswordHash("old hash");
        var database = TestDoubles.Database([alice]);
        _users.PasswordValidators.Add(TestDoubles.RejectingValidator("Too short."));
        var handler = new Handler(database, _users);

        // Act
        var result = await handler.HandleAsync(new Command(alice.Id, Current, "short"), Ct);

        // Assert
        result.Error.ShouldBe(Errors.InvalidPassword(["Too short."]));
        alice.PasswordHash.ShouldBe("old hash");
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreTheNewHash_WhenTheCurrentPasswordIsRightAndTheNewOneIsValid()
    {
        // Arrange
        var alice = TestDoubles.Member("alice");
        alice.SetPasswordHash("old hash");
        var database = TestDoubles.Database([alice]);
        _users.PasswordValidators.Add(TestDoubles.AcceptingValidator());
        _hasher.HashPassword(alice, New).Returns("new hash");
        var handler = new Handler(database, _users);

        // Act
        var result = await handler.HandleAsync(new Command(alice.Id, Current, New), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        alice.PasswordHash.ShouldBe("new hash");
        TestDoubles.Published(database).ShouldBeEmpty();
        await database.Received(1).SaveChangesAsync(Ct);
    }
}
