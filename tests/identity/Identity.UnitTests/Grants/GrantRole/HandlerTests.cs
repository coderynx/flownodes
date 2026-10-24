using Coderynx.Functional.Results.Errors;
using Coderynx.Functional.Results.Successes;
using Core.Contracts.Identity;
using Identity.Application;
using Identity.Application.Grants.GrantRole;
using Identity.Domain.Namespaces;
using Identity.Domain.Users;
using Identity.UnitTests.Users;
using Xunit;

namespace Identity.UnitTests.Grants.GrantRole;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestUsers.Member("bob");
        var alice = TestUsers.Member("alice");
        var database = TestUsers.Database([bob, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(bob.Id, alice.Id, "Engineer", "default"), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
        alice.RoleGrants.ShouldBeEmpty();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsUnknown()
    {
        // Arrange
        var alice = TestUsers.Member("alice");
        var handler = new Handler(TestUsers.Database([alice]));

        // Act
        var result = await handler.HandleAsync(new Command(Guid.NewGuid(), alice.Id, "Administrator", null), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
        alice.IsAdministrator.ShouldBeFalse();
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
        var alice = TestUsers.Member("alice");
        var database = TestUsers.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, role, @namespace), Ct);

        // Assert
        result.Error.ShouldBe(expected);
        alice.IsAdministrator.ShouldBeFalse();
        alice.RoleGrants.ShouldBeEmpty();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldCheckTheRoleBeforeTheUser_WhenBothAreWrong()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var handler = new Handler(TestUsers.Database([administrator]));

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, Guid.NewGuid(), "Wizard", "default"), Ct);

        // Assert
        result.Error.ShouldBe(Errors.InvalidRole("Wizard"));
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var database = TestUsers.Database([administrator]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(
            new Command(administrator.Id, Guid.NewGuid(), "Engineer", "default"),
            Ct);

        // Assert
        result.Error.ShouldBe(Errors.UserNotFound());
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Theory]
    [InlineData("Administrator", null)]
    [InlineData("administrator", "")]
    public async Task
        HandleAsync_ShouldMakeTheUserAnAdministratorAndPublishAdministratorGranted_WhenTheRoleIsAdministrator(
            string role,
            string? @namespace)
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var alice = TestUsers.Member("alice");
        var database = TestUsers.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, role, @namespace), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Created);
        result.Value.ShouldBeSameAs(alice);
        alice.IsAdministrator.ShouldBeTrue();
        TestUsers.Published(database).ShouldBe([new AdministratorGranted(alice.Id, 1)]);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFoundWithoutSaving_WhenTheUserIsAlreadyAnAdministrator()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var other = TestUsers.Administrator("other");
        var database = TestUsers.Database([administrator, other]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, other.Id, "Administrator", null), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        other.Seq.ShouldBe(1);
        TestUsers.Published(database).ShouldBeEmpty();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Theory]
    [InlineData("Engineer", Role.Engineer)]
    [InlineData("operator", Role.Operator)]
    public async Task HandleAsync_ShouldGrantTheRoleAndPublishRoleGranted_WhenTheNamespaceIsTheDefault(
        string role,
        Role expected)
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var alice = TestUsers.Member("alice");
        var database = TestUsers.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, role, "default"), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Created);
        var grant = alice.RoleGrants.ShouldHaveSingleItem();
        grant.Namespace.ShouldBe("default");
        grant.Role.ShouldBe(expected);
        TestUsers.Published(database).ShouldBe([new RoleGranted(alice.Id, "default", expected.ToString(), 1)]);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldGrantTheRole_WhenTheRegistryAnnouncedTheNamespace()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var alice = TestUsers.Member("alice");
        var plant = KnownNamespace.Create("plant", Guid.NewGuid(), 1);
        var database = TestUsers.Database([administrator, alice], namespaces: [plant]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "Engineer", "plant"), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Created);
        TestUsers.Published(database).ShouldBe([new RoleGranted(alice.Id, "plant", "Engineer", 1)]);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnBadRequest_WhenTheNamespaceIsUnknown()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var alice = TestUsers.Member("alice");
        var plant = KnownNamespace.Create("plant", Guid.NewGuid(), 1);
        var database = TestUsers.Database([administrator, alice], namespaces: [plant]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "Engineer", "mine"), Ct);

        // Assert
        result.Error.ShouldBe(Errors.UnknownNamespace("mine"));
        alice.RoleGrants.ShouldBeEmpty();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFoundWithoutSaving_WhenTheUserAlreadyHoldsTheRole()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var alice = TestUsers.Member("alice");
        alice.Grant("default", Role.Operator);
        var database = TestUsers.Database([administrator, alice]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(administrator.Id, alice.Id, "Operator", "default"), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        alice.Seq.ShouldBe(1);
        TestUsers.Published(database).ShouldBeEmpty();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(Ct);
    }

    public static TheoryData<string?, string?, Error> MisfitRoles() =>
        new()
        {
            { "Engineer", null, Errors.NamespaceRequired("Engineer") },
            { "engineer", "", Errors.NamespaceRequired("Engineer") },
            { "Operator", null, Errors.NamespaceRequired("Operator") },
            { "Administrator", "default", Errors.NamespaceNotAllowed() },
            { "Wizard", "default", Errors.InvalidRole("Wizard") },
            { null, "default", Errors.InvalidRole(string.Empty) },
            { "7", "default", Errors.InvalidRole("7") },
            { "Operator, Engineer", "default", Errors.InvalidRole("Operator, Engineer") }
        };
}
