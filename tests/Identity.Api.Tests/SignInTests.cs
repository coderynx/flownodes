using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Xunit;

namespace Identity.Api.Tests;

public sealed class SignInTests(PostgresFixture postgres)
{
    [Fact]
    public async Task DeviceFlow_ShouldIssueAnAccessTokenAndARefreshToken_WhenTheUserConfirmsTheCode()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        await app.AdministratorAsync(Ct);

        // Act
        var tokens = await app.SignInWithDeviceFlowAsync(
            IdentityApp.AdministratorName,
            IdentityApp.AdministratorPassword,
            Ct);

        // Assert
        tokens.AccessToken.ShouldNotBeNullOrWhiteSpace();
        tokens.RefreshToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AccessToken_ShouldCarrySubjectAndNoRolesAndExpireInTenMinutes_WhenIssued()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var tokens = await app.SignInWithDeviceFlowAsync(
            IdentityApp.AdministratorName,
            IdentityApp.AdministratorPassword,
            Ct);
        var me = await administrator.GetFromJsonAsync<JsonElement>("/api/v1/identity/me", Ct);

        // Act
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(tokens.AccessToken);

        // Assert
        jwt.Subject.ShouldBe(me.GetProperty("id").GetString());
        jwt.Claims.Select(claim => claim.Type).ShouldNotContain("role");
        (jwt.ValidTo - jwt.IssuedAt).ShouldBe(TimeSpan.FromMinutes(10), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TokenEndpoint_ShouldRefuseThePasswordGrant_WhenCredentialsAreSent()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        await app.AdministratorAsync(Ct);

        // Act
        var response = await app.PostTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = IdentityApp.ClientId,
                ["username"] = IdentityApp.AdministratorName,
                ["password"] = IdentityApp.AdministratorPassword
            },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("unsupported_grant_type");
    }

    [Fact]
    public async Task TokenEndpoint_ShouldIssueNewTokens_WhenARefreshTokenIsPresented()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        await app.AdministratorAsync(Ct);
        var tokens = await app.SignInWithDeviceFlowAsync(
            IdentityApp.AdministratorName,
            IdentityApp.AdministratorPassword,
            Ct);

        // Act
        var response = await app.PostTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = IdentityApp.ClientId,
                ["refresh_token"] = tokens.RefreshToken!
            },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var refreshed = await response.Content.ReadFromJsonAsync<IdentityApp.TokenSet>(Ct);
        refreshed!.AccessToken.ShouldNotBeNullOrWhiteSpace();
        refreshed.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        (await app.ClientFor(refreshed.AccessToken).GetAsync("/api/v1/identity/me", Ct)).StatusCode.ShouldBe(
            HttpStatusCode.OK);
    }

    [Fact]
    public async Task SignInPage_ShouldLockTheUserOut_WhenFiveAttemptsFail()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        await app.AdministratorAsync(Ct);
        using var browser = app.NewBrowser();
        for (var attempt = 0; attempt < 5; attempt++)
        {
            await browser.PostFormAsync("/signin", "/signin", Credentials("wrong password here"), Ct);
        }

        // Act
        var response = await browser.PostFormAsync(
            "/signin",
            "/signin",
            Credentials(IdentityApp.AdministratorPassword),
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("Too many failed attempts");
    }

    [Fact]
    public async Task SignInPage_ShouldRefuseTheUser_WhenThePasswordIsWrong()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        await app.AdministratorAsync(Ct);
        using var browser = app.NewBrowser();

        // Act
        var response = await browser.PostFormAsync("/signin", "/signin", Credentials("wrong password here"), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("username or password is wrong");
    }

    [Theory]
    [InlineData("GET", "/api/v1/identity/me")]
    [InlineData("GET", "/api/v1/identity/users")]
    [InlineData("POST", "/api/v1/identity/users")]
    [InlineData("PATCH", "/api/v1/identity/users/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/v1/identity/users/00000000-0000-0000-0000-000000000001")]
    [InlineData("GET", "/api/v1/identity/users/00000000-0000-0000-0000-000000000001/tokens")]
    [InlineData("POST", "/api/v1/identity/role-grants")]
    [InlineData("DELETE", "/api/v1/identity/role-grants")]
    [InlineData("PUT", "/api/v1/identity/me/password")]
    public async Task IdentityApi_ShouldReturnUnauthorized_WhenNoAccessTokenIsPresented(string method, string route)
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);

        // Act
        var response = await app.Client.SendAsync(new HttpRequestMessage(new HttpMethod(method), route), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AccessToken_ShouldStillBeValid_WhenTheSiloRestartsWithTheSameInstallationKey()
    {
        // Arrange
        var installationKey = Convert.ToBase64String(new byte[32].Select((_, index) => (byte)(index + 1)).ToArray());
        string accessToken;
        string connectionString;
        await using (var first = await IdentityApp.StartAsync(postgres, installationKey))
        {
            await first.AdministratorAsync(Ct);
            accessToken = (await first.SignInWithDeviceFlowAsync(
                IdentityApp.AdministratorName,
                IdentityApp.AdministratorPassword,
                Ct)).AccessToken;
            connectionString = first.ConnectionString;
        }

        // Act
        await using var second = await IdentityApp.StartAsync(postgres, installationKey, connectionString);

        // Assert
        (await second.ClientFor(accessToken).GetAsync("/api/v1/identity/me", Ct)).StatusCode
            .ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Startup_ShouldFail_WhenTheInstallationKeyCannotDecryptTheSigningKey()
    {
        // Arrange
        string connectionString;
        await using (var first = await IdentityApp.StartAsync(postgres))
        {
            connectionString = first.ConnectionString;
        }

        var act = () => IdentityApp.StartAsync(
            postgres,
            Convert.ToBase64String(new byte[32].Select((_, index) => (byte)(index + 7)).ToArray()),
            connectionString);

        // Act
        // Assert
        await act.ShouldThrowAsync<Exception>();
    }

    private static Dictionary<string, string> Credentials(string password) =>
        new()
        {
            ["Username"] = IdentityApp.AdministratorName,
            ["Password"] = password
        };

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
