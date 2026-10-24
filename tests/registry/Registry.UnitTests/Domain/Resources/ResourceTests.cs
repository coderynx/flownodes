using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class ResourceTests
{
    [Fact]
    public void Create_ShouldHoldNamespaceKindAndName()
    {
        // Arrange
        var name = NamespaceName.Create("acme");

        // Act
        var resource = Resource.Create(name, "Asset", "pump-1");

        // Assert
        resource.Namespace.ShouldBe(name);
        resource.Kind.ShouldBe("Asset");
        resource.Name.ShouldBe("pump-1");
    }
}
