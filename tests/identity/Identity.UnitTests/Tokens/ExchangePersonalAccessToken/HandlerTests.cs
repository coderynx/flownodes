using Identity.Application.Tokens.ExchangePersonalAccessToken;
using Xunit;

namespace Identity.UnitTests.Tokens.ExchangePersonalAccessToken;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldFindTheOwner_WhenTheTokenIsActive()
    {
        // Arrange
        var owner = TestDoubles.Member("alice");
        var database = TestDoubles.Database([owner], [TokenDoubles.TokenOf(owner, TokenDoubles.Now.AddDays(1))]);
        var handler = new Handler(database, TestDoubles.Clock(TokenDoubles.Now));

        // Act
        var result = await handler.HandleAsync(new Command(TokenDoubles.Secret), TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeSameAs(owner);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseTheExchange_WhenTheSecretIsUnknown()
    {
        // Arrange
        var owner = TestDoubles.Member("alice");
        var database = TestDoubles.Database([owner], [TokenDoubles.TokenOf(owner)]);
        var handler = new Handler(database, TestDoubles.Clock(TokenDoubles.Now));

        // Act
        var result = await handler.HandleAsync(
            new Command("fnpat_another-secret"),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("token.invalid");
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseTheExchange_WhenTheTokenHasExpired()
    {
        // Arrange
        var owner = TestDoubles.Member("alice");
        var database = TestDoubles.Database([owner], [TokenDoubles.TokenOf(owner, TokenDoubles.Now)]);
        var handler = new Handler(database, TestDoubles.Clock(TokenDoubles.Now));

        // Act
        var result = await handler.HandleAsync(new Command(TokenDoubles.Secret), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("token.invalid");
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseTheExchange_WhenTheTokenIsRevoked()
    {
        // Arrange
        var owner = TestDoubles.Member("alice");
        var token = TokenDoubles.TokenOf(owner);
        token.Revoke(TokenDoubles.Now.AddHours(-1));
        var database = TestDoubles.Database([owner], [token]);
        var handler = new Handler(database, TestDoubles.Clock(TokenDoubles.Now));

        // Act
        var result = await handler.HandleAsync(new Command(TokenDoubles.Secret), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("token.invalid");
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseTheExchange_WhenTheOwnerIsDisabled()
    {
        // Arrange
        var owner = TestDoubles.Disabled(TestDoubles.Member("bob"));
        var database = TestDoubles.Database([owner], [TokenDoubles.TokenOf(owner)]);
        var handler = new Handler(database, TestDoubles.Clock(TokenDoubles.Now));

        // Act
        var result = await handler.HandleAsync(new Command(TokenDoubles.Secret), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("token.invalid");
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseTheExchange_WhenTheOwnerNoLongerExists()
    {
        // Arrange
        var owner = TestDoubles.Member("alice");
        var database = TestDoubles.Database([], [TokenDoubles.TokenOf(owner)]);
        var handler = new Handler(database, TestDoubles.Clock(TokenDoubles.Now));

        // Act
        var result = await handler.HandleAsync(new Command(TokenDoubles.Secret), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("token.invalid");
    }
}
