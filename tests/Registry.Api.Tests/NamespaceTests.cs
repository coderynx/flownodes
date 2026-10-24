using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Registry.Application.Database;
using Registry.Domain.Namespaces;
using Registry.Domain.Resources;
using Xunit;

namespace Registry.Api.Tests;

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
        body.ShouldBe(new NamespaceBody("default"));
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
    [InlineData("a")]
    [InlineData("plant-7")]
    [InlineData("0123456789012345678901234567890123456789012345678901234567890ab")]
    public async Task PutNamespace_ShouldCreateNamespace_WhenNameIsADnsLabel(string name)
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var response = await app.Client.PutAsync($"{Route}/{name}", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<NamespaceBody>(Ct)).ShouldBe(new NamespaceBody(name));
        await app.JoinAsync(name, Ct);
        (await app.Client.GetAsync($"{Route}/{name}", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("Acme")]
    [InlineData("under_score")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("01234567890123456789012345678901234567890123456789012345678901abc")]
    public async Task PutNamespace_ShouldReturnBadRequest_WhenNameIsNotADnsLabel(string name)
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var response = await app.Client.PutAsync($"{Route}/{name}", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await app.Client.GetAsync($"{Route}/{name}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutNamespace_ShouldReturnOk_WhenNamespaceExists()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.Client.PutAsync($"{Route}/acme", null, Ct);

        // Act
        var response = await app.Client.PutAsync($"{Route}/acme", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<NamespaceBody>(Ct)).ShouldBe(new NamespaceBody("acme"));
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
        await app.Client.PutAsync($"{Route}/zeta", null, Ct);
        await app.Client.PutAsync($"{Route}/alpha", null, Ct);

        // Act
        var response = await app.Client.GetAsync(Route, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListBody>(Ct);
        body!.Total.ShouldBe(3);
        body.Items.Select(item => item.Name).ShouldBe(["alpha", "default", "zeta"]);
    }

    [Fact]
    public async Task ListNamespaces_ShouldPageFilterAndReverse_WhenQueryParametersAreGiven()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.Client.PutAsync($"{Route}/plant-a", null, Ct);
        await app.Client.PutAsync($"{Route}/plant-b", null, Ct);
        await app.Client.PutAsync($"{Route}/plant-c", null, Ct);

        // Act
        var response = await app.Client.GetAsync($"{Route}?q=plant&sort=name&desc=true&offset=1&limit=1", Ct);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ListBody>(Ct);
        body!.Total.ShouldBe(3);
        body.Items.Select(item => item.Name).ShouldBe(["plant-b"]);
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
        await app.Client.PutAsync($"{Route}/acme", null, Ct);

        // Act
        var response = await app.Client.DeleteAsync($"{Route}/acme", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
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
        await app.Client.PutAsync($"{Route}/acme", null, Ct);
        await app.JoinAsync("acme", Ct);
        await using (var scope = app.Services.CreateAsyncScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
            database.Resources.Add(Resource.Create(NamespaceName.Create("acme"), "Connection", "broker"));
            await database.SaveChangesAsync(Ct);
        }

        // Act
        var response = await app.Client.DeleteAsync($"{Route}/acme", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await app.Client.GetAsync($"{Route}/acme", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record NamespaceBody(string Name);

    private sealed record ListBody(IReadOnlyList<NamespaceBody> Items, int Total);
}
