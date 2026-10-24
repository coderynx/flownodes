using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Contracts.Identity;
using Core.Contracts.Resources;
using Xunit;

namespace Identity.IntegrationTests;

public sealed class RoleGrantTests(PostgresFixture postgres)
{
    private const string Grants = "/api/v1/identity/role-grants";
    private const string Users = "/api/v1/identity/users";
    private const string AlicePassword = "alice has a long password";

    [Fact]
    public async Task Setup_ShouldPublishTheFirstUsersGrantsWithAscendingSeq_WhenTheInstallationIsSetUp()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);

        // Act
        var administrator = await app.AdministratorAsync(Ct);

        // Assert
        var me = await administrator.GetFromJsonAsync<JsonElement>("/api/v1/identity/me", Ct);
        var id = me.GetProperty("id").GetGuid();
        var granted = await app.Messages.WaitForAsync<AdministratorGranted>(message => message.UserId == id, Ct);
        var engineer = await app.Messages.WaitForAsync<RoleGranted>(
            message => message.UserId == id && message.Role == "Engineer",
            Ct);
        var operatorRole = await app.Messages.WaitForAsync<RoleGranted>(
            message => message.UserId == id && message.Role == "Operator",
            Ct);
        new[] { granted.Seq, engineer.Seq, operatorRole.Seq }.Order().ShouldBe([1, 2, 3]);
        engineer.Namespace.ShouldBe("default");
    }

    [Fact]
    public async Task GrantRole_ShouldGrantTheRoleAndPublishRoleGranted_WhenAnAdministratorGrantsEngineerInDefault()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        var response = await administrator.PostAsJsonAsync(
            Grants,
            new { userId = alice, role = "Engineer", @namespace = "default" },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var message = await app.Messages.WaitForAsync<RoleGranted>(granted => granted.UserId == alice, Ct);
        message.ShouldBe(new RoleGranted(alice, "default", "Engineer", 1));
        var user = (await administrator.GetFromJsonAsync<UserList>(Users, Ct))!.Items.Single(u => u.Id == alice);
        user.RoleGrants.ShouldBe([new RoleGrantBody("default", "Engineer")]);
    }

    [Fact]
    public async Task GrantRole_ShouldReturnOkAndPublishNothing_WhenTheUserAlreadyHoldsTheRole()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        await administrator.PostAsJsonAsync(
            Grants,
            new { userId = alice, role = "Operator", @namespace = "default" },
            Ct);

        // Act
        var response = await administrator.PostAsJsonAsync(
            Grants,
            new { userId = alice, role = "Operator", @namespace = "default" },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        await administrator.PostAsJsonAsync(
            Grants,
            new { userId = alice, role = "Engineer", @namespace = "default" },
            Ct);
        await app.Messages.WaitForAsync<RoleGranted>(m => m.UserId == alice && m.Role == "Engineer", Ct);
        app.Messages.Received.OfType<RoleGranted>().Count(m => m.UserId == alice && m.Role == "Operator").ShouldBe(1);
    }

    [Fact]
    public async Task GrantRole_ShouldReturnBadRequest_WhenTheNamespaceIsUnknown()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        var response = await administrator.PostAsJsonAsync(
            Grants,
            new { userId = alice, role = "Engineer", @namespace = "acme" },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GrantRole_ShouldGrantTheRole_WhenTheRegistryAnnouncedTheNamespace()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        await app.PublishAsync(Applied("acme", Guid.NewGuid(), 1));

        // Act
        var response = await GrantWhenKnownAsync(administrator, alice, "acme");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GrantRole_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        var client = await app.SignedInAsync("alice", AlicePassword, Ct);

        // Act
        var response = await client.PostAsJsonAsync(
            Grants,
            new { userId = alice, role = "Engineer", @namespace = "default" },
            Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GrantRole_ShouldReturnBadRequest_WhenRoleAndNamespaceDoNotFit()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        var response = await administrator.PostAsJsonAsync(Grants, new { userId = alice, role = "Engineer" }, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RevokeRole_ShouldRevokeTheRoleAndPublishRoleRevoked_WhenAnAdministratorRevokesIt()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        await administrator.PostAsJsonAsync(
            Grants,
            new { userId = alice, role = "Operator", @namespace = "default" },
            Ct);

        // Act
        var response = await administrator.DeleteAsync($"{Grants}?userId={alice}&role=Operator&namespace=default", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var message = await app.Messages.WaitForAsync<RoleRevoked>(revoked => revoked.UserId == alice, Ct);
        message.ShouldBe(new RoleRevoked(alice, "default", "Operator", 2));
    }

    [Fact]
    public async Task GrantRole_ShouldMakeAnAdministratorAndPublishAdministratorGranted_WhenNoNamespaceIsGiven()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        var response = await administrator.PostAsJsonAsync(Grants, new { userId = alice, role = "Administrator" }, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        await app.Messages.WaitForAsync<AdministratorGranted>(granted => granted.UserId == alice, Ct);
        var created = await app.SignedInAsync("alice", AlicePassword, Ct);
        (await created.GetAsync(Users, Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokeRole_ShouldPublishAdministratorRevoked_WhenAnotherAdministratorRemains()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        await administrator.PostAsJsonAsync(Grants, new { userId = alice, role = "Administrator" }, Ct);

        // Act
        var response = await administrator.DeleteAsync($"{Grants}?userId={alice}&role=Administrator", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await app.Messages.WaitForAsync<AdministratorRevoked>(revoked => revoked.UserId == alice, Ct);
    }

    [Fact]
    public async Task RevokeRole_ShouldReturnConflict_WhenTheUserIsTheLastActiveAdministrator()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var root = await MyIdAsync(administrator);

        // Act
        var response = await administrator.DeleteAsync($"{Grants}?userId={root}&role=Administrator", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DisableUser_ShouldReturnConflict_WhenTheUserIsTheLastActiveAdministrator()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var root = await MyIdAsync(administrator);

        // Act
        var response = await administrator.PostAsync($"{Users}/{root}/disable", null, Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnConflict_WhenTheUserIsTheLastActiveAdministrator()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var root = await MyIdAsync(administrator);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        await administrator.PostAsJsonAsync(Grants, new { userId = alice, role = "Administrator" }, Ct);
        await administrator.PostAsync($"{Users}/{alice}/disable", null, Ct);

        // Act
        var response = await administrator.DeleteAsync($"{Users}/{root}", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ResourceDeleted_ShouldDropTheNamespacesGrantsAndPublishRoleRevoked_WhenTheRegistryDeletesIt()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        var uid = Guid.NewGuid();
        await app.PublishAsync(Applied("acme", uid, 1));
        (await GrantWhenKnownAsync(administrator, alice, "acme")).StatusCode.ShouldBe(HttpStatusCode.Created);

        // Act
        await app.PublishAsync(new ResourceDeleted("acme", "Namespace", "acme", uid, 1, 2));

        // Assert
        var revoked = await app.Messages.WaitForAsync<RoleRevoked>(message => message.Namespace == "acme", Ct);
        revoked.UserId.ShouldBe(alice);
        revoked.Role.ShouldBe("Engineer");
        var user = (await administrator.GetFromJsonAsync<UserList>(Users, Ct))!.Items.Single(u => u.Id == alice);
        user.RoleGrants.ShouldBeEmpty();
        (await administrator.PostAsJsonAsync(
            Grants,
            new { userId = alice, role = "Engineer", @namespace = "acme" },
            Ct)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DisableUser_ShouldPublishUserDisabledAndRevokeTokens_WhenAnAdministratorDisablesTheUser()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        var tokens = await app.SignInWithDeviceFlowAsync("alice", AlicePassword, Ct);
        var aliceClient = app.ClientFor(tokens.AccessToken);
        var created =
            await (await aliceClient.PostAsJsonAsync("/api/v1/identity/users/me/tokens", new { name = "ci" }, Ct))
                .Content.ReadFromJsonAsync<JsonElement>(Ct);

        // Act
        await administrator.PostAsync($"{Users}/{alice}/disable", null, Ct);

        // Assert
        await app.Messages.WaitForAsync<UserDisabled>(message => message.UserId == alice, Ct);
        var refresh = await app.PostTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = IdentityApp.ClientId,
                ["refresh_token"] = tokens.RefreshToken!
            },
            Ct);
        refresh.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var exchange = await app.PostTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = IdentityApp.TokenExchangeGrant,
                ["client_id"] = IdentityApp.ClientId,
                ["subject_token"] = created.GetProperty("secret").GetString()!,
                ["subject_token_type"] = IdentityApp.PersonalAccessTokenType
            },
            Ct);
        exchange.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await aliceClient.GetAsync("/api/v1/identity/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task EnableUser_ShouldPublishUserEnabled_WhenAnAdministratorEnablesTheUser()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        await administrator.PostAsync($"{Users}/{alice}/disable", null, Ct);

        // Act
        await administrator.PostAsync($"{Users}/{alice}/enable", null, Ct);

        // Assert
        var enabled = await app.Messages.WaitForAsync<UserEnabled>(message => message.UserId == alice, Ct);
        enabled.Seq.ShouldBe(2);
    }

    [Fact]
    public async Task DeleteUser_ShouldPublishUserDeletedAndRevokeTokens_WhenAnAdministratorDeletesTheUser()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);
        var tokens = await app.SignInWithDeviceFlowAsync("alice", AlicePassword, Ct);

        // Act
        await administrator.DeleteAsync($"{Users}/{alice}", Ct);

        // Assert
        await app.Messages.WaitForAsync<UserDeleted>(message => message.UserId == alice, Ct);
        var refresh = await app.PostTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = IdentityApp.ClientId,
                ["refresh_token"] = tokens.RefreshToken!
            },
            Ct);
        refresh.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_ShouldPublishUserRenamed_WhenTheUsernameChanges()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", AlicePassword, Ct);

        // Act
        await administrator.PatchAsJsonAsync($"{Users}/{alice}", new { username = "alicia" }, Ct);

        // Assert
        var renamed = await app.Messages.WaitForAsync<UserRenamed>(message => message.UserId == alice, Ct);
        renamed.ShouldBe(new UserRenamed(alice, "alicia", 1));
    }

    private static ResourceApplied Applied(string name, Guid uid, long seq) =>
        new(name, "Namespace", name, uid, 1, seq, JsonSerializer.SerializeToElement(new { }));

    private static async Task<HttpResponseMessage> GrantWhenKnownAsync(HttpClient administrator, Guid user, string name)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        while (true)
        {
            var response = await administrator.PostAsJsonAsync(
                Grants,
                new { userId = user, role = "Engineer", @namespace = name },
                timeout.Token);
            if (response.StatusCode != HttpStatusCode.BadRequest)
            {
                return response;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }
    }

    private static async Task<Guid> MyIdAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<JsonElement>("/api/v1/identity/me", Ct)).GetProperty("id").GetGuid();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private sealed record RoleGrantBody(string Namespace, string Role);

    private sealed record UserBody(Guid Id, string Username, IReadOnlyList<RoleGrantBody> RoleGrants);

    private sealed record UserList(IReadOnlyList<UserBody> Items);
}
