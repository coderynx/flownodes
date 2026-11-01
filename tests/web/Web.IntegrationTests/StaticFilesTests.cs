using System.Net;
using System.Text.RegularExpressions;
using Xunit;

namespace Web.IntegrationTests;

public sealed partial class StaticFilesTests(PostgresFixture postgres)
{
    private const string RootElement = "<div id=\"root\">";

    [Theory]
    [InlineData("/")]
    [InlineData("/alarms/7")]
    [InlineData("/namespaces/plant/devices?tab=state")]
    public async Task ClientRoute_ShouldServeIndexHtml_WhenReloaded(string path)
    {
        // Arrange
        await using var app = await WebApp.StartAsync(postgres);

        // Act
        var response = await app.Client.GetStringAsync(path, TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContain(RootElement);
    }

    [Fact]
    public async Task WebUi_ShouldLoadAnonymously_WhenTheHostRequiresAuthenticationByDefault()
    {
        // Arrange
        var cancellation = TestContext.Current.CancellationToken;
        await using var app = await WebApp.StartAsync(postgres, requireAuthentication: true);
        var html = await app.Client.GetStringAsync("/alarms/7", cancellation);

        // Act
        var script = await app.Client.GetAsync(ScriptPath(html), cancellation);

        // Assert
        html.ShouldContain(RootElement);
        script.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClientRoute_ShouldRequireRevalidation_WhenServingIndexHtml()
    {
        // Arrange
        await using var app = await WebApp.StartAsync(postgres);

        // Act
        var response = await app.Client.GetAsync("/alarms/7", TestContext.Current.CancellationToken);

        // Assert
        response.Headers.CacheControl?.NoCache.ShouldBeTrue();
    }

    [Fact]
    public async Task BuiltScript_ShouldBeServed_WhenIndexHtmlLinksIt()
    {
        // Arrange
        await using var app = await WebApp.StartAsync(postgres);
        var html = await app.Client.GetStringAsync("/", TestContext.Current.CancellationToken);

        // Act
        var response = await app.Client.GetAsync(ScriptPath(html), TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/javascript");
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("/api/v1/nothing")]
    [InlineData("/API/v1/nothing")]
    [InlineData("/bff/nothing")]
    [InlineData("/connect/nothing")]
    [InlineData("/assets/missing.js")]
    public async Task UnknownPath_ShouldReturnNotFound_WhenItIsNotAClientRoute(string path)
    {
        // Arrange
        await using var app = await WebApp.StartAsync(postgres);

        // Act
        var response = await app.Client.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task KnownApiPath_ShouldReturnMethodNotAllowed_WhenTheMethodIsWrong()
    {
        // Arrange
        await using var app = await WebApp.StartAsync(postgres);

        // Act
        var response = await app.Client.PostAsync("/api/v1/known", null, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    [Fact]
    public async Task ClientRoute_ShouldServeIndexHtml_WhenOnlyPrefixedWithApi()
    {
        // Arrange
        await using var app = await WebApp.StartAsync(postgres);

        // Act
        var response = await app.Client.GetStringAsync("/apikeys", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldContain(RootElement);
    }

    private static string ScriptPath(string html)
    {
        var match = ScriptSource().Match(html);
        match.Success.ShouldBeTrue();

        return match.Groups[1].Value;
    }

    [GeneratedRegex("<script[^>]+src=\"([^\"]+)\"")]
    private static partial Regex ScriptSource();
}
