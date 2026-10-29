using System.Net;
using Xunit;

namespace Engine.IntegrationTests;

public sealed class OrleansDashboardTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Dashboard_ShouldNotRequireAuthentication_WhenTheHostIsInDevelopment()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync(environment: "Development");

        // Act
        var response = await app.Anonymous.GetAsync("/orleans", Ct);

        // Assert
        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Dashboard_ShouldNotBeMapped_WhenTheHostIsNotInDevelopment()
    {
        // Arrange
        await using var app = await EngineApp.StartAsync();
        var (_, member) = await app.MemberAsync("Operator", "acme");

        // Act
        var response = await member.GetAsync("/orleans", Ct);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
