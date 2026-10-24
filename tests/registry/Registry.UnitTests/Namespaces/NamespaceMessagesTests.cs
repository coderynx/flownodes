using System.Text.Json;
using Registry.Application.Namespaces;
using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Namespaces;

public sealed class NamespaceMessagesTests
{
    [Fact]
    public void ToApplied_ShouldDescribeNamespaceAsItsOwnNamespace()
    {
        // Arrange
        var created = Namespace.Create(NamespaceName.Create("acme"));

        // Act
        var message = created.ToApplied();

        // Assert
        message.Namespace.ShouldBe("acme");
        message.Kind.ShouldBe(Namespace.Kind);
        message.Name.ShouldBe("acme");
        message.Uid.ShouldBe(created.Uid);
        message.Generation.ShouldBe(1);
        message.Seq.ShouldBe(1);
        message.Spec.ValueKind.ShouldBe(JsonValueKind.Object);
        message.Spec.EnumerateObject().ShouldBeEmpty();
    }

    [Fact]
    public void ToDeleted_ShouldCarryDeletionSeq_WhenNamespaceWasDeleted()
    {
        // Arrange
        var created = Namespace.Create(NamespaceName.Create("acme"));
        created.Delete();

        // Act
        var message = created.ToDeleted();

        // Assert
        message.Namespace.ShouldBe("acme");
        message.Kind.ShouldBe(Namespace.Kind);
        message.Name.ShouldBe("acme");
        message.Uid.ShouldBe(created.Uid);
        message.Generation.ShouldBe(1);
        message.Seq.ShouldBe(2);
    }
}
