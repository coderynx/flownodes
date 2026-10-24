using Coderynx.Functional.Results.Successes;
using Core.Contracts.Identity;
using Identity.Application.Namespaces.RemoveNamespace;
using Identity.Domain.Namespaces;
using Identity.Domain.Users;
using Xunit;

namespace Identity.UnitTests.Namespaces.RemoveNamespace;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldForgetTheNamespace_WhenTheDeletionIsAboutItAndNewer()
    {
        // Arrange
        var known = KnownNamespace.Create("acme", NamespaceDoubles.Uid, 1);
        var database = NamespaceDoubles.Database([known], []);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(
            new Command("acme", NamespaceDoubles.Uid, 2),
            TestContext.Current.CancellationToken);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        database.KnownNamespaces.Received(1).Remove(known);
        await database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldRevokeEveryRoleInTheNamespaceAndPublishEachWithItsSeq_WhenHoldersExist()
    {
        // Arrange
        var holder = NamespaceDoubles.UserWith(
            "alice",
            ("acme", Role.Engineer),
            ("acme", Role.Operator),
            ("globex", Role.Operator));
        var database = NamespaceDoubles.Database([KnownNamespace.Create("acme", NamespaceDoubles.Uid, 1)], [holder]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command("acme", NamespaceDoubles.Uid, 2), TestContext.Current.CancellationToken);

        // Assert
        holder.RoleGrants.Select(grant => (grant.Namespace, grant.Role)).ShouldBe([("globex", Role.Operator)]);
        holder.Seq.ShouldBe(5);
        await database.Received(1).PublishAsync(new RoleRevoked(holder.Id, "acme", "Engineer", 4));
        await database.Received(1).PublishAsync(new RoleRevoked(holder.Id, "acme", "Operator", 5));
        await database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldLeaveOtherUsersAlone_WhenTheyHoldNoRoleInTheNamespace()
    {
        // Arrange
        var bystander = NamespaceDoubles.UserWith("bob", ("globex", Role.Engineer));
        var database = NamespaceDoubles.Database([KnownNamespace.Create("acme", NamespaceDoubles.Uid, 1)], [bystander]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command("acme", NamespaceDoubles.Uid, 2), TestContext.Current.CancellationToken);

        // Assert
        bystander.RoleGrants.Count.ShouldBe(1);
        bystander.Seq.ShouldBe(1);
        database.ReceivedCalls().ShouldNotContain(call => call.GetMethodInfo().Name == "PublishAsync");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    public async Task HandleAsync_ShouldKeepTheNamespace_WhenTheDeletionIsNotNewer(long seq)
    {
        // Arrange
        var known = KnownNamespace.Create("acme", NamespaceDoubles.Uid, 1);
        var database = NamespaceDoubles.Database([known], []);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(
            new Command("acme", NamespaceDoubles.Uid, seq),
            TestContext.Current.CancellationToken);

        // Assert
        database.KnownNamespaces.DidNotReceiveWithAnyArgs().Remove(null!);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepTheNamespace_WhenTheDeletionIsAboutAnEarlierIncarnation()
    {
        // Arrange
        var known = KnownNamespace.Create("acme", NamespaceDoubles.Uid, 1);
        var database = NamespaceDoubles.Database([known], []);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command("acme", Guid.NewGuid(), 9), TestContext.Current.CancellationToken);

        // Assert
        database.KnownNamespaces.DidNotReceiveWithAnyArgs().Remove(null!);
    }

    [Fact]
    public async Task HandleAsync_ShouldStillDropTheGrants_WhenTheNamespaceIsNotKnown()
    {
        // Arrange
        var holder = NamespaceDoubles.UserWith("alice", ("acme", Role.Engineer));
        var database = NamespaceDoubles.Database([], [holder]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(
            new Command("acme", NamespaceDoubles.Uid, 2),
            TestContext.Current.CancellationToken);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        holder.RoleGrants.ShouldBeEmpty();
        await database.Received(1).PublishAsync(new RoleRevoked(holder.Id, "acme", "Engineer", 2));
        database.KnownNamespaces.DidNotReceiveWithAnyArgs().Remove(null!);
    }

    [Fact]
    public async Task HandleAsync_ShouldIgnoreTheMessage_WhenItIsAboutTheDefaultNamespace()
    {
        // Arrange
        var holder = NamespaceDoubles.UserWith("alice", (KnownNamespace.Default, Role.Engineer));
        var known = KnownNamespace.Create(KnownNamespace.Default, NamespaceDoubles.Uid, 1);
        var database = NamespaceDoubles.Database([known], [holder]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(
            new Command(KnownNamespace.Default, NamespaceDoubles.Uid, 2),
            TestContext.Current.CancellationToken);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        holder.RoleGrants.Count.ShouldBe(1);
        database.KnownNamespaces.DidNotReceiveWithAnyArgs().Remove(null!);
        database.ReceivedCalls().ShouldNotContain(call => call.GetMethodInfo().Name == "PublishAsync");
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
