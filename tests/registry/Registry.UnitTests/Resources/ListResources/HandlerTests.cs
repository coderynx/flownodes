using Registry.Application.Resources.ListResources;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Resources.ListResources;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldListOnlyTheKindSortedByName_WhenOtherKindsAndNamespacesHoldResources()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create(
            [Namespace.Create(NamespaceName.Create("acme"))],
            [
                Widgets.Stored("acme", "b"), Widgets.Stored("acme", "a"), Widgets.Stored("globex", "c"),
                Resource.Create(NamespaceName.Create("acme"), "Gizmo", ResourceName.Create("z"), Widgets.Json("{}"), 2)
            ],
            [caller]);

        // Act
        var result = await new Handler(database, WidgetKind.Catalog).HandleAsync(
            new Query(caller.Id, "acme", "widgets", null, null, false, 0, 50),
            Ct);

        // Assert
        result.Value.Items.Select(resource => resource.Name.Value).ShouldBe(["a", "b"]);
        result.Value.Total.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ShouldFilterReverseAndPage_WhenAsked()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create(
            [Namespace.Create(NamespaceName.Create("acme"))],
            [
                Widgets.Stored("acme", "pump-1"), Widgets.Stored("acme", "pump-2"), Widgets.Stored("acme", "pump-3"),
                Widgets.Stored("acme", "valve")
            ],
            [caller]);

        // Act
        var result = await new Handler(database, WidgetKind.Catalog).HandleAsync(
            new Query(caller.Id, "acme", "widgets", "pump", "name", true, 1, 1),
            Ct);

        // Assert
        result.Value.Items.Select(resource => resource.Name.Value).ShouldBe(["pump-2"]);
        result.Value.Total.ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnInvalidInput_WhenTheSortKeyIsUnknown()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))], users: [caller]);

        // Act
        var result = await new Handler(database, WidgetKind.Catalog).HandleAsync(
            new Query(caller.Id, "acme", "widgets", null, "size", false, 0, 50),
            Ct);

        // Assert
        result.Error.Code.ShouldBe("resource.unknown_sort_key");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNamespaceNotFound_WhenCallerHoldsNoRoleInTheNamespace()
    {
        // Arrange
        var caller = Callers.Member("globex");
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))], users: [caller]);

        // Act
        var result = await new Handler(database, WidgetKind.Catalog).HandleAsync(
            new Query(caller.Id, "acme", "widgets", null, null, false, 0, 50),
            Ct);

        // Assert
        result.Error.Code.ShouldBe("namespace.not_found");
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
