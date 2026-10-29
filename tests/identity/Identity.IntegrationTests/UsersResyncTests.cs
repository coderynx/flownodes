using System.Net.Http.Json;
using Core.Contracts.Identity;
using Xunit;

namespace Identity.IntegrationTests;

public sealed class UsersResyncTests(PostgresFixture postgres)
{
    private const string Grants = "/api/v1/identity/role-grants";
    private const string Password = "a long enough password";

    [Fact]
    public async Task UsersResyncRequested_ShouldPublishEveryUsersGrantsAndEveryDeletedUser_WhenNoUserIdIsGiven()
    {
        // Arrange
        await using var app = await IdentityApp.StartAsync(postgres);
        var administrator = await app.AdministratorAsync(Ct);
        var alice = await IdentityApp.CreateUserAsync(administrator, "alice", Password, Ct);
        var bob = await IdentityApp.CreateUserAsync(administrator, "bob", Password, Ct);
        await administrator.PostAsJsonAsync(
            Grants,
            new { userId = alice, role = "Operator", @namespace = "default" },
            Ct);
        await administrator.PostAsJsonAsync(
            Grants,
            new { userId = bob, role = "Engineer", @namespace = "default" },
            Ct);
        (await administrator.DeleteAsync($"/api/v1/identity/users/{bob}", Ct)).EnsureSuccessStatusCode();
        var deletion = await app.Messages.WaitForAsync<UserDeleted>(deleted => deleted.UserId == bob, Ct);

        // Act
        await app.PublishAsync(new UsersResyncRequested());

        // Assert
        var aliceAccess =
            await app.Messages.WaitForAsync<UserAccessPublished>(published => published.UserId == alice, Ct);
        aliceAccess.Username.ShouldBe("alice");
        aliceAccess.IsDeleted.ShouldBeFalse();
        aliceAccess.Grants.ShouldBe([new PublishedRoleGrant("default", "Operator")]);
        aliceAccess.Seq.ShouldBe(1);
        var bobAccess = await app.Messages.WaitForAsync<UserAccessPublished>(published => published.UserId == bob, Ct);
        bobAccess.IsDeleted.ShouldBeTrue();
        bobAccess.Grants.ShouldBeEmpty();
        bobAccess.Seq.ShouldBe(deletion.Seq);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
