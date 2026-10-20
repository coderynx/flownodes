using System.Reflection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Engine.Infrastructure.Orleans;

/// <summary>
/// Applies the Orleans ADO.NET PostgreSQL scripts (main, clustering, persistence) once, before the silo starts.
/// </summary>
internal sealed class OrleansSchemaInitializer(string connectionString) : IHostedService
{
    private static readonly string[] Scripts =
    [
        "PostgreSQL-Main.sql",
        "PostgreSQL-Clustering.sql",
        "PostgreSQL-Persistence.sql"
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var lockCommand = new NpgsqlCommand("SELECT pg_advisory_xact_lock(7019243)", connection, transaction))
        {
            await lockCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var existsCommand = new NpgsqlCommand("SELECT to_regclass('orleansquery') IS NOT NULL", connection, transaction))
        {
            if ((bool)(await existsCommand.ExecuteScalarAsync(cancellationToken))!)
                return;
        }

        foreach (var script in Scripts)
        {
            await using var command = new NpgsqlCommand(await ReadScriptAsync(script), connection, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<string> ReadScriptAsync(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith(name, StringComparison.Ordinal));
        await using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync();
    }
}
