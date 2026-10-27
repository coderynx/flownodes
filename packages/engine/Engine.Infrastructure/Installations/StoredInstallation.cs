using System.Reflection;
using Engine.Application.Installations;
using Engine.Domain.Installations;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Engine.Infrastructure.Installations;

internal sealed class StoredInstallation(NpgsqlDataSource database) : IHostedService, IInstallationIdentity
{
    private const string Sql = """
                               SELECT pg_advisory_xact_lock(7019245);
                               CREATE TABLE IF NOT EXISTS installation (
                                   singleton boolean PRIMARY KEY DEFAULT true CHECK (singleton),
                                   id uuid NOT NULL);
                               INSERT INTO installation (singleton, id) VALUES (true, @id) ON CONFLICT DO NOTHING;
                               """;

    private const string ReadSql = "SELECT id FROM installation WHERE singleton";
    private Installation? _current;
    public Installation Current =>
        _current ?? throw new InvalidOperationException("The Installation is not loaded yet.");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var insert = new NpgsqlCommand(Sql, connection, transaction))
        {
            insert.Parameters.AddWithValue("id", Guid.NewGuid());
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var read = new NpgsqlCommand(ReadSql, connection, transaction);
        var id = (Guid)(await read.ExecuteScalarAsync(cancellationToken))!;
        await transaction.CommitAsync(cancellationToken);

        _current = new Installation(id, HostVersion());
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static string HostVersion() =>
        Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "unknown";
}
