using Testcontainers.PostgreSql;
using Xunit;

[assembly: AssemblyFixture(typeof(Identity.IntegrationTests.PostgresFixture))]

namespace Identity.IntegrationTests;

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithCommand("-c", "max_connections=1000")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"identity_{Guid.NewGuid():N}";

        var result = await _container.ExecScriptAsync($"CREATE DATABASE {name}");
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Creating database {name} failed: {result.Stderr}");
        }

        return _container.GetConnectionString()
            .Replace("Database=postgres", $"Database={name}", StringComparison.Ordinal);
    }
}
