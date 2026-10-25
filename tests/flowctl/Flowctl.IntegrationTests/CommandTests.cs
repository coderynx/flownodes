using System.Text.Json;
using Core.Contracts.Resources;
using Xunit;

namespace Flowctl.IntegrationTests;

public sealed class CommandTests(PostgresFixture postgres)
{
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
                                      spec:
                                        parent: root
                                        size: 1
                                      """;

    [Fact]
    public async Task Apply_ShouldCreateEveryResourceAndPrintWhatItDid_WhenTheManifestIsValid()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync("acme");
        var file = app.WriteFile("widgets.yaml", TwoWidgets);

        // Act
        var run = await app.RunAsync("apply", "-f", file, "-n", "acme");

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.ShouldBe(Lines("widget/root created", "widget/child created"));
        var list = await app.RunAsync("get", "widgets", "-n", "acme");
        list.Output.ShouldBe(
            Lines(
                "NAME    GENERATION   OBSERVED   READY",
                "child   1            -          -",
                "root    1            -          -"));
    }

    [Fact]
    public async Task Apply_ShouldPrintEveryProblemAndChangeNothing_WhenTheManifestIsInvalid()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();
        var file = app.WriteFile(
            "broken.yaml",
            """
            apiVersion: flownodes.io/v1alpha1
            kind: Widget
            metadata:
              name: fine
            spec: {}
            ---
            apiVersion: flownodes.io/v1alpha1
            kind: Widget
            metadata:
              name: broken
            spec:
              size: big
            """);

        // Act
        var run = await app.RunAsync("apply", "-f", file);

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.ShouldContain("nothing in it was changed");
        run.Error.ShouldContain("document 1 Widget/broken spec.size: Must be a number.");
        (await app.RunAsync("get", "widget", "fine")).ExitCode.ShouldBe(1);
    }

    [Fact]
    public async Task Get_ShouldListEveryIntegrationWithItsVersionsAndDeviceTypes_WhenAskedForIntegrations()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();

        // Act
        var run = await app.RunAsync("get", "integrations");

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        run.Output.ShouldBe(
            Lines(
                "ID                    VERSION   SDK     DEVICE TYPES          STATUS",
                "legacy                -         0.9.0   -                     failed: IncompatibleSdk: Built against SDK 0.9.0.",
                "flownodes.simulator   0.1.0     0.1.0   Thermometer, Switch   loaded"));
    }

    [Fact]
    public async Task Get_ShouldRefuse_WhenIntegrationsAreAskedForByName()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();

        // Act
        var run = await app.RunAsync("get", "integrations", "flownodes.simulator");

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.ShouldContain("Integrations are listed");
    }

    [Fact]
    public async Task Diff_ShouldPrintEveryChangeWithoutApplyingIt_WhenTheSpecDiffers()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();
        await app.RunAsync("apply", "-f", app.WriteFile("v1.yaml", Widget("w-1", "size: 3\n  old: true")));
        var changed = app.WriteFile("v2.yaml", Widget("w-1", "size: 1\n  label: new"));

        // Act
        var run = await app.RunAsync("diff", "-f", changed);

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.ShouldBe(
            Lines(
                "widget/w-1 configured (dry run)",
                "  - spec.old: true",
                "  ~ spec.size: 3 -> 1",
                "  + spec.label: \"new\""));
        (await app.RunAsync("get", "widget", "w-1", "-o", "json")).Output.ShouldContain("\"old\":true");
    }

    [Fact]
    public async Task Apply_ShouldApplyEveryFileAsOneManifest_WhenGivenSeveralYamlFiles()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();
        var first = app.WriteFile("a.yaml", Widget("a", "size: 1") + "\n...\n");
        var second = app.WriteFile("b.yaml", "%YAML 1.2\n---\n" + Widget("b", "size: 1"));

        // Act
        var run = await app.RunAsync("apply", "-f", first, "-f", second);

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        run.Output.ShouldBe(Lines("widget/a created", "widget/b created"));
    }

    [Fact]
    public async Task Apply_ShouldApplyEveryFileAsOneManifest_WhenGivenSeveralJsonFiles()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();
        var first = app.WriteFile(
            "a.json",
            """{"apiVersion": "flownodes.io/v1alpha1", "kind": "Widget", "metadata": {"name": "a"}, "spec": {}}""");
        var second = app.WriteFile(
            "b.json",
            """[{"apiVersion": "flownodes.io/v1alpha1", "kind": "Widget", "metadata": {"name": "b"}, "spec": {}}]""");

        // Act
        var run = await app.RunAsync("apply", "-f", first, "-f", second);

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        run.Output.ShouldBe(Lines("widget/a created", "widget/b created"));
    }

    [Fact]
    public async Task Get_ShouldShowObservedGenerationAndReadiness_WhenTheEngineReportedAStatus()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();
        await app.RunAsync("apply", "-f", app.WriteFile("w.yaml", Widget("w-1", "size: 1")));
        var uid = JsonDocument.Parse((await app.RunAsync("get", "widget", "w-1", "-o", "json")).Output)
            .RootElement.GetProperty("metadata")
            .GetProperty("uid")
            .GetGuid();
        var status = JsonSerializer.SerializeToElement(
            new { conditions = new[] { new { type = "Ready", status = "True" } } });

        // Act
        await app.PublishAsync(new StatusReported("default", "Widget", "w-1", uid, 1, 1, status));

        // Assert
        await FlowctlApp.EventuallyAsync(async () => (await app.RunAsync("get", "widgets")).Output.Contains("True"));
        var run = await app.RunAsync("get", "widgets");
        run.Output.ShouldBe(Lines("NAME   GENERATION   OBSERVED   READY", "w-1    1            1          True"));
    }

    [Fact]
    public async Task Get_ShouldPrintTheResourceAsYamlWithItsSpecAndStatus_WhenGettingOne()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();
        await app.RunAsync("apply", "-f", app.WriteFile("w.yaml", Widget("w-1", "size: 1")));

        // Act
        var run = await app.RunAsync("get", "widget", "w-1");

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.ShouldContain("kind: Widget");
        run.Output.ShouldContain("generation: 1");
        run.Output.ShouldContain("size: 1");
        run.Output.ShouldContain("status:");
    }

    [Fact]
    public async Task Get_ShouldListEveryResource_WhenThereAreMoreThanOnePage()
    {
        // Arrange
        const int count = 205;
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();
        var manifest = string.Join(
            "\n---\n",
            Enumerable.Range(0, count).Select(index => Widget($"w-{index:D3}", "size: 1")));
        await app.RunAsync("apply", "-f", app.WriteFile("many.yaml", manifest));

        // Act
        var run = await app.RunAsync("get", "widgets", "-o", "json");

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        var body = JsonDocument.Parse(run.Output).RootElement;
        body.GetProperty("items").GetArrayLength().ShouldBe(count);
        body.GetProperty("total").GetInt32().ShouldBe(count);
    }

    [Fact]
    public async Task Get_ShouldListNamespacesThroughTheInstallationWideRoute_WhenTheKindIsNamespace()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();

        // Act
        var run = await app.RunAsync("get", "namespace");

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        run.Output.ShouldContain("default   1");
    }

    [Fact]
    public async Task Get_ShouldPrintTheProblemAndFail_WhenTheResourceDoesNotExist()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();

        // Act
        var run = await app.RunAsync("get", "widgets", "w-9");

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.Trim().ShouldBe("error: Widget 'w-9' does not exist.");
    }

    [Fact]
    public async Task Delete_ShouldDeleteTheResourceAndNameItAsTheServerDoes_WhenGivenAKindAndAName()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync("acme");
        await app.RunAsync("apply", "-f", app.WriteFile("w.yaml", Widget("w-1", "size: 1")), "-n", "acme");

        // Act
        var run = await app.RunAsync("delete", "widgets", "w-1", "-n", "acme");

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        run.Output.Trim().ShouldBe("widget/w-1 deleted");
        (await app.RunAsync("get", "widget", "w-1", "-n", "acme")).ExitCode.ShouldBe(1);
    }

    [Fact]
    public async Task Delete_ShouldReportWithoutDeleting_WhenGivenAManifestAndDryRun()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();
        var file = app.WriteFile("widgets.yaml", TwoWidgets);
        await app.RunAsync("apply", "-f", file);

        // Act
        var run = await app.RunAsync("delete", "-f", file, "--dry-run");

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        run.Output.ShouldBe(Lines("widget/root deleted (dry run)", "widget/child deleted (dry run)"));
        (await app.RunAsync("get", "widget", "root")).ExitCode.ShouldBe(0);
    }

    [Fact]
    public async Task Delete_ShouldPrintWhoStillReferencesIt_WhenTheResourceIsReferenced()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();
        await app.RunAsync("apply", "-f", app.WriteFile("widgets.yaml", TwoWidgets));

        // Act
        var run = await app.RunAsync("delete", "widget", "root");

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.ShouldContain("Widget/root metadata.name: Still referenced by Widget 'child'");
    }

    [Fact]
    public async Task Revisions_ShouldPrintWhoChangedTheResource_WhenItWasAppliedAndDeleted()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        await app.EngineerAsync();
        var file = app.WriteFile("w.yaml", Widget("w-1", "size: 1"));
        await app.RunAsync("apply", "-f", file);
        await app.RunAsync("delete", "-f", file);

        // Act
        var run = await app.RunAsync("revisions", "widget", "w-1");

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        var rows = run.Output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        rows[0].ShouldStartWith("GENERATION   CHANGE    BY     AT");
        rows[1].ShouldStartWith("1            deleted   erin");
        rows[2].ShouldStartWith("1            applied   erin");
    }

    [Fact]
    public async Task Command_ShouldFailWithAHint_WhenNotSignedIn()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);

        // Act
        var run = await app.RunAsync("get", "widgets");

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.ShouldContain("flowctl login");
    }

    private static string Widget(string name, string spec) =>
        $"""
         apiVersion: flownodes.io/v1alpha1
         kind: Widget
         metadata:
           name: {name}
         spec:
           {spec}
         """;

    private static string Lines(params string[] lines) =>
        string.Join(Environment.NewLine, lines) + Environment.NewLine;
}
