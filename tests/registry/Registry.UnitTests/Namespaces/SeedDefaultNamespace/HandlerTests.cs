using Coderynx.Functional.Results.Successes;
using Core.Contracts.Resources;
using Registry.Application.Namespaces.SeedDefaultNamespace;
using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Namespaces.SeedDefaultNamespace;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldCreateDefaultWithoutACaller_WhenDefaultIsMissing()
    {
        // Arrange
        var database = TestDatabase.Create();
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Created);
        result.Value.Name.IsDefault.ShouldBeTrue();
        database.Namespaces.Received(1).Add(result.Value);
        await database.Received(1)
            .PublishAsync(Arg.Is<ResourceApplied>(message => message.Name == NamespaceName.Default));
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnFoundWithoutSaving_WhenDefaultExists()
    {
        // Arrange
        var existing = Namespace.Create(NamespaceName.Create(NamespaceName.Default));
        var database = TestDatabase.Create([existing]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Command(), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        result.Value.ShouldBeSameAs(existing);
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceApplied>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
