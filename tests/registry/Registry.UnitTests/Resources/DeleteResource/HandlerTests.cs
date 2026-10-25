using Core.Contracts.Resources;
using Core.Domain.Authorization;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Application.Resources.DeleteResource;
using Registry.Domain.Namespaces;
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

    private static Handler Handler(IApplicationDbContext database) =>
        new(database, WidgetKind.Catalog, ResourceHandlers.Time(), ResourceHandlers.Retention());

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
