using Coderynx.Functional.Results.Successes;
using Core.Application.Authorization;
using Core.Contracts.Identity;
using Core.Domain.Authorization;
using Registry.Application.Access.ApplyIdentityMessage;
using Xunit;

namespace Registry.UnitTests.Access.ApplyIdentityMessage;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldStartReplicaAndSave_WhenUserIsNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var database = TestDatabase.Create();
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        var result = await handler.HandleAsync(new Command(new RoleGranted(userId, "acme", "Engineer", 1)), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        database.UserAccess.Received(1)
            .Add(Arg.Is<UserAccess>(user => user.Id == userId && user.Holds("acme", NamespaceRole.Engineer)));
        await ((IUserAccessSource)database).Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldGrantRole_WhenMessageIsRoleGranted()
    {
        // Arrange
        var user = UserAccess.Create(Guid.NewGuid());
        var database = TestDatabase.Create(users: [user]);
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        await handler.HandleAsync(new Command(new RoleGranted(user.Id, "acme", "Operator", 1)), Ct);

        // Assert
        user.Holds("acme", NamespaceRole.Operator).ShouldBeTrue();
        database.UserAccess.DidNotReceive().Add(Arg.Any<UserAccess>());
        await ((IUserAccessSource)database).Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRevokeRole_WhenMessageIsRoleRevoked()
    {
        // Arrange
        var user = UserAccess.Create(Guid.NewGuid());
        user.GrantRole("acme", NamespaceRole.Operator, 1);
        user.GrantRole("acme", NamespaceRole.Engineer, 2);
        var database = TestDatabase.Create(users: [user]);
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        await handler.HandleAsync(new Command(new RoleRevoked(user.Id, "acme", "Engineer", 3)), Ct);

        // Assert
        user.Holds("acme", NamespaceRole.Engineer).ShouldBeFalse();
        user.Holds("acme", NamespaceRole.Operator).ShouldBeTrue();
        await ((IUserAccessSource)database).Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldGrantAdministrator_WhenMessageIsAdministratorGranted()
    {
        // Arrange
        var user = UserAccess.Create(Guid.NewGuid());
        var database = TestDatabase.Create(users: [user]);
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        await handler.HandleAsync(new Command(new AdministratorGranted(user.Id, 1)), Ct);

        // Assert
        user.IsAdministrator.ShouldBeTrue();
        await ((IUserAccessSource)database).Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRevokeAdministrator_WhenMessageIsAdministratorRevoked()
    {
        // Arrange
        var user = Callers.Administrator();
        var database = TestDatabase.Create(users: [user]);
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        await handler.HandleAsync(new Command(new AdministratorRevoked(user.Id, 2)), Ct);

        // Assert
        user.IsAdministrator.ShouldBeFalse();
        await ((IUserAccessSource)database).Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldDisableUser_WhenMessageIsUserDisabled()
    {
        // Arrange
        var user = UserAccess.Create(Guid.NewGuid());
        var database = TestDatabase.Create(users: [user]);
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        await handler.HandleAsync(new Command(new UserDisabled(user.Id, 1)), Ct);

        // Assert
        user.IsDisabled.ShouldBeTrue();
        await ((IUserAccessSource)database).Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldEnableUser_WhenMessageIsUserEnabled()
    {
        // Arrange
        var user = UserAccess.Create(Guid.NewGuid());
        user.Disable(1);
        var database = TestDatabase.Create(users: [user]);
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        await handler.HandleAsync(new Command(new UserEnabled(user.Id, 2)), Ct);

        // Assert
        user.IsDisabled.ShouldBeFalse();
        await ((IUserAccessSource)database).Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteUserAndDropRoles_WhenMessageIsUserDeleted()
    {
        // Arrange
        var user = Callers.Administrator();
        user.GrantRole("acme", NamespaceRole.Operator, 2);
        var database = TestDatabase.Create(users: [user]);
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        await handler.HandleAsync(new Command(new UserDeleted(user.Id, 3)), Ct);

        // Assert
        user.IsDeleted.ShouldBeTrue();
        user.IsAdministrator.ShouldBeFalse();
        user.Grants.ShouldBeEmpty();
        await ((IUserAccessSource)database).Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRecordUsername_WhenMessageIsUserRenamed()
    {
        // Arrange
        var user = UserAccess.Create(Guid.NewGuid());
        var database = TestDatabase.Create(users: [user]);
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        await handler.HandleAsync(new Command(new UserRenamed(user.Id, "olive", 1)), Ct);

        // Assert
        user.Username.ShouldBe("olive");
        await ((IUserAccessSource)database).Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldChangeNothing_WhenMessageIsNotNewerThanLastApplied()
    {
        // Arrange
        var user = UserAccess.Create(Guid.NewGuid());
        user.Disable(5);
        var database = TestDatabase.Create(users: [user]);
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        await handler.HandleAsync(new Command(new UserEnabled(user.Id, 5)), Ct);

        // Assert
        user.IsDisabled.ShouldBeTrue();
        user.LastSeq.ShouldBe(5);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUpdatedWithoutSaving_WhenMessageIsNotAnIdentityMessage()
    {
        // Arrange
        var database = TestDatabase.Create();
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        var result = await handler.HandleAsync(new Command("not a message"), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        database.UserAccess.DidNotReceive().Add(Arg.Any<UserAccess>());
        await ((IUserAccessSource)database).DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowArgumentException_WhenRoleIsUnknown()
    {
        // Arrange
        var user = UserAccess.Create(Guid.NewGuid());
        var database = TestDatabase.Create(users: [user]);
        var handler = new Handler(new UserAccessProjection(database));

        // Act
        var act = () => handler.HandleAsync(new Command(new RoleGranted(user.Id, "acme", "Owner", 1)), Ct);

        // Assert
        await act.ShouldThrowAsync<ArgumentException>();
        await ((IUserAccessSource)database).DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
