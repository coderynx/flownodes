using System.Net;
using Core.Contracts.Identity;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class UsersResyncTests
{
    private const string Api = "/api/v1/engine";
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Request_ShouldSucceed_WhenIdentityPublishesTheAccessOfAUserTheGrainHasNoRecordOf()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var userId = Guid.NewGuid();
        using var member = app.SignedIn(userId);
        (await member.GetAsync($"{Api}/installation", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        // Act
        await app.PublishAccessAsync(
            new UserAccessPublished(userId, "olive", false, false, false, [new("acme", "Operator")], 4));

        // Assert
        (await member.GetAsync($"{Api}/installation", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_ShouldAskIdentityForTheUserOnce_WhenTheGrainHasNoRecordOfThem()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var userId = Guid.NewGuid();
        using var stranger = app.SignedIn(userId);

        // Act
        for (var attempt = 0; attempt < 3; attempt++)
        {
            (await stranger.GetAsync($"{Api}/installation", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        // Assert
        app.Resync.Requests.ShouldBe([userId]);
    }

    [Fact]
    public async Task Request_ShouldNotAskIdentity_WhenTheGrainKnowsTheUserWithoutGrants()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (userId, member) = await app.MemberAsync("Operator", "acme");
        await app.RevokeAsync(userId, "Operator", "acme");

        // Act
        var response = await member.GetAsync($"{Api}/installation", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        app.Resync.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task UserAccessPublished_ShouldDropTheGrant_WhenTheGrainMissedItsRevocation()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (userId, member) = await app.MemberAsync("Operator", "acme");
        await app.GrantAsync(userId, "Operator", "globex");

        // Act
        await app.PublishAccessAsync(
            new UserAccessPublished(userId, "olive", false, false, false, [new("globex", "Operator")], 9));

        // Assert
        (await member.GetAsync($"{Api}/namespaces/acme/alarms", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await member.GetAsync($"{Api}/namespaces/globex/alarms", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UserAccessPublished_ShouldRejectTheUser_WhenTheGrainMissedTheirDeletion()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (userId, member) = await app.MemberAsync("Operator", "acme");

        // Act
        await app.PublishAccessAsync(new UserAccessPublished(userId, null, false, true, false, [], 9));

        // Assert
        (await member.GetAsync($"{Api}/installation", Ct)).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UserAccessPublished_ShouldChangeNothing_WhenItIsNotNewerThanTheLastApplied()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (userId, member) = await app.MemberAsync("Operator", "acme");

        // Act
        await app.PublishAccessAsync(new UserAccessPublished(userId, null, false, true, false, [], 1));

        // Assert
        (await member.GetAsync($"{Api}/installation", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
