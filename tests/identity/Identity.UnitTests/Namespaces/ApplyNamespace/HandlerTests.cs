using Coderynx.Functional.Results.Successes;
using Identity.Application.Namespaces.ApplyNamespace;
using Identity.Domain.Namespaces;
using Xunit;

namespace Identity.UnitTests.Namespaces.ApplyNamespace;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRecordTheNamespace_WhenItIsNew()
    {
        // Arrange
        var database = NamespaceDoubles.Database([], []);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(
            new Command("acme", NamespaceDoubles.Uid, 1),
            TestContext.Current.CancellationToken);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        database.KnownNamespaces.Received(1)
            .Add(
                Arg.Is<KnownNamespace>(known =>
                    known.Name == "acme" && known.Uid == NamespaceDoubles.Uid && known.Seq == 1));
        await database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldAdvanceTheSeq_WhenTheMessageIsNewer()
    {
        // Arrange
        var known = KnownNamespace.Create("acme", NamespaceDoubles.Uid, 1);
        var database = NamespaceDoubles.Database([known], []);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(
            new Command("acme", NamespaceDoubles.Uid, 2),
            TestContext.Current.CancellationToken);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        known.Seq.ShouldBe(2);
        database.KnownNamespaces.DidNotReceiveWithAnyArgs().Add(null!);
        await database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldTakeTheNewIncarnation_WhenTheNamespaceWasRecreated()
    {
        // Arrange
        var known = KnownNamespace.Create("acme", NamespaceDoubles.Uid, 5);
        var database = NamespaceDoubles.Database([known], []);
        var handler = new Handler(database);
        var recreated = Guid.NewGuid();

        // Act
        var result = await handler.HandleAsync(
            new Command("acme", recreated, 1),
            TestContext.Current.CancellationToken);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Updated);
        known.Uid.ShouldBe(recreated);
        known.Seq.ShouldBe(1);
        await database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(4)]
    public async Task HandleAsync_ShouldIgnoreTheMessage_WhenItIsNotNewer(long seq)
    {
        // Arrange
        var known = KnownNamespace.Create("acme", NamespaceDoubles.Uid, 5);
        var database = NamespaceDoubles.Database([known], []);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(
            new Command("acme", NamespaceDoubles.Uid, seq),
            TestContext.Current.CancellationToken);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        known.Seq.ShouldBe(5);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
