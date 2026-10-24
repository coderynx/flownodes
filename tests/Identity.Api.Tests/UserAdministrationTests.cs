using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Identity.Api.Tests;

public sealed class UserAdministrationTests(PostgresFixture postgres)
{
    private const string Users = "/api/v1/identity/users";
    private const string AlicePassword = "alice has a long password";

    [Fact]
    public async Task CreateUser_ShouldCreateTheUser_WhenAnAdministratorAsks()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);

        // Act
        var response = await administrator.PostAsJsonAsync(
            Users,
            new { username = "alice", displayName = "Alice A.", password = AlicePassword },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var list = await administrator.GetFromJsonAsync<UserList>(Users, Ct);
        list!.Total.ShouldBe(2);
        list.Items.Single(user => user.Username == "alice").DisplayName.ShouldBe("Alice A.");
        (await app.SignedInAsync("alice", AlicePassword, Ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateUser_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        var alice = await app.SignedInAsync("alice", AlicePassword, Ct);

        // Act
        var response = await alice.PostAsJsonAsync(
            Users,
            new { username = "mallory", password = "mallory has a long password" },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("12345678901")]
    public async Task CreateUser_ShouldReturnBadRequest_WhenThePasswordIsShorterThanTwelveCharacters(string password)
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);

        // Act
        var response = await administrator.PostAsJsonAsync(Users, new { username = "alice", password }, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateUser_ShouldAcceptAPasswordWithoutCompositionRules_WhenItHasTwelveCharacters()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);

        // Act
        var response = await administrator.PostAsJsonAsync(
            Users,
            new { username = "alice", password = "aaaaaaaaaaaa" },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateUser_ShouldReturnConflict_WhenTheUsernameDiffersOnlyInCase()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        var response = await administrator.PostAsJsonAsync(
            Users,
            new { username = "ALICE", password = AlicePassword },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateUser_ShouldRenameTheUserAndKeepTheirId_WhenAnAdministratorAsks()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var id = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        var response = await administrator.PatchAsJsonAsync($"{Users}/{id}", new { username = "alicia" }, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var renamed = await response.Content.ReadFromJsonAsync<UserBody>(Ct);
        renamed!.Id.ShouldBe(id);
        renamed.Username.ShouldBe("alicia");
        (await app.SignedInAsync("alicia", AlicePassword, Ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task UpdateUser_ShouldReturnConflict_WhenTheNewUsernameIsTaken()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var id = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        var response = await administrator.PatchAsJsonAsync(
            $"{Users}/{id}",
            new { username = IdentityApp.AdministratorName.ToUpperInvariant() },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DisableUser_ShouldStopTheUserSigningIn_WhenAnAdministratorDisablesThem()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var id = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        var response = await administrator.PostAsync($"{Users}/{id}/disable", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        using var browser = app.NewBrowser();
        var signIn = await browser.PostFormAsync(
            "/signin",
            "/signin",
            new Dictionary<string, string> { ["Username"] = "alice", ["Password"] = AlicePassword },
            Ct);
        signIn.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task EnableUser_ShouldLetTheUserSignInAgain_WhenAnAdministratorEnablesThem()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var id = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        await administrator.PostAsync($"{Users}/{id}/disable", null, Ct);

        // Act
        var response = await administrator.PostAsync($"{Users}/{id}/enable", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await app.SignedInAsync("alice", AlicePassword, Ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteUser_ShouldRemoveTheUser_WhenAnAdministratorDeletesThem()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var id = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        var response = await administrator.DeleteAsync($"{Users}/{id}", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await administrator.GetFromJsonAsync<UserList>(Users, Ct))!.Items.Select(user => user.Username)
            .ShouldBe([IdentityApp.AdministratorName]);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);

        // Act
        var response = await administrator.DeleteAsync($"{Users}/{Guid.NewGuid()}", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResetPassword_ShouldReplaceThePassword_WhenAnAdministratorResetsIt()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var id = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        var response = await administrator.PutAsJsonAsync(
            $"{Users}/{id}/password",
            new { password = "a brand new passphrase" },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await app.SignedInAsync("alice", "a brand new passphrase", Ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task ChangePassword_ShouldReplaceThePassword_WhenTheUserKnowsTheCurrentOne()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        var alice = await app.SignedInAsync("alice", AlicePassword, Ct);

        // Act
        var response = await alice.PutAsJsonAsync(
            "/api/v1/identity/me/password",
            new { currentPassword = AlicePassword, newPassword = "my own new passphrase" },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await app.SignedInAsync("alice", "my own new passphrase", Ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_WhenTheCurrentPasswordIsWrong()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        var alice = await app.SignedInAsync("alice", AlicePassword, Ct);

        // Act
        var response = await alice.PutAsJsonAsync(
            "/api/v1/identity/me/password",
            new { currentPassword = "not the current one", newPassword = "my own new passphrase" },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListUsers_ShouldPageFilterAndSort_WhenQueryParametersAreGiven()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        await IdentityApp.CreateUserAsync(administrator, "plant-a", AlicePassword, Ct);
        await IdentityApp.CreateUserAsync(administrator, "plant-b", AlicePassword, Ct);
        await IdentityApp.CreateUserAsync(administrator, "plant-c", AlicePassword, Ct);

        // Act
        var list = await administrator.GetFromJsonAsync<UserList>(
            $"{Users}?q=plant&sort=username&desc=true&offset=1&limit=1",
            Ct);

        // Assert
        list!.Total.ShouldBe(3);
        list.Items.Select(user => user.Username).ShouldBe(["plant-b"]);
        (await administrator.GetAsync($"{Users}?sort=colour", Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record UserBody(Guid Id, string Username, string? DisplayName);

    private sealed record UserList(IReadOnlyList<UserBody> Items, int Total);
}
