using Coderynx.Functional.Results.Errors;
using Coderynx.Functional.Results.Successes;
using Core.Contracts.Identity;
using Identity.Application;
using Identity.Application.Grants.RevokeRole;
using Identity.Domain.Users;
using Xunit;

namespace Identity.UnitTests.Grants.RevokeRole;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestDoubles.Member("bob");
        var alice = TestDoubles.Member("alice");
        alice.Grant("default", Role.Engineer);
        var database = TestDoubles.Database([bob, alice]);
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
        var administrator = TestDoubles.Disabled(TestDoubles.Administrator());
        var other = TestDoubles.Administrator("other");
        var handler = new Handler(TestDoubles.Database([administrator, other]));

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
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Administrator("other");
        var database = TestDoubles.Database([administrator, other]);
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
        var administrator = TestDoubles.Administrator();
        var handler = new Handler(TestDoubles.Database([administrator]));

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
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var database = TestDoubles.Database([administrator, alice]);
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
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Disabled(TestDoubles.Administrator("other"));
        var database = TestDoubles.Database([administrator, other]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(
            new Command(administrator.Id, administrator.Id, "Administrator", null),
            Ct);

        // Assert
        result.Error.ShouldBe(Errors.LastAdministrator());
        administrator.IsAdministrator.ShouldBeTrue();
        TestDoubles.Published(database).ShouldBeEmpty();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRevokeAdministratorAndPublishAdministratorRevoked_WhenAnotherActiveAdministratorExists()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Administrator("other");
        var database = TestDoubles.Database([administrator, other]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id, "administrator", ""), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        other.IsAdministrator.ShouldBeFalse();
        TestDoubles.Published(database).ShouldBe([new AdministratorRevoked(other.Id, 2)]);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRevokeAdministrator_WhenTheUserIsADisabledAdministrator()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Disabled(TestDoubles.Administrator("other"));
        var database = TestDoubles.Database([administrator, other]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id, "Administrator", null), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        other.IsAdministrator.ShouldBeFalse();
        TestDoubles.Published(database).ShouldBe([new AdministratorRevoked(other.Id, 3)]);
    }

    [Fact]
    public async Task HandleAsync_ShouldRevokeTheRoleAndPublishRoleRevoked_WhenTheUserHoldsIt()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        alice.Grant("default", Role.Operator);
        alice.Grant("default", Role.Engineer);
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "operator", "default"), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        alice.RoleGrants.ShouldHaveSingleItem().Role.ShouldBe(Role.Engineer);
        TestDoubles.Published(database).ShouldBe([new RoleRevoked(alice.Id, "default", "Operator", 3)]);
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
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        alice.Grant("default", Role.Operator);
        var database = TestDoubles.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, role, @namespace), Ct);

        // Assert
        result.Error.ShouldBe(Errors.RoleNotHeld());
        alice.RoleGrants.Count.ShouldBe(1);
        TestDoubles.Published(database).ShouldBeEmpty();
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
