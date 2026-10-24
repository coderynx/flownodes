using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Core.Application.Authorization;
using Core.Contracts.Identity;
using Core.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Registry.Application.Database;
using Xunit;

namespace Registry.IntegrationTests;

public sealed class AccessTests(PostgresFixture postgres)
{
    private const string Route = "/api/v1/registry/namespaces";

    [Theory]
    [InlineData("GET", Route)]
    [InlineData("GET", $"{Route}/default")]
    [InlineData("PUT", $"{Route}/acme")]
    [InlineData("DELETE", $"{Route}/acme")]
    public async Task Request_ShouldReturnUnauthorized_WhenThereIsNoAccessToken(string method, string route)
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        var response = await app.Anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), route), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_ShouldReturnUnauthorized_WhenTheAccessTokenIsNotIssuedByIdentity()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        using var client = app.ClientFor("not.a.token");

        // Act
        var response = await client.GetAsync(Route, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PutNamespace_ShouldReturnForbidden_WhenCallerIsNotAnAdministrator()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var (_, engineer) = await app.MemberAsync("erin", "Engineer", "default");
        await RegistryApp.EventuallyAsync(
            async () => (await engineer.GetAsync($"{Route}/default", Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        var response = await engineer.PutAsync($"{Route}/acme", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await app.Client.GetAsync($"{Route}/acme", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteNamespace_ShouldReturnForbidden_WhenCallerIsNotAnAdministrator()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);
        var (_, engineer) = await app.MemberAsync("erin", "Engineer", "acme");
        await RegistryApp.EventuallyAsync(
            async () => (await engineer.GetAsync($"{Route}/acme", Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        var response = await engineer.DeleteAsync($"{Route}/acme", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await app.JoinAsync("acme", Ct);
    }

    [Fact]
    public async Task ListNamespaces_ShouldListOnlyNamespacesWithARole_WhenCallerIsNotAnAdministrator()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);
        await app.CreateNamespaceAsync("globex", Ct);
        var (userId, member) = await app.MemberAsync("olive", "Operator", "acme");
        await app.GrantAsync(userId, "Engineer", "globex");
        await RegistryApp.EventuallyAsync(
            async () => (await member.GetAsync($"{Route}/globex", Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        var response = await member.GetAsync(Route, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListBody>(Ct);
        body!.Items.Select(item => item.Name).ShouldBe(["acme", "globex"]);
        body.Total.ShouldBe(2);
    }

    [Fact]
    public async Task ListNamespaces_ShouldListEveryNamespace_WhenCallerIsAnAdministrator()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);

        // Act
        var response = await app.Client.GetAsync(Route, Ct);

        // Assert
        var body = await response.Content.ReadFromJsonAsync<ListBody>(Ct);
        body!.Items.Select(item => item.Name).ShouldBe(["acme", "default"]);
    }

    [Fact]
    public async Task GetNamespace_ShouldReturnNotFound_WhenCallerHoldsNoRoleInIt()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);
        var (_, member) = await app.MemberAsync("olive", "Operator", "default");
        await RegistryApp.EventuallyAsync(
            async () => (await member.GetAsync($"{Route}/default", Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        var existing = await member.GetAsync($"{Route}/acme", Ct);
        var absent = await member.GetAsync($"{Route}/absent", Ct);

        // Assert
        existing.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        absent.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetNamespace_ShouldReturnNotFound_WhenCallerIsAnAdministratorWithoutARoleInIt()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);

        // Act
        var response = await app.Client.GetAsync($"{Route}/acme", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RequireRole_ShouldReturnForbidden_WhenMemberHoldsOnlyTheOtherRole()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var (userId, member) = await app.MemberAsync("olive", "Operator", "default");
        await RegistryApp.EventuallyAsync(
            async () => (await member.GetAsync($"{Route}/default", Ct)).IsSuccessStatusCode,
            Ct);
        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Act
        var engineer = await database.RequireRoleAsync(userId, "default", NamespaceRole.Engineer, Ct);
        var operatorRole = await database.RequireRoleAsync(userId, "default", NamespaceRole.Operator, Ct);
        var stranger = await database.RequireRoleAsync(userId, "acme", NamespaceRole.Operator, Ct);

        // Assert
        engineer.Error.Code.ShouldBe(AccessErrors.ForbiddenCode);
        operatorRole.IsSuccess.ShouldBeTrue();
        stranger.Error.Code.ShouldBe("namespace.not_found");
    }

    [Fact]
    public async Task Request_ShouldBeRejected_WhenCallerIsDisabledInIdentity()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var (userId, member) = await app.MemberAsync("olive", "Operator", "default");
        await RegistryApp.EventuallyAsync(
            async () => (await member.GetAsync($"{Route}/default", Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        await app.DisableAsync(userId);

        // Assert
        var status = await RegistryApp.StatusEventuallyAsync(
            () => member.GetAsync($"{Route}/default", Ct),
            HttpStatusCode.Unauthorized,
            Ct);
        status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_ShouldBeRejected_WhenCallerIsDemotedInIdentity()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var (userId, member) = await app.MemberAsync("olive", "Operator", "default");
        await RegistryApp.EventuallyAsync(
            async () => (await member.GetAsync($"{Route}/default", Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        await app.RevokeAsync(userId, "Operator", "default");

        // Assert
        var status = await RegistryApp.StatusEventuallyAsync(
            () => member.GetAsync($"{Route}/default", Ct),
            HttpStatusCode.NotFound,
            Ct);
        status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutNamespace_ShouldReturnForbidden_WhenAdministratorIsRevokedInIdentity()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var userId = await app.CreateUserAsync("ada");
        await app.GrantAsync(userId, "Administrator", null);
        var administrator = app.SignedIn("ada");
        await RegistryApp.EventuallyAsync(
            async () => (await administrator.PutAsync($"{Route}/acme", null, Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        await app.RevokeAsync(userId, "Administrator", null);

        // Assert
        var status = await RegistryApp.StatusEventuallyAsync(
            () => administrator.PutAsync($"{Route}/acme", null, Ct),
            HttpStatusCode.Forbidden,
            Ct);
        status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Messages_ShouldKeepUsernameAndDisabledFlag_WhenIdentityReportsThem()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var userId = Guid.NewGuid();

        // Act
        await app.PublishAsync(new UserRenamed(userId, "grace", 1));
        await app.PublishAsync(new UserDisabled(userId, 2));

        // Assert
        await RegistryApp.EventuallyAsync(async () => (await FindAsync(app, userId))?.IsDisabled == true, Ct);
        var user = await FindAsync(app, userId);
        user!.Username.ShouldBe("grace");
    }

    [Fact]
    public async Task Messages_ShouldBeDropped_WhenTheirSeqIsNotNewerThanTheLastApplied()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var userId = Guid.NewGuid();
        await app.PublishAsync(new RoleGranted(userId, "default", "Operator", 2));

        // Act
        await app.PublishAsync(new RoleRevoked(userId, "default", "Operator", 1));
        await app.PublishAsync(new RoleRevoked(userId, "default", "Operator", 2));
        await app.PublishAsync(new AdministratorGranted(userId, 3));

        // Assert
        await RegistryApp.EventuallyAsync(async () => (await FindAsync(app, userId))?.IsAdministrator == true, Ct);
        var user = await FindAsync(app, userId);
        user!.Grants.Select(grant => (grant.Namespace, grant.Role)).ShouldBe([("default", NamespaceRole.Operator)]);
    }

    [Fact]
    public async Task Request_ShouldReturnUnauthorized_WhenTheAuthorizationSchemeIsNotBearer()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var client = app.Anonymous;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "cm9vdDpyb290");

        // Act
        var response = await client.GetAsync(Route, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static async Task<UserAccess?> FindAsync(RegistryApp app, Guid userId)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        return await database.UserAccess.AsNoTracking()
            .Include(user => user.Grants)
            .SingleOrDefaultAsync(user => user.Id == userId, TestContext.Current.CancellationToken);
    }

    private sealed record NamespaceBody(string Name);

    private sealed record ListBody(IReadOnlyList<NamespaceBody> Items, int Total);
}
