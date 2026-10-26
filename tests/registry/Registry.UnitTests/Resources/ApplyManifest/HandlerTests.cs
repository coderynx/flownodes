using System.Text.Json;
using Coderynx.Functional.Results;
using Coderynx.Functional.Results.Errors;
using Core.Contracts.Resources;
using Core.Domain.Authorization;
using Registry.Application.Database;
using Registry.Application.Resources;
using Registry.Application.Resources.ApplyManifest;
using Registry.Application.Secrets;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.UnitTests.Resources.ApplyManifest;

public sealed class HandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
    private readonly IResourceAccess _access = Substitute.For<IResourceAccess>();
    private readonly ISecretStorage _secrets = Substitute.For<ISecretStorage>();
    private readonly IRevisionLog _revisions = Substitute.For<IRevisionLog>();

    [Fact]
    public async Task HandleAsync_ShouldReturnTheAccessError_WhenTheCallerMayNotChangeTheManifest()
    {
        // Arrange
        var database = TestDatabase.Create([Acme()]);
        var handler = Handler(database);
        _access.AuthoriseChangesAsync(Arg.Any<Guid>(), Arg.Any<ManifestReading>(), Ct)
            .Returns(Errors.NamespaceNotFound("acme"));

        // Act
        var result = await handler.HandleAsync(Apply(Guid.NewGuid(), Widgets.Document("w-1")), Ct);

        // Assert
        result.Error.Code.ShouldBe("namespace.not_found");
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateRecordPublishAndSave_WhenTheResourceIsNew()
    {
        // Arrange
        var caller = Engineer();
        var acme = Acme();
        var database = TestDatabase.Create([acme]);

        // Act
        var result = await Handler(database, caller)
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
        await _revisions.Received(1)
            .RecordAsync(
                Arg.Is<Revision>(revision =>
                    revision.Name.Value == "w-1" && revision.Author == new RevisionAuthor(caller.Id, "erin") &&
                    revision.AppliedAt == Now),
                Ct);
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
        var database = TestDatabase.Create([acme], [existing, parent]);

        // Act
        var result = await Handler(database, caller)
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
        var database = TestDatabase.Create([acme], [existing]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Apply(caller.Id, Widgets.Document("w-1", "{}")), Ct);

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
        var database = TestDatabase.Create([Acme()]);
        var gizmo = Widgets.Json(
            """
            {"apiVersion": "flownodes.io/v1alpha1", "kind": "Gizmo", "metadata": {"name": "g"},
             "spec": {}}
            """);

        // Act
        var result = await Handler(database, caller)
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
        var database = TestDatabase.Create([Acme()]);

        // Act
        var result = await Handler(database, caller)
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
        var database = TestDatabase.Create([Acme()]);

        // Act
        var result = await Handler(database, caller)
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
        var database = TestDatabase.Create([Acme()]);
        var command = new Command(caller.Id, [Widgets.Document("w-1", @namespace: "globex")], "acme", false);

        // Act
        var result = await Handler(database, caller).HandleAsync(command, Ct);

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
        var database = TestDatabase.Create([Acme()], [existing]);

        // Act
        var result = await Handler(database, caller)
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
        var database = TestDatabase.Create([acme], [existing]);
        var command = new Command(
            caller.Id,
            [Widgets.Document("w-1", """{"size": 4}"""), Widgets.Document("w-2")],
            "acme",
            true);

        // Act
        var result = await Handler(database, caller).HandleAsync(command, Ct);

        // Assert
        result.Value.Select(applied => (applied.Action, applied.Generation))
            .ShouldBe([(ApplyAction.Configured, 2L), (ApplyAction.Created, 1L)]);
        existing.Generation.ShouldBe(1);
        acme.Seq.ShouldBe(1);
        database.Resources.DidNotReceive().Add(Arg.Any<Resource>());
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldCreateANamespace_WhenAnAdministratorAppliesOne()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create([Acme()]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Apply(caller.Id, NamespaceDocument("globex")), Ct);

        // Assert
        var applied = result.Value.ShouldHaveSingleItem();
        applied.Action.ShouldBe(ApplyAction.Created);
        applied.Kind.ShouldBe(Namespace.Kind);
        applied.Namespace.ShouldBeNull();
        database.Namespaces.Received(1).Add(Arg.Is<Namespace>(created => created.Name.Value == "globex"));
        await database.Received(1)
            .PublishAsync(
                Arg.Is<ResourceApplied>(message => message.Kind == Namespace.Kind && message.Name == "globex"));
        await _revisions.Received(1).RecordAsync(Arg.Is<Revision>(revision => revision.Kind == Namespace.Kind), Ct);
        await database.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldLeaveAnExistingNamespaceUnchanged_WhenAnAdministratorAppliesItAgain()
    {
        // Arrange
        var caller = Callers.Administrator();
        var database = TestDatabase.Create([Acme()]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Apply(caller.Id, NamespaceDocument("acme")), Ct);

        // Assert
        result.Value.ShouldHaveSingleItem().Action.ShouldBe(ApplyAction.Unchanged);
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectAResource_WhenItBreaksARuleAgainstAnotherResource()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()], [Widgets.Stored("acme", "root", """{"size": 2}""")]);

        // Act
        var result = await Handler(database, caller)
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
            ]);

        // Act
        var result = await Handler(database, caller)
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
            ]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Apply(caller.Id, Widgets.Document("other")), Ct);

        // Assert
        result.Value.ShouldHaveSingleItem().Action.ShouldBe(ApplyAction.Created);
    }

    [Fact]
    public async Task HandleAsync_ShouldStoreFingerprintsInTheSpecAndEncryptedValuesApart_WhenASecretIsNew()
    {
        // Arrange
        var caller = Engineer();
        var resources = new List<Resource>();
        var database = TestDatabase.Create([Acme()], resources);

        // Act
        var result = await Handler(database, caller).HandleAsync(Apply(caller.Id, SecretDocument("db", "hunter2")), Ct);

        // Assert
        var applied = result.Value.ShouldHaveSingleItem();
        applied.Action.ShouldBe(ApplyAction.Created);
        applied.Changes.ShouldAllBe(change => change.To!.Value.GetString() == SecretKind.Hidden);
        database.Resources.Received(1)
            .Add(Arg.Is<Resource>(resource => !resource.Spec.GetRawText().Contains("hunter2")));
        await _secrets.Received(1)
            .StoreAsync(
                Arg.Is<ManifestEntry>(entry => entry.Values!.Count == 1 && entry.Values["password"] == "hunter2"),
                Ct);
        await database.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldReportChangedWithoutValuesAndKeepTheNewValue_WhenASecretValueChanges()
    {
        // Arrange
        var caller = Engineer();
        var vault = ResourceHandlers.Vault();
        var stored = Resource.Create(
            NamespaceName.Create("acme"),
            SecretKind.KindName,
            ResourceName.Create("db"),
            SecretKind.Seal(Widgets.Json("""{"data": {"password": "old"}}"""), vault.Fingerprint),
            2);
        var database = TestDatabase.Create([Acme()], [stored]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Apply(caller.Id, SecretDocument("db", "new")), Ct);

        // Assert
        var applied = result.Value.ShouldHaveSingleItem();
        applied.Action.ShouldBe(ApplyAction.Configured);
        applied.Changes.Single().Path.ShouldBe("spec.data.password");
        applied.Changes.Single().To!.Value.GetString().ShouldBe(SecretKind.Hidden);
        await _secrets.Received(1).StoreAsync(Arg.Is<ManifestEntry>(entry => entry.Values!["password"] == "new"), Ct);
    }

    [Fact]
    public async Task HandleAsync_ShouldLeaveASecretAlone_WhenItsValuesAreUnchanged()
    {
        // Arrange
        var caller = Engineer();
        var vault = ResourceHandlers.Vault();
        var stored = Resource.Create(
            NamespaceName.Create("acme"),
            SecretKind.KindName,
            ResourceName.Create("db"),
            SecretKind.Seal(Widgets.Json("""{"data": {"password": "same"}}"""), vault.Fingerprint),
            2);
        var database = TestDatabase.Create([Acme()], [stored]);

        // Act
        var result = await Handler(database, caller).HandleAsync(Apply(caller.Id, SecretDocument("db", "same")), Ct);

        // Assert
        result.Value.ShouldHaveSingleItem().Action.ShouldBe(ApplyAction.Unchanged);
        await _secrets.DidNotReceive().StoreAsync(Arg.Any<ManifestEntry>(), Arg.Any<CancellationToken>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectTheManifest_WhenAWidgetReferencesAMissingSecret()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(Apply(caller.Id, Widgets.Document("w-1", """{"secret": "db"}""")), Ct);

        // Assert
        var rejected = result.Error.ShouldBeOfType<ManifestRejected>();
        rejected.Problems.ShouldHaveSingleItem().Path.ShouldBe("spec.secret");
        await ShouldNotHaveSavedAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldApply_WhenAWidgetReferencesASecretInTheSameManifest()
    {
        // Arrange
        var caller = Engineer();
        var database = TestDatabase.Create([Acme()]);

        // Act
        var result = await Handler(database, caller)
            .HandleAsync(
                Apply(caller.Id, SecretDocument("db", "hunter2"), Widgets.Document("w-1", """{"secret": "db"}""")),
                Ct);

        // Assert
        result.Value.Count.ShouldBe(2);
    }

    private static JsonElement SecretDocument(string name, string password) =>
        Widgets.Json(
            JsonSerializer.Serialize(
                new
                {
                    apiVersion = "flownodes.io/v1alpha1",
                    kind = "Secret",
                    metadata = new { name },
                    spec = new { data = new { password } }
                }));

    private static UserAccess Engineer() => Callers.Named(Callers.Member("acme", NamespaceRole.Engineer), "erin");
    private static Namespace Acme() => Namespace.Create(NamespaceName.Create("acme"));

    private static Command Apply(Guid callerId, params JsonElement[] documents) =>
        new(callerId, documents, "acme", false);

    private static JsonElement NamespaceDocument(string name) =>
        Widgets.Json(
            $$$"""{"apiVersion": "flownodes.io/v1alpha1", "kind": "Namespace", "metadata": {"name": "{{{name}}}"}}""");

    private Handler Handler(IApplicationDbContext database, UserAccess? caller = null)
    {
        _access.AuthoriseChangesAsync(Arg.Any<Guid>(), Arg.Any<ManifestReading>(), Arg.Any<CancellationToken>())
            .Returns(Result.Found(caller));

        return new Handler(
            database,
            WidgetKind.Catalog,
            ResourceHandlers.Time(),
            ResourceHandlers.Vault(),
            _access,
            _secrets,
            _revisions);
    }

    private static async Task ShouldNotHaveSavedAsync(IApplicationDbContext database)
    {
        await database.DidNotReceive().PublishAsync(Arg.Any<ResourceApplied>());
        await database.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
