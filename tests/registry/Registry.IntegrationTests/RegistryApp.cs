using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using Core.Contracts.Identity;
using JasperFx.CodeGeneration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Registry.Application.Bootstrap;
using Registry.Domain.Resources;
using Registry.Infrastructure.Bootstrap;
using Registry.Presentation.Bootstrap;
using Wolverine;
using Wolverine.Postgresql;

namespace Registry.IntegrationTests;

/// <summary>
/// The Registry as the monolith runs it, with a stand-in for Identity beside it: tokens come from a real HTTP issuer
/// the Registry discovers, and Role Grants reach the Registry as the messages Identity would publish (ADR-0026).
/// </summary>
public sealed class RegistryApp : IAsyncDisposable
{
    public const string AdministratorName = "root";
    private readonly WebApplication _app;
    private readonly TestIdentity _identity;
    private readonly ConcurrentDictionary<string, Guid> _users = new();
    private readonly ConcurrentDictionary<Guid, long> _seqs = new();

    private RegistryApp(WebApplication app, TestIdentity identity)
    {
        _app = app;
        _identity = identity;
        Client = app.GetTestClient();
        Anonymous = app.GetTestClient();
    }

    public Guid AdministratorId { get; private set; }
    public HttpClient Client { get; }
    public HttpClient Anonymous { get; }
    public IServiceProvider Services => _app.Services;
    public ResourceMessages Messages => _app.Services.GetRequiredService<ResourceMessages>();
    public IdentityRequests IdentityRequests => _app.Services.GetRequiredService<IdentityRequests>();

    public static async Task<RegistryApp> StartAsync(PostgresFixture postgres)
    {
        var identity = await TestIdentity.StartAsync();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["ConnectionStrings:registry"] = await postgres.CreateDatabaseAsync();
        builder.Configuration["Flownodes:InstallationKey"] =
            Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        builder.Configuration["Flownodes:Identity:Authority"] = identity.Address.ToString();

        builder.AddApplication();
        builder.AddInfrastructure();
        builder.AddPresentation();

        builder.Services.AddSingleton<ResourceMessages>();
        builder.Services.AddSingleton<IdentityRequests>();
        builder.Services.AddSingleton<IResourceKind, WidgetKind>();
        builder.Services.ConfigureWolverine(options =>
        {
            options.CodeGeneration.TypeLoadMode = TypeLoadMode.Dynamic;
            options.Discovery.IncludeAssembly(typeof(RegistryApp).Assembly);
            options.ListenToPostgresqlQueue("resources");
        });

        var app = builder.Build();
        app.UseInfrastructure();
        app.UsePresentation();

        await app.StartAsync();

        var running = new RegistryApp(app, identity);
        await running.SetUpAdministratorAsync();

        return running;
    }

    public HttpClient ClientFor(string accessToken)
    {
        var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    public async Task<Guid> CreateUserAsync(string username)
    {
        var userId = Guid.NewGuid();
        _users[username] = userId;
        await PublishAsync(new UserRenamed(userId, username, NextSeq(userId)));

        return userId;
    }

    public Guid UnreportedUser(string username)
    {
        var userId = Guid.NewGuid();
        _users[username] = userId;

        return userId;
    }

    public async Task GrantAsync(Guid userId, string role, string? @namespace)
    {
        if (role == "Administrator")
        {
            await PublishAsync(new AdministratorGranted(userId, NextSeq(userId)));
        }
        else
        {
            await PublishAsync(new RoleGranted(userId, @namespace!, role, NextSeq(userId)));
        }
    }

    public async Task RevokeAsync(Guid userId, string role, string? @namespace)
    {
        if (role == "Administrator")
        {
            await PublishAsync(new AdministratorRevoked(userId, NextSeq(userId)));
        }
        else
        {
            await PublishAsync(new RoleRevoked(userId, @namespace!, role, NextSeq(userId)));
        }
    }

    public async Task DisableAsync(Guid userId)
    {
        await PublishAsync(new UserDisabled(userId, NextSeq(userId)));
    }

    public async Task<(Guid Id, HttpClient Client)> MemberAsync(string username, string role, string @namespace)
    {
        var userId = await CreateUserAsync(username);
        await GrantAsync(userId, role, @namespace);

        return (userId, SignedIn(username));
    }

    public HttpClient SignedIn(string username) => ClientFor(_identity.IssueToken(_users[username]));

    public async Task CreateNamespaceAsync(string name, CancellationToken cancellationToken)
    {
        var response = await ApplyNamespaceAsync(Client, name, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public static Task<HttpResponseMessage> ApplyNamespaceAsync(
        HttpClient client,
        string name,
        CancellationToken cancellationToken) =>
        ApplyAsync(
            client,
            $"""
             apiVersion: flownodes.io/v1alpha1
             kind: Namespace
             metadata:
               name: {name}
             """,
            cancellationToken);

    public static Task<HttpResponseMessage> ApplyAsync(
        HttpClient client,
        string yaml,
        CancellationToken cancellationToken,
        string query = "") =>
        client.PostAsync($"/api/v1/registry/apply{query}", Yaml(yaml), cancellationToken);

    public static Task<HttpResponseMessage> DeleteManifestAsync(
        HttpClient client,
        string yaml,
        CancellationToken cancellationToken,
        string query = "") =>
        client.PostAsync($"/api/v1/registry/delete{query}", Yaml(yaml), cancellationToken);

    private static StringContent Yaml(string yaml) => new(yaml, System.Text.Encoding.UTF8, "application/yaml");

    public async Task JoinAsync(string @namespace, CancellationToken cancellationToken)
    {
        await GrantAsync(AdministratorId, "Operator", @namespace);
        await EventuallyAsync(
            async () => (await Client.GetAsync($"/api/v1/registry/namespaces/{@namespace}", cancellationToken))
                .IsSuccessStatusCode,
            cancellationToken);
    }

    public static async Task EventuallyAsync(Func<Task<bool>> condition, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(30));

        while (!await condition())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), timeout.Token);
        }
    }

    public static async Task<HttpStatusCode> StatusEventuallyAsync(
        Func<Task<HttpResponseMessage>> request,
        HttpStatusCode expected,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        var status = (await request()).StatusCode;

        while (status != expected && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            status = (await request()).StatusCode;
        }

        return status;
    }

    public Task PublishAsync<TMessage>(TMessage message) where TMessage : notnull =>
        _app.Services.GetRequiredService<IMessageBus>().PublishAsync(message).AsTask();

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        Anonymous.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _identity.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
    }

    private async Task SetUpAdministratorAsync()
    {
        AdministratorId = await CreateUserAsync(AdministratorName);
        await GrantAsync(AdministratorId, "Administrator", null);
        await GrantAsync(AdministratorId, "Engineer", "default");
        await GrantAsync(AdministratorId, "Operator", "default");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _identity.IssueToken(AdministratorId));
        await EventuallyAsync(
            async () => (await Client.GetAsync("/api/v1/registry/namespaces/default")).IsSuccessStatusCode,
            CancellationToken.None);
    }

    private long NextSeq(Guid userId) => _seqs.AddOrUpdate(userId, 1, (_, seq) => seq + 1);
}
