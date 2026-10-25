using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Core.Contracts.Identity;
using Identity.Application.Bootstrap;
using Identity.Infrastructure.Bootstrap;
using Identity.Presentation.Bootstrap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Wolverine;
using Wolverine.Postgresql;

namespace Identity.IntegrationTests;

public sealed partial class IdentityApp : IAsyncDisposable
{
    public const string AdministratorName = "root";
    public const string AdministratorPassword = "correct horse battery staple";
    public const string DeviceGrant = "urn:ietf:params:oauth:grant-type:device_code";
    public const string TokenExchangeGrant = "urn:ietf:params:oauth:grant-type:token-exchange";
    public const string PersonalAccessTokenType = "urn:flownodes:params:oauth:token-type:personal-access-token";
    public const string ClientId = "flowctl";
    private readonly WebApplication _app;
    private readonly LogCapture _logs;
    private bool _administratorCreated;

    private IdentityApp(WebApplication app, LogCapture logs)
    {
        _app = app;
        _logs = logs;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }
    public IServiceProvider Services => _app.Services;
    public IdentityMessages Messages => _app.Services.GetRequiredService<IdentityMessages>();
    public string? SetupToken => _logs.SetupToken;
    public string ConnectionString { get; private init; } = string.Empty;

    public static async Task<IdentityApp> StartAsync(
        PostgresFixture postgres,
        string? installationKey = null,
        string? connectionString = null)
    {
        connectionString ??= await postgres.CreateDatabaseAsync();
        var logs = new LogCapture();
        var app = Build(connectionString, installationKey, logs);

        await app.StartAsync();

        return new IdentityApp(app, logs)
        {
            ConnectionString = connectionString
        };
    }

    internal static WebApplication Build(string connectionString, string? installationKey, ILoggerProvider? logs = null)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        if (logs is not null)
        {
            builder.Logging.AddProvider(logs);
        }

        builder.Configuration["ConnectionStrings:identity"] = connectionString;
        builder.Configuration["Flownodes:InstallationKey"] =
            installationKey ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        builder.Configuration["Flownodes:Identity:AllowInsecureTransport"] = "true";

        builder.AddApplication();
        builder.AddInfrastructure();
        builder.AddPresentation();

        builder.Services.AddSingleton<IdentityMessages>();
        builder.Services.ConfigureWolverine(options =>
        {
            options.Discovery.IncludeAssembly(typeof(IdentityApp).Assembly);
            options.PublishMessage<RoleGranted>().ToPostgresqlQueue("identity-test");
            options.PublishMessage<RoleRevoked>().ToPostgresqlQueue("identity-test");
            options.PublishMessage<AdministratorGranted>().ToPostgresqlQueue("identity-test");
            options.PublishMessage<AdministratorRevoked>().ToPostgresqlQueue("identity-test");
            options.PublishMessage<UserDisabled>().ToPostgresqlQueue("identity-test");
            options.PublishMessage<UserEnabled>().ToPostgresqlQueue("identity-test");
            options.PublishMessage<UserDeleted>().ToPostgresqlQueue("identity-test");
            options.PublishMessage<UserRenamed>().ToPostgresqlQueue("identity-test");
            options.ListenToPostgresqlQueue("identity-test");
        });

        var app = builder.Build();
        app.UseInfrastructure();
        app.UsePresentation();

        return app;
    }

    public Browser NewBrowser() => new(_app.GetTestServer().CreateHandler());

    public HttpClient ClientFor(string accessToken)
    {
        var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    public static async Task<HttpResponseMessage> CompleteSetupAsync(
        Browser browser,
        string setupToken,
        string username,
        string password,
        CancellationToken cancellationToken) =>
        await browser.PostFormAsync(
            "/signin",
            "/signin?handler=Setup",
            new Dictionary<string, string>
            {
                ["SetupToken"] = setupToken,
                ["Username"] = username,
                ["Password"] = password
            },
            cancellationToken);

    public async Task<HttpClient> AdministratorAsync(CancellationToken cancellationToken)
    {
        if (!_administratorCreated && SetupToken is { } setupToken)
        {
            _administratorCreated = true;
            using var browser = NewBrowser();
            var setup = await CompleteSetupAsync(
                browser,
                setupToken,
                AdministratorName,
                AdministratorPassword,
                cancellationToken);
            setup.StatusCode.ShouldBe(System.Net.HttpStatusCode.Redirect);
        }

        var tokens = await SignInWithDeviceFlowAsync(AdministratorName, AdministratorPassword, cancellationToken);

        return ClientFor(tokens.AccessToken);
    }

    public static async Task<Guid> CreateUserAsync(
        HttpClient administrator,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var response = await administrator.PostAsJsonAsync(
            "/api/v1/identity/users",
            new { username, password },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedUser>(cancellationToken);

        return created!.Id;
    }

    public async Task<HttpClient> SignedInAsync(string username, string password, CancellationToken cancellationToken)
    {
        var tokens = await SignInWithDeviceFlowAsync(username, password, cancellationToken);

        return ClientFor(tokens.AccessToken);
    }

    public async Task<TokenSet> SignInWithDeviceFlowAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        var authorization = await Client.PostAsync(
            "/connect/device",
            new FormUrlEncodedContent(
                new Dictionary<string, string>
                {
                    ["client_id"] = ClientId,
                    ["scope"] = "offline_access"
                }),
            cancellationToken);
        authorization.EnsureSuccessStatusCode();
        var device = await authorization.Content.ReadFromJsonAsync<DeviceAuthorization>(cancellationToken);

        using var browser = NewBrowser();
        var signIn = await browser.PostFormAsync(
            "/signin",
            "/signin",
            new Dictionary<string, string>
            {
                ["Username"] = username,
                ["Password"] = password
            },
            cancellationToken);
        signIn.StatusCode.ShouldBe(System.Net.HttpStatusCode.Redirect);

        var verify = await browser.PostFormAsync(
            $"/connect/verify?user_code={device!.UserCode}",
            "/connect/verify",
            new Dictionary<string, string>
            {
                ["user_code"] = device.UserCode,
                ["Decision"] = "accept"
            },
            cancellationToken);
        verify.StatusCode.ShouldBe(System.Net.HttpStatusCode.Redirect);

        var response = await PostTokenAsync(
            new Dictionary<string, string>
            {
                ["grant_type"] = DeviceGrant,
                ["device_code"] = device.DeviceCode,
                ["client_id"] = ClientId
            },
            cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<TokenSet>(cancellationToken))!;
    }

    public Task<HttpResponseMessage> PostTokenAsync(
        IDictionary<string, string> form,
        CancellationToken cancellationToken) =>
        Client.PostAsync("/connect/token", new FormUrlEncodedContent(form), cancellationToken);

    public Task PublishAsync<TMessage>(TMessage message) where TMessage : notnull =>
        _app.Services.GetRequiredService<IMessageBus>().PublishAsync(message).AsTask();

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
    }

    public sealed record CreatedUser(Guid Id);

    public sealed record TokenSet(
        [property: JsonPropertyName("access_token")]
        string AccessToken,
        [property: JsonPropertyName("refresh_token")]
        string? RefreshToken);

    public sealed record DeviceAuthorization(
        [property: JsonPropertyName("device_code")]
        string DeviceCode,
        [property: JsonPropertyName("user_code")]
        string UserCode);

    private sealed partial class LogCapture : ILoggerProvider
    {
        private readonly ConcurrentQueue<string> _lines = new();
        public string? SetupToken =>
            _lines.Select(line => SetupTokenLine().Match(line))
                .Where(match => match.Success)
                .Select(match => match.Groups["token"].Value)
                .LastOrDefault();
        public ILogger CreateLogger(string categoryName) => new CapturingLogger(_lines);

        public void Dispose()
        {
        }

        [GeneratedRegex(@"Setup token: (?<token>\S+)")]
        private static partial Regex SetupTokenLine();

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
