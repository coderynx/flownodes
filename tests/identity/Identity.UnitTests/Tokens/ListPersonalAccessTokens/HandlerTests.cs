using Core.Application.Authorization;
using Identity.Application.Tokens.ListPersonalAccessTokens;
using Identity.Domain.Tokens;
using Xunit;

namespace Identity.UnitTests.Tokens.ListPersonalAccessTokens;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldListTheCallersUnrevokedTokensOldestFirst_WhenTheCallerListsTheirOwn()
    {
        // Arrange
        var caller = TestDoubles.Member("alice");
        var other = TestDoubles.Disabled(TestDoubles.Member("bob"));
        var newer = PersonalAccessToken.Create(caller.Id, "newer", "hash-1", TokenDoubles.Now, null);
        var older = PersonalAccessToken.Create(caller.Id, "older", "hash-2", TokenDoubles.Now.AddDays(-2), null);
        var expired = PersonalAccessToken.Create(
            caller.Id,
            "expired",
            "hash-3",
            TokenDoubles.Now.AddDays(-3),
            TokenDoubles.Now.AddDays(-1));
        var revoked = PersonalAccessToken.Create(caller.Id, "revoked", "hash-4", TokenDoubles.Now, null);
        revoked.Revoke(TokenDoubles.Now);
        var othersToken = PersonalAccessToken.Create(other.Id, "theirs", "hash-5", TokenDoubles.Now, null);
        var database = TestDoubles.Database([caller, other], [newer, revoked, othersToken, older, expired]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Query(caller.Id, caller.Id), TestContext.Current.CancellationToken);

        // Assert
        result.Value.Select(token => token.Name).ShouldBe(["expired", "older", "newer"]);
    }

    [Fact]
    public async Task HandleAsync_ShouldListNothing_WhenTheCallerHasNoTokens()
    {
        // Arrange
        var caller = TestDoubles.Member("alice");
        var handler = new Handler(TestDoubles.Database([caller], []));

        // Act
        var result = await handler.HandleAsync(new Query(caller.Id, caller.Id), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ShouldForbid_WhenTheCallerListsAnotherUsersTokens()
    {
        // Arrange
        var caller = TestDoubles.Member("alice");
        var other = TestDoubles.Disabled(TestDoubles.Member("bob"));
        var handler = new Handler(TestDoubles.Database([caller, other], [TokenDoubles.TokenOf(other)]));

        // Act
        var result = await handler.HandleAsync(new Query(caller.Id, other.Id), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.ForbiddenCode);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuse_WhenTheCallerIsDisabled()
    {
        // Arrange
        var caller = TestDoubles.Disabled(TestDoubles.Member("bob"));
        var handler = new Handler(TestDoubles.Database([caller], [TokenDoubles.TokenOf(caller)]));

        // Act
        var result = await handler.HandleAsync(new Query(caller.Id, caller.Id), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuse_WhenTheCallerIsUnknown()
    {
        // Arrange
        var handler = new Handler(TestDoubles.Database([], []));
        var callerId = Guid.NewGuid();

        // Act
        var result = await handler.HandleAsync(new Query(callerId, callerId), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
    }
}
