using Core.Contracts.Identity;
using Identity.Application.Users.ResyncUsers;
using Identity.Domain.Users;
using Xunit;

namespace Identity.UnitTests.Users.ResyncUsers;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldPublishTheWholeStateOfTheUser_WhenTheUserExists()
    {
        // Arrange
        var alice = TestDoubles.Member("alice");
        alice.Grant("acme", Role.Operator);
        alice.Grant("globex", Role.Engineer);
        alice.GrantAdministrator();
        alice.Disable();
        var database = TestDoubles.Database([alice]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command(alice.Id), Ct);

        // Assert
        var published = TestDoubles.Published(database).ShouldHaveSingleItem().ShouldBeOfType<UserAccessPublished>();
        published.UserId.ShouldBe(alice.Id);
        published.Username.ShouldBe("alice");
        published.IsDisabled.ShouldBeTrue();
        published.IsDeleted.ShouldBeFalse();
        published.IsAdministrator.ShouldBeTrue();
        published.Grants.ShouldBe(
            [new PublishedRoleGrant("acme", "Operator"), new PublishedRoleGrant("globex", "Engineer")]);
        published.Seq.ShouldBe(4);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishOnlyThatUser_WhenAUserIdIsGiven()
    {
        // Arrange
        var alice = TestDoubles.Administrator("alice");
        var bob = TestDoubles.Administrator("bob");
        var database = TestDoubles.Database([alice, bob], deletedUsers: [Deleted(TestDoubles.Administrator("carol"))]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command(bob.Id), Ct);

        // Assert
        TestDoubles.Published(database)
            .Cast<UserAccessPublished>()
            .Select(published => published.UserId)
            .ShouldBe([bob.Id]);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishTheTombstone_WhenTheUserWasDeleted()
    {
        // Arrange
        var carol = TestDoubles.Administrator("carol");
        var database = TestDoubles.Database([], deletedUsers: [Deleted(carol)]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command(carol.Id), Ct);

        // Assert
        var published = TestDoubles.Published(database).ShouldHaveSingleItem().ShouldBeOfType<UserAccessPublished>();
        published.UserId.ShouldBe(carol.Id);
        published.Username.ShouldBeNull();
        published.IsDeleted.ShouldBeTrue();
        published.IsAdministrator.ShouldBeFalse();
        published.Grants.ShouldBeEmpty();
        published.Seq.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishNothing_WhenIdentityHasNeverSeenTheUser()
    {
        // Arrange
        var database = TestDoubles.Database(
            [TestDoubles.Administrator()],
            deletedUsers: [Deleted(TestDoubles.Administrator("carol"))]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command(Guid.NewGuid()), Ct);

        // Assert
        TestDoubles.Published(database).ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishEveryUserAndEveryTombstone_WhenNoUserIdIsGiven()
    {
        // Arrange
        var alice = TestDoubles.Administrator("alice");
        var bob = TestDoubles.Administrator("bob");
        var carol = TestDoubles.Administrator("carol");
        var database = TestDoubles.Database([alice, bob], deletedUsers: [Deleted(carol)]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command(null), Ct);

        // Assert
        TestDoubles.Published(database)
            .Cast<UserAccessPublished>()
            .Select(published => (published.UserId, published.IsDeleted))
            .ShouldBe([(alice.Id, false), (bob.Id, false), (carol.Id, true)], ignoreOrder: true);
    }

    [Fact]
    public async Task HandleAsync_ShouldSkipTheUser_WhenNothingWasEverPublishedAboutThem()
    {
        // Arrange
        var newcomer = TestDoubles.Member("newcomer");
        var database = TestDoubles.Database([newcomer]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command(null), Ct);

        // Assert
        TestDoubles.Published(database).ShouldBeEmpty();
    }

    private static DeletedUser Deleted(User user)
    {
        user.Delete();

        return DeletedUser.Create(user);
    }
}
