using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Core.Contracts.Identity;
using Engine.Application.Authorization;
using Engine.Application.Bootstrap;
using Engine.Application.Integrations;
using Engine.Domain.Integrations;
using Engine.Infrastructure.Authorization;
using Engine.Infrastructure.Messaging;
using Engine.Presentation.Bootstrap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Engine.IntegrationTests;

/// <summary>
/// The Engine's HTTP surface over one Silo with in-memory storage, with a stand-in for Identity beside it: tokens come
/// from a real HTTP issuer the Engine discovers, and Role Grants reach the Engine as the messages Identity would
/// publish (ADR-0026).
/// </summary>
public sealed class EngineApp : IAsyncDisposable
{
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

    public static async Task<EngineApp> StartAsync(string? alarmDatabase = null)
    {
        var identity = await TestIdentity.StartAsync();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["Flownodes:Identity:Authority"] = identity.Address.ToString();
        builder.Configuration["ResyncPeriod"] = TimeSpan.FromMinutes(15).ToString();
        if (alarmDatabase is not null)
        {
            builder.Configuration["AlarmDatabase"] = alarmDatabase;
        }

        builder.AddApplication();
        builder.AddPresentation();
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(FreePort(), FreePort());
            new EngineCluster.SiloConfigurator().Configure(silo);
            silo.ConfigureServices(services =>
            {
                services.AddSingleton<IRoleGrants, GrainRoleGrants>();
                services.AddSingleton<IInstalledIntegrations, NoIntegrations>();
            });
        });

        var app = builder.Build();
        app.UsePresentation();
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
        var grain = Grains.GetGrain<Infrastructure.Namespaces.INamespaceGrain>(@namespace);
        await grain.ApplyAsync(EngineCluster.NamespaceUid, 1);
        await grain.ResourceAppliedAsync(kind, name, EngineCluster.ResourceUid(kind, name), 1, seq, spec);
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _identity.IssueToken(userId));

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

    private sealed class NoIntegrations : IInstalledIntegrations
    {
        public IReadOnlyList<InstalledIntegration> All => [];
    }
}
