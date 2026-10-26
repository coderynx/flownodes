using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class RoleAccessTests
{
    private const string Api = "/api/v1/engine";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("GET", $"{Api}/integrations")]
    [InlineData("GET", $"{Api}/namespaces/acme/devices/t1/datapoints")]
    [InlineData("PUT", $"{Api}/namespaces/acme/devices/t1/datapoints/target")]
    [InlineData("POST", $"{Api}/namespaces/acme/devices/t1/actions/set")]
    [InlineData("GET", $"{Api}/namespaces/acme/assets/room/datapoints")]
    [InlineData("PUT", $"{Api}/namespaces/acme/assets/room/datapoints/target")]
    [InlineData("GET", $"{Api}/namespaces/acme/alarms")]
    [InlineData("GET", $"{Api}/namespaces/acme/alarms/history")]
    [InlineData("GET", $"{Api}/namespaces/acme/assets/room/alarms")]
    [InlineData("POST", $"{Api}/namespaces/acme/alarms/6b3f0e2a-0f0e-4f0e-8f0e-0f0e0f0e0f0e/acknowledge")]
    public async Task Request_ShouldReturnUnauthorized_WhenThereIsNoAccessToken(string method, string route)
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();

        // Act
        var response = await app.Anonymous.SendAsync(new HttpRequestMessage(new HttpMethod(method), route), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("GET", "devices/t1/datapoints")]
    [InlineData("PUT", "devices/t1/datapoints/target")]
    [InlineData("POST", "devices/t1/actions/set")]
    [InlineData("GET", "assets/room/datapoints")]
    [InlineData("PUT", "assets/room/datapoints/target")]
    [InlineData("GET", "alarms")]
    [InlineData("GET", "alarms/history")]
    [InlineData("GET", "assets/room/alarms")]
    [InlineData("POST", "alarms/6b3f0e2a-0f0e-4f0e-8f0e-0f0e0f0e0f0e/acknowledge")]
    public async Task Request_ShouldReturnNotFound_WhenTheCallerHoldsNoRoleInTheNamespace(string method, string route)
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (_, outsider) = await app.MemberAsync("Operator", "other");

        // Act
        var response = await outsider.SendAsync(Request(method, $"{Api}/namespaces/acme/{route}"), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await TitleAsync(response)).ShouldBe("namespace.not_found");
    }

    [Theory]
    [InlineData("PUT", "devices/t1/datapoints/target")]
    [InlineData("POST", "devices/t1/actions/set")]
    [InlineData("PUT", "assets/room/datapoints/target")]
    public async Task Request_ShouldReturnForbidden_WhenTheCallerIsNotAnOperatorInTheNamespace(
        string method,
        string route)
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (_, engineer) = await app.MemberAsync("Engineer", "acme");

        // Act
        var response = await engineer.SendAsync(Request(method, $"{Api}/namespaces/acme/{route}"), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Operator", "GET", "devices/t1/datapoints")]
    [InlineData("Engineer", "GET", "devices/t1/datapoints")]
    [InlineData("Operator", "GET", "assets/room/datapoints")]
    [InlineData("Engineer", "GET", "assets/room/datapoints")]
    [InlineData("Operator", "PUT", "devices/t1/datapoints/target")]
    [InlineData("Operator", "POST", "devices/t1/actions/set")]
    [InlineData("Operator", "PUT", "assets/room/datapoints/target")]
    public async Task Request_ShouldReachTheRuntime_WhenTheCallerHoldsTheRoleTheRequestNeeds(
        string role,
        string method,
        string route)
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (_, member) = await app.MemberAsync(role, "acme");

        // Act
        var response = await member.SendAsync(Request(method, $"{Api}/namespaces/acme/{route}"), Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await TitleAsync(response)).ShouldNotBe("namespace.not_found");
    }

    [Theory]
    [InlineData("Operator")]
    [InlineData("Engineer")]
    public async Task ListIntegrations_ShouldSucceed_WhenTheCallerHoldsAnyRole(string role)
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (_, member) = await app.MemberAsync(role, "acme");

        // Act
        var response = await member.GetAsync($"{Api}/integrations", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ListIntegrations_ShouldReturnForbidden_WhenTheCallerHoldsNoRole()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (userId, client) = await app.MemberAsync("Operator", "acme");
        await app.RevokeAsync(userId, "Operator", "acme");

        // Act
        var response = await client.GetAsync($"{Api}/integrations", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Request_ShouldReturnNotFound_WhenTheRoleWasRevoked()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (userId, member) = await app.MemberAsync("Operator", "acme");
        await app.RevokeAsync(userId, "Operator", "acme");

        // Act
        var response = await member.GetAsync($"{Api}/namespaces/acme/devices/t1/datapoints", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await TitleAsync(response)).ShouldBe("namespace.not_found");
    }

    [Fact]
    public async Task Request_ShouldReturnUnauthorized_WhenTheUserIsDisabled()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (userId, member) = await app.MemberAsync("Operator", "acme");
        await app.DisableAsync(userId);

        // Act
        var response = await member.GetAsync($"{Api}/namespaces/acme/devices/t1/datapoints", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_ShouldReturnUnauthorized_WhenTheUserIsDeleted()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (userId, member) = await app.MemberAsync("Operator", "acme");
        await app.DeleteAsync(userId);

        // Act
        var response = await member.GetAsync($"{Api}/namespaces/acme/devices/t1/datapoints", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Request_ShouldReturnUnauthorized_WhenIdentityNeverReportedTheUser()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        using var stranger = app.SignedIn(Guid.NewGuid());

        // Act
        var response = await stranger.GetAsync($"{Api}/namespaces/acme/devices/t1/datapoints", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static HttpRequestMessage Request(string method, string route)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), route);
        if (method != "GET")
        {
            request.Content = new StringContent(
                """{"value": 1, "parameters": {}}""",
                Encoding.UTF8,
                "application/json");
        }

        return request;
    }

    private static async Task<string?> TitleAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));

        return body.RootElement.GetProperty("title").GetString();
    }
}
