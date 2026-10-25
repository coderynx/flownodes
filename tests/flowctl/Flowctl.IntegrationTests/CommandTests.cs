using System.Net;
using Xunit;

namespace Flowctl.IntegrationTests;

public sealed class CommandTests
{
    private const string Manifest = """
                                    apiVersion: flownodes.io/v1alpha1
                                    kind: Widget
                                    metadata:
                                      name: w-1
                                    spec:
                                      size: 1
                                    """;

    [Fact]
    public async Task Apply_ShouldPostTheManifestAsYamlWithTheAccessToken_WhenSignedIn()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        var file = flowctl.WriteFile("widget.yaml", Manifest);
        flowctl.Stub.On(
            HttpMethod.Post,
            "/api/v1/registry/apply?namespace=acme",
            HttpStatusCode.OK,
            """
            {"dryRun": false, "resources": [
              {"kind": "Widget", "namespace": "acme", "name": "w-1", "action": "created", "generation": 1,
               "changes": []}]}
            """);

        // Act
        var run = await flowctl.RunAsync("apply", "-f", file, "-n", "acme");

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.Trim().ShouldBe("widget/w-1 created");
        var request = flowctl.Stub.Requests.ShouldHaveSingleItem();
        request.Authorization.ShouldBe("Bearer token-1");
        request.ContentType.ShouldBe("application/yaml");
        request.Body.ShouldBe(Manifest);
    }

    [Fact]
    public async Task Apply_ShouldPrintEveryProblemAndFail_WhenTheServerRejectsTheManifest()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        var file = flowctl.WriteFile("widget.yaml", Manifest);
        flowctl.Stub.On(
            HttpMethod.Post,
            "/api/v1/registry/apply",
            HttpStatusCode.UnprocessableEntity,
            """
            {"title": "manifest.invalid", "detail": "The Manifest has 2 problem(s), so nothing in it was changed.",
             "errors": [
               {"document": 0, "kind": "Widget", "name": "w-1", "path": "spec.size", "message": "Must be a number."},
               {"document": 1, "kind": null, "name": null, "path": "kind", "message": "Required."}]}
            """,
            "application/problem+json");

        // Act
        var run = await flowctl.RunAsync("apply", "-f", file);

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.ShouldContain("nothing in it was changed");
        run.Error.ShouldContain("document 0 Widget/w-1 spec.size: Must be a number.");
        run.Error.ShouldContain("document 1 ? kind: Required.");
    }

    [Fact]
    public async Task Diff_ShouldDryRunAndPrintEveryChange_WhenGivenAManifest()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        var file = flowctl.WriteFile("widget.yaml", Manifest);
        flowctl.Stub.On(
            HttpMethod.Post,
            "/api/v1/registry/apply?dryRun=true",
            HttpStatusCode.OK,
            """
            {"dryRun": true, "resources": [
              {"kind": "Widget", "name": "w-1", "action": "configured", "generation": 2, "changes": [
                {"path": "spec.size", "op": "changed", "from": 3, "to": 1},
                {"path": "spec.label", "op": "added", "to": "new"},
                {"path": "spec.old", "op": "removed", "from": true}]}]}
            """);

        // Act
        var run = await flowctl.RunAsync("diff", "-f", file);

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.ShouldBe(
            string.Join(
                Environment.NewLine,
                "widget/w-1 configured (dry run)",
                "  ~ spec.size: 3 -> 1",
                "  + spec.label: \"new\"",
                "  - spec.old: true",
                string.Empty));
    }

    [Fact]
    public async Task Apply_ShouldSendOneJsonArray_WhenEveryFileIsJson()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        var first = flowctl.WriteFile("a.json", """{"kind": "Widget", "metadata": {"name": "a"}}""");
        var second = flowctl.WriteFile("b.json", """[{"kind": "Widget", "metadata": {"name": "b"}}]""");
        flowctl.Stub.On(
            HttpMethod.Post,
            "/api/v1/registry/apply",
            HttpStatusCode.OK,
            """{"dryRun": false, "resources": []}""");

        // Act
        await flowctl.RunAsync("apply", "-f", first, "-f", second);

        // Assert
        var request = flowctl.Stub.Requests.ShouldHaveSingleItem();
        request.ContentType.ShouldBe("application/json");
        request.Body.ShouldBe(
            """[{"kind":"Widget","metadata":{"name":"a"}},{"kind":"Widget","metadata":{"name":"b"}}]""");
    }

    [Fact]
    public async Task Get_ShouldPrintATableWithGenerationsAndReadiness_WhenListingAKind()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        flowctl.Stub.On(
            HttpMethod.Get,
            "/api/v1/registry/namespaces/acme/widgets",
            HttpStatusCode.OK,
            """
            {"items": [
              {"kind": "Widget", "metadata": {"name": "pump-1", "generation": 3}, "spec": {},
               "status": {"observedGeneration": 2, "conditions": [{"type": "Ready", "status": "False"}]}},
              {"kind": "Widget", "metadata": {"name": "w-2", "generation": 1}, "spec": {}, "status": null}],
             "total": 2}
            """);

        // Act
        var run = await flowctl.RunAsync("get", "Widget", "-n", "acme");

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.ShouldBe(
            string.Join(
                Environment.NewLine,
                "NAME     GENERATION   OBSERVED   READY",
                "pump-1   3            2          False",
                "w-2      1            -          -",
                string.Empty));
    }

    [Fact]
    public async Task Get_ShouldPrintTheServersYaml_WhenGettingOneResource()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        const string yaml = "apiVersion: flownodes.io/v1alpha1\nkind: Widget\n";
        flowctl.Stub.On(
            HttpMethod.Get,
            "/api/v1/registry/namespaces/default/widgets/w-1",
            HttpStatusCode.OK,
            yaml,
            "application/yaml");

        // Act
        var run = await flowctl.RunAsync("get", "widget", "w-1");

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.ShouldBe(yaml);
        flowctl.Stub.Requests.ShouldHaveSingleItem().Accept.ShouldBe("application/yaml");
    }

    [Fact]
    public async Task Get_ShouldUseTheInstallationWideRoute_WhenTheKindIsNamespace()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        flowctl.Stub.On(
            HttpMethod.Get,
            "/api/v1/registry/namespaces",
            HttpStatusCode.OK,
            """{"items": [{"kind": "Namespace", "metadata": {"name": "default", "generation": 1}}], "total": 1}""");

        // Act
        var run = await flowctl.RunAsync("get", "namespaces");

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.ShouldContain("default   1");
    }

    [Fact]
    public async Task Get_ShouldPrintTheProblemAndFail_WhenTheResourceDoesNotExist()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        flowctl.Stub.On(
            HttpMethod.Get,
            "/api/v1/registry/namespaces/default/widgets/w-9",
            HttpStatusCode.NotFound,
            """{"title": "resource.not_found", "detail": "Widget 'w-9' does not exist."}""",
            "application/problem+json");

        // Act
        var run = await flowctl.RunAsync("get", "widgets", "w-9");

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.Trim().ShouldBe("error: Widget 'w-9' does not exist.");
    }

    [Fact]
    public async Task Delete_ShouldDeleteTheNamedResource_WhenGivenAKindAndAName()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        flowctl.Stub.On(HttpMethod.Delete, "/api/v1/registry/namespaces/acme/widgets/w-1", HttpStatusCode.NoContent);

        // Act
        var run = await flowctl.RunAsync("delete", "widget", "w-1", "-n", "acme");

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.Trim().ShouldBe("widget/w-1 deleted");
    }

    [Fact]
    public async Task Delete_ShouldPostTheManifestToTheBatchDelete_WhenGivenAFile()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        var file = flowctl.WriteFile("widget.yaml", Manifest);
        flowctl.Stub.On(
            HttpMethod.Post,
            "/api/v1/registry/delete?dryRun=true",
            HttpStatusCode.OK,
            """{"dryRun": true, "resources": [{"kind": "Widget", "namespace": "default", "name": "w-1"}]}""");

        // Act
        var run = await flowctl.RunAsync("delete", "-f", file, "--dry-run");

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.Trim().ShouldBe("widget/w-1 deleted (dry run)");
        flowctl.Stub.Requests.ShouldHaveSingleItem().Body.ShouldBe(Manifest);
    }

    [Fact]
    public async Task Revisions_ShouldPrintWhoChangedWhatAndWhen_WhenTheResourceHasRevisions()
    {
        // Arrange
        using var flowctl = Flowctl.SignedIn();
        flowctl.Stub.On(
            HttpMethod.Get,
            "/api/v1/registry/namespaces/default/widgets/w-1/revisions",
            HttpStatusCode.OK,
            """
            {"items": [
              {"generation": 2, "deleted": true, "appliedBy": {"userId": "3f1c0000-0000-0000-0000-000000000000",
               "username": null}, "appliedAt": "2026-10-25T12:30:00+00:00"},
              {"generation": 2, "deleted": false, "appliedBy": {"userId": "3f1c0000-0000-0000-0000-000000000000",
               "username": "erin"}, "appliedAt": "2026-10-25T12:00:00+00:00"}],
             "total": 2}
            """);

        // Act
        var run = await flowctl.RunAsync("revisions", "widgets", "w-1");

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.ShouldBe(
            string.Join(
                Environment.NewLine,
                "GENERATION   CHANGE    BY                                     AT",
                "2            deleted   3f1c0000-0000-0000-0000-000000000000   2026-10-25 12:30:00Z",
                "2            applied   erin                                   2026-10-25 12:00:00Z",
                string.Empty));
    }

    [Fact]
    public async Task Command_ShouldFailWithAHint_WhenNotSignedIn()
    {
        // Arrange
        using var flowctl = new Flowctl();

        // Act
        var run = await flowctl.RunAsync("get", "widgets");

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.ShouldContain("flowctl login");
        flowctl.Stub.Requests.ShouldBeEmpty();
    }
}
