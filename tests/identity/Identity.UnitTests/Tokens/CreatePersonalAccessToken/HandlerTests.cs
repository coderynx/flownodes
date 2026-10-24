using Core.Application.Authorization;
using Identity.Application.Database;
using Identity.Application.Tokens;
using Identity.Application.Tokens.CreatePersonalAccessToken;
using Xunit;

namespace Identity.UnitTests.Tokens.CreatePersonalAccessToken;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldStoreTheTokenAndReturnItsSecret_WhenTheCallerCreatesATokenForThemself()
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var database = TokenDoubles.Database([caller], []);
        var handler = new Handler(database, TokenDoubles.Clock());
        var expiresAt = TokenDoubles.Now.AddDays(30);

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, "  laptop  ", expiresAt),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var token = result.Value.Token;
        result.Value.Secret.ShouldStartWith("fnpat_");
        token.UserId.ShouldBe(caller.Id);
        token.Name.ShouldBe("laptop");
        token.SecretHash.ShouldBe(PersonalAccessTokenSecrets.Hash(result.Value.Secret));
        token.CreatedAt.ShouldBe(TokenDoubles.Now);
        token.ExpiresAt.ShouldBe(expiresAt);
        database.PersonalAccessTokens.Received(1).Add(token);
        await database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldGenerateADifferentSecret_WhenCalledTwice()
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var handler = new Handler(TokenDoubles.Database([caller], []), TokenDoubles.Clock());
        var command = new Command(caller.Id, caller.Id, "laptop", null);
        var first = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Act
        var second = await handler.HandleAsync(command, TestContext.Current.CancellationToken);

        // Assert
        second.Value.Secret.ShouldNotBe(first.Value.Secret);
        second.Value.Token.SecretHash.ShouldNotBe(first.Value.Token.SecretHash);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuse_WhenTheCallerIsUnknown()
    {
        // Arrange
        var database = TokenDoubles.Database([], []);
        var handler = new Handler(database, TokenDoubles.Clock());
        var callerId = Guid.NewGuid();

        // Act
        var result = await handler.HandleAsync(
            new Command(callerId, callerId, "laptop", null),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
        await AssertNothingStoredAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuse_WhenTheCallerIsDisabled()
    {
        // Arrange
        var caller = TokenDoubles.DisabledUser();
        var database = TokenDoubles.Database([caller], []);
        var handler = new Handler(database, TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, "laptop", null),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.UnauthenticatedCode);
        await AssertNothingStoredAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldForbid_WhenTheTokenIsForAnotherUser()
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var other = TokenDoubles.DisabledUser();
        var database = TokenDoubles.Database([caller, other], []);
        var handler = new Handler(database, TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, other.Id, "laptop", null),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe(AccessErrors.ForbiddenCode);
        await AssertNothingStoredAsync(database);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleAsync_ShouldRefuseTheName_WhenItIsBlank(string name)
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var database = TokenDoubles.Database([caller], []);
        var handler = new Handler(database, TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, name, null),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("token.invalid_name");
        await AssertNothingStoredAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuseTheName_WhenItIsLongerThan100Characters()
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var database = TokenDoubles.Database([caller], []);
        var handler = new Handler(database, TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, new string('x', 101), null),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("token.invalid_name");
        await AssertNothingStoredAsync(database);
    }

    [Fact]
    public async Task HandleAsync_ShouldAcceptTheName_WhenItIsExactly100Characters()
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var handler = new Handler(TokenDoubles.Database([caller], []), TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, new string('x', 100), null),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task HandleAsync_ShouldRefuseTheExpiry_WhenItIsNotInTheFuture(int minutesFromNow)
    {
        // Arrange
        var caller = TokenDoubles.ActiveUser();
        var database = TokenDoubles.Database([caller], []);
        var handler = new Handler(database, TokenDoubles.Clock());

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, "laptop", TokenDoubles.Now.AddMinutes(minutesFromNow)),
            TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("token.expiry_in_the_past");
        await AssertNothingStoredAsync(database);
    }

    private static async Task AssertNothingStoredAsync(IIdentityDbContext database)
    {
        database.PersonalAccessTokens.DidNotReceiveWithAnyArgs().Add(null!);
        await database.DidNotReceiveWithAnyArgs().SaveChangesAsync(TestContext.Current.CancellationToken);
    }
}
