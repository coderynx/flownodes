using Identity.Application;
using Xunit;

namespace Identity.UnitTests;

public sealed class CallerChecksTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RequireActiveAsync_ShouldReturnUnauthenticated_WhenTheCallerIsUnknown()
    {
        // Arrange
        var checks = new CallerChecks(TestDoubles.Database([TestDoubles.Member("alice")]));

        // Act
        var result = await checks.RequireActiveAsync(Guid.NewGuid(), Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
    }

    [Fact]
    public async Task RequireActiveAsync_ShouldReturnUnauthenticated_WhenTheCallerIsDisabled()
    {
        // Arrange
        var alice = TestDoubles.Disabled(TestDoubles.Member("alice"));
        var checks = new CallerChecks(TestDoubles.Database([alice]));

        // Act
        var result = await checks.RequireActiveAsync(alice.Id, Ct);

        // Assert
        result.Error.ShouldBe(Errors.Unauthenticated());
    }

    [Fact]
    public async Task RequireActiveAsync_ShouldReturnTheCaller_WhenTheCallerIsEnabled()
    {
        // Arrange
        var alice = TestDoubles.Member("alice");
        var checks = new CallerChecks(TestDoubles.Database([alice]));

        // Act
        var result = await checks.RequireActiveAsync(alice.Id, Ct);

        // Assert
        result.Value.ShouldBeSameAs(alice);
    }

    [Fact]
    public async Task RequireAdministratorAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var alice = TestDoubles.Member("alice");
        var checks = new CallerChecks(TestDoubles.Database([alice]));

        // Act
        var result = await checks.RequireAdministratorAsync(alice.Id, Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
    }

    [Fact]
    public async Task RequireAdministratorAsync_ShouldReturnTheCaller_WhenTheCallerIsAnAdministrator()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var checks = new CallerChecks(TestDoubles.Database([administrator]));

        // Act
        var result = await checks.RequireAdministratorAsync(administrator.Id, Ct);

        // Assert
        result.Value.ShouldBeSameAs(administrator);
    }

    [Fact]
    public async Task FindUserAsync_ShouldReturnNotFound_WhenTheUserDoesNotExist()
    {
        // Arrange
        var checks = new CallerChecks(TestDoubles.Database([]));

        // Act
        var result = await checks.FindUserAsync(Guid.NewGuid(), Ct);

        // Assert
        result.Error.ShouldBe(Errors.UserNotFound());
    }

    [Fact]
    public async Task RequireAdministratorAndFindUserAsync_ShouldReturnTheUser_WhenTheCallerIsAnAdministrator()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var alice = TestDoubles.Member("alice");
        var checks = new CallerChecks(TestDoubles.Database([administrator, alice]));

        // Act
        var result = await checks.RequireAdministratorAndFindUserAsync(administrator.Id, alice.Id, Ct);

        // Assert
        result.Value.ShouldBeSameAs(alice);
    }

    [Fact]
    public async Task RequireAdministratorAndFindUserAsync_ShouldReturnForbidden_WhenTheCallerIsNotAnAdministrator()
    {
        // Arrange
        var bob = TestDoubles.Member("bob");
        var alice = TestDoubles.Member("alice");
        var checks = new CallerChecks(TestDoubles.Database([bob, alice]));

        // Act
        var result = await checks.RequireAdministratorAndFindUserAsync(bob.Id, alice.Id, Ct);

        // Assert
        result.Error.ShouldBe(Errors.Forbidden());
    }

    [Fact]
    public async Task AnotherActiveAdministratorExistsAsync_ShouldBeFalse_WhenTheOnlyOtherAdministratorIsDisabled()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Disabled(TestDoubles.Administrator("other"));
        var checks = new CallerChecks(TestDoubles.Database([administrator, other]));

        // Act
        var exists = await checks.AnotherActiveAdministratorExistsAsync(administrator.Id, Ct);

        // Assert
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task AnotherActiveAdministratorExistsAsync_ShouldBeTrue_WhenAnotherAdministratorIsEnabled()
    {
        // Arrange
        var administrator = TestDoubles.Administrator();
        var other = TestDoubles.Administrator("other");
        var checks = new CallerChecks(TestDoubles.Database([administrator, other]));

        // Act
        var exists = await checks.AnotherActiveAdministratorExistsAsync(administrator.Id, Ct);

        // Assert
        exists.ShouldBeTrue();
    }
}
