using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Identity.IntegrationTests;

public sealed class PersonalAccessTokenTests(PostgresFixture postgres)
{
    [Fact]
    public async Task CreateToken_ShouldReturnTheSecretOnce_WhenTheUserCreatesATokenForThemself()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);

        // Act
        var created = await CreateTokenAsync(administrator, "laptop", null);

        // Assert
        created.Secret!.ShouldStartWith("fnpat_");
        var listed = await administrator.GetFromJsonAsync<TokenList>("/api/v1/identity/users/me/tokens", Ct);
        listed!.Items.Select(item => item.Name).ShouldBe(["laptop"]);
        listed.Items.Single().Secret.ShouldBeNull();
    }

    [Fact]
    public async Task TokenEndpoint_ShouldIssueAnAccessTokenForTheOwner_WhenAPersonalAccessTokenIsExchanged()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var created = await CreateTokenAsync(administrator, "ci", null);

        // Act
        var response = await ExchangeAsync(app, created.Secret!);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tokens = await response.Content.ReadFromJsonAsync<IdentityApp.TokenSet>(Ct);
        var me = await app.ClientFor(tokens!.AccessToken).GetFromJsonAsync<MeBody>("/api/v1/identity/me", Ct);
        me!.Username.ShouldBe(IdentityApp.AdministratorName);
    }

    [Fact]
    public async Task TokenEndpoint_ShouldRefuseTheExchange_WhenTheSecretIsUnknown()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        await app.AdministratorAsync(Ct);

        // Act
        var response = await ExchangeAsync(app, "fnpat_not-a-real-secret");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("invalid_grant");
    }

    [Fact]
    public async Task TokenEndpoint_ShouldRefuseTheExchange_WhenTheTokenIsRevoked()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var created = await CreateTokenAsync(administrator, "ci", null);

        // Act
        var revoked = await administrator.DeleteAsync($"/api/v1/identity/users/me/tokens/{created.Id}", Ct);
        var response = await ExchangeAsync(app, created.Secret!);

        // Assert
        revoked.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await administrator.GetFromJsonAsync<TokenList>("/api/v1/identity/users/me/tokens", Ct))!.Items
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task TokenEndpoint_ShouldRefuseTheExchange_WhenTheTokenHasExpired()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var created = await CreateTokenAsync(administrator, "short", DateTimeOffset.UtcNow.AddSeconds(2));
        await Task.Delay(TimeSpan.FromSeconds(3), Ct);

        // Act
        var response = await ExchangeAsync(app, created.Secret!);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateToken_ShouldReturnBadRequest_WhenTheExpiryIsInThePast()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);

        // Act
        var response = await administrator.PostAsJsonAsync(
            "/api/v1/identity/users/me/tokens",
            new { name = "old", expiresAt = DateTimeOffset.UtcNow.AddDays(-1) },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListTokens_ShouldReturnForbidden_WhenAUserAsksForAnotherUsersTokens()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        await IdentityApp.CreateUserAsync(administrator, "alice", "alice has a long password", Ct);
        var bobId = await IdentityApp.CreateUserAsync(administrator, "bob", "bob has a long password", Ct);
        var alice = await app.SignedInAsync("alice", "alice has a long password", Ct);

        // Act
        var response = await alice.GetAsync($"/api/v1/identity/users/{bobId}/tokens", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    private static async Task<HttpResponseMessage> ExchangeAsync(IdentityApp app, string secret) =>
        await app.PostTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = IdentityApp.TokenExchangeGrant,
                ["client_id"] = IdentityApp.ClientId,
                ["subject_token"] = secret,
                ["subject_token_type"] = IdentityApp.PersonalAccessTokenType
            },
            Ct);

    private static async Task<TokenBody> CreateTokenAsync(HttpClient client, string name, DateTimeOffset? expiresAt)
    {
        var response = await client.PostAsJsonAsync("/api/v1/identity/users/me/tokens", new { name, expiresAt }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<TokenBody>(Ct))!;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record TokenBody(Guid Id, string Name, string? Secret, DateTimeOffset? ExpiresAt);

    private sealed record TokenList(IReadOnlyList<TokenBody> Items);

    private sealed record MeBody(string Username);
}
