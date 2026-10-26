using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Engine.Infrastructure.Alarms;

/// <summary>
/// Creates the Alarm history table at start, where it is missing.
/// </summary>
internal sealed class AlarmSchema(NpgsqlDataSource database) : IHostedService
{
    private const string Sql = """
                               SELECT pg_advisory_xact_lock(7019244);
                               CREATE TABLE IF NOT EXISTS alarms (
                                   id uuid PRIMARY KEY,
                                   namespace text NOT NULL,
                                   asset text NOT NULL,
                                   condition text NOT NULL,
                                   priority smallint NOT NULL,
                                   message text NOT NULL,
                                   raised_at timestamptz NOT NULL,
                                   active_count integer NOT NULL,
                                   acknowledged_at timestamptz NULL,
                                   acknowledged_by uuid NULL,
                                   acknowledgement_comment text NULL,
                                   cleared_at timestamptz NULL,
                                   ended_at timestamptz NULL,
                                   outcome text NULL);
                               CREATE INDEX IF NOT EXISTS alarms_namespace_raised ON alarms (namespace, raised_at DESC);
                               CREATE INDEX IF NOT EXISTS alarms_live ON alarms (namespace) WHERE ended_at IS NULL;
                               CREATE INDEX IF NOT EXISTS alarms_ended ON alarms (ended_at) WHERE ended_at IS NOT NULL;
                               """;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(Sql, connection, transaction);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
