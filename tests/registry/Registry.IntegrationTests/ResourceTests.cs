using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Contracts.Resources;
using Xunit;

namespace Registry.IntegrationTests;

public sealed class ResourceTests(PostgresFixture postgres)
{
    private const string Widgets = "/api/v1/registry/namespaces/acme/widgets";

    private const string TwoWidgets = """
                                      apiVersion: flownodes.io/v1alpha1
                                      kind: Widget
                                      metadata:
                                        name: root
                                      spec:
                                        size: 3
                                      ---
                                      apiVersion: flownodes.io/v1alpha1
                                      kind: Widget
                                      metadata:
                                        name: child
                                        namespace: acme
                                      spec:
                                        parent: root
                                        label: "007"
                                      status:
                                        ignored: true
                                      """;

    [Fact]
    public async Task Apply_ShouldCreateEveryResourceAndPublishGapFreeSeqs_WhenTheManifestIsValid()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, TwoWidgets, Ct, "?namespace=acme");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApplyBody>(Ct);
        body!.Resources.Select(resource => (resource.Name, resource.Action, resource.Generation))
            .ShouldBe([("root", "created", 1L), ("child", "created", 1L)]);
        var root = await app.Messages.WaitForAppliedAsync("root", Ct);
        var child = await app.Messages.WaitForAppliedAsync("child", Ct);
        (root.Seq, child.Seq).ShouldBe((2L, 3L));
        child.Spec.GetProperty("label").GetString().ShouldBe("007");

        var stored = await engineer.GetFromJsonAsync<ResourceBody>($"{Widgets}/child", Ct);
        stored!.Kind.ShouldBe("Widget");
        stored.Metadata.Namespace.ShouldBe("acme");
        stored.Metadata.Generation.ShouldBe(1);
        stored.Metadata.Uid.ShouldBe(child.Uid);
        stored.Spec.GetProperty("parent").GetString().ShouldBe("root");
        stored.Spec.TryGetProperty("status", out _).ShouldBeFalse();
        stored.Status.ShouldBeNull();
    }

    [Fact]
    public async Task Apply_ShouldChangeNothing_WhenTheSameManifestIsAppliedAgain()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, TwoWidgets, Ct, "?namespace=acme");

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, TwoWidgets, Ct, "?namespace=acme");

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ApplyBody>(Ct);
        body!.Resources.ShouldAllBe(resource => resource.Action == "unchanged");
        var revisions = await engineer.GetFromJsonAsync<RevisionsBody>($"{Widgets}/root/revisions", Ct);
        revisions!.Total.ShouldBe(1);
    }

    [Fact]
    public async Task Apply_ShouldStartAGenerationAndRecordWhoDidIt_WhenTheSpecChanges()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, TwoWidgets, Ct, "?namespace=acme");

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, Widget("root", "size: 4"), Ct, "?namespace=acme");

        // Assert
        var applied = (await response.Content.ReadFromJsonAsync<ApplyBody>(Ct))!.Resources.ShouldHaveSingleItem();
        applied.Action.ShouldBe("configured");
        applied.Generation.ShouldBe(2);
        applied.Changes.ShouldHaveSingleItem().ShouldBe(new ChangeBody("spec.size", "changed"));
        var revisions = await engineer.GetFromJsonAsync<RevisionsBody>($"{Widgets}/root/revisions", Ct);
        revisions!.Items.Select(revision => revision.Generation).ShouldBe([2L, 1L]);
        revisions.Items[0].AppliedBy.Username.ShouldBe("erin");
        revisions.Items[0].Spec!.Value.GetProperty("size").GetInt32().ShouldBe(4);
    }

    [Fact]
    public async Task Apply_ShouldRejectTheWholeManifestWithEveryProblem_WhenAnyResourceIsInvalid()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        var manifest = $"""
                        {Widget("fine", "size: 1")}
                        ---
                        {Widget("broken", "size: big")}
                        ---
                        {Widget("orphan", "parent: nobody")}
                        """;

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, manifest, Ct, "?namespace=acme");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<ProblemBody>(Ct);
        problem!.Errors.Select(error => (error.Document, error.Name, error.Path))
            .ShouldBe([(1, "broken", "spec.size"), (2, "orphan", "spec.parent")]);
        (await engineer.GetAsync($"{Widgets}/fine", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Apply_ShouldReturnBadRequest_WhenTheBodyIsNotYamlOrJson()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, "kind: [unclosed", Ct, "?namespace=acme");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Apply_ShouldAcceptAJsonArray_WhenSentAsJson()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        var manifest = new[]
        {
            new
            {
                apiVersion = "flownodes.io/v1alpha1",
                kind = "Widget",
                metadata = new { name = "w-1", @namespace = "acme" },
                spec = new { size = 1 }
            }
        };

        // Act
        var response = await engineer.PostAsJsonAsync("/api/v1/registry/apply", manifest, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await engineer.GetAsync($"{Widgets}/w-1", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Apply_ShouldReturnConflict_WhenTheGenerationPreconditionIsStale()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Widget("w-1", "size: 1"), Ct, "?namespace=acme");
        await RegistryApp.ApplyAsync(engineer, Widget("w-1", "size: 2"), Ct, "?namespace=acme");
        var stale = """
                    apiVersion: flownodes.io/v1alpha1
                    kind: Widget
                    metadata:
                      name: w-1
                      generation: 1
                    spec:
                      size: 3
                    """;

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, stale, Ct, "?namespace=acme");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var stored = await engineer.GetFromJsonAsync<ResourceBody>($"{Widgets}/w-1", Ct);
        stored!.Spec.GetProperty("size").GetInt32().ShouldBe(2);
    }

    [Fact]
    public async Task Apply_ShouldReportTheDiffWithoutStoringIt_WhenItIsADryRun()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Widget("w-1", "size: 1"), Ct, "?namespace=acme");

        // Act
        var response = await RegistryApp.ApplyAsync(
            engineer,
            Widget("w-1", "size: 2"),
            Ct,
            "?namespace=acme&dryRun=true");

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ApplyBody>(Ct);
        body!.DryRun.ShouldBeTrue();
        body.Resources.ShouldHaveSingleItem().Action.ShouldBe("configured");
        var stored = await engineer.GetFromJsonAsync<ResourceBody>($"{Widgets}/w-1", Ct);
        stored!.Metadata.Generation.ShouldBe(1);
    }

    [Fact]
    public async Task Resources_ShouldBeInvisibleAndUntouchable_WhenTheEngineerBelongsToAnotherNamespace()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Widget("w-1", "size: 1"), Ct, "?namespace=acme");
        await app.CreateNamespaceAsync("globex", Ct);
        var (_, outsider) = await app.MemberAsync("olga", "Engineer", "globex");
        await RegistryApp.EventuallyAsync(
            async () => (await outsider.GetAsync("/api/v1/registry/namespaces/globex", Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        var get = await outsider.GetAsync($"{Widgets}/w-1", Ct);
        var list = await outsider.GetAsync(Widgets, Ct);
        var revisions = await outsider.GetAsync($"{Widgets}/w-1/revisions", Ct);
        var apply = await RegistryApp.ApplyAsync(outsider, Widget("w-1", "size: 9"), Ct, "?namespace=acme");
        var delete = await outsider.DeleteAsync($"{Widgets}/w-1", Ct);

        // Assert
        get.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        list.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        revisions.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        apply.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        delete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var stored = await engineer.GetFromJsonAsync<ResourceBody>($"{Widgets}/w-1", Ct);
        stored!.Spec.GetProperty("size").GetInt32().ShouldBe(1);
    }

    [Fact]
    public async Task Apply_ShouldReturnForbidden_WhenTheCallerIsAnOperator()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Widget("w-1", "size: 1"), Ct, "?namespace=acme");
        var (_, operatorClient) = await app.MemberAsync("otto", "Operator", "acme");
        await RegistryApp.EventuallyAsync(
            async () => (await operatorClient.GetAsync($"{Widgets}/w-1", Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        var apply = await RegistryApp.ApplyAsync(operatorClient, Widget("w-1", "size: 2"), Ct, "?namespace=acme");
        var delete = await operatorClient.DeleteAsync($"{Widgets}/w-1", Ct);

        // Assert
        apply.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        delete.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await operatorClient.GetAsync(Widgets, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheResourcePublishAndLeaveATombstone_WhenNothingReferencesIt()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, TwoWidgets, Ct, "?namespace=acme");

        // Act
        var response = await engineer.DeleteAsync($"{Widgets}/child", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await engineer.GetAsync($"{Widgets}/child", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var deleted = await app.Messages.WaitForDeletedAsync("child", Ct);
        deleted.Seq.ShouldBe(4);
        var revisions = await engineer.GetFromJsonAsync<RevisionsBody>($"{Widgets}/child/revisions", Ct);
        revisions!.Items.Select(revision => revision.Deleted).ShouldBe([true, false]);
    }

    [Fact]
    public async Task Delete_ShouldReturnConflict_WhenTheResourceIsStillReferenced()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, TwoWidgets, Ct, "?namespace=acme");

        // Act
        var response = await engineer.DeleteAsync($"{Widgets}/root", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("Widget 'child'");
        (await engineer.GetAsync($"{Widgets}/root", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteManifest_ShouldDeleteEveryListedResource_WhenTheyOnlyReferenceEachOther()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, TwoWidgets, Ct, "?namespace=acme");

        // Act
        var response = await RegistryApp.DeleteManifestAsync(engineer, TwoWidgets, Ct, "?namespace=acme");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await engineer.GetFromJsonAsync<ListBody>(Widgets, Ct);
        list!.Total.ShouldBe(0);
    }

    [Fact]
    public async Task DeleteManifest_ShouldDeleteNothing_WhenAnyListedResourceIsMissing()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Widget("w-1", "size: 1"), Ct, "?namespace=acme");
        var manifest = $"""
                        {Widget("w-1", "size: 1")}
                        ---
                        {Widget("w-2", "size: 1")}
                        """;

        // Act
        var response = await RegistryApp.DeleteManifestAsync(engineer, manifest, Ct, "?namespace=acme");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await engineer.GetAsync($"{Widgets}/w-1", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ShouldShowTheStatusAndObservedGeneration_WhenTheEngineReportedOne()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Widget("w-1", "size: 1"), Ct, "?namespace=acme");
        var applied = await app.Messages.WaitForAppliedAsync("w-1", Ct);
        var status = JsonSerializer.SerializeToElement(
            new { conditions = new[] { new { type = "Ready", status = "True" } } });

        // Act
        await app.PublishAsync(new StatusReported("acme", "Widget", "w-1", applied.Uid, 1, 1, status));

        // Assert
        await RegistryApp.EventuallyAsync(
            async () => (await engineer.GetFromJsonAsync<ResourceBody>($"{Widgets}/w-1", Ct))!.Status is not null,
            Ct);
        var stored = await engineer.GetFromJsonAsync<ResourceBody>($"{Widgets}/w-1", Ct);
        stored!.Status!.Value.GetProperty("observedGeneration").GetInt64().ShouldBe(1);
        stored.Status.Value.GetProperty("conditions")[0].GetProperty("type").GetString().ShouldBe("Ready");
    }

    [Fact]
    public async Task Get_ShouldAnswerInYaml_WhenTheCallerAcceptsYaml()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, TwoWidgets, Ct, "?namespace=acme");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{Widgets}/child");
        request.Headers.Accept.ParseAdd("application/yaml");

        // Act
        var response = await engineer.SendAsync(request, Ct);

        // Assert
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/yaml");
        var yaml = await response.Content.ReadAsStringAsync(Ct);
        yaml.ShouldContain("kind: Widget");
        yaml.ShouldContain("label: \"007\"");
        yaml.ShouldContain("parent: root");
    }

    [Fact]
    public async Task Apply_ShouldKeepSeqsGapFree_WhenEngineersApplyToOneNamespaceConcurrently()
    {
        // Arrange
        const int applies = 12;
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);

        // Act
        var responses = await Task.WhenAll(
            Enumerable.Range(0, applies)
                .Select(index => RegistryApp.ApplyAsync(
                    engineer,
                    Widget($"w-{index}", "size: 1"),
                    Ct,
                    "?namespace=acme")));

        // Assert
        responses.ShouldAllBe(response => response.StatusCode == HttpStatusCode.OK);
        await RegistryApp.EventuallyAsync(
            () => Task.FromResult(
                app.Messages.Received.OfType<ResourceApplied>().Count(message => message.Kind == "Widget") == applies),
            Ct);
        app.Messages.Received.OfType<ResourceApplied>()
            .Where(message => message.Kind == "Widget")
            .Select(message => message.Seq)
            .Order()
            .ShouldBe(Enumerable.Range(2, applies).Select(seq => (long)seq));
    }

    private Task<RegistryApp> StartAsync() => RegistryApp.StartAsync(postgres);

    private static string Widget(string name, string spec) =>
        $"""
         apiVersion: flownodes.io/v1alpha1
         kind: Widget
         metadata:
           name: {name}
         spec:
           {spec}
         """;

    private static async Task<HttpClient> EngineerAsync(RegistryApp app)
    {
        await app.CreateNamespaceAsync("acme", Ct);
        var (_, engineer) = await app.MemberAsync("erin", "Engineer", "acme");
        await RegistryApp.EventuallyAsync(async () => (await engineer.GetAsync(Widgets, Ct)).IsSuccessStatusCode, Ct);

        return engineer;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record ApplyBody(bool DryRun, IReadOnlyList<AppliedBody> Resources);

    private sealed record AppliedBody(
        string Kind,
        string? Namespace,
        string Name,
        string Action,
        long Generation,
        IReadOnlyList<ChangeBody> Changes);

    private sealed record ChangeBody(string Path, string Op);

    private sealed record MetadataBody(string Name, string? Namespace, long Generation, Guid Uid);

    private sealed record ResourceBody(string Kind, MetadataBody Metadata, JsonElement Spec, JsonElement? Status);

    private sealed record ListBody(IReadOnlyList<ResourceBody> Items, int Total);

    private sealed record AuthorBody(Guid UserId, string? Username);

    private sealed record RevisionBody(long Generation, bool Deleted, AuthorBody AppliedBy, JsonElement? Spec);

    private sealed record RevisionsBody(IReadOnlyList<RevisionBody> Items, int Total);

    private sealed record ErrorBody(int Document, string? Kind, string? Name, string Path, string Message);

    private sealed record ProblemBody(IReadOnlyList<ErrorBody> Errors);
}
