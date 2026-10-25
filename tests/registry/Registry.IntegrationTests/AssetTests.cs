using System.Net;
using Xunit;

namespace Registry.IntegrationTests;

public sealed class AssetTests(PostgresFixture postgres)
{
    private const string Train = """
                                 apiVersion: flownodes.io/v1alpha1
                                 kind: AssetType
                                 metadata:
                                   name: train
                                   namespace: acme
                                 spec:
                                   dataPoints:
                                     - name: speed
                                       type: number
                                 """;

    private const string Fleet = """
                                 apiVersion: flownodes.io/v1alpha1
                                 kind: Asset
                                 metadata:
                                   name: fleet
                                   namespace: acme
                                 spec: {}
                                 """;

    private const string Train1 = """
                                  apiVersion: flownodes.io/v1alpha1
                                  kind: Asset
                                  metadata:
                                    name: train-1
                                    namespace: acme
                                  spec:
                                    assetType: train
                                    parent: fleet
                                    dataPoints:
                                      - name: doors
                                        type: bool
                                  """;

    [Fact]
    public async Task Apply_ShouldAcceptTheTree_WhenTheAssetsComeBeforeTheirTypeAndParent()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, Join(Train1, Fleet, Train), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Apply_ShouldRejectTheManifest_WhenTheAssetTypeIsMissing()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, Join(Fleet, Train1), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("AssetType 'train'");
    }

    [Fact]
    public async Task Apply_ShouldRejectTheAssetTypeAndNameTheInstance_WhenItsChangeClashesWithAnInstance()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Join(Train, Fleet, Train1), Ct);
        var changed = Train.Replace("name: speed", "name: doors", StringComparison.Ordinal);

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, changed, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadAsStringAsync(Ct);
        body.ShouldContain("train-1");
        body.ShouldContain("changes its meaning");
    }

    [Fact]
    public async Task Apply_ShouldRejectTheManifest_WhenTheParentsLoop()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        var a = Fleet.Replace("spec: {}", "spec:\n  parent: b", StringComparison.Ordinal);
        var b = a.Replace("name: fleet", "name: b", StringComparison.Ordinal)
            .Replace("parent: b", "parent: fleet", StringComparison.Ordinal);

        // Act
        var response = await RegistryApp.ApplyAsync(engineer, Join(a, b), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("loop");
    }

    [Fact]
    public async Task Delete_ShouldRefuseTheParentAndTheTypeThenRemoveTheTree_WhenTheTreeIsDeletedInOneBatch()
    {
        // Arrange
        await using var app = await StartAsync();
        var engineer = await EngineerAsync(app);
        await RegistryApp.ApplyAsync(engineer, Join(Train, Fleet, Train1), Ct);
        const string Base = "/api/v1/registry/namespaces/acme";

        // Act
        var parent = await engineer.DeleteAsync($"{Base}/assets/fleet", Ct);
        var type = await engineer.DeleteAsync($"{Base}/assettypes/train", Ct);
        var batch = await RegistryApp.DeleteManifestAsync(engineer, Join(Train, Fleet, Train1), Ct);

        // Assert
        parent.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        type.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        batch.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await engineer.GetAsync($"{Base}/assets/fleet", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static string Join(params string[] documents) => string.Join("\n---\n", documents);
    private Task<RegistryApp> StartAsync() => RegistryApp.StartAsync(postgres);

    private static async Task<HttpClient> EngineerAsync(RegistryApp app)
    {
        await app.CreateNamespaceAsync("acme", Ct);
        var (_, engineer) = await app.MemberAsync("erin", "Engineer", "acme");
        await RegistryApp.EventuallyAsync(
            async () => (await engineer.GetAsync("/api/v1/registry/namespaces/acme/assets", Ct)).IsSuccessStatusCode,
            Ct);

        return engineer;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
