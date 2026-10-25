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
