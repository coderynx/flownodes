using Xunit;

namespace Flowctl.IntegrationTests;

public sealed class LoginTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Login_ShouldExchangeThePersonalAccessTokenAndKeepIt_WhenGivenOne()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        var engineer = await app.UserAsync("erin", ("Engineer", "default"));
        var personalAccessToken = app.Identity.CreatePersonalAccessToken(engineer);

        // Act
        var run = await app.RunAsync("login", "--server", FlowctlApp.Server, "--token", personalAccessToken);

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        app.ReadProfile().GetProperty("personalAccessToken").GetString().ShouldBe(personalAccessToken);
        await FlowctlApp.EventuallyAsync(async () => (await app.RunAsync("get", "widgets")).ExitCode == 0);
    }

    [Fact]
    public async Task Login_ShouldFail_WhenThePersonalAccessTokenIsUnknown()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);

        // Act
        var run = await app.RunAsync("login", "--server", FlowctlApp.Server, "--token", "fn_pat_unknown");

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.ShouldContain("invalid_grant");
        File.Exists(app.ProfilePath).ShouldBeFalse();
    }

    [Fact]
    public async Task Login_ShouldWaitUntilTheUserApprovesTheDevice_WhenSigningInThroughTheBrowser()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        var engineer = await app.UserAsync("erin", ("Engineer", "default"));
        app.Identity.SignInNextDevice(engineer, pendingPolls: 1);

        // Act
        var run = await app.RunAsync("login", "--server", FlowctlApp.Server);

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        run.Output.ShouldContain("http://localhost/connect/verify?user_code=ABCD-EFGH");
        run.Output.ShouldContain("Signed in to http://localhost/.");
        app.ReadProfile().GetProperty("refreshToken").GetString().ShouldNotBeNull();
        await FlowctlApp.EventuallyAsync(async () => (await app.RunAsync("get", "widgets")).ExitCode == 0);
    }

    [Fact]
    public async Task Login_ShouldFail_WhenTheUserDeniesTheDevice()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        app.Identity.SignInNextDevice(null, pendingPolls: 0);

        // Act
        var run = await app.RunAsync("login", "--server", FlowctlApp.Server);

        // Assert
        run.ExitCode.ShouldBe(1);
        run.Error.ShouldContain("access_denied");
        File.Exists(app.ProfilePath).ShouldBeFalse();
    }

    [Fact]
    public async Task Command_ShouldRefreshTheAccessToken_WhenItHasExpired()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        var engineer = await app.EngineerAsync();
        app.WriteProfile(
            new
            {
                server = FlowctlApp.Server,
                accessToken = "expired",
                expiresAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                refreshToken = app.Identity.CreateRefreshToken(engineer)
            });

        // Act
        var run = await app.RunAsync("get", "widgets");

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        app.ReadProfile().GetProperty("accessToken").GetString().ShouldNotBe("expired");
    }

    [Fact]
    public async Task Command_ShouldSignInWithTheTokenFromTheEnvironment_WhenFlowctlTokenIsSet()
    {
        // Arrange
        await using var app = await FlowctlApp.StartAsync(postgres);
        var engineer = await app.EngineerAsync();
        File.Delete(app.ProfilePath);
        app.Variables["FLOWCTL_SERVER"] = FlowctlApp.Server;
        app.Variables["FLOWCTL_TOKEN"] = app.Identity.CreatePersonalAccessToken(engineer);

        // Act
        var run = await app.RunAsync("get", "widgets");

        // Assert
        run.ExitCode.ShouldBe(0, run.Error);
        File.Exists(app.ProfilePath).ShouldBeFalse();
    }
}
