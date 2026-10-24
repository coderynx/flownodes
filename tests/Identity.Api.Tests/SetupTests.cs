using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Identity.Api.Tests;

public sealed class SetupTests(PostgresFixture postgres)
{
    [Fact]
    public async Task Startup_ShouldLogASetupToken_WhenInstallationHasNoUser()
    {
        // Arrange
        // Act
        await using var app = await IdentityApp.StartAsync(postgres);

        // Assert
        app.SetupToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Startup_ShouldFail_WhenInstallationKeyIsMissing()
    {
        // Arrange
        var act = () => IdentityApp.StartAsync(postgres, installationKey: string.Empty);

        // Act
        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Startup_ShouldFail_WhenInstallationKeyIsNot256Bits()
    {
        // Arrange
        var act = () => IdentityApp.StartAsync(postgres, installationKey: Convert.ToBase64String(new byte[16]));

        // Act
        // Assert
        await act.ShouldThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SignInPage_ShouldOfferSetup_WhenInstallationHasNoUser()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        using var browser = app.NewBrowser();

        // Act
        var response = await browser.GetAsync("/signin", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("SetupToken");
    }

    [Fact]
    public async Task Setup_ShouldMakeTheFirstUserAnAdministratorWithBothRolesInDefault_WhenSetupTokenIsUsed()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        using var browser = app.NewBrowser();

        // Act
        var setup = await IdentityApp.CompleteSetupAsync(
            browser,
            app.SetupToken!,
            IdentityApp.AdministratorName,
            IdentityApp.AdministratorPassword,
            Ct);

        // Assert
        setup.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var administrator = await app.SignedInAsync(
            IdentityApp.AdministratorName,
            IdentityApp.AdministratorPassword,
            Ct);
        var me = await administrator.GetFromJsonAsync<MeBody>("/api/v1/identity/me", Ct);
        me!.Username.ShouldBe(IdentityApp.AdministratorName);
        me.Administrator.ShouldBeTrue();
        me.RoleGrants.ShouldBe(
            [new RoleGrantBody("default", "Engineer"), new RoleGrantBody("default", "Operator")],
            ignoreOrder: true);
    }

    [Fact]
    public async Task Setup_ShouldBeRefused_WhenSetupTokenIsWrong()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        using var browser = app.NewBrowser();

        // Act
        var setup = await IdentityApp.CompleteSetupAsync(
            browser,
            "not-the-token",
            IdentityApp.AdministratorName,
            IdentityApp.AdministratorPassword,
            Ct);

        // Assert
        setup.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await setup.Content.ReadAsStringAsync(Ct)).ShouldContain("setup token");
        (await browser.GetAsync("/signin", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await (await browser.GetAsync("/signin", Ct)).Content.ReadAsStringAsync(Ct)).ShouldContain("SetupToken");
    }

    [Fact]
    public async Task Setup_ShouldBeGone_WhenAUserExists()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        using var browser = app.NewBrowser();
        await IdentityApp.CompleteSetupAsync(
            browser,
            app.SetupToken!,
            IdentityApp.AdministratorName,
            IdentityApp.AdministratorPassword,
            Ct);

        // Act
        var again = await IdentityApp.CompleteSetupAsync(
            browser,
            app.SetupToken!,
            "intruder",
            IdentityApp.AdministratorPassword,
            Ct);

        // Assert
        again.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await (await browser.GetAsync("/signin", Ct)).Content.ReadAsStringAsync(Ct)).ShouldNotContain("SetupToken");
    }

    [Fact]
    public async Task Setup_ShouldBeGone_WhenAnotherSiloAlreadyCreatedTheFirstUser()
    {
        // Arrange
        var installationKey = Convert.ToBase64String(new byte[32].Select((_, index) => (byte)(index + 3)).ToArray());
        await using var first = await IdentityApp.StartAsync(postgres, installationKey);
        await using var second = await IdentityApp.StartAsync(postgres, installationKey, first.ConnectionString);
        using var firstBrowser = first.NewBrowser();
        using var secondBrowser = second.NewBrowser();
        await IdentityApp.CompleteSetupAsync(
            firstBrowser,
            first.SetupToken!,
            IdentityApp.AdministratorName,
            IdentityApp.AdministratorPassword,
            Ct);

        // Act
        var again = await IdentityApp.CompleteSetupAsync(
            secondBrowser,
            second.SetupToken!,
            "intruder",
            IdentityApp.AdministratorPassword,
            Ct);

        // Assert
        again.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Startup_ShouldNotLogASetupToken_WhenAUserExists()
    {
        // Arrange
        var installationKey = Convert.ToBase64String(new byte[32].Select((_, index) => (byte)index).ToArray());
        string connectionLeftOver;
        await using (var first = await IdentityApp.StartAsync(postgres, installationKey))
        {
            using var browser = first.NewBrowser();
            await IdentityApp.CompleteSetupAsync(
                browser,
                first.SetupToken!,
                IdentityApp.AdministratorName,
                IdentityApp.AdministratorPassword,
                Ct);
            connectionLeftOver = first.ConnectionString;
        }

        // Act
        await using var second = await IdentityApp.StartAsync(postgres, installationKey, connectionLeftOver);

        // Assert
        second.SetupToken.ShouldBeNull();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record RoleGrantBody(string Namespace, string Role);

    private sealed record MeBody(string Username, bool Administrator, IReadOnlyList<RoleGrantBody> RoleGrants);
}
