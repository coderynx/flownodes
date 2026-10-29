using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text.Json;
using Core.Contracts.Identity;
using Core.Contracts.Resources;
using Engine.Application.Authorization;
using Engine.Application.Bootstrap;
using Engine.Application.Installations;
using Engine.Application.Integrations;
using Engine.Domain.Installations;
using Engine.Domain.Integrations;
using Engine.Infrastructure.Authorization;
using Engine.Infrastructure.Bootstrap;
using Engine.Infrastructure.Messaging;
using Engine.Presentation.Bootstrap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Dashboard;

namespace Engine.IntegrationTests;

/// <summary>
/// The Engine's HTTP surface over one Silo with in-memory storage, with a stand-in for Identity beside it: tokens come
/// from a real HTTP issuer the Engine discovers, and Role Grants reach the Engine as the messages Identity would
/// publish (ADR-0026).
/// </summary>
public sealed class EngineApp : IAsyncDisposable
{
    public static readonly Guid InstallationId = Guid.NewGuid();
    private readonly WebApplication _app;
    private readonly TestIdentity _identity;
    private readonly Dictionary<Guid, long> _seqs = [];

    private EngineApp(WebApplication app, TestIdentity identity)
    {
        _app = app;
        _identity = identity;
        Anonymous = app.GetTestClient();
    }

    public HttpClient Anonymous { get; }

    public static async Task<EngineApp> StartAsync(string? alarmDatabase = null, string environment = "Production")
    {
        var identity = await TestIdentity.StartAsync();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseTestServer();
        builder.Host.UseDefaultServiceProvider(options => options.ValidateOnBuild = false);
        builder.Configuration["Flownodes:Identity:Authority"] = identity.Address.ToString();
        builder.Configuration["ResyncPeriod"] = TimeSpan.FromMinutes(15).ToString();
        if (alarmDatabase is not null)
        {
            builder.Configuration["AlarmDatabase"] = alarmDatabase;
        }

        builder.Services.AddHealthChecks();
        builder.AddApplication();
        builder.AddPresentation();
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(FreePort(), FreePort());
            silo.AddDashboard(_ => { });
            new EngineCluster.SiloConfigurator().Configure(silo);
            silo.ConfigureServices(services =>
            {
                services.AddSingleton<IRoleGrants, GrainRoleGrants>();
                services.AddSingleton<IInstalledIntegrations, NoIntegrations>();
                services.AddSingleton<IInstallationIdentity, FixedInstallation>();
            });
        });

        var app = builder.Build();
        app.UsePresentation();
        app.UseInfrastructure();
        await app.StartAsync();

        return new EngineApp(app, identity);
    }

    public Task GrantAsync(Guid userId, string role, string @namespace) =>
        IdentityMessageHandler.Handle(new RoleGranted(userId, @namespace, role, NextSeq(userId)), Grains);

    public Task RevokeAsync(Guid userId, string role, string @namespace) =>
        IdentityMessageHandler.Handle(new RoleRevoked(userId, @namespace, role, NextSeq(userId)), Grains);

    public Task DisableAsync(Guid userId) =>
        IdentityMessageHandler.Handle(new UserDisabled(userId, NextSeq(userId)), Grains);

    public Task DeleteAsync(Guid userId) =>
        IdentityMessageHandler.Handle(new UserDeleted(userId, NextSeq(userId)), Grains);

    public async Task ApplyAsync(string @namespace, string kind, string name, string spec, long seq)
    {
        await NamespaceMessageHandler.Handle(
            new ResourceApplied(
                @namespace,
                "Namespace",
                @namespace,
                EngineCluster.NamespaceUid,
                1,
                1,
                JsonDocument.Parse("{}").RootElement),
            Grains);
        await NamespaceMessageHandler.Handle(
            new ResourceApplied(
                @namespace,
                kind,
                name,
                EngineCluster.ResourceUid(kind, name),
                1,
                seq,
                JsonDocument.Parse(spec).RootElement),
            Grains);
    }

    public async Task<(Guid Id, HttpClient Client)> MemberAsync(string role, string @namespace)
    {
        var userId = Guid.NewGuid();
        await GrantAsync(userId, role, @namespace);

        return (userId, SignedIn(userId));
    }

    public HttpClient SignedIn(Guid userId)
    {
        var client = _app.GetTestClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _identity.IssueToken(userId));

        return client;
    }

    public async ValueTask DisposeAsync()
    {
        Anonymous.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
        await _identity.DisposeAsync();
    }

    private IGrainFactory Grains => _app.Services.GetRequiredService<IGrainFactory>();

    private long NextSeq(Guid userId)
    {
        _seqs[userId] = _seqs.GetValueOrDefault(userId) + 1;

        return _seqs[userId];
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        return port;
    }

    private sealed class FixedInstallation : IInstallationIdentity
    {
        public Installation Current { get; } = new(InstallationId, "0.1.0");
    }

    private sealed class NoIntegrations : IInstalledIntegrations
    {
        public IReadOnlyList<InstalledIntegration> All => [];
    }
}
