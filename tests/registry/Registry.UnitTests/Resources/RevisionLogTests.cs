using Registry.Application.Resources;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Resources;

public sealed class RevisionLogTests
{
    private static readonly RevisionAuthor Author = new(Guid.NewGuid(), "erin");

    [Fact]
    public async Task RecordAsync_ShouldAddTheRevision_WhenTheResourceHasNoHistory()
    {
        // Arrange
        var resource = Widgets.Stored("acme", "w-1");
        var database = TestDatabase.Create();
        var revision = Revision.Applied(resource, Author, ResourceHandlers.Now);

        // Act
        await Log(database, 50).RecordAsync(revision, Ct);

        // Assert
        database.Revisions.Received(1).Add(revision);
        database.Revisions.DidNotReceive().RemoveRange(Arg.Any<IEnumerable<Revision>>());
    }

    [Fact]
    public async Task RecordAsync_ShouldDropTheOldestRevisions_WhenTheResourceKeepsMoreThanTheRetention()
    {
        // Arrange
        var resource = Widgets.Stored("acme", "w-1");
        var history = Enumerable.Range(0, 3)
            .Select(_ => Revision.Applied(resource, Author, ResourceHandlers.Now))
            .ToList();
        var database = TestDatabase.Create(revisions: history);
        var revision = Revision.Applied(resource, Author, ResourceHandlers.Now);

        // Act
        await Log(database, 2).RecordAsync(revision, Ct);

        // Assert
        database.Revisions.Received(1).RemoveRange(Arg.Is<IEnumerable<Revision>>(removed => removed.Count() == 2));
        database.Revisions.Received(1).Add(revision);
    }

    [Fact]
    public async Task RecordAsync_ShouldLeaveOtherResourcesHistoryAlone_WhenTheRetentionIsReached()
    {
        // Arrange
        var other = Widgets.Stored("acme", "w-2");
        var history = Enumerable.Range(0, 3)
            .Select(_ => Revision.Applied(other, Author, ResourceHandlers.Now))
            .ToList();
        var database = TestDatabase.Create(revisions: history);

        // Act
        await Log(database, 1)
            .RecordAsync(Revision.Applied(Widgets.Stored("acme", "w-1"), Author, ResourceHandlers.Now), Ct);

        // Assert
        database.Revisions.DidNotReceive().RemoveRange(Arg.Any<IEnumerable<Revision>>());
    }

    [Fact]
    public async Task RecordAsync_ShouldAddTheTombstone_WhenANamespaceIsDeleted()
    {
        // Arrange
        var acme = Namespace.Create(NamespaceName.Create("acme"));
        var database = TestDatabase.Create();
        var tombstone = Revision.Tombstone(acme, Author, ResourceHandlers.Now);

        // Act
        await Log(database, 50).RecordAsync(tombstone, Ct);

        // Assert
        database.Revisions.Received(1)
            .Add(Arg.Is<Revision>(revision => revision.Deleted && revision.Kind == Namespace.Kind));
    }

    private static RevisionLog Log(Application.Database.IApplicationDbContext database, int keep) =>
        new(database, ResourceHandlers.Retention(keep));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
