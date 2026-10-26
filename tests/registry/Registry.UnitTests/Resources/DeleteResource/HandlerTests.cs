using Core.Contracts.Resources;
using Core.Domain.Authorization;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Application.Resources.DeleteResource;
using Registry.Application.Secrets;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Resources.DeleteResource;

public sealed class HandlerTests
{
    private readonly ISecretStorage _secrets = Substitute.For<ISecretStorage>();
    private readonly IRevisionLog _revisions = Substitute.For<IRevisionLog>();

    [Fact]
    public async Task HandleAsync_ShouldDeleteTheResource_WhenItsKindIsGivenAsAPlural()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var widget = Widgets.Stored("acme", "w-1");
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))], [widget]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Command(caller.Id, "acme", "widgets", "w-1", false), Ct);

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
        var database = TestDatabase.Create([acme]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Command(caller.Id, null, "namespaces", "acme", false), Ct);

        // Assert
        result.Value.ShouldBe(new DeletedResource(Namespace.Kind, null, "acme"));
        database.Namespaces.Received(1).Remove(acme);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheKindIsUnknown()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Command(caller.Id, "acme", "gizmos", "g-1", false), Ct);

        // Assert
        result.Error.Code.ShouldBe("kind.not_found");
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheNameIsNotADnsLabel()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Command(caller.Id, "acme", "widgets", "W 1", false), Ct);

        // Assert
        result.Error.Code.ShouldBe("resource.not_found");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheAccessError_WhenTheCallerMayNotDeleteAndTheNameIsInvalid()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))]);
        var access = ResourceHandlers.Access();
        access.AuthoriseChangesAsync(Arg.Any<Guid>(), Arg.Any<ManifestReading>(), Ct)
            .Returns(Errors.NamespaceNotFound("acme"));

        // Act
        var result = await Handler(database, access)
            .HandleAsync(new Command(caller.Id, "acme", "widgets", "W 1", false), Ct);

        // Assert
        result.Error.Code.ShouldBe("namespace.not_found");
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteTheResource_WhenItsKindIsGivenInTheSingular()
    {
        // Arrange
        var caller = Callers.Member("acme", NamespaceRole.Engineer);
        var database = TestDatabase.Create(
            [Namespace.Create(NamespaceName.Create("acme"))],
            [Widgets.Stored("acme", "w-1")]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Command(caller.Id, "acme", "widget", "w-1", false), Ct);

        // Assert
        result.Value.Kind.ShouldBe(WidgetKind.Kind);
    }

    [Fact]
    public async Task HandleAsync_ShouldForgetTheValues_WhenASecretIsDeleted()
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
        var database = TestDatabase.Create([Namespace.Create(acme)], [secret]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Command(caller.Id, "acme", "secrets", "db", false), Ct);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _secrets.Received(1)
            .ForgetAsync(Arg.Is<ManifestEntry>(entry => entry.Name == secret.Name && entry.Namespace == acme), Ct);
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
        var database = TestDatabase.Create([Namespace.Create(acme)], [secret, widget]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Command(caller.Id, "acme", "secrets", "db", false), Ct);

        // Assert
        result.Error.ShouldBeOfType<DeletionRefused>();
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private Handler Handler(IApplicationDbContext database, UserAccess caller) =>
        Handler(database, ResourceHandlers.Access(caller));

    private Handler Handler(IApplicationDbContext database, IResourceAccess access) =>
        new(WidgetKind.Catalog, access, ResourceHandlers.Deletion(database, access, _secrets, _revisions));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
