using System.Text.Json;
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

    private sealed class GadgetKind : IResourceKind
    {
        public string Name => "Gadget";
        public string Plural => "gadgets";
        public IEnumerable<SpecError> Validate(JsonElement spec) => [];
        public IEnumerable<SpecReference> References(JsonElement spec) => [];
    }
}
