using Core.Contracts.Resources;
using Registry.Application.Namespaces.ResyncNamespaces;
using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Namespaces.ResyncNamespaces;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldPublishEveryNamespace_WhenNoNameIsGiven()
    {
        // Arrange
        var database = TestDatabase.Create(
            [Namespace.Create(NamespaceName.Create("acme")), Namespace.Create(NamespaceName.Create("globex"))]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command(null), Ct);

        // Assert
        await database.Received(1).PublishAsync(Arg.Is<ResourceApplied>(message => message.Name == "acme"));
        await database.Received(1).PublishAsync(Arg.Is<ResourceApplied>(message => message.Name == "globex"));
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishOnlyThatNamespace_WhenNameIsGiven()
    {
        // Arrange
        var database = TestDatabase.Create(
            [Namespace.Create(NamespaceName.Create("acme")), Namespace.Create(NamespaceName.Create("globex"))]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command("acme"), Ct);

        // Assert
        await database.Received(1).PublishAsync(Arg.Any<ResourceApplied>());
        await database.Received(1).PublishAsync(Arg.Is<ResourceApplied>(message => message.Name == "acme"));
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishEveryResourceWithTheSeqOfItsLastChange_WhenItsNamespaceIsResynced()
    {
        // Arrange
        var acme = Namespace.Create(NamespaceName.Create("acme"));
        var widget = Widgets.Stored("acme", "w-1", seq: 2);
        acme.NextSeq();
        acme.NextSeq();
        var database = TestDatabase.Create(
            [acme, Namespace.Create(NamespaceName.Create("globex"))],
            [widget, Widgets.Stored("globex", "w-9")]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command("acme"), Ct);

        // Assert
        await database.Received(1)
            .PublishAsync(Arg.Is<ResourceApplied>(message => message.Kind == Namespace.Kind && message.Seq == 3));
        await database.Received(1)
            .PublishAsync(
                Arg.Is<ResourceApplied>(message =>
                    message.Name == "w-1" && message.Uid == widget.Uid && message.Seq == 2 &&
                    message.Spec.ValueKind == System.Text.Json.JsonValueKind.Object));
        await database.DidNotReceive().PublishAsync(Arg.Is<ResourceApplied>(message => message.Name == "w-9"));
    }

    [Fact]
    public async Task HandleAsync_ShouldPublishNothing_WhenNameIsNotValid()
    {
        // Arrange
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("acme"))]);
        var handler = new Handler(database);

        // Act
        await handler.HandleAsync(new Command("Not Valid!"), Ct);

        // Assert
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceApplied>());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
