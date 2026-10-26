using System.Text.Json;
using Coderynx.Functional.Results.Errors;
using Core.Contracts.Resources;
using Core.Domain.Authorization;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Application.Resources.DeleteResources;
using Registry.Application.Secrets;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Resources.DeleteResources;

public sealed class HandlerTests
{
    private readonly ISecretStorage _secrets = Substitute.For<ISecretStorage>();
    private readonly IRevisionLog _revisions = Substitute.For<IRevisionLog>();

    [Fact]
    public async Task HandleAsync_ShouldRemovePublishRecordAndSave_WhenTheResourceExists()
    {
        // Arrange
        var caller = Engineer();
        var acme = Acme();
        var widget = Widgets.Stored("acme", "w-1");
        var database = TestDatabase.Create([acme], [widget]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Delete(caller.Id, Target("w-1")), Ct);

        // Assert
        result.Value.ShouldHaveSingleItem().ShouldBe(new DeletedResource(WidgetKind.Kind, "acme", "w-1"));
        database.Resources.Received(1).Remove(widget);
        await _revisions.Received(1)
            .RecordAsync(
                Arg.Is<Revision>(revision =>
                    revision.Deleted && revision.Uid == widget.Uid && revision.Author.Username == "erin"),
                Ct);
        await database.Received(1)
            .PublishAsync(Arg.Is<ResourceDeleted>(message => message.Name == "w-1" && message.Seq == 2));
        await database.Received(1).LockNamespacesAsync(Arg.Any<IReadOnlyList<string>>(), Ct);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteNothing_WhenAnyTargetDoesNotExist()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "w-1")]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Delete(caller.Id, Target("w-1"), Target("w-2")), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.NotFound);
        result.Error.Code.ShouldBe("resource.not_found");
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflictNamingTheReferrers_WhenATargetIsStillReferenced()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create(
            [Acme()],
            [Widgets.Stored("acme", "root"), Widgets.Stored("acme", "child", """{"parent": "root"}""")]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Delete(caller.Id, Target("root")), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
        var refusal = result.Error.ShouldBeOfType<DeletionRefused>().Problems.ShouldHaveSingleItem();
        refusal.Name.ShouldBe("root");
        refusal.Message.ShouldContain("Widget 'child'");
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteBoth_WhenTheReferrerIsDeletedInTheSameBatch()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create(
            [Acme()],
            [Widgets.Stored("acme", "root"), Widgets.Stored("acme", "child", """{"parent": "root"}""")]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(Delete(caller.Id, Target("root"), Target("child")), Ct);

        // Assert
        result.Value.Select(deleted => deleted.Name).ShouldBe(["root", "child"]);
        await database.Received(2).PublishAsync(Arg.Any<ResourceDeleted>());
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReportWithoutDeleting_WhenItIsADryRun()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "w-1")]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(new Command(caller.Id, [Target("w-1")], "acme", true), Ct);

        // Assert
        result.Value.ShouldHaveSingleItem().Name.ShouldBe("w-1");
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteANamespace_WhenAnAdministratorDeletesAnEmptyOne()
    {
        // Arrange
        var caller = Callers.Administrator();
        var acme = Acme();
        var database = TestDatabase.Create([acme]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Delete(caller.Id, NamespaceTarget("acme")), Ct);

        // Assert
        result.Value.ShouldHaveSingleItem().ShouldBe(new DeletedResource(Namespace.Kind, null, "acme"));
        database.Namespaces.Received(1).Remove(acme);
        await database.Received(1)
            .PublishAsync(Arg.Is<ResourceDeleted>(message => message.Kind == Namespace.Kind && message.Seq == 2));
        await _revisions.Received(1).RecordAsync(Arg.Is<Revision>(revision => revision.Deleted), Ct);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenTheNamespaceStillHoldsResources()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "w-1")]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Delete(caller.Id, NamespaceTarget("acme")), Ct);

        // Assert
        result.Error.ShouldBeOfType<DeletionRefused>()
            .Problems.ShouldHaveSingleItem()
            .Message.ShouldContain("still holds Resources");
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenTheNamespaceIsDefault()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("default"))]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Delete(caller.Id, NamespaceTarget("default")), Ct);

        // Assert
        result.Error.ShouldBeOfType<DeletionRefused>()
            .Problems.ShouldHaveSingleItem()
            .Message.ShouldContain("default Namespace cannot be deleted");
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRejected_WhenATargetCannotBeRead()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(Delete(caller.Id, Widgets.Json("""{"kind": "Widget"}""")), Ct);

        // Assert
        result.Error.ShouldBeOfType<ManifestRejected>().Problems.ShouldHaveSingleItem().Path.ShouldBe("metadata");
        await ShouldNotHaveDeletedAsync(database);
    }

    private static UserAccess Engineer() => Callers.Named(Callers.Member("acme", NamespaceRole.Engineer), "erin");
    private static Namespace Acme() => Namespace.Create(NamespaceName.Create("acme"));

    private static JsonElement Target(string name) =>
        Widgets.Json($$$"""{"kind": "Widget", "metadata": {"name": "{{{name}}}"}}""");

    private static JsonElement NamespaceTarget(string name) =>
        Widgets.Json($$$"""{"kind": "Namespace", "metadata": {"name": "{{{name}}}"}}""");

    private static Command Delete(Guid callerId, params JsonElement[] targets) => new(callerId, targets, "acme", false);

    private Handler Handler(IApplicationDbContext database, UserAccess caller) =>
        new(
            WidgetKind.Catalog,
            ResourceHandlers.Deletion(database, ResourceHandlers.Access(caller), _secrets, _revisions));

    private static async Task ShouldNotHaveDeletedAsync(IApplicationDbContext database)
    {
        database.Resources.DidNotReceive().Remove(Arg.Any<Resource>());
        database.Namespaces.DidNotReceive().Remove(Arg.Any<Namespace>());
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceDeleted>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldListEveryRefusedResource_WhenSeveralTargetsAreStillReferenced()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create(
            [Acme()],
            [
                Widgets.Stored("acme", "a"), Widgets.Stored("acme", "b"),
                Widgets.Stored("acme", "uses-a", """{"parent": "a"}"""),
                Widgets.Stored("acme", "uses-b", """{"parent": "b"}""")
            ]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Delete(caller.Id, Target("a"), Target("b")), Ct);

        // Assert
        result.Error.ShouldBeOfType<DeletionRefused>()
            .Problems.Select(problem => (problem.Document, problem.Name))
            .ShouldBe([(0, "a"), (1, "b")]);
        await ShouldNotHaveDeletedAsync(database);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
