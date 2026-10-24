using Coderynx.Functional.Results.Successes;
using Core.Application.Authorization;
using Identity.Application.Tokens.RevokePersonalAccessToken;
using Xunit;

namespace Identity.UnitTests.Tokens.RevokePersonalAccessToken;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldRevokeTheTokenNow_WhenTheCallerRevokesTheirOwn()
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var token = TokenDoubles.TokenOf(caller);
        var database = TokenDoubles.Database([caller], [token]);
        var handler = new Handler(database, TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, token.Id),
            TestContext.Current.CancellationToken);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Deleted);
        token.RevokedAt.ShouldBe(TokenDoubles.Now);
        await database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepTheFirstRevocation_WhenTheTokenIsAlreadyRevoked()
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var token = TokenDoubles.TokenOf(caller);
        token.Revoke(TokenDoubles.Now.AddHours(-1));
        var handler = new Handler(TokenDoubles.Database([caller], [token]), TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, token.Id),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        token.RevokedAt.ShouldBe(TokenDoubles.Now.AddHours(-1));
    }

    [Fact]
    public async Task HandleAsync_ShouldReportNotFound_WhenTheTokenDoesNotExist()
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var database = TokenDoubles.Database([caller], []);
        var handler = new Handler(database, TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, Guid.NewGuid()),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("token.not_found");
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldReportNotFound_WhenTheTokenBelongsToAnotherUser()
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var other = TokenDoubles.DisabledUser();
        var othersToken = TokenDoubles.TokenOf(other);
        var database = TokenDoubles.Database([caller, other], [othersToken]);
        var handler = new Handler(database, TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, othersToken.Id),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("token.not_found");
        othersToken.RevokedAt.ShouldBeNull();
    }

    [Fact]
    public async Task HandleAsync_ShouldForbid_WhenTheCallerRevokesAnotherUsersToken()
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var other = TokenDoubles.DisabledUser();
        var othersToken = TokenDoubles.TokenOf(other);
        var database = TokenDoubles.Database([caller, other], [othersToken]);
        var handler = new Handler(database, TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, other.Id, othersToken.Id),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.ForbiddenCode);
        othersToken.RevokedAt.ShouldBeNull();
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuse_WhenTheCallerIsDisabled()
    {
        // Arrange
        var caller = TokenDoubles.DisabledUser();
        var token = TokenDoubles.TokenOf(caller);
        var database = TokenDoubles.Database([caller], [token]);
        var handler = new Handler(database, TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, token.Id),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
        token.RevokedAt.ShouldBeNull();
    }
}
