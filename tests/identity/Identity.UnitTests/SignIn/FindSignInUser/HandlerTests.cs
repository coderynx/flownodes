using Identity.Application.SignIn.FindSignInUser;
using Xunit;

namespace Identity.UnitTests.SignIn.FindSignInUser;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldFindTheUser_WhenTheUserIsEnabled()
    {
        // Arrange
        var user = TestDoubles.Member("alice");
        var handler = new Handler(TestDoubles.Database([user, TestDoubles.Disabled(TestDoubles.Member("bob"))]));

        // Act
        var result = await handler.HandleAsync(new Query(user.Id), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeSameAs(user);
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuse_WhenTheUserIsDisabled()
    {
        // Arrange
        var user = TestDoubles.Disabled(TestDoubles.Member("bob"));
        var handler = new Handler(TestDoubles.Database([user]));

        // Act
        var result = await handler.HandleAsync(new Query(user.Id), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("signin.invalid_credentials");
    }

    [Fact]
    public async Task HandleAsync_ShouldRefuse_WhenTheUserDoesNotExist()
    {
        // Arrange
        var handler = new Handler(TestDoubles.Database([TestDoubles.Member("alice")]));

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid()), TestContext.Current.CancellationToken);

        // Assert
        result.Error.Code.ShouldBe("signin.invalid_credentials");
    }
}
