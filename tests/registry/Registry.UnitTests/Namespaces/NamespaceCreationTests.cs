using Coderynx.Functional.Results.Errors;
using Coderynx.Functional.Results.Successes;
using Core.Contracts.Resources;
using Microsoft.EntityFrameworkCore;
using Registry.Application.Namespaces;
using Registry.Domain.Namespaces;
using Xunit;

namespace Registry.UnitTests.Namespaces;

public sealed class NamespaceCreationTests
{
    [Fact]
    public async Task EnsureAsync_ShouldReturnInvalidInput_WhenNameIsNotADnsLabel()
    {
        // Arrange
        var database = TestDatabase.Create();

        // Act
        var result = await NamespaceCreation.EnsureAsync(database, "Acme", Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.InvalidInput);
        result.Error.Code.ShouldBe("namespace.invalid_name");
        database.Namespaces.DidNotReceive().Add(Arg.Any<Namespace>());
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceApplied>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureAsync_ShouldReturnExistingWithoutSaving_WhenNamespaceExists()
    {
        // Arrange
        var existing = Namespace.Create(NamespaceName.Create("acme"));
        var database = TestDatabase.Create([existing]);

        // Act
        var result = await NamespaceCreation.EnsureAsync(database, "acme", Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        result.Value.ShouldBeSameAs(existing);
        database.Namespaces.DidNotReceive().Add(Arg.Any<Namespace>());
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceApplied>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnsureAsync_ShouldAddPublishAndSave_WhenNamespaceIsNew()
    {
        // Arrange
        var database = TestDatabase.Create();

        // Act
        var result = await NamespaceCreation.EnsureAsync(database, "acme", Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Created);
        var created = result.Value;
        created.Name.Value.ShouldBe("acme");
        database.Namespaces.Received(1).Add(created);
        await database.Received(1)
            .PublishAsync(
                Arg.Is<ResourceApplied>(message =>
                    message.Name == "acme" && message.Uid == created.Uid && message.Seq == 1));
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task EnsureAsync_ShouldReturnWinner_WhenConcurrentCreationWinsTheSave()
    {
        // Arrange
        var namespaces = new List<Namespace>();
        var winner = Namespace.Create(NamespaceName.Create("acme"));
        var database = TestDatabase.Create(namespaces);
        database.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                namespaces.Add(winner);

                return Task.FromException(new DbUpdateException());
            });

        // Act
        var result = await NamespaceCreation.EnsureAsync(database, "acme", Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        result.Value.ShouldBeSameAs(winner);
        database.Namespaces.Received(1).Remove(Arg.Is<Namespace>(loser => loser != winner));
    }

    [Fact]
    public async Task EnsureAsync_ShouldRethrow_WhenSaveFailsAndNoNamespaceWasCreated()
    {
        // Arrange
        var database = TestDatabase.Create();
        database.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException(new DbUpdateException()));

        // Act
        var act = () => NamespaceCreation.EnsureAsync(database, "acme", Ct);

        // Assert
        await act.ShouldThrowAsync<DbUpdateException>();
        database.Namespaces.Received(1).Remove(Arg.Any<Namespace>());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
