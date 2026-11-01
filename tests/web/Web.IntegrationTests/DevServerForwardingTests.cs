using System.Net;
using System.Net.WebSockets;
using System.Text;
using Xunit;

namespace Web.IntegrationTests;

public sealed class DevServerForwardingTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("/alarms/7")]
    [InlineData("/src/main.tsx?t=1")]
    [InlineData("/@vite/client")]
    public async Task Request_ShouldReachTheDevServer_WhenOutsideTheHostsPaths(string path)
    {
        // Arrange
        await using var devServer = await DevServer.StartAsync();
        await using var app = await WebApp.StartAsync(devServer.Address, Environments.Development);

        // Act
        var response = await app.Client.GetStringAsync(path, TestContext.Current.CancellationToken);

        // Assert
        response.ShouldBe($"dev server saw {path}");
    }

    [Fact]
    public async Task Request_ShouldReachTheDevServerAnonymously_WhenTheHostRequiresAuthenticationByDefault()
    {
        // Arrange
        await using var devServer = await DevServer.StartAsync();
        await using var app = await WebApp.StartAsync(
            devServer.Address,
            Environments.Development,
            requireAuthentication: true);

        // Act
        var response = await app.Client.GetStringAsync("/alarms/7", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldBe("dev server saw /alarms/7");
    }

    [Theory]
    [InlineData("/api/v1/known", "known")]
    [InlineData("/bff/session", "session")]
    [InlineData("/signin", "signin")]
    public async Task Request_ShouldStayOnTheHost_WhenTheHostMapsThePath(string path, string body)
    {
        // Arrange
        await using var devServer = await DevServer.StartAsync();
        await using var app = await WebApp.StartAsync(devServer.Address, Environments.Development);

        // Act
        var response = await app.Client.GetStringAsync(path, TestContext.Current.CancellationToken);

        // Assert
        response.ShouldBe(body);
    }

    [Theory]
    [InlineData("/api/v1/nothing")]
    [InlineData("/bff/nothing")]
    [InlineData("/connect/nothing")]
    public async Task Request_ShouldReturnNotFound_WhenUnderAReservedPrefixAndUnmapped(string path)
    {
        // Arrange
        await using var devServer = await DevServer.StartAsync();
        await using var app = await WebApp.StartAsync(devServer.Address, Environments.Development);

        // Act
        var response = await app.Client.GetAsync(path, TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HotReloadSocket_ShouldReachTheDevServer_WhenOpenedOnTheHostsOrigin()
    {
        // Arrange
        var cancellation = TestContext.Current.CancellationToken;
        await using var devServer = await DevServer.StartAsync();
        await using var app = await WebApp.StartAsync(devServer.Address, Environments.Development);
        using var socket = new ClientWebSocket();
        socket.Options.AddSubProtocol("vite-hmr");
        var address = new UriBuilder(app.Address) { Scheme = "ws", Query = "token=abc" }.Uri;
        var buffer = new byte[256];
        await socket.ConnectAsync(address, cancellation);

        // Act
        var received = await socket.ReceiveAsync(buffer, cancellation);

        // Assert
        socket.SubProtocol.ShouldBe("vite-hmr");
        Encoding.UTF8.GetString(buffer, 0, received.Count).ShouldBe("dev server saw /?token=abc");
    }

    [Fact]
    public async Task Request_ShouldNotReachTheDevServer_WhenTheHostIsNotInDevelopment()
    {
        // Arrange
        await using var devServer = await DevServer.StartAsync();
        await using var app = await WebApp.StartAsync(devServer.Address);

        // Act
        var response = await app.Client.GetStringAsync("/alarms/7", TestContext.Current.CancellationToken);

        // Assert
        response.ShouldNotContain("dev server saw");
    }
}
