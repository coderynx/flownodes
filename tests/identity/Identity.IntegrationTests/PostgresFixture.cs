using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(Identity.IntegrationTests.PostgresFixture))]

namespace Identity.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly Lazy<Task<PostgreSqlContainer>> _container = new(StartContainerAsync);
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_container.IsValueCreated)
        {
            await (await _container.Value).DisposeAsync();
        }
    }

    public async Task<string> CreateDatabaseAsync()
    {
        var container = await _container.Value;
        var name = $"identity_{Guid.NewGuid():N}";

        var result = await container.ExecScriptAsync($"CREATE DATABASE {name}");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Creating database {name} failed: {result.Stderr}");
        }

        return container.GetConnectionString()
            .Replace("Database=postgres", $"Database={name}", StringComparison.Ordinal);
    }

    private static async Task<PostgreSqlContainer> StartContainerAsync()
    {
        var container = new PostgreSqlBuilder("postgres:17-alpine").WithCommand("-c", "max_connections=1000").Build();
        await container.StartAsync();

        return container;
    }
}
