using Microsoft.AspNetCore.Authorization;
using Npgsql;
using Web.Bootstrap;

namespace Web.IntegrationTests;

public sealed class WebApp : IAsyncDisposable
{
    private readonly WebApplication _app;

    private WebApp(WebApplication app)
    {
        _app = app;
        Address = new Uri(app.Urls.First());
        Client = new HttpClient { BaseAddress = Address };
    }

    public Uri Address { get; }
    public HttpClient Client { get; }

    public static async Task<WebApp> StartAsync(
        PostgresFixture postgres,
        string? devServer = null,
        string? environment = null,
        bool requireAuthentication = false)
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = environment ?? Environments.Production });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.WebHost.UseStaticWebAssets();
        builder.Logging.ClearProviders();
        builder.Configuration["Flownodes:Web:DevServer"] = devServer;
        builder.Configuration["ConnectionStrings:web"] = await postgres.CreateDatabaseAsync();
        builder.Configuration["Flownodes:Identity:Authority"] = "https://127.0.0.1:1/";
        builder.Configuration["Flownodes:InstallationKey"] = Convert.ToBase64String(new byte[32]);
        builder.AddWeb();

        if (requireAuthentication)
        {
            builder.Services.AddAuthentication().AddCookie();
            builder.Services.AddAuthorization(options =>
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        }

        var app = builder.Build();

        if (requireAuthentication)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        app.MapGet("/api/v1/known", () => "known");
        app.MapGet("/signin", () => "signin");
        app.UseWeb();

        await app.StartAsync();

        return new WebApp(app);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
    }
}
