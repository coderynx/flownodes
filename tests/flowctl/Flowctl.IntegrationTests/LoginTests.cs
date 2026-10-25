using System.Net;
using Xunit;

namespace Flowctl.IntegrationTests;

public sealed class LoginTests
{
    private const string Tokens = """{"access_token": "fresh", "refresh_token": "refresh-2", "expires_in": 600}""";

    [Fact]
    public async Task Login_ShouldExchangeThePersonalAccessTokenAndRememberIt_WhenGivenOne()
    {
        // Arrange
        using var flowctl = new Flowctl();
        flowctl.Stub.On(
            HttpMethod.Post,
            "/connect/token",
            HttpStatusCode.OK,
            """{"access_token": "from-pat", "expires_in": 600}""");

        // Act
        var run = await flowctl.RunAsync("login", "--server", "https://flownodes.test", "--token", "fn_pat_1");

        // Assert
        run.ExitCode.ShouldBe(0);
        var request = flowctl.Stub.Requests.ShouldHaveSingleItem();
        request.Body.ShouldContain("grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Atoken-exchange");
        request.Body.ShouldContain("subject_token=fn_pat_1");
        var profile = flowctl.ReadProfile();
        profile.GetProperty("server").GetString().ShouldBe(Flowctl.Server);
        profile.GetProperty("accessToken").GetString().ShouldBe("from-pat");
        profile.GetProperty("personalAccessToken").GetString().ShouldBe("fn_pat_1");
    }

    [Fact]
    public async Task Login_ShouldPollUntilTheUserApproves_WhenSigningInThroughTheBrowser()
    {
        // Arrange
        using var flowctl = new Flowctl();
        var polls = 0;
        flowctl.Stub.On(
                HttpMethod.Post,
                "/connect/device",
                HttpStatusCode.OK,
                """
                {"device_code": "dev-1", "user_code": "ABCD-EFGH", "verification_uri": "https://flownodes.test/connect/verify",
                 "verification_uri_complete": "https://flownodes.test/connect/verify?user_code=ABCD-EFGH",
                 "interval": 1}
                """)
            .On(
                HttpMethod.Post,
                "/connect/token",
                _ => ++polls < 3
                    ? StubServer.Response(HttpStatusCode.BadRequest, """{"error": "authorization_pending"}""")
                    : StubServer.Response(HttpStatusCode.OK, Tokens));

        // Act
        var run = await flowctl.RunAsync("login", "--server", "https://flownodes.test");

        // Assert
        run.ExitCode.ShouldBe(0);
        run.Output.ShouldContain("https://flownodes.test/connect/verify?user_code=ABCD-EFGH");
        run.Output.ShouldContain("ABCD-EFGH");
        polls.ShouldBe(3);
        flowctl.Stub.Requests[0].Body.ShouldContain("scope=offline_access");
        flowctl.ReadProfile().GetProperty("refreshToken").GetString().ShouldBe("refresh-2");
    }

    [Fact]
    public async Task Login_ShouldFail_WhenTheUserDeniesTheDevice()
    {
        // Arrange
        using var flowctl = new Flowctl();
        flowctl.Stub.On(
                HttpMethod.Post,
                "/connect/device",
                HttpStatusCode.OK,
                """{"device_code": "dev-1", "user_code": "X", "verification_uri": "https://flownodes.test/v"}""")
            .On(HttpMethod.Post, "/connect/token", HttpStatusCode.BadRequest, """{"error": "access_denied"}""");

        // Act
        var run = await flowctl.RunAsync("login", "--server", "https://flownodes.test");

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.ShouldContain("access_denied");
        File.Exists(flowctl.ProfilePath).ShouldBeFalse();
    }

    [Fact]
    public async Task Command_ShouldRefreshTheAccessToken_WhenItHasExpired()
    {
        // Arrange
        using var flowctl = new Flowctl();
        flowctl.WriteProfile(
            new
            {
                server = Flowctl.Server,
                accessToken = "stale",
                expiresAt = "2026-10-25T11:00:00Z",
                refreshToken = "refresh-1"
            });
        flowctl.Stub.On(HttpMethod.Post, "/connect/token", HttpStatusCode.OK, Tokens)
            .On(HttpMethod.Get, "/api/v1/registry/namespaces", HttpStatusCode.OK, """{"items": [], "total": 0}""");

        // Act
        var run = await flowctl.RunAsync("get", "namespaces");

        // Assert
        run.ExitCode.ShouldBe(0);
        flowctl.Stub.Requests[0].Body.ShouldContain("refresh_token=refresh-1");
        flowctl.Stub.Requests[1].Authorization.ShouldBe("Bearer fresh");
        flowctl.ReadProfile().GetProperty("accessToken").GetString().ShouldBe("fresh");
    }

    [Fact]
    public async Task Command_ShouldExchangeTheTokenFromTheEnvironment_WhenFlowctlTokenIsSet()
    {
        // Arrange
        using var flowctl = new Flowctl();
        flowctl.Variables["FLOWCTL_SERVER"] = "https://flownodes.test";
        flowctl.Variables["FLOWCTL_TOKEN"] = "fn_pat_ci";
        flowctl.Stub
            .On(HttpMethod.Post, "/connect/token", HttpStatusCode.OK, """{"access_token": "ci", "expires_in": 600}""")
            .On(HttpMethod.Get, "/api/v1/registry/namespaces", HttpStatusCode.OK, """{"items": [], "total": 0}""");

        // Act
        var run = await flowctl.RunAsync("get", "namespaces");

        // Assert
        run.ExitCode.ShouldBe(0);
        flowctl.Stub.Requests[0].Body.ShouldContain("subject_token=fn_pat_ci");
        flowctl.Stub.Requests[1].Authorization.ShouldBe("Bearer ci");
        File.Exists(flowctl.ProfilePath).ShouldBeFalse();
    }
}
