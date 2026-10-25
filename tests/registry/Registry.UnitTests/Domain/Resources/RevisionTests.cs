using System.Text.Json;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Domain.Resources;

public sealed class RevisionTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly RevisionAuthor Author = new(Guid.NewGuid(), "erin");

    [Fact]
    public void Applied_ShouldRecordSpecGenerationAuthorAndTime()
    {
        // Arrange
        var resource = Resource.Create(
            NamespaceName.Create("acme"),
            "Widget",
            ResourceName.Create("w-1"),
            JsonDocument.Parse("""{"size": 1}""").RootElement,
            3);

        // Act
        var revision = Revision.Applied(resource, Author, Now);

        // Assert
        revision.Namespace.Value.ShouldBe("acme");
        revision.Kind.ShouldBe("Widget");
        revision.Name.Value.ShouldBe("w-1");
        revision.Uid.ShouldBe(resource.Uid);
        revision.Generation.ShouldBe(1);
        revision.Spec!.Value.GetProperty("size").GetInt32().ShouldBe(1);
        revision.Deleted.ShouldBeFalse();
        revision.Author.ShouldBe(Author);
        revision.AppliedAt.ShouldBe(Now);
    }

    [Fact]
    public void Tombstone_ShouldRecordTheDeletionWithoutASpec()
    {
        // Arrange
        var resource = Resource.Create(
            NamespaceName.Create("acme"),
            "Widget",
            ResourceName.Create("w-1"),
            JsonDocument.Parse("{}").RootElement,
            3);

        // Act
        var revision = Revision.Tombstone(resource, Author, Now);

        // Assert
        revision.Deleted.ShouldBeTrue();
        revision.Spec.ShouldBeNull();
        revision.Generation.ShouldBe(1);
        revision.Uid.ShouldBe(resource.Uid);
    }

    [Fact]
    public void Applied_ShouldRecordANamespaceAsItsOwnNamespace_WhenGivenANamespace()
    {
        // Arrange
        var @namespace = Namespace.Create(NamespaceName.Create("acme"));

        // Act
        var revision = Revision.Applied(@namespace, Author, Now);

        // Assert
        revision.Namespace.Value.ShouldBe("acme");
        revision.Kind.ShouldBe(Namespace.Kind);
        revision.Name.Value.ShouldBe("acme");
        revision.Uid.ShouldBe(@namespace.Uid);
        revision.Spec!.Value.EnumerateObject().ShouldBeEmpty();
    }
}
