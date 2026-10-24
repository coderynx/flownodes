using Coderynx.Functional.Results.Successes;
using Identity.Application;
using Identity.Application.Self.GetProfile;
using Identity.Domain.Users;
using Identity.UnitTests.Users;
using Xunit;

namespace Identity.UnitTests.Self.GetProfile;

public sealed class HandlerTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsUnknown()
    {
        // Arrange
        var handler = new Handler(TestUsers.Database([TestUsers.Member("alice")]));

        // Act
        var result = await handler.HandleAsync(new Query(Guid.NewGuid()), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUnauthenticated_WhenTheCallerIsDisabled()
    {
        // Arrange
        var alice = TestUsers.Disabled(TestUsers.Member("alice"));
        var handler = new Handler(TestUsers.Database([alice]));

        // Act
        var result = await handler.HandleAsync(new Query(alice.Id), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTheCallerWithTheirRoleGrants_WhenTheCallerIsActive()
    {
        // Arrange
        var alice = TestUsers.Member("alice");
        alice.Grant("default", Role.Engineer);
        var handler = new Handler(TestUsers.Database([TestUsers.Administrator(), alice]));

        // Act
        var result = await handler.HandleAsync(new Query(alice.Id), Ct);

        // Assert
        result.Success.Kind.ShouldBe(SuccessKind.Found);
        result.Value.ShouldBeSameAs(alice);
        result.Value.RoleGrants.ShouldHaveSingleItem().Role.ShouldBe(Role.Engineer);
    }
}
