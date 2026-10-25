using System.Text.Json;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class NamespaceSnapshotTests
{
    [Fact]
    public void Find_ShouldReturnTheSpec_WhenTheResourceIsInTheSnapshot()
    {
        // Arrange
        var snapshot = Snapshot();

        // Act
        var found = snapshot.Find("Widget", "w-1");
        var missing = snapshot.Find("Widget", "w-9");

        // Assert
        found!.Value.GetProperty("size").GetInt32().ShouldBe(1);
        missing.ShouldBeNull();
    }

    [Fact]
    public void OfKind_ShouldListOnlyThatKind()
    {
        // Arrange
        var snapshot = Snapshot();

        // Act
        var widgets = snapshot.OfKind("Widget");

        // Assert
        widgets.Select(resource => resource.Name.Value).ShouldBe(["w-1"]);
    }

    private static NamespaceSnapshot Snapshot() =>
        new(
            NamespaceName.Create("acme"),
            [
                new SnapshotResource("Widget", ResourceName.Create("w-1"), Json("""{"size": 1}""")),
                new SnapshotResource("Gizmo", ResourceName.Create("g-1"), Json("{}"))
            ]);

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
