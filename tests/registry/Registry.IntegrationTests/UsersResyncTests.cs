using System.Net;
using Core.Contracts.Identity;
using Xunit;

namespace Registry.IntegrationTests;

public sealed class UsersResyncTests(PostgresFixture postgres)
{
    private const string Route = "/api/v1/registry/namespaces";

    [Fact]
    public async Task Start_ShouldAskIdentityForEveryUser_WhenTheRegistryStarts()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);

        // Act
        await app.IdentityRequests.WaitForAsync(null, Ct);

        // Assert
        app.IdentityRequests.Received.Count(request => request.UserId is null).ShouldBe(1);
    }

    [Fact]
    public async Task Request_ShouldSucceed_WhenIdentityPublishesTheAccessOfAUserTheReplicaHasNoRecordOf()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var userId = app.UnreportedUser("olive");
        var olive = app.SignedIn("olive");
        (await olive.GetAsync($"{Route}/default", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        await app.IdentityRequests.WaitForAsync(userId, Ct);

        // Act
        await app.PublishAsync(
            new UserAccessPublished(userId, "olive", false, false, false, [new("default", "Operator")], 1));

        // Assert
        var status = await RegistryApp.StatusEventuallyAsync(
            () => olive.GetAsync($"{Route}/default", Ct),
            HttpStatusCode.OK,
            Ct);
        status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_ShouldAskIdentityOnlyOnce_WhenAnUnknownUserCallsRepeatedly()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var userId = app.UnreportedUser("olive");
        var olive = app.SignedIn("olive");

        // Act
        for (var attempt = 0; attempt < 3; attempt++)
        {
            (await olive.GetAsync($"{Route}/default", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Assert
        await app.IdentityRequests.WaitForAsync(userId, Ct);
        await Task.Delay(TimeSpan.FromSeconds(1), Ct);
        app.IdentityRequests.Received.Count(request => request.UserId == userId).ShouldBe(1);
    }

    [Fact]
    public async Task UserAccessPublished_ShouldDropTheGrant_WhenTheReplicaMissedItsRevocation()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        await app.CreateNamespaceAsync("acme", Ct);
        var (userId, member) = await app.MemberAsync("olive", "Operator", "default");
        await app.GrantAsync(userId, "Operator", "acme");
        await RegistryApp.EventuallyAsync(
            async () => (await member.GetAsync($"{Route}/acme", Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        await app.PublishAsync(
            new UserAccessPublished(userId, "olive", false, false, false, [new("default", "Operator")], 9));

        // Assert
        var status = await RegistryApp.StatusEventuallyAsync(
            () => member.GetAsync($"{Route}/acme", Ct),
            HttpStatusCode.NotFound,
            Ct);
        status.ShouldBe(HttpStatusCode.NotFound);
        (await member.GetAsync($"{Route}/default", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserAccessPublished_ShouldRejectTheUser_WhenTheReplicaMissedTheirDeletion()
    {
        // Arrange
        await using var app = await RegistryApp.StartAsync(postgres);
        var (userId, member) = await app.MemberAsync("olive", "Operator", "default");
        await RegistryApp.EventuallyAsync(
            async () => (await member.GetAsync($"{Route}/default", Ct)).IsSuccessStatusCode,
            Ct);

        // Act
        await app.PublishAsync(new UserAccessPublished(userId, null, false, true, false, [], 9));

        // Assert
        var status = await RegistryApp.StatusEventuallyAsync(
            () => member.GetAsync($"{Route}/default", Ct),
            HttpStatusCode.Unauthorized,
            Ct);
        status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;
}
