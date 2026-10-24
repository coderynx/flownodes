using Coderynx.Functional.Results.Errors;
using Coderynx.Functional.Results.Successes;
using Core.Contracts.Identity;
using Identity.Application;
using Identity.Application.Grants.RevokeRole;
using Identity.Domain.Users;
using Identity.UnitTests.Users;
using Xunit;

namespace Identity.UnitTests.Grants.RevokeRole;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestUsers.Member("bob");
        var alice = TestUsers.Member("alice");
        alice.Grant("default", Role.Engineer);
        var database = TestUsers.Database([bob, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(bob.Id, alice.Id, "Engineer", "default"), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
        alice.RoleGrants.Count.ShouldBe(1);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsDisabled()
    {
        // Arrange
        var administrator = TestUsers.Disabled(TestUsers.Administrator());
        var other = TestUsers.Administrator("other");
        var handler = new Handler(TestUsers.Database([administrator, other]));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id, "Administrator", null), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
        other.IsAdministrator.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(MisfitRoles))]
    public async Task HandleAsync_ShouldReturnBadRequest_WhenRoleAndNamespaceDoNotFit(
        string? role,
        string? @namespace,
        Error expected)
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var other = TestUsers.Administrator("other");
        var database = TestUsers.Database([administrator, other]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id, role, @namespace), Ct);

        // Assert
        result.Error.ShouldBe(expected);
        other.IsAdministrator.ShouldBeTrue();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var handler = new Handler(TestUsers.Database([administrator]));

        // Act
        var result = await handler.HandleAsync(
            new Command(administrator.Id, Guid.NewGuid(), "Engineer", "default"),
            Ct);

        // Assert
        result.Error.ShouldBe(Errors.UserNotFound());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheUserIsNotAnAdministrator()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var alice = TestUsers.Member("alice");
        var database = TestUsers.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "Administrator", null), Ct);

        // Assert
        result.Error.ShouldBe(Errors.RoleNotHeld());
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenTheUserIsTheLastActiveAdministrator()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var other = TestUsers.Disabled(TestUsers.Administrator("other"));
        var database = TestUsers.Database([administrator, other]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(
            new Command(administrator.Id, administrator.Id, "Administrator", null),
            Ct);

        // Assert
        result.Error.ShouldBe(Errors.LastAdministrator());
        administrator.IsAdministrator.ShouldBeTrue();
        TestUsers.Published(database).ShouldBeEmpty();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRevokeAdministratorAndPublishAdministratorRevoked_WhenAnotherActiveAdministratorExists()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var other = TestUsers.Administrator("other");
        var database = TestUsers.Database([administrator, other]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id, "administrator", ""), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        other.IsAdministrator.ShouldBeFalse();
        TestUsers.Published(database).ShouldBe([new AdministratorRevoked(other.Id, 2)]);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRevokeAdministrator_WhenTheUserIsADisabledAdministrator()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var other = TestUsers.Disabled(TestUsers.Administrator("other"));
        var database = TestUsers.Database([administrator, other]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id, "Administrator", null), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        other.IsAdministrator.ShouldBeFalse();
        TestUsers.Published(database).ShouldBe([new AdministratorRevoked(other.Id, 3)]);
    }

    [Fact]
    public async Task HandleAsync_ShouldRevokeTheRoleAndPublishRoleRevoked_WhenTheUserHoldsIt()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var alice = TestUsers.Member("alice");
        alice.Grant("default", Role.Operator);
        alice.Grant("default", Role.Engineer);
        var database = TestUsers.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "operator", "default"), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        alice.RoleGrants.ShouldHaveSingleItem().Role.ShouldBe(Role.Engineer);
        TestUsers.Published(database).ShouldBe([new RoleRevoked(alice.Id, "default", "Operator", 3)]);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Theory]
    [InlineData("Engineer", "default")]
    [InlineData("Operator", "plant")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheUserDoesNotHoldTheRoleInTheNamespace(
        string role,
        string @namespace)
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var alice = TestUsers.Member("alice");
        alice.Grant("default", Role.Operator);
        var database = TestUsers.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, role, @namespace), Ct);

        // Assert
        result.Error.ShouldBe(Errors.RoleNotHeld());
        alice.RoleGrants.Count.ShouldBe(1);
        TestUsers.Published(database).ShouldBeEmpty();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    public static TheoryData<string?, string?, Error> MisfitRoles() =>
        new()
        {
            { "Engineer", null, Errors.NamespaceRequired("Engineer") },
            { "Administrator", "default", Errors.NamespaceNotAllowed() },
            { "Wizard", "default", Errors.InvalidRole("Wizard") },
            { null, null, Errors.InvalidRole(string.Empty) }
        };
}
