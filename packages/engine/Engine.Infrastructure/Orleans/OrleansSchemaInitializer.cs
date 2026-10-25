using System.Reflection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Engine.Infrastructure.Orleans;

internal sealed class OrleansSchemaInitializer(string connectionString) : IHostedService
{
    private const string LockSql = "SELECT pg_advisory_xact_lock(7019243)";
    private const string ExistsSql = "SELECT to_regclass(@table) IS NOT NULL";

    private static readonly (string Script, string Table)[] Scripts =
    [
        ("PostgreSQL-Main.sql", "orleansquery"),
        ("PostgreSQL-Clustering.sql", "orleansmembershiptable"),
        ("PostgreSQL-Persistence.sql", "orleansstorage"),
        ("PostgreSQL-Reminders.sql", "orleansreminderstable")
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var lockCommand = new NpgsqlCommand(LockSql, connection, transaction))
        {
            await lockCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var (script, table) in Scripts)
        {
            if (await ExistsAsync(connection, transaction, table, cancellationToken))
            {
                continue;
            }

            await using var command = new NpgsqlCommand(await ReadScriptAsync(script), connection, transaction);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<bool> ExistsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string table,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(ExistsSql, connection, transaction);
        command.Parameters.AddWithValue("table", table);

        return (bool)(await command.ExecuteScalarAsync(cancellationToken))!;
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
