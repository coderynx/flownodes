using System.Text.Json;
using Coderynx.Functional.Results.Errors;
using Core.Application.Authorization;
using Core.Contracts.Resources;
using Core.Domain.Authorization;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Application.Resources.DeleteResources;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Resources.DeleteResources;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRemovePublishRecordAndSave_WhenTheResourceExists()
    {
        // Arrange
        var caller = Engineer();
        var acme = Acme();
        var widget = Widgets.Stored("acme", "w-1");
        var database = TestDatabase.Create([acme], [widget], [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Delete(caller.Id, Target("w-1")), Ct);

        // Assert
        result.Value.ShouldHaveSingleItem().ShouldBe(new DeletedResource(WidgetKind.Kind, "acme", "w-1"));
        database.Resources.Received(1).Remove(widget);
        database.Revisions.Received(1)
            .Add(
                Arg.Is<Revision>(revision =>
                    revision.Deleted && revision.Uid == widget.Uid && revision.Author.Username == "erin"));
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
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "w-1")], [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Delete(caller.Id, Target("w-1"), Target("w-2")), Ct);

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
            [Widgets.Stored("acme", "root"), Widgets.Stored("acme", "child", """{"parent": "root"}""")],
            [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Delete(caller.Id, Target("root")), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
        result.Error.Code.ShouldBe("resource.referenced");
        result.Error.Message.ShouldContain("Widget 'child'");
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldDeleteBoth_WhenTheReferrerIsDeletedInTheSameBatch()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create(
            [Acme()],
            [Widgets.Stored("acme", "root"), Widgets.Stored("acme", "child", """{"parent": "root"}""")],
            [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Delete(caller.Id, Target("root"), Target("child")), Ct);

        // Assert
        result.Value.Select(deleted => deleted.Name).ShouldBe(["root", "child"]);
        await database.Received(2).PublishAsync(Arg.Any<ResourceDeleted>());
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenCallerIsOnlyAnOperator()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "w-1")], [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Delete(caller.Id, Target("w-1")), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.ForbiddenCode);
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReportWithoutDeleting_WhenItIsADryRun()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "w-1")], [caller]);

        // Act
        var result = await Handler(database).HandleAsync(new Command(caller.Id, [Target("w-1")], "acme", true), Ct);

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
        var database = TestDatabase.Create([acme], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Delete(caller.Id, NamespaceTarget("acme")), Ct);

        // Assert
        result.Value.ShouldHaveSingleItem().ShouldBe(new DeletedResource(Namespace.Kind, null, "acme"));
        database.Namespaces.Received(1).Remove(acme);
        await database.Received(1)
            .PublishAsync(Arg.Is<ResourceDeleted>(message => message.Kind == Namespace.Kind && message.Seq == 2));
        database.Revisions.Received(1).Add(Arg.Is<Revision>(revision => revision.Deleted));
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenTheNamespaceStillHoldsResources()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "w-1")], [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Delete(caller.Id, NamespaceTarget("acme")), Ct);

        // Assert
        result.Error.Code.ShouldBe("namespace.holds_resources");
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenTheNamespaceIsDefault()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create([Namespace.Create(NamespaceName.Create("default"))], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Delete(caller.Id, NamespaceTarget("default")), Ct);

        // Assert
        result.Error.Code.ShouldBe("namespace.default_cannot_be_deleted");
        await ShouldNotHaveDeletedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnRejected_WhenATargetCannotBeRead()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Delete(caller.Id, Widgets.Json("""{"kind": "Widget"}""")), Ct);

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

    private static Handler Handler(IApplicationDbContext database) =>
        new(database, WidgetKind.Catalog, ResourceHandlers.Time(), ResourceHandlers.Retention());

    private static async Task ShouldNotHaveDeletedAsync(IApplicationDbContext database)
    {
        database.Resources.DidNotReceive().Remove(Arg.Any<Resource>());
        database.Namespaces.DidNotReceive().Remove(Arg.Any<Namespace>());
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceDeleted>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
