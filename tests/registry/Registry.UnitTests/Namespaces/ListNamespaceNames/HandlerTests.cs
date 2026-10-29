using Registry.Application.Namespaces.ListNamespaceNames;
using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Namespaces.ListNamespaceNames;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnTheNameOfEveryNamespace_WhenNamespacesExist()
    {
        // Arrange
        var database = TestDatabase.Create(
            [Namespace.Create(NamespaceName.Create("acme")), Namespace.Create(NamespaceName.Create("globex"))]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Query(), Ct);

        // Assert
        result.Value.ShouldBe(["acme", "globex"], ignoreOrder: true);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNoName_WhenNoNamespaceExists()
    {
        // Arrange
        var database = TestDatabase.Create([]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Query(), Ct);

        // Assert
        result.Value.ShouldBeEmpty();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
