using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Identity.Application.Bootstrap;
using Identity.Infrastructure.Bootstrap;
using Identity.Presentation.Bootstrap;
using JasperFx.CodeGeneration;
using Npgsql;
using OpenIddict.Server;
using Web.Bootstrap;
using Wolverine;

namespace Flownodes.IntegrationTests;

/// <summary>
/// Identity and Web in one real host over HTTP, reaching each other only through the OIDC protocol, as the monolith
/// runs them.
/// </summary>
public sealed partial class SignInHost : IAsyncDisposable
{
    public const string ProbePath = "/api/v1/probe";
    private readonly WebApplication _app;
    private readonly LogCapture _logs;

    private SignInHost(
        WebApplication app,
        LogCapture logs,
        Uri address,
        string identityDatabase,
        string webDatabase,
        string installationKey)
    {
        _app = app;
        _logs = logs;
        Address = address;
        IdentityDatabase = identityDatabase;
        WebDatabase = webDatabase;
        InstallationKey = installationKey;
    }

    public Uri Address { get; }
    public string IdentityDatabase { get; }
    public string WebDatabase { get; }
    public string InstallationKey { get; }
    public IServiceProvider Services => _app.Services;
    public string SetupToken =>
        _logs.Lines.Select(line => SetupTokenLine().Match(line)).Last(match => match.Success).Groups["token"].Value;

    public static async Task<SignInHost> StartAsync(
        PostgresFixture postgres,
        TimeSpan? accessTokenLifetime = null,
        SignInHost? previous = null)
    {
        var port = FreePort();
        var address = new Uri($"http://127.0.0.1:{port}/");
        var identityDatabase = previous?.IdentityDatabase ?? await postgres.CreateDatabaseAsync();
        var webDatabase = previous?.WebDatabase ?? await postgres.CreateDatabaseAsync();
        var installationKey = previous?.InstallationKey ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var logs = new LogCapture();

        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions { EnvironmentName = Environments.Production });
        builder.WebHost.UseUrls(address.ToString());
        builder.WebHost.UseStaticWebAssets();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.AddProvider(logs);
        builder.Configuration["ConnectionStrings:identity"] = identityDatabase;
        builder.Configuration["ConnectionStrings:web"] = webDatabase;
        builder.Configuration["Flownodes:InstallationKey"] = installationKey;
        builder.Configuration["Flownodes:Identity:Authority"] = address.ToString();
        builder.Configuration["Flownodes:Identity:AllowInsecureTransport"] = "true";

        builder.AddApplication();
        builder.AddInfrastructure();
        builder.AddPresentation();
        builder.AddWeb();

        builder.Services.ConfigureWolverine(options => options.CodeGeneration.TypeLoadMode = TypeLoadMode.Dynamic);
        if (accessTokenLifetime is { } lifetime)
        {
            builder.Services.PostConfigure<OpenIddictServerOptions>(options => options.AccessTokenLifetime = lifetime);
        }

        var app = builder.Build();
        app.UseWebSession();
        app.UseInfrastructure();
        app.UsePresentation();
        app.MapMethods(
                ProbePath,
                ["GET", "POST", "PUT", "PATCH", "DELETE"],
                (HttpRequest request) => request.Headers.Authorization.ToString())
            .AllowAnonymous();
        app.UseWeb();

        await app.StartAsync();

        return new SignInHost(app, logs, address, identityDatabase, webDatabase, installationKey);
    }

    public Browser NewBrowser() => new(Address);

    public static async Task<long> CountAsync(string database, string table, CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(database);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand($"SELECT count(*) FROM {table}", connection);

        return (long)(await command.ExecuteScalarAsync(cancellationToken))!;
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    [GeneratedRegex(@"Setup token: (?<token>\S+)")]
    private static partial Regex SetupTokenLine();

    private sealed class LogCapture : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _lines = new();
        public IEnumerable<string> Lines => _lines;
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_lines);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(ConcurrentQueue<string> lines) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                lines.Enqueue(formatter(state, exception));
            }
        }
    }
}
