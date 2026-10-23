using JasperFx.CodeGeneration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Registry.Application.Bootstrap;
using Registry.Infrastructure.Bootstrap;
using Registry.Presentation.Bootstrap;
using Wolverine;
using Wolverine.Postgresql;

namespace Registry.Api.Tests;

public sealed class RegistryApp : IAsyncDisposable
{
    private readonly WebApplication _app;

    private RegistryApp(WebApplication app)
    {
        _app = app;
        Client = app.GetTestClient();
    }

    public HttpClient Client { get; }
    public IServiceProvider Services => _app.Services;
    public ResourceMessages Messages => _app.Services.GetRequiredService<ResourceMessages>();

    public static async Task<RegistryApp> StartAsync(PostgresFixture postgres)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration["ConnectionStrings:registry"] = await postgres.CreateDatabaseAsync();

        builder.AddApplication();
        builder.AddInfrastructure();
        builder.AddPresentation();

        builder.Services.AddSingleton<ResourceMessages>();
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

        return new RegistryApp(app);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.DisposeAsync();
    }
}
