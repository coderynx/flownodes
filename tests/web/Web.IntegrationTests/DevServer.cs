using System.Net.WebSockets;
using System.Text;

namespace Web.IntegrationTests;

public sealed class DevServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private DevServer(WebApplication app)
    {
        _app = app;
        Address = app.Urls.First();
    }

    public string Address { get; }

    public static async Task<DevServer> StartAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();

        var app = builder.Build();
        app.UseWebSockets();
        app.Run(async context =>
        {
            var seen = $"dev server saw {context.Request.Path}{context.Request.QueryString}";

            if (!context.WebSockets.IsWebSocketRequest)
            {
                await context.Response.WriteAsync(seen);
                return;
            }

            using var socket = await context.WebSockets.AcceptWebSocketAsync("vite-hmr");
            await socket.SendAsync(Encoding.UTF8.GetBytes(seen), WebSocketMessageType.Text, true, default);
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, default);
        });

        await app.StartAsync();

        return new DevServer(app);
    }

    public ValueTask DisposeAsync() => _app.DisposeAsync();
}
