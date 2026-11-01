using System.Net;
using System.Text.Json;
using OpenIddict.Abstractions;
using Xunit;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Flownodes.IntegrationTests;

public sealed class WebSignInTests(PostgresFixture postgres)
{
    private const string Administrator = "root";
    private const string Password = "correct horse battery staple";
    private const string ClientHeader = "X-Flownodes-Client";
    private static readonly TimeSpan ShorterThanTheRefreshMargin = TimeSpan.FromSeconds(30);
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SignIn_ShouldReturnToThePageAskedFor_WhenTheUserSignsInAtIdentity()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = host.NewBrowser();
        var page = await browser.GetAsync("/bff/signin?returnUrl=%2Falarms%2F7", Ct);

        // Act
        var response = await browser.SubmitSignInPageAsync(page, "?handler=Setup", Setup(host), Ct);

        // Assert
        page.RequestMessage!.RequestUri!.AbsolutePath.ShouldBe("/signin");
        response.RequestMessage!.RequestUri!.PathAndQuery.ShouldBe("/alarms/7");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SignIn_ShouldReturnToTheRoot_WhenTheReturnUrlIsOnAnotherSite()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = host.NewBrowser();
        var page = await browser.GetAsync("/bff/signin?returnUrl=https%3A%2F%2Fevil.example%2F", Ct);

        // Act
        var response = await browser.SubmitSignInPageAsync(page, "?handler=Setup", Setup(host), Ct);

        // Assert
        response.RequestMessage!.RequestUri!.ToString().ShouldBe(host.Address.ToString());
    }

    [Fact]
    public async Task SignIn_ShouldStillReturnToThePageAskedFor_WhenTheSignInCookiesExpiredOnIdentitysPage()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = host.NewBrowser();
        var page = await browser.GetAsync("/bff/signin?returnUrl=%2Falarms%2F7", Ct);
        browser.ForgetSignInCookies();

        // Act
        var response = await browser.SubmitSignInPageAsync(page, "?handler=Setup", Setup(host), Ct);

        // Assert
        response.RequestMessage!.RequestUri!.PathAndQuery.ShouldBe("/alarms/7");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        browser.SessionCookie.ShouldNotBeNull();
    }

    [Fact]
    public async Task SignIn_ShouldFailInsteadOfLooping_WhenTheBrowserRefusesTheSignInCookies()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = new Browser(host.Address) { RefusesSignInCookies = true };
        var page = await browser.GetAsync("/bff/signin", Ct);

        // Act
        var response = await browser.SubmitSignInPageAsync(page, "?handler=Setup", Setup(host), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        browser.SessionCookie.ShouldBeNull();
    }

    [Fact]
    public async Task Session_ShouldReturnUnauthorized_WhenThereIsNoCookie()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = host.NewBrowser();

        // Act
        var response = await browser.GetAsync("/bff/session", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Session_ShouldReturnTheUsersIdAndName_WhenSignedIn()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = await SignedInAdministratorAsync(host);
        var me = await browser.GetAsync("/api/v1/identity/me", Ct);
        var profile = await me.Content.ReadFromJsonAsync<JsonElement>(Ct);

        // Act
        var response = await browser.GetAsync("/bff/session", Ct);

        // Assert
        var session = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        session.GetProperty("id").GetString().ShouldBe(profile.GetProperty("id").GetString());
        session.GetProperty("name").GetString().ShouldBe(Administrator);
    }

    [Fact]
    public async Task Session_ShouldLiveInWebsSchema_WhenSignedIn()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);

        // Act
        using var browser = await SignedInAdministratorAsync(host);

        // Assert
        (await SignInHost.CountAsync(host.WebDatabase, "web.sessions", Ct)).ShouldBe(1);
        (await SignInHost.CountAsync(host.WebDatabase, "web.\"DataProtectionKeys\"", Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task Cookie_ShouldCarryOnlyAProtectedSessionId_WhenSignedIn()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);

        // Act
        using var browser = await SignedInAdministratorAsync(host);

        // Assert
        var header = browser.SessionCookieHeaders.ShouldHaveSingleItem().ToLowerInvariant();
        header.ShouldContain("path=/");
        header.ShouldContain("secure");
        header.ShouldContain("samesite=lax");
        header.ShouldContain("httponly");
        header.ShouldNotContain("domain=");
        browser.SessionCookie!.Length.ShouldBeLessThan(1024);
    }

    [Fact]
    public async Task Request_ShouldAuthenticateAtAModule_WhenItCarriesOnlyTheCookie()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = await SignedInAdministratorAsync(host);

        // Act
        var response = await browser.GetAsync("/api/v1/identity/me", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_ShouldKeepItsOwnBearerToken_WhenItAlsoCarriesTheCookie()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = await SignedInAdministratorAsync(host);
        using var request = new HttpRequestMessage(HttpMethod.Get, SignInHost.ProbePath);
        request.Headers.TryAddWithoutValidation("Authorization", "Bearer own");

        // Act
        var response = await browser.SendAsync(request, Ct);

        // Assert
        (await response.Content.ReadAsStringAsync(Ct)).ShouldBe("Bearer own");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task UnsafeRequest_ShouldCarryTheToken_WhenMethodHasTheClientHeader(string method)
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = await SignedInAdministratorAsync(host);
        using var request = new HttpRequestMessage(new HttpMethod(method), SignInHost.ProbePath);
        request.Headers.Add(ClientHeader, "web");

        // Act
        var response = await browser.SendAsync(request, Ct);

        // Assert
        (await response.Content.ReadAsStringAsync(Ct)).ShouldStartWith("Bearer ");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task UnsafeRequest_ShouldNotCarryTheToken_WhenMethodLacksTheClientHeader(string method)
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = await SignedInAdministratorAsync(host);
        using var request = new HttpRequestMessage(new HttpMethod(method), SignInHost.ProbePath);

        // Act
        var response = await browser.SendAsync(request, Ct);

        // Assert
        (await response.Content.ReadAsStringAsync(Ct)).ShouldBeEmpty();
    }

    [Fact]
    public async Task AccessToken_ShouldStayTheSame_WhenItIsFarFromExpiry()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = await SignedInAdministratorAsync(host);
        var first = await ProbeAsync(browser);

        // Act
        var second = await ProbeAsync(browser);

        // Assert
        second.ShouldBe(first);
    }

    [Fact]
    public async Task AccessToken_ShouldRefresh_WhenItIsCloseToExpiry()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres, ShorterThanTheRefreshMargin);
        using var browser = await SignedInAdministratorAsync(host);
        var first = await ProbeAsync(browser);

        // Act
        var second = await ProbeAsync(browser);

        // Assert
        second.ShouldNotBe(first);
    }

    [Fact]
    public async Task Session_ShouldEnd_WhenItsUserIsDisabledAndTheTokenComesUpForRefresh()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres, ShorterThanTheRefreshMargin);
        using var administrator = await SignedInAdministratorAsync(host);
        using var create = new HttpRequestMessage(HttpMethod.Post, "/api/v1/identity/users")
        {
            Content = JsonContent.Create(new { username = "erin", password = Password })
        };
        create.Headers.Add(ClientHeader, "web");
        var created = await administrator.SendAsync(create, Ct);
        var userId = (await created.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("id").GetString();
        using var erin = host.NewBrowser();
        var page = await erin.GetAsync("/bff/signin", Ct);
        await erin.SubmitSignInPageAsync(page, string.Empty, Credentials("erin"), Ct);
        await erin.GetAsync("/bff/session", Ct);
        using var disable = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/identity/users/{userId}/disable");
        disable.Headers.Add(ClientHeader, "web");
        await administrator.SendAsync(disable, Ct);

        // Act
        var response = await erin.GetAsync("/bff/session", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        erin.SessionCookie.ShouldBeNull();
        (await SignInHost.CountAsync(host.WebDatabase, "web.sessions", Ct)).ShouldBe(1);
    }

    [Fact]
    public async Task SignOut_ShouldDeleteTheSessionAndRevokeItsRefreshToken_WhenSignedIn()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = await SignedInAdministratorAsync(host);
        using var signOut = new HttpRequestMessage(HttpMethod.Post, "/bff/signout");

        // Act
        var response = await browser.SendAsync(signOut, Ct);

        // Assert
        response.RequestMessage!.RequestUri!.AbsolutePath.ShouldBe("/");
        browser.SessionCookie.ShouldBeNull();
        (await SignInHost.CountAsync(host.WebDatabase, "web.sessions", Ct)).ShouldBe(0);
        (await RefreshTokenStatusesAsync(host)).ShouldAllBe(status => status == Statuses.Revoked);
    }

    [Fact]
    public async Task SignIn_ShouldAskForThePassword_WhenTheUserSignedOut()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = await SignedInAdministratorAsync(host);
        using var signOut = new HttpRequestMessage(HttpMethod.Post, "/bff/signout");
        await browser.SendAsync(signOut, Ct);

        // Act
        var response = await browser.GetAsync("/bff/signin", Ct);

        // Assert
        response.RequestMessage!.RequestUri!.AbsolutePath.ShouldBe("/signin");
    }

    [Fact]
    public async Task SignIn_ShouldAskForThePassword_WhenTheUserSignedOutWithTheWebSessionAlreadyGone()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        using var browser = await SignedInAdministratorAsync(host);
        browser.ForgetSessionCookie();
        using var signOut = new HttpRequestMessage(HttpMethod.Post, "/bff/signout");
        await browser.SendAsync(signOut, Ct);

        // Act
        var response = await browser.GetAsync("/bff/signin", Ct);

        // Assert
        response.RequestMessage!.RequestUri!.AbsolutePath.ShouldBe("/signin");
    }

    [Fact]
    public async Task WebClient_ShouldBeConfidentialAndLimitedToCodeAndRefreshGrants_WhenSeeded()
    {
        // Arrange
        await using var host = await SignInHost.StartAsync(postgres);
        await using var scope = host.Services.CreateAsyncScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // Act
        var client = await applications.FindByClientIdAsync("web", Ct);

        // Assert
        client.ShouldNotBeNull();
        (await applications.GetClientTypeAsync(client, Ct)).ShouldBe(ClientTypes.Confidential);
        (await applications.GetPermissionsAsync(client, Ct))
            .Where(permission => permission.StartsWith(Permissions.Prefixes.GrantType, StringComparison.Ordinal))
            .ShouldBe(
                [Permissions.GrantTypes.AuthorizationCode, Permissions.GrantTypes.RefreshToken],
                ignoreOrder: true);
    }

    [Fact]
    public async Task WebClient_ShouldFollowTheAuthority_WhenTheInstallationMoves()
    {
        // Arrange
        await using var first = await SignInHost.StartAsync(postgres);
        (await SignedInAdministratorAsync(first)).Dispose();
        await using var moved = await SignInHost.StartAsync(postgres, previous: first);
        using var browser = moved.NewBrowser();
        var page = await browser.GetAsync("/bff/signin", Ct);

        // Act
        var response = await browser.SubmitSignInPageAsync(page, string.Empty, Credentials(Administrator), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await browser.GetAsync("/bff/session", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static Dictionary<string, string> Setup(SignInHost host) =>
        new(Credentials(Administrator)) { ["SetupToken"] = host.SetupToken };

    private static Dictionary<string, string> Credentials(string username) =>
        new() { ["Username"] = username, ["Password"] = Password };

    private static async Task<Browser> SignedInAdministratorAsync(SignInHost host)
    {
        var browser = host.NewBrowser();
        var page = await browser.GetAsync("/bff/signin", Ct);
        var response = await browser.SubmitSignInPageAsync(page, "?handler=Setup", Setup(host), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        browser.SessionCookie.ShouldNotBeNull();

        return browser;
    }

    private static async Task<string> ProbeAsync(Browser browser)
    {
        var response = await browser.GetAsync(SignInHost.ProbePath, Ct);

        return await response.Content.ReadAsStringAsync(Ct);
    }

    private static async Task<List<string?>> RefreshTokenStatusesAsync(SignInHost host)
    {
        await using var scope = host.Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictTokenManager>();
        var statuses = new List<string?>();
        await foreach (var token in manager.ListAsync(count: null, offset: null, Ct))
        {
            if (await manager.GetTypeAsync(token, Ct) == TokenTypeIdentifiers.RefreshToken)
            {
                statuses.Add(await manager.GetStatusAsync(token, Ct));
            }
        }

        return statuses;
    }
}
