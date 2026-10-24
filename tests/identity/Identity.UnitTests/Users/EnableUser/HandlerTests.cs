using Coderynx.Functional.Results.Successes;
using Core.Contracts.Identity;
using Identity.Application;
using Identity.Application.Users.EnableUser;
using Xunit;

namespace Identity.UnitTests.Users.EnableUser;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestDoubles.Member("bob");
        var alice = TestDoubles.Disabled(TestDoubles.Member("alice"));
        var database = TestDoubles.Database([bob, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(bob.Id, alice.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
        alice.IsDisabled.ShouldBeTrue();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsDisabled()
    {
        // Arrange
        var administrator = TestDoubles.Disabled(TestDoubles.Administrator());
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, administrator.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
        administrator.IsDisabled.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, Guid.NewGuid()), Ct);

        // Assert
        result.Error.ShouldBe(Errors.UserNotFound());
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldEnableTheUserAndPublishUserEnabled_WhenTheUserIsDisabled()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Disabled(TestDoubles.Member("alice"));
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        alice.IsDisabled.ShouldBeFalse();
        TestDoubles.Published(database).ShouldBe([new UserEnabled(alice.Id, 2)]);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldSucceedWithoutPublishing_WhenTheUserIsAlreadyEnabled()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        alice.Seq.ShouldBe(0);
        TestDoubles.Published(database).ShouldBeEmpty();
        await database.Received(1).SaveChangesAsync(Ct);
    }
}
