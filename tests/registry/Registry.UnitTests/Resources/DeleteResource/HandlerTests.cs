using Core.Contracts.Resources;
using Core.Domain.Authorization;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Application.Resources.DeleteResource;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Resources.DeleteResource;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldDeleteTheResource_WhenItsKindIsGivenAsAPlural()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var widget = Widgets.Stored("acme", "w-1");
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))], [widget], [caller]);

        // Act
        var result = await Handler(database).HandleAsync(new Command(caller.Id, "acme", "widgets", "w-1", false), Ct);

        // Assert
        result.Value.ShouldBe(new DeletedResource(WidgetKind.Kind, "acme", "w-1"));
        database.Resources.Received(1).Remove(widget);
        await database.Received(1).PublishAsync(Arg.Any<ResourceDeleted>());
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteTheNamespace_WhenTheKindIsNamespaces()
    {
        // Arrange
        var caller = Callers.Administrator();
        var acme = Namespace.Create(NamespaceName.Create("acme"));
        var database = TestDatabase.Create([acme], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(new Command(caller.Id, null, "namespaces", "acme", false), Ct);

        // Assert
        result.Value.ShouldBe(new DeletedResource(Namespace.Kind, null, "acme"));
        database.Namespaces.Received(1).Remove(acme);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheKindIsUnknown()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(new Command(caller.Id, "acme", "gizmos", "g-1", false), Ct);

        // Assert
        result.Error.Code.ShouldBe("kind.not_found");
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheNameIsNotADnsLabel()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(new Command(caller.Id, "acme", "widgets", "W 1", false), Ct);

        // Assert
        result.Error.Code.ShouldBe("resource.not_found");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenAnOperatorDeletesAnInvalidName()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(new Command(caller.Id, "acme", "widgets", "W 1", false), Ct);

        // Assert
        result.Error.Code.ShouldBe(Core.Application.Authorization.AccessErrors.ForbiddenCode);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteTheResource_WhenItsKindIsGivenInTheSingular()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var database = TestDatabase.Create(
            [Namespace.Create(NamespaceName.Create("acme"))],
            [Widgets.Stored("acme", "w-1")],
            [caller]);

        // Act
        var result = await Handler(database).HandleAsync(new Command(caller.Id, "acme", "widget", "w-1", false), Ct);

        // Assert
        result.Value.Kind.ShouldBe(WidgetKind.Kind);
    }

    [Fact]
    public async Task HandleAsync_ShouldRemoveTheValues_WhenASecretIsDeleted()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var acme = NamespaceName.Create("acme");
        var secret = Resource.Create(
            acme,
            SecretKind.KindName,
            ResourceName.Create("db"),
            Widgets.Json("""{"data": {"password": "fp"}}"""),
            2);
        var value = SecretValue.Create(acme, secret.Name, "password", [1]);
        var other = SecretValue.Create(acme, ResourceName.Create("other"), "password", [2]);
        var database = TestDatabase.Create([Namespace.Create(acme)], [secret], [caller], secretValues: [value, other]);

        // Act
        var result = await Handler(database).HandleAsync(new Command(caller.Id, "acme", "secrets", "db", false), Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        database.SecretValues.Received(1)
            .RemoveRange(Arg.Is<IEnumerable<SecretValue>>(values => values.SequenceEqual(new[] { value })));
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuse_WhenAWidgetStillReferencesTheSecret()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var acme = NamespaceName.Create("acme");
        var secret = Resource.Create(
            acme,
            SecretKind.KindName,
            ResourceName.Create("db"),
            Widgets.Json("""{"data": {"password": "fp"}}"""),
            2);
        var widget = Widgets.Stored("acme", "w-1", """{"secret": "db"}""");
        var database = TestDatabase.Create([Namespace.Create(acme)], [secret, widget], [caller]);

        // Act
        var result = await Handler(database).HandleAsync(new Command(caller.Id, "acme", "secrets", "db", false), Ct);

        // Assert
        result.Error.ShouldBeOfType<DeletionRefused>();
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static Handler Handler(IApplicationDbContext database) =>
        new(database, WidgetKind.Catalog, ResourceHandlers.Time(), ResourceHandlers.Retention());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
