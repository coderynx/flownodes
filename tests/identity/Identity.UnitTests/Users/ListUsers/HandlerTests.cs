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
        var bob = TestUsers.Member("bob");
        var handler = new Handler(TestUsers.Database([bob]));

        // Act
        var result = await handler.HandleAsync(new Query(bob.Id, null, null, false, 0, 50), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsUnknown()
    {
        // Arrange
        var handler = new Handler(TestUsers.Database([TestUsers.Administrator()]));

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
        var administrator = TestUsers.Administrator();
        var handler = new Handler(TestUsers.Database([administrator]));

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
        var administrator = TestUsers.Administrator("carol");
        var database = TestUsers.Database([TestUsers.Member("bob"), administrator, TestUsers.Member("Alice")]);
        var handler = new Handler(database);

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
        var administrator = TestUsers.Administrator("carol");
        var database = TestUsers.Database([TestUsers.Member("bob"), administrator, TestUsers.Member("Alice")]);
        var handler = new Handler(database);

        // Act
        var result = await handler.HandleAsync(new Query(administrator.Id, null, null, true, 0, 50), Ct);

        // Assert
        Names(result.Value).ShouldBe(["carol", "bob", "Alice"]);
    }

    [Fact]
    public async Task HandleAsync_ShouldKeepOnlyUsernamesContainingTheText_WhenQIsSetIgnoringCase()
    {
        // Arrange
        var administrator = TestUsers.Administrator();
        var users = new[]
            { administrator, TestUsers.Member("Alice"), TestUsers.Member("malice"), TestUsers.Member("bob") };
        var handler = new Handler(TestUsers.Database(users));

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
        var administrator = TestUsers.Administrator("a0");
        var users = new[] { administrator, TestUsers.Member("a1"), TestUsers.Member("a2"), TestUsers.Member("a3") };
        var handler = new Handler(TestUsers.Database(users));

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
        var administrator = TestUsers.Administrator("a0");
        var handler = new Handler(TestUsers.Database([administrator, TestUsers.Member("a1")]));

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
        var administrator = TestUsers.Administrator("a000");
        var members = Enumerable.Range(1, 210).Select(index => TestUsers.Member($"a{index:000}"));
        var handler = new Handler(TestUsers.Database(members.Append(administrator)));

        // Act
        var result = await handler.HandleAsync(new Query(administrator.Id, null, null, false, 0, limit), Ct);

        // Assert
        result.Value.Items.Count.ShouldBe(expected);
        result.Value.Total.ShouldBe(211);
    }

    private static string[] Names(UserPage page) => page.Items.Select(user => user.Username.Value).ToArray();
}
