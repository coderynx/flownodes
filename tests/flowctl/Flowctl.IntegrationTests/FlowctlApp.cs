using System.Collections.Concurrent;
using System.Text.Json;
using Core.Contracts.Identity;
using Engine.Application.Integrations;
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

namespace Flowctl.IntegrationTests;

/// <summary>
/// The Registry as the monolith runs it, on Postgres, with a stand-in for Identity beside it, and flowctl run in
/// process against it with its own profile file.
/// </summary>
public sealed class FlowctlApp : IAsyncDisposable
{
    public const string Server = "http://localhost/";
    private readonly WebApplication _app;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"flowctl-{Guid.NewGuid():N}");
    private readonly ConcurrentDictionary<Guid, long> _seqs = new();

    private FlowctlApp(WebApplication app, TestIdentity identity)
    {
        _app = app;
        Identity = identity;
        Directory.CreateDirectory(_directory);
    }

    public TestIdentity Identity { get; }
    public string ProfilePath => Path.Combine(_directory, "flowctl.json");
    public Dictionary<string, string> Variables { get; } = [];

    public static async Task<FlowctlApp> StartAsync(PostgresFixture postgres)
    {
        var identity = await TestIdentity.StartAsync();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["ConnectionStrings:registry"] = await postgres.CreateDatabaseAsync();
        builder.Configuration["Flownodes:Identity:Authority"] = identity.Address.ToString();

        builder.AddApplication();
        builder.AddInfrastructure();
        builder.AddPresentation();

        Engine.Application.Bootstrap.DependencyInjection.AddApplication(builder);
        Engine.Presentation.Bootstrap.DependencyInjection.AddPresentation(builder);

        builder.Services.AddSingleton<IResourceKind, WidgetKind>();
        builder.Services.AddSingleton<IInstalledIntegrations, InstalledIntegrationsStub>();
        builder.Services.ConfigureWolverine(options => options.CodeGeneration.TypeLoadMode = TypeLoadMode.Dynamic);

        var app = builder.Build();
        app.UseInfrastructure();
        app.UsePresentation();
        Engine.Presentation.Bootstrap.HostConfiguration.UsePresentation(app);
        identity.MapConnect(app);

        await app.StartAsync();

        return new FlowctlApp(app, identity);
    }

    public async Task<Guid> UserAsync(string username, params (string Role, string? Namespace)[] grants)
    {
        var userId = Guid.NewGuid();
        await PublishAsync(new UserRenamed(userId, username, NextSeq(userId)));

        foreach (var (role, @namespace) in grants)
        {
            object grant = role == "Administrator"
                ? new AdministratorGranted(userId, NextSeq(userId))
                : new RoleGranted(userId, @namespace!, role, NextSeq(userId));
            await PublishAsync(grant);
        }

        return userId;
    }

    public void SignIn(Guid userId) =>
        WriteProfile(
            new
            {
                server = Server,
                accessToken = Identity.IssueToken(userId),
                expiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
            });

    public async Task<Guid> EngineerAsync(string @namespace = "default")
    {
        if (@namespace != "default")
        {
            var administrator = await UserAsync($"admin-{@namespace}", ("Administrator", null));
            SignIn(administrator);
            var file = WriteFile(
                "namespace.yaml",
                $"""
                 apiVersion: flownodes.io/v1alpha1
                 kind: Namespace
                 metadata:
                   name: {@namespace}
                 """);
            await EventuallyAsync(async () => (await RunAsync("apply", "-f", file)).ExitCode == 0);
        }

        var engineer = await UserAsync("erin", ("Engineer", @namespace));
        SignIn(engineer);
        await EventuallyAsync(async () => (await RunAsync("get", "widgets", "-n", @namespace)).ExitCode == 0);

        return engineer;
    }

    public void WriteProfile(object profile) => File.WriteAllText(ProfilePath, JsonSerializer.Serialize(profile));
    public JsonElement ReadProfile() => JsonDocument.Parse(File.ReadAllText(ProfilePath)).RootElement.Clone();

    public string WriteFile(string name, string text)
    {
        var path = Path.Combine(_directory, name);
        File.WriteAllText(path, text);

        return path;
    }

    public async Task<Run> RunAsync(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var context = new CliContext(
            output,
            error,
            new StringReader(string.Empty),
            _app.GetTestServer().CreateHandler(),
            ProfilePath,
            name => Variables.GetValueOrDefault(name),
            Task.Delay,
            TimeProvider.System);

        var exitCode = await Cli.RunAsync(args, context);

        return new Run(exitCode, output.ToString(), error.ToString());
    }

    public Task PublishAsync<TMessage>(TMessage message) where TMessage : notnull =>
        _app.Services.GetRequiredService<IMessageBus>().PublishAsync(message).AsTask();

    public static async Task EventuallyAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);

        while (!await condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("The condition never held.");
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
        await Identity.DisposeAsync();
        NpgsqlConnection.ClearAllPools();
        Directory.Delete(_directory, true);
    }

    private long NextSeq(Guid userId) => _seqs.AddOrUpdate(userId, 1, (_, seq) => seq + 1);
}

public sealed record Run(int ExitCode, string Output, string Error);
