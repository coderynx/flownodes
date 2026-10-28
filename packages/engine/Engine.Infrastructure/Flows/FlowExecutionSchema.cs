using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Engine.Infrastructure.Flows;

internal sealed class FlowExecutionSchema(NpgsqlDataSource database) : IHostedService
{
    private const string Sql = """
                               SELECT pg_advisory_xact_lock(7019245);
                               CREATE TABLE IF NOT EXISTS flow_executions (
                                   id uuid PRIMARY KEY,
                                   namespace text NOT NULL,
                                   flow text NOT NULL,
                                   flow_uid uuid NOT NULL,
                                   generation bigint NOT NULL,
                                   spec text NOT NULL,
                                   trigger_node text NOT NULL,
                                   state text NOT NULL,
                                   current_node text NULL,
                                   variables text NOT NULL,
                                   visits text NOT NULL,
                                   steps integer NOT NULL,
                                   deadline timestamptz NULL,
                                   lateness_ms bigint NULL,
                                   error_code text NULL,
                                   error_message text NULL,
                                   error_node text NULL,
                                   error_attempts integer NULL,
                                   cancel_requested boolean NOT NULL,
                                   started_at timestamptz NOT NULL,
                                   updated_at timestamptz NOT NULL,
                                   finished_at timestamptz NULL);
                               CREATE INDEX IF NOT EXISTS flow_executions_flow_started
                                   ON flow_executions (namespace, flow, started_at DESC);
                               CREATE INDEX IF NOT EXISTS flow_executions_active
                                   ON flow_executions (namespace, flow, flow_uid) WHERE finished_at IS NULL;
                               CREATE INDEX IF NOT EXISTS flow_executions_finished
                                   ON flow_executions (finished_at) WHERE finished_at IS NOT NULL;
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
