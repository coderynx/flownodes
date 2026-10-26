using Core.Application.Authorization;
using Identity.Application.Database;
using Identity.Application.Tokens;
using Identity.Application.Tokens.CreatePersonalAccessToken;
using Xunit;

namespace Identity.UnitTests.Tokens.CreatePersonalAccessToken;

public sealed class HandlerTests
{
    private readonly IPersonalAccessTokenSecrets _secrets = TokenDoubles.Secrets();

    [Fact]
    public async Task HandleAsync_ShouldStoreTheTokenAndReturnItsSecret_WhenTheCallerCreatesATokenForThemself()
    {
        // Arrange
        var caller = TestDoubles.Member("alice");
        var database = TestDoubles.Database([caller], []);
        var handler = new Handler(
            database,
            TestDoubles.Checks(database),
            _secrets,
            TestDoubles.Clock(TokenDoubles.Now));
        var expiresAt = TokenDoubles.Now.AddDays(30);

        // Act
        var result = await handler.HandleAsync(
            new Command(caller.Id, caller.Id, "  laptop  ", expiresAt),
            TestContext.Current.CancellationToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var token = result.Value.Token;
        result.Value.Secret.ShouldBe("fnpat_generated");
        token.UserId.ShouldBe(caller.Id);
        token.Name.ShouldBe("laptop");
        token.SecretHash.ShouldBe(_secrets.Hash("fnpat_generated"));
        token.CreatedAt.ShouldBe(TokenDoubles.Now);
        token.ExpiresAt.ShouldBe(expiresAt);
        database.PersonalAccessTokens.Received(1).Add(token);
        await database.Received(1).SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuse_WhenTheCallerIsUnknown()
    {
        // Arrange
        var database = TestDoubles.Database([], []);
        var handler = new Handler(
            database,
            TestDoubles.Checks(database),
            _secrets,
            TestDoubles.Clock(TokenDoubles.Now));
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
        var caller = TestDoubles.Disabled(TestDoubles.Member("bob"));
        var database = TestDoubles.Database([caller], []);
        var handler = new Handler(
            database,
            TestDoubles.Checks(database),
            _secrets,
            TestDoubles.Clock(TokenDoubles.Now));

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
        var caller = TestDoubles.Member("alice");
        var other = TestDoubles.Disabled(TestDoubles.Member("bob"));
        var database = TestDoubles.Database([caller, other], []);
        var handler = new Handler(
            database,
            TestDoubles.Checks(database),
            _secrets,
            TestDoubles.Clock(TokenDoubles.Now));

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
        var caller = TestDoubles.Member("alice");
        var database = TestDoubles.Database([caller], []);
        var handler = new Handler(
            database,
            TestDoubles.Checks(database),
            _secrets,
            TestDoubles.Clock(TokenDoubles.Now));

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
        var caller = TestDoubles.Member("alice");
        var database = TestDoubles.Database([caller], []);
        var handler = new Handler(
            database,
            TestDoubles.Checks(database),
            _secrets,
            TestDoubles.Clock(TokenDoubles.Now));

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
        var caller = TestDoubles.Member("alice");
        var database = TestDoubles.Database([caller], []);
        var handler = new Handler(
            database,
            TestDoubles.Checks(database),
            _secrets,
            TestDoubles.Clock(TokenDoubles.Now));

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
        var caller = TestDoubles.Member("alice");
        var database = TestDoubles.Database([caller], []);
        var handler = new Handler(
            database,
            TestDoubles.Checks(database),
            _secrets,
            TestDoubles.Clock(TokenDoubles.Now));

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
