using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.IntegrationTests;

public sealed class NamespaceTests(PostgresFixture postgres)
{
    private const string Route = "/api/v1/registry/namespaces";

    [Fact]
    public async Task GetNamespace_ShouldReturnDefault_WhenInstallationIsFresh()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var response = await app.Client.GetAsync($"{Route}/default", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<NamespaceBody>(Ct);
        body!.Kind.ShouldBe("Namespace");
        body.Metadata.Name.ShouldBe("default");
        body.Metadata.Generation.ShouldBe(1);
    }

    [Fact]
    public async Task DeleteNamespace_ShouldReturnConflict_WhenNamespaceIsDefault()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var response = await app.Client.DeleteAsync($"{Route}/default", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await app.Client.GetAsync($"{Route}/default", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("acme")]
    public async Task ApplyNamespace_ShouldCreateNamespace_WhenNameIsADnsLabel(string name)
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var response = await RegistryApp.ApplyNamespaceAsync(app.Client, name, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Actions(response)).ShouldBe([(name, "created")]);
        await app.JoinAsync(name, Ct);
        (await app.Client.GetAsync($"{Route}/{name}", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Acme")]
    public async Task ApplyNamespace_ShouldReturnUnprocessable_WhenNameIsNotADnsLabel(string name)
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var response = await RegistryApp.ApplyNamespaceAsync(app.Client, name, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await app.Client.GetAsync($"{Route}/{name}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ApplyNamespace_ShouldLeaveItUnchanged_WhenNamespaceExists()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);

        // Act
        var response = await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Actions(response)).ShouldBe([("acme", "unchanged")]);
    }

    [Fact]
    public async Task ApplyNamespace_ShouldCreateOnceAndNeverFail_WhenRequestsRace()
    {
        // Arrange
        const int names = 8;
        const int requestsPerName = 32;
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var responses = await Task.WhenAll(
            Enumerable.Range(0, names)
                .Select(name => Task.WhenAll(
                    Enumerable.Range(0, requestsPerName)
                        .Select(_ => RegistryApp.ApplyNamespaceAsync(app.Client, $"race-{name}", Ct)))));

        // Assert
        foreach (var perName in responses)
        {
            perName.ShouldAllBe(response => response.StatusCode == HttpStatusCode.OK);
            var actions = await Task.WhenAll(perName.Select(Actions));
            actions.Count(action => action.Single().Action == "created").ShouldBe(1);
        }
    }

    [Fact]
    public async Task GetNamespace_ShouldReturnNotFound_WhenNamespaceIsAbsent()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var response = await app.Client.GetAsync($"{Route}/absent", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListNamespaces_ShouldReturnNamespacesByName_WhenSeveralExist()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "zeta", Ct);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "alpha", Ct);

        // Act
        var response = await app.Client.GetAsync(Route, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListBody>(Ct);
        body!.Total.ShouldBe(3);
        body.Items.Select(item => item.Metadata.Name).ShouldBe(["alpha", "default", "zeta"]);
    }

    [Fact]
    public async Task ListNamespaces_ShouldPageFilterAndReverse_WhenQueryParametersAreGiven()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "plant-a", Ct);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "plant-b", Ct);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "plant-c", Ct);

        // Act
        var response = await app.Client.GetAsync($"{Route}?q=plant&sort=name&desc=true&offset=1&limit=1", Ct);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ListBody>(Ct);
        body!.Total.ShouldBe(3);
        body.Items.Select(item => item.Metadata.Name).ShouldBe(["plant-b"]);
    }

    [Fact]
    public async Task ListNamespaces_ShouldReturnBadRequest_WhenSortKeyIsUnknown()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var response = await app.Client.GetAsync($"{Route}?sort=colour", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("name");
    }

    [Fact]
    public async Task DeleteNamespace_ShouldRemoveNamespace_WhenItHoldsNoResources()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);

        // Act
        var response = await app.Client.DeleteAsync($"{Route}/acme", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await app.Client.GetAsync($"{Route}/acme", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteNamespace_ShouldReturnNotFound_WhenNamespaceIsAbsent()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var response = await app.Client.DeleteAsync($"{Route}/absent", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteNamespace_ShouldReturnConflict_WhenNamespaceHoldsResources()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await RegistryApp.ApplyNamespaceAsync(app.Client, "acme", Ct);
        await app.JoinAsync("acme", Ct);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            database.Resources.Add(
                Resource.Create(
                    NamespaceName.Create("acme"),
                    "Connection",
                    ResourceName.Create("broker"),
                    JsonSerializer.SerializeToElement(new { }),
                    2));
            await database.SaveChangesAsync(Ct);
        }

        // Act
        var response = await app.Client.DeleteAsync($"{Route}/acme", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await app.Client.GetAsync($"{Route}/acme", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<IReadOnlyList<(string Name, string Action)>> Actions(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<ApplyBody>(Ct);

        return [.. body!.Resources.Select(resource => (resource.Name, resource.Action))];
    }

    private sealed record Metadata(string Name, long Generation);

    private sealed record NamespaceBody(string Kind, Metadata Metadata);

    private sealed record ListBody(IReadOnlyList<NamespaceBody> Items, int Total);

    private sealed record AppliedBody(string Name, string Action);

    private sealed record ApplyBody(IReadOnlyList<AppliedBody> Resources);
}
