using System.Text.Json;
using Coderynx.Functional.Results.Errors;
using Core.Application.Authorization;
using Core.Contracts.Resources;
using Core.Domain.Authorization;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Application.Resources.ApplyManifest;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Resources.ApplyManifest;

public sealed class HandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenCallerIsUnknown()
    {
        // Arrange
        var database = TestDatabase.Create([Acme()]);

        // Act
        var result = await Handler(database).HandleAsync(Apply(Guid.NewGuid(), Widgets.Document("w-1")), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenCallerHoldsNoRoleInTheNamespace()
    {
        // Arrange
        var caller = Callers.Member("globex", NamespaceRole.Engineer);
        var database = TestDatabase.Create([Acme()], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Apply(caller.Id, Widgets.Document("w-1")), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.NotFound);
        result.Error.Code.ShouldBe("namespace.not_found");
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenCallerIsOnlyAnOperator()
    {
        // Arrange
        var caller = Callers.Member("acme");
        var database = TestDatabase.Create([Acme()], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Apply(caller.Id, Widgets.Document("w-1")), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.ForbiddenCode);
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateRecordPublishAndSave_WhenTheResourceIsNew()
    {
        // Arrange
        var caller = Engineer();
        var acme = Acme();
        var database = TestDatabase.Create([acme], users: [caller]);

        // Act
        var result = await Handler(database)
            .HandleAsync(Apply(caller.Id, Widgets.Document("w-1", """{"size": 3}""")), Ct);

        // Assert
        var applied = result.Value.ShouldHaveSingleItem();
        applied.Action.ShouldBe(ApplyAction.Created);
        applied.Generation.ShouldBe(1);
        applied.Namespace.ShouldBe("acme");
        applied.Changes.Select(change => change.Path).ShouldBe(["spec.size"]);
        acme.Seq.ShouldBe(2);
        await database.Received(1)
            .LockNamespacesAsync(Arg.Is<IReadOnlyList<string>>(names => names.SequenceEqual(new[] { "acme" })), Ct);
        database.Resources.Received(1)
            .Add(Arg.Is<Resource>(resource => resource.Name.Value == "w-1" && resource.Generation == 1));
        database.Revisions.Received(1)
            .Add(
                Arg.Is<Revision>(revision =>
                    revision.Name.Value == "w-1" && revision.Author == new RevisionAuthor(caller.Id, "erin") &&
                    revision.AppliedAt == Now));
        await database.Received(1)
            .PublishAsync(
                Arg.Is<ResourceApplied>(message =>
                    message.Namespace == "acme" && message.Kind == WidgetKind.Kind && message.Name == "w-1" &&
                    message.Generation == 1 && message.Seq == 2));
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldChangeNothing_WhenTheSpecMeansTheSame()
    {
        // Arrange
        var caller = Engineer();
        var acme = Acme();
        var existing = Widgets.Stored("acme", "w-1", """{"size": 3, "parent": "w-0"}""");
        var parent = Widgets.Stored("acme", "w-0");
        var database = TestDatabase.Create([acme], [existing, parent], [caller]);

        // Act
        var result = await Handler(database)
            .HandleAsync(Apply(caller.Id, Widgets.Document("w-1", """{"parent": "w-0", "size": 3.0}""")), Ct);

        // Assert
        var applied = result.Value.ShouldHaveSingleItem();
        applied.Action.ShouldBe(ApplyAction.Unchanged);
        applied.Changes.ShouldBeEmpty();
        existing.Generation.ShouldBe(1);
        acme.Seq.ShouldBe(1);
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReplaceTheSpecAndStartAGeneration_WhenTheSpecChanges()
    {
        // Arrange
        var caller = Engineer();
        var acme = Acme();
        var existing = Widgets.Stored("acme", "w-1", """{"size": 3}""");
        var database = TestDatabase.Create([acme], [existing], [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Apply(caller.Id, Widgets.Document("w-1", "{}")), Ct);

        // Assert
        var applied = result.Value.ShouldHaveSingleItem();
        applied.Action.ShouldBe(ApplyAction.Configured);
        applied.Generation.ShouldBe(2);
        applied.Changes.Select(change => (change.Path, change.Kind)).ShouldBe([("spec.size", SpecChangeKind.Removed)]);
        existing.Generation.ShouldBe(2);
        existing.Seq.ShouldBe(2);
        await database.Received(1)
            .PublishAsync(Arg.Is<ResourceApplied>(message => message.Generation == 2 && message.Seq == 2));
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectTheWholeManifestWithEveryProblem_WhenAnyResourceIsInvalid()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], users: [caller]);
        var gizmo = Widgets.Json(
            """
            {"apiVersion": "flownodes.io/v1alpha1", "kind": "Gizmo", "metadata": {"name": "g"},
             "spec": {}}
            """);

        // Act
        var result = await Handler(database)
            .HandleAsync(
                Apply(
                    caller.Id,
                    Widgets.Document("w-1"),
                    Widgets.Document("Bad_Name"),
                    Widgets.Document("w-2", """{"size": "big"}"""),
                    Widgets.Document("w-3", """{"parent": "missing"}"""),
                    gizmo),
                Ct);

        // Assert
        var rejected = result.Error.ShouldBeOfType<ManifestRejected>();
        rejected.Kind.ShouldBe(ErrorKind.InvalidInput);
        rejected.Problems.Select(problem => (problem.Document, problem.Path))
            .ShouldBe([(1, "metadata.name"), (2, "spec.size"), (3, "spec.parent"), (4, "kind")]);
        rejected.Problems[2].Message.ShouldContain("missing");
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldResolveAReference_WhenTheReferencedResourceIsInTheSameManifest()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], users: [caller]);

        // Act
        var result = await Handler(database)
            .HandleAsync(
                Apply(caller.Id, Widgets.Document("child", """{"parent": "root"}"""), Widgets.Document("root")),
                Ct);

        // Assert
        result.Value.Select(applied => applied.Action).ShouldBe([ApplyAction.Created, ApplyAction.Created]);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectAResource_WhenTheSameResourceIsListedTwice()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], users: [caller]);

        // Act
        var result = await Handler(database)
            .HandleAsync(Apply(caller.Id, Widgets.Document("w-1"), Widgets.Document("w-1")), Ct);

        // Assert
        var problem = result.Error.ShouldBeOfType<ManifestRejected>().Problems.ShouldHaveSingleItem();
        problem.Document.ShouldBe(1);
        problem.Path.ShouldBe("metadata.name");
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectAResource_WhenItsNamespaceDiffersFromTheRequestedOne()
    {
        // Arrange
        var caller = Engineer();
        caller.GrantRole("globex", NamespaceRole.Engineer, caller.LastSeq + 1);
        var database = TestDatabase.Create([Acme()], users: [caller]);
        var command = new Command(caller.Id, [Widgets.Document("w-1", @namespace: "globex")], "acme", false);

        // Act
        var result = await Handler(database).HandleAsync(command, Ct);

        // Assert
        result.Error.ShouldBeOfType<ManifestRejected>()
            .Problems.ShouldHaveSingleItem()
            .Path.ShouldBe("metadata.namespace");
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnConflict_WhenTheGenerationPreconditionIsStale()
    {
        // Arrange
        var caller = Engineer();
        var existing = Widgets.Stored("acme", "w-1", """{"size": 3}""");
        existing.Apply(Widgets.Json("""{"size": 4}"""), 3);
        var database = TestDatabase.Create([Acme()], [existing], [caller]);

        // Act
        var result = await Handler(database)
            .HandleAsync(Apply(caller.Id, Widgets.Document("w-1", """{"size": 5}""", generation: 1)), Ct);

        // Assert
        result.Error.Kind.ShouldBe(ErrorKind.Conflict);
        result.Error.Code.ShouldBe("resource.generation_conflict");
        existing.Generation.ShouldBe(2);
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReportWithoutChangingAnything_WhenItIsADryRun()
    {
        // Arrange
        var caller = Engineer();
        var acme = Acme();
        var existing = Widgets.Stored("acme", "w-1", """{"size": 3}""");
        var database = TestDatabase.Create([acme], [existing], [caller]);
        var command = new Command(
            caller.Id,
            [Widgets.Document("w-1", """{"size": 4}"""), Widgets.Document("w-2")],
            "acme",
            true);

        // Act
        var result = await Handler(database).HandleAsync(command, Ct);

        // Assert
        result.Value.Select(applied => (applied.Action, applied.Generation))
            .ShouldBe([(ApplyAction.Configured, 2L), (ApplyAction.Created, 1L)]);
        existing.Generation.ShouldBe(1);
        acme.Seq.ShouldBe(1);
        database.Resources.DidNotReceive().Add(Arg.Any<Resource>());
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldDropTheOldestRevisions_WhenAResourceKeepsMoreThanTheRetention()
    {
        // Arrange
        var caller = Engineer();
        var existing = Widgets.Stored("acme", "w-1", """{"size": 3}""");
        var history = Enumerable.Range(0, 3)
            .Select(_ => Revision.Applied(existing, new RevisionAuthor(caller.Id, "erin"), Now))
            .ToList();
        var database = TestDatabase.Create([Acme()], [existing], [caller], history);

        // Act
        await Handler(database, keep: 2).HandleAsync(Apply(caller.Id, Widgets.Document("w-1", """{"size": 4}""")), Ct);

        // Assert
        database.Revisions.Received(1).RemoveRange(Arg.Is<IEnumerable<Revision>>(removed => removed.Count() == 2));
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateANamespace_WhenAnAdministratorAppliesOne()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create([Acme()], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Apply(caller.Id, NamespaceDocument("globex")), Ct);

        // Assert
        var applied = result.Value.ShouldHaveSingleItem();
        applied.Action.ShouldBe(ApplyAction.Created);
        applied.Kind.ShouldBe(Namespace.Kind);
        applied.Namespace.ShouldBeNull();
        database.Namespaces.Received(1).Add(Arg.Is<Namespace>(created => created.Name.Value == "globex"));
        await database.Received(1)
            .PublishAsync(
                Arg.Is<ResourceApplied>(message => message.Kind == Namespace.Kind && message.Name == "globex"));
        database.Revisions.Received(1).Add(Arg.Is<Revision>(revision => revision.Kind == Namespace.Kind));
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldLeaveAnExistingNamespaceUnchanged_WhenAnAdministratorAppliesItAgain()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create([Acme()], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Apply(caller.Id, NamespaceDocument("acme")), Ct);

        // Assert
        result.Value.ShouldHaveSingleItem().Action.ShouldBe(ApplyAction.Unchanged);
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenANonAdministratorAppliesANamespace()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], users: [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Apply(caller.Id, NamespaceDocument("globex")), Ct);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.ForbiddenCode);
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNamespaceNotFound_WhenAnUnreadableResourceTargetsANamespaceWithoutARole()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], users: [caller]);
        var command = new Command(caller.Id, [Widgets.Document("Bad_Name", @namespace: "globex")], null, false);

        // Act
        var result = await Handler(database).HandleAsync(command, Ct);

        // Assert
        result.Error.Code.ShouldBe("namespace.not_found");
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectAResource_WhenItBreaksARuleAgainstAnotherResource()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "root", """{"size": 2}""")], [caller]);

        // Act
        var result = await Handler(database)
            .HandleAsync(Apply(caller.Id, Widgets.Document("child", """{"parent": "root", "size": 5}""")), Ct);

        // Assert
        var problem = result.Error.ShouldBeOfType<ManifestRejected>().Problems.ShouldHaveSingleItem();
        (problem.Document, problem.Path).ShouldBe((0, "spec.size"));
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectTheManifest_WhenItBreaksAResourceItDoesNotList()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create(
            [Acme()],
            [
                Widgets.Stored("acme", "root", """{"size": 5}"""),
                Widgets.Stored("acme", "child", """{"parent": "root", "size": 4}""")
            ],
            [caller]);

        // Act
        var result = await Handler(database)
            .HandleAsync(Apply(caller.Id, Widgets.Document("root", """{"size": 1}""")), Ct);

        // Assert
        var problem = result.Error.ShouldBeOfType<ManifestRejected>().Problems.ShouldHaveSingleItem();
        (problem.Document, problem.Name, problem.Path).ShouldBe((null, "child", "spec.size"));
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotBlameTheManifest_WhenAResourceItDoesNotListWasAlreadyInvalid()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create(
            [Acme()],
            [
                Widgets.Stored("acme", "root", """{"size": 1}"""),
                Widgets.Stored("acme", "child", """{"parent": "root", "size": 4}""")
            ],
            [caller]);

        // Act
        var result = await Handler(database).HandleAsync(Apply(caller.Id, Widgets.Document("other")), Ct);

        // Assert
        result.Value.ShouldHaveSingleItem().Action.ShouldBe(ApplyAction.Created);
    }

    private static UserAccess Engineer() => Callers.Named(Callers.Member("acme", NamespaceRole.Engineer), "erin");
    private static Namespace Acme() => Namespace.Create(NamespaceName.Create("acme"));

    private static Command Apply(Guid callerId, params JsonElement[] documents) =>
        new(callerId, documents, "acme", false);

    private static JsonElement NamespaceDocument(string name) =>
        Widgets.Json(
            $$$"""{"apiVersion": "flownodes.io/v1alpha1", "kind": "Namespace", "metadata": {"name": "{{{name}}}"}}""");

    private static Handler Handler(IApplicationDbContext database, int keep = 50)
    {
        return new Handler(database, WidgetKind.Catalog, ResourceHandlers.Time(), ResourceHandlers.Retention(keep));
    }

    private static async Task ShouldNotHaveSavedAsync(IApplicationDbContext database)
    {
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceApplied>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
