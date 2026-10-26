using Core.Domain.Authorization;
using Registry.Application.Database;
using Registry.Application.Resources.ListRevisions;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Resources.ListRevisions;

public sealed class HandlerTests
{
    private static readonly RevisionAuthor Author = new(Guid.NewGuid(), "erin");

    [Fact]
    public async Task HandleAsync_ShouldListTheResourcesRevisionsNewestFirst_WhenCallerIsAMember()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var widget = Widgets.Stored("acme", "w-1");
        var first = Revision.Applied(widget, Author, ResourceHandlers.Now);
        widget.Apply(Widgets.Json("""{"size": 2}"""), 3);
        var second = Revision.Applied(widget, Author, ResourceHandlers.Now.AddMinutes(1));
        var other = Revision.Applied(Widgets.Stored("acme", "w-2"), Author, ResourceHandlers.Now);
        var database = TestDatabase.Create(revisions: [first, second, other]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Query(caller.Id, "acme", "widgets", "w-1", 0, 50), Ct);

        // Assert
        result.Value.Items.Select(revision => revision.Generation).ShouldBe([2L, 1L]);
        result.Value.Total.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ShouldListATombstone_WhenTheResourceWasDeleted()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var widget = Widgets.Stored("acme", "w-1");
        var database = TestDatabase.Create(
            revisions:
            [
                Revision.Applied(widget, Author, ResourceHandlers.Now),
                Revision.Tombstone(widget, Author, ResourceHandlers.Now)
            ]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Query(caller.Id, "acme", "widgets", "w-1", 0, 50), Ct);

        // Assert
        result.Value.Items.Count(revision => revision.Deleted).ShouldBe(1);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTheResourceHasNoRevisions()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create();

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Query(caller.Id, "acme", "widgets", "w-1", 0, 50), Ct);

        // Assert
        result.Error.Code.ShouldBe("resource.not_found");
    }

    [Fact]
    public async Task HandleAsync_ShouldListANamespacesRevisions_WhenCallerIsAnAdministrator()
    {
        // Arrange
        var caller = Callers.Administrator();
        var acme = Namespace.Create(NamespaceName.Create("acme"));
        var database = TestDatabase.Create(revisions: [Revision.Applied(acme, Author, ResourceHandlers.Now)]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Query(caller.Id, null, "namespaces", "acme", 0, 50), Ct);

        // Assert
        result.Value.Items.ShouldHaveSingleItem().Kind.ShouldBe(Namespace.Kind);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheError_WhenTheAccessCheckFails()
    {
        // Arrange
        var widget = Widgets.Stored("acme", "w-1");
        var database = TestDatabase.Create(revisions: [Revision.Applied(widget, Author, ResourceHandlers.Now)]);
        var error = AccessDoubles.NamespaceNotFound();
        var handler = new Handler(
            database,
            AccessDoubles.ChecksFailingWith(error),
            AccessDoubles.ReadsFailingWith(error));

        // Act
        var resource = await handler.HandleAsync(new Query(Guid.NewGuid(), "acme", "widgets", "w-1", 0, 50), Ct);
        var @namespace = await handler.HandleAsync(new Query(Guid.NewGuid(), null, "namespaces", "acme", 0, 50), Ct);

        // Assert
        resource.Error.Code.ShouldBe("namespace.not_found");
        @namespace.Error.Code.ShouldBe("namespace.not_found");
    }

    private static Handler Handler(IApplicationDbContext database, UserAccess caller) =>
        new(database, AccessDoubles.ChecksFor(caller), AccessDoubles.ReadsFor("acme"));

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
