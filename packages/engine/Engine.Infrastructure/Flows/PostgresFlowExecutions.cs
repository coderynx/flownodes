using Engine.Application.Flows;
using Engine.Domain.Flows;
using Npgsql;
using NpgsqlTypes;

namespace Engine.Infrastructure.Flows;

internal sealed class PostgresFlowExecutions(NpgsqlDataSource database) : IFlowExecutions
{
    private const string Columns =
        "id, namespace, flow, flow_uid, generation, spec, trigger_node, state, current_node, variables, visits, " +
        "steps, deadline, lateness_ms, error_code, error_message, error_node, error_attempts, cancel_requested, " +
        "started_at, updated_at, finished_at";

    private const string UpsertSql = $"""
                                      INSERT INTO flow_executions ({Columns})
                                      VALUES (@id, @namespace, @flow, @flow_uid, @generation, @spec, @trigger_node, @state,
                                              @current_node, @variables, @visits, @steps, @deadline, @lateness_ms, @error_code,
                                              @error_message, @error_node, @error_attempts, @cancel_requested, @started_at,
                                              @updated_at, @finished_at)
                                      ON CONFLICT (id) DO UPDATE SET
                                          state = EXCLUDED.state,
                                          current_node = EXCLUDED.current_node,
                                          variables = EXCLUDED.variables,
                                          visits = EXCLUDED.visits,
                                          steps = EXCLUDED.steps,
                                          deadline = EXCLUDED.deadline,
                                          lateness_ms = EXCLUDED.lateness_ms,
                                          error_code = EXCLUDED.error_code,
                                          error_message = EXCLUDED.error_message,
                                          error_node = EXCLUDED.error_node,
                                          error_attempts = EXCLUDED.error_attempts,
                                          cancel_requested = EXCLUDED.finished_at IS NULL AND
                                              (flow_executions.cancel_requested OR EXCLUDED.cancel_requested),
                                          updated_at = EXCLUDED.updated_at,
                                          finished_at = EXCLUDED.finished_at
                                      WHERE flow_executions.finished_at IS NULL
                                      RETURNING cancel_requested
                                      """;

    public async Task<bool> SaveAsync(FlowExecution execution, CancellationToken cancellationToken = default)
    {
        await using var command = database.CreateCommand(UpsertSql);
        command.Parameters.AddWithValue("id", execution.Id);
        command.Parameters.AddWithValue("namespace", execution.Namespace);
        command.Parameters.AddWithValue("flow", execution.Flow);
        command.Parameters.AddWithValue("flow_uid", execution.FlowUid);
        command.Parameters.AddWithValue("generation", execution.Generation);
        command.Parameters.AddWithValue("spec", execution.Spec);
        command.Parameters.AddWithValue("trigger_node", execution.Trigger);
        command.Parameters.AddWithValue("state", execution.State.ToString());
        command.Parameters.AddWithValue("current_node", NpgsqlDbType.Text, Nullable(execution.CurrentNode));
        command.Parameters.AddWithValue("variables", execution.Variables);
        command.Parameters.AddWithValue("visits", System.Text.Json.JsonSerializer.Serialize(execution.Visits));
        command.Parameters.AddWithValue("steps", execution.Steps);
        command.Parameters.AddWithValue(
            "deadline",
            NpgsqlDbType.TimestampTz,
            Nullable(execution.Deadline?.ToUniversalTime()));
        command.Parameters.AddWithValue(
            "lateness_ms",
            NpgsqlDbType.Bigint,
            Nullable((long?)execution.Lateness?.TotalMilliseconds));
        command.Parameters.AddWithValue("error_code", NpgsqlDbType.Text, Nullable(execution.Error?.Code));
        command.Parameters.AddWithValue("error_message", NpgsqlDbType.Text, Nullable(execution.Error?.Message));
        command.Parameters.AddWithValue("error_node", NpgsqlDbType.Text, Nullable(execution.Error?.Node));
        command.Parameters.AddWithValue("error_attempts", NpgsqlDbType.Integer, Nullable(execution.Error?.Attempts));
        command.Parameters.AddWithValue("cancel_requested", execution.CancelRequested);
        command.Parameters.AddWithValue("started_at", execution.StartedAt.ToUniversalTime());
        command.Parameters.AddWithValue("updated_at", execution.UpdatedAt.ToUniversalTime());
        command.Parameters.AddWithValue(
            "finished_at",
            NpgsqlDbType.TimestampTz,
            Nullable(execution.FinishedAt?.ToUniversalTime()));

        return await command.ExecuteScalarAsync(cancellationToken) is true;
    }

    public async Task<FlowExecution?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var command = database.CreateCommand($"SELECT {Columns} FROM flow_executions WHERE id = @id");
        command.Parameters.AddWithValue("id", id);

        return (await ReadAsync(command, cancellationToken)).SingleOrDefault();
    }

    public async Task<IReadOnlyList<Guid>> ListActiveAsync(
        string @namespace,
        string flow,
        Guid flowUid,
        CancellationToken cancellationToken = default)
    {
        await using var command = database.CreateCommand(
            "SELECT id FROM flow_executions " +
            "WHERE namespace = @namespace AND flow = @flow AND flow_uid = @flow_uid AND finished_at IS NULL");
        Scope(command, @namespace, flow, flowUid);

        return await IdsAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> RequestCancelAsync(
        string @namespace,
        string flow,
        Guid flowUid,
        CancellationToken cancellationToken = default)
    {
        await using var command = database.CreateCommand(
            "UPDATE flow_executions SET cancel_requested = true " +
            "WHERE namespace = @namespace AND flow = @flow AND flow_uid = @flow_uid AND finished_at IS NULL " +
            "RETURNING id");
        Scope(command, @namespace, flow, flowUid);

        return await IdsAsync(command, cancellationToken);
    }

    public async Task<FlowExecutionPage> PageAsync(
        string @namespace,
        string flow,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string filter = "namespace = @namespace AND flow = @flow";
        await using var count = database.CreateCommand($"SELECT count(*) FROM flow_executions WHERE {filter}");
        count.Parameters.AddWithValue("namespace", @namespace);
        count.Parameters.AddWithValue("flow", flow);
        var total = Convert.ToInt32(await count.ExecuteScalarAsync(cancellationToken));

        await using var page = database.CreateCommand(
            $"SELECT {Columns} FROM flow_executions WHERE {filter} " +
            "ORDER BY started_at DESC, id DESC OFFSET @offset LIMIT @limit");
        page.Parameters.AddWithValue("namespace", @namespace);
        page.Parameters.AddWithValue("flow", flow);
        page.Parameters.AddWithValue("offset", offset);
        page.Parameters.AddWithValue("limit", limit);

        return new FlowExecutionPage(await ReadAsync(page, cancellationToken), total);
    }

    public async Task PurgeAsync(DateTimeOffset finishedBefore, int keep, CancellationToken cancellationToken = default)
    {
        await using var command = database.CreateCommand(
            """
            DELETE FROM flow_executions doomed
            USING (SELECT id, row_number() OVER (PARTITION BY namespace, flow ORDER BY finished_at DESC, id DESC) AS rank
                   FROM flow_executions WHERE finished_at IS NOT NULL) ranked
            WHERE doomed.id = ranked.id AND ranked.rank > @keep AND doomed.finished_at < @cutoff
            """);
        command.Parameters.AddWithValue("keep", keep);
        command.Parameters.AddWithValue("cutoff", finishedBefore.ToUniversalTime());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Scope(NpgsqlCommand command, string @namespace, string flow, Guid flowUid)
    {
        command.Parameters.AddWithValue("namespace", @namespace);
        command.Parameters.AddWithValue("flow", flow);
        command.Parameters.AddWithValue("flow_uid", flowUid);
    }

    private static object Nullable(object? value) => value ?? DBNull.Value;

    private static async Task<IReadOnlyList<Guid>> IdsAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var ids = new List<Guid>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    private static async Task<IReadOnlyList<FlowExecution>> ReadAsync(
        NpgsqlCommand command,
        CancellationToken cancellationToken)
    {
        var executions = new List<FlowExecution>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var error = reader.IsDBNull(14)
                ? null
                : new FlowExecutionError(
                    reader.GetString(14),
                    reader.GetString(15),
                    reader.GetString(16),
                    reader.GetInt32(17));
            executions.Add(
                FlowExecution.Restore(
                    new FlowExecutionCheckpoint(
                        reader.GetGuid(0),
                        reader.GetString(1),
                        reader.GetString(2),
                        reader.GetGuid(3),
                        reader.GetInt64(4),
                        reader.GetString(5),
                        reader.GetString(6),
                        Enum.Parse<FlowExecutionState>(reader.GetString(7)),
                        reader.IsDBNull(8) ? null : reader.GetString(8),
                        reader.GetString(9),
                        reader.GetString(10),
                        reader.GetInt32(11),
                        reader.IsDBNull(12) ? null : reader.GetFieldValue<DateTimeOffset>(12),
                        reader.IsDBNull(13) ? null : TimeSpan.FromMilliseconds(reader.GetInt64(13)),
                        error,
                        reader.GetBoolean(18),
                        reader.GetFieldValue<DateTimeOffset>(19),
                        reader.GetFieldValue<DateTimeOffset>(20),
                        reader.IsDBNull(21) ? null : reader.GetFieldValue<DateTimeOffset>(21))));
        }

        return executions;
    }
}
