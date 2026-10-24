using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Domain.Namespaces;

public sealed class NamespaceTests
{
    [Fact]
    public void Create_ShouldStartAtGenerationAndSeqOne()
    {
        // Arrange
        var name = NamespaceName.Create("acme");

        // Act
        var created = Namespace.Create(name);

        // Assert
        created.Name.ShouldBe(name);
        created.Generation.ShouldBe(1);
        created.Seq.ShouldBe(1);
        created.Uid.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_ShouldGiveEachIncarnationItsOwnUid_WhenNameIsReused()
    {
        // Arrange
        var name = NamespaceName.Create("acme");

        // Act
        var first = Namespace.Create(name);
        var second = Namespace.Create(name);

        // Assert
        first.Uid.ShouldNotBe(second.Uid);
    }

    [Fact]
    public void Delete_ShouldAdvanceSeqAndKeepGeneration_WhenNamespaceIsNotDefault()
    {
        // Arrange
        var created = Namespace.Create(NamespaceName.Create("acme"));

        // Act
        created.Delete();

        // Assert
        created.Seq.ShouldBe(2);
        created.Generation.ShouldBe(1);
    }

    [Fact]
    public void Delete_ShouldThrowInvalidOperationException_WhenNamespaceIsDefault()
    {
        // Arrange
        var created = Namespace.Create(NamespaceName.Create(NamespaceName.Default));

        // Act
        var act = created.Delete;

        // Assert
        act.ShouldThrow<InvalidOperationException>();
        created.Seq.ShouldBe(1);
    }
}
