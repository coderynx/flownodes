using Identity.Application.Setup.GetSetupState;
using Xunit;

namespace Identity.UnitTests.Setup.GetSetupState;

public sealed class HandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReportSetupPending_WhenNoUserExists()
    {
        // Arrange
        var handler = new Handler(SetupDoubles.Database([]));

        // Act
        var result = await handler.HandleAsync(new Query(), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleAsync_ShouldReportSetupDone_WhenAUserExists()
    {
        // Arrange
        var handler = new Handler(SetupDoubles.Database([SetupDoubles.ExistingUser()]));

        // Act
        var result = await handler.HandleAsync(new Query(), TestContext.Current.CancellationToken);

        // Assert
        result.Value.ShouldBeFalse();
    }
}
