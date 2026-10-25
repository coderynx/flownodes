using System.Text.Json;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class KindCatalogTests
{
    [Fact]
    public void Find_ShouldReturnTheKind_WhenGivenItsNameOrPlural()
    {
        // Arrange
        var catalog = new KindCatalog([new GadgetKind()]);

        // Act
        var byName = catalog.Find("Gadget");
        var byPlural = catalog.FindByPlural("gadgets");

        // Assert
        byName.ShouldBeOfType<GadgetKind>();
        byPlural.ShouldBeOfType<GadgetKind>();
    }

    [Theory]
    [InlineData("gadget")]
    [InlineData("Gizmo")]
    [InlineData("Namespace")]
    public void Find_ShouldReturnNull_WhenTheNameIsNotANamespacedKind(string name)
    {
        // Arrange
        var catalog = new KindCatalog([new GadgetKind()]);

        // Act
        var kind = catalog.Find(name);

        // Assert
        kind.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenTwoKindsShareAName()
    {
        // Act
        var act = () => new KindCatalog([new GadgetKind(), new GadgetKind()]);

        // Assert
        act.ShouldThrow<ArgumentException>();
    }

    [Theory]
    [InlineData("gadgets")]
    [InlineData("gadget")]
    public void FindByRoute_ShouldReturnTheKind_WhenGivenItsPluralOrLowercaseName(string route)
    {
        // Arrange
        var catalog = new KindCatalog([new GadgetKind()]);

        // Act
        var kind = catalog.FindByRoute(route);

        // Assert
        kind.ShouldBeOfType<GadgetKind>();
    }

    [Fact]
    public void Referrers_ShouldReturnTheResourcesWhoseSpecReferencesTheTarget_WhenInTheSameNamespace()
    {
        // Arrange
        var catalog = new KindCatalog([new GadgetKind()]);
        var acme = NamespaceName.Create("acme");
        var referrer = Resource.Create(acme, "Gadget", ResourceName.Create("a"), Json("""{"parent": "root"}"""), 2);
        var other = Resource.Create(acme, "Gadget", ResourceName.Create("b"), Json("{}"), 3);
        var elsewhere = Resource.Create(
            NamespaceName.Create("globex"),
            "Gadget",
            ResourceName.Create("c"),
            Json("""{"parent": "root"}"""),
            2);

        // Act
        var referrers = catalog.Referrers(acme, "Gadget", ResourceName.Create("root"), [referrer, other, elsewhere]);

        // Assert
        referrers.ShouldBe([referrer]);
    }

    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private sealed class GadgetKind : IResourceKind
    {
        public string Name => "Gadget";
        public string Plural => "gadgets";
        public IEnumerable<SpecError> Validate(JsonElement spec, NamespaceSnapshot @namespace) => [];

        public IEnumerable<SpecReference> References(JsonElement spec) =>
            spec.TryGetProperty("parent", out var parent)
                ? [new SpecReference(Name, parent.GetString()!, "spec.parent")]
                : [];
    }
}
