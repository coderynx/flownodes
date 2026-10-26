using Identity.Application;
using Identity.Application.Users.ListUsers;
using Xunit;

namespace Identity.UnitTests.Users.ListUsers;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestDoubles.Member("bob");
        var database = TestDoubles.Database([bob]);
        var handler = new Handler(database, TestDoubles.Checks(database));

        // Act
        var result = await handler.HandleAsync(new Query(bob.Id, null, null, false, 0, 50), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsUnknown()
    {
        // Arrange
        var database = TestDoubles.Database([TestDoubles.Administrator()]);
        var handler = new Handler(database, TestDoubles.Checks(database));

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid(), null, null, false, 0, 50), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
    }

    [Theory]
    [InlineData("email")]
    [InlineData("Username")]
    public async Task HandleAsync_ShouldReturnBadRequest_WhenTheSortKeyIsUnknown(string sort)
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var database = TestDoubles.Database([administrator]);
        var handler = new Handler(database, TestDoubles.Checks(database));

        // Act
        var result = await handler.HandleAsync(new Query(administrator.Id, null, sort, false, 0, 50), Ct);

        // Assert
        result.Error.ShouldBe(Errors.UnknownSortKey(sort));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("username")]
    public async Task HandleAsync_ShouldSortByUsernameIgnoringCase_WhenTheSortKeyIsUsernameOrNone(string? sort)
    {
        // Arrange
        var administrator = TestDoubles.Administrator("carol");
        var database = TestDoubles.Database([TestDoubles.Member("bob"), administrator, TestDoubles.Member("Alice")]);
        var handler = new Handler(database, TestDoubles.Checks(database));

        // Act
        var result = await handler.HandleAsync(new Query(administrator.Id, null, sort, false, 0, 50), Ct);

        // Assert
        Names(result.Value).ShouldBe(["Alice", "bob", "carol"]);
        result.Value.Total.ShouldBe(3);
    }

    [Fact]
    public async Task HandleAsync_ShouldReverseTheOrder_WhenDescIsSet()
    {
        // Arrange
        var administrator = TestDoubles.Administrator("carol");
        var database = TestDoubles.Database([TestDoubles.Member("bob"), administrator, TestDoubles.Member("Alice")]);
        var handler = new Handler(database, TestDoubles.Checks(database));

        // Act
        var result = await handler.HandleAsync(new Query(administrator.Id, null, null, true, 0, 50), Ct);

        // Assert
        Names(result.Value).ShouldBe(["carol", "bob", "Alice"]);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepOnlyUsernamesContainingTheText_WhenQIsSetIgnoringCase()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var users = new[]
            { administrator, TestDoubles.Member("Alice"), TestDoubles.Member("malice"), TestDoubles.Member("bob") };
        var database = TestDoubles.Database(users);
        var handler = new Handler(database, TestDoubles.Checks(database));

        // Act
        var result = await handler.HandleAsync(new Query(administrator.Id, "ALI", null, false, 0, 50), Ct);

        // Assert
        Names(result.Value).ShouldBe(["Alice", "malice"]);
        result.Value.Total.ShouldBe(2);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnOnePageAndTheTotal_WhenOffsetAndLimitAreSet()
    {
        // Arrange
        var administrator = TestDoubles.Administrator("a0");
        var users = new[]
            { administrator, TestDoubles.Member("a1"), TestDoubles.Member("a2"), TestDoubles.Member("a3") };
        var database = TestDoubles.Database(users);
        var handler = new Handler(database, TestDoubles.Checks(database));

        // Act
        var result = await handler.HandleAsync(new Query(administrator.Id, null, null, false, 1, 2), Ct);

        // Assert
        Names(result.Value).ShouldBe(["a1", "a2"]);
        result.Value.Total.ShouldBe(4);
    }

    [Fact]
    public async Task HandleAsync_ShouldStartAtTheFirstUser_WhenTheOffsetIsNegative()
    {
        // Arrange
        var administrator = TestDoubles.Administrator("a0");
        var database = TestDoubles.Database([administrator, TestDoubles.Member("a1")]);
        var handler = new Handler(database, TestDoubles.Checks(database));

        // Act
        var result = await handler.HandleAsync(new Query(administrator.Id, null, null, false, -5, 50), Ct);

        // Assert
        Names(result.Value).ShouldBe(["a0", "a1"]);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-3, 1)]
    [InlineData(500, 200)]
    public async Task HandleAsync_ShouldClampTheLimit_WhenItIsOutsideOneToTwoHundred(int limit, int expected)
    {
        // Arrange
        var administrator = TestDoubles.Administrator("a000");
        var members = Enumerable.Range(1, 210).Select(index => TestDoubles.Member($"a{index:000}"));
        var database = TestDoubles.Database(members.Append(administrator));
        var handler = new Handler(database, TestDoubles.Checks(database));

        // Act
        var result = await handler.HandleAsync(new Query(administrator.Id, null, null, false, 0, limit), Ct);

        // Assert
        result.Value.Items.Count.ShouldBe(expected);
        result.Value.Total.ShouldBe(211);
    }

    private static string[] Names(UserPage page) => page.Items.Select(user => user.Username.Value).ToArray();
}
