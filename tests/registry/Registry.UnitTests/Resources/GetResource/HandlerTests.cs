using Core.Domain.Authorization;
using Registry.Application.Resources.GetResource;
using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Resources.GetResource;

public sealed class HandlerTests
{
    [Theory]
    [InlineData(NamespaceRole.Operator)]
    [InlineData(NamespaceRole.Engineer)]
    public async Task HandleAsync_ShouldReturnTheResource_WhenCallerHoldsAnyRoleInTheNamespace(NamespaceRole role)
    {
        // Arrange
        var caller = Callers.Member("acme", role);
        var widget = Widgets.Stored("acme", "w-1", """{"size": 1}""");
        var database = TestDatabase.Create([Acme()], [widget, Widgets.Stored("globex", "w-1")]);

        // Act
        var result = await new Handler(database, AccessDoubles.ReadsFor("acme")).HandleAsync(
            new Query(caller.Id, "acme", "widgets", "w-1"),
            Ct);

        // Assert
        result.Value.ShouldBeSameAs(widget);
    }

    [Theory]
    [InlineData("gizmos", "w-1", "kind.not_found")]
    [InlineData("widgets", "w-2", "resource.not_found")]
    [InlineData("widgets", "W_1", "resource.not_found")]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheKindOrResourceDoesNotExist(
        string kinds,
        string name,
        string code)
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "w-1")]);

        // Act
        var result = await new Handler(database, AccessDoubles.ReadsFor("acme")).HandleAsync(
            new Query(caller.Id, "acme", kinds, name),
            Ct);

        // Assert
        result.Error.Code.ShouldBe(code);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheError_WhenTheAccessCheckFails()
    {
        // Arrange
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "w-1")]);
        var access = AccessDoubles.ReadsFailingWith(AccessDoubles.NamespaceNotFound());

        // Act
        var result = await new Handler(database, access).HandleAsync(
            new Query(Guid.NewGuid(), "acme", "widgets", "w-1"),
            Ct);

        // Assert
        result.Error.Code.ShouldBe("namespace.not_found");
    }

    private static Namespace Acme() => Namespace.Create(NamespaceName.Create("acme"));
    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
