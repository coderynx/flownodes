using Engine.Application.Alarms;
using Engine.Domain.Alarms;
using Npgsql;

namespace Engine.Infrastructure.Alarms;

internal sealed class PostgresAlarmHistory(NpgsqlDataSource database) : IAlarmHistory
{
    private const string Columns =
        "id, asset, condition, priority, message, raised_at, active_count, acknowledged_at, acknowledged_by, " +
        "acknowledgement_comment, cleared_at, ended_at, outcome, data_point, acknowledged_by_name";

    private const string UpsertSql = """
                                     INSERT INTO alarms (id, namespace, asset, condition, data_point, priority, message, raised_at, active_count,
                                                         acknowledged_at, acknowledged_by, acknowledged_by_name, acknowledgement_comment, cleared_at, ended_at, outcome)
                                     VALUES (@id, @namespace, @asset, @condition, @data_point, @priority, @message, @raised_at, @active_count,
                                             @acknowledged_at, @acknowledged_by, @acknowledged_by_name, @acknowledgement_comment, @cleared_at, @ended_at, @outcome)
                                     ON CONFLICT (id) DO UPDATE SET
                                         priority = EXCLUDED.priority,
                                         message = EXCLUDED.message,
                                         active_count = EXCLUDED.active_count,
                                         acknowledged_at = EXCLUDED.acknowledged_at,
                                         acknowledged_by = EXCLUDED.acknowledged_by,
                                         acknowledged_by_name = EXCLUDED.acknowledged_by_name,
                                         acknowledgement_comment = EXCLUDED.acknowledgement_comment,
                                         cleared_at = EXCLUDED.cleared_at,
                                         ended_at = EXCLUDED.ended_at,
                                         outcome = EXCLUDED.outcome
                                     """;

    public async Task UpsertAsync(
        string @namespace,
        IReadOnlyList<Alarm> alarms,
        CancellationToken cancellationToken = default)
    {
        foreach (var alarm in alarms)
        {
            await using var command = database.CreateCommand(UpsertSql);
            command.Parameters.AddWithValue("id", alarm.Id);
            command.Parameters.AddWithValue("namespace", @namespace);
            command.Parameters.AddWithValue("asset", alarm.Asset);
            command.Parameters.AddWithValue("condition", alarm.Condition);
            command.Parameters.AddWithValue("data_point", Nullable(alarm.DataPoint));
            command.Parameters.AddWithValue("priority", (short)alarm.Priority);
            command.Parameters.AddWithValue("message", alarm.Message);
            command.Parameters.AddWithValue("raised_at", alarm.RaisedAt.ToUniversalTime());
            command.Parameters.AddWithValue("active_count", alarm.ActiveCount);
            command.Parameters.AddWithValue("acknowledged_at", Nullable(alarm.AcknowledgedAt?.ToUniversalTime()));
            command.Parameters.AddWithValue("acknowledged_by", Nullable(alarm.AcknowledgedBy));
            command.Parameters.AddWithValue("acknowledged_by_name", Nullable(alarm.AcknowledgedByName));
            command.Parameters.AddWithValue("acknowledgement_comment", Nullable(alarm.AcknowledgementComment));
            command.Parameters.AddWithValue("cleared_at", Nullable(alarm.ClearedAt?.ToUniversalTime()));
            command.Parameters.AddWithValue("ended_at", Nullable(alarm.EndedAt?.ToUniversalTime()));
            command.Parameters.AddWithValue("outcome", Nullable(alarm.Outcome?.ToString()));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Alarm>> ListLiveAsync(
        string @namespace,
        CancellationToken cancellationToken = default)
    {
        await using var command = database.CreateCommand(
            $"SELECT {Columns} FROM alarms WHERE namespace = @namespace AND ended_at IS NULL " +
            "ORDER BY priority DESC, raised_at, id");
        command.Parameters.AddWithValue("namespace", @namespace);

        return await ReadAsync(command, cancellationToken);
    }

    public async Task<Alarm?> FindAsync(string @namespace, Guid id, CancellationToken cancellationToken = default)
    {
        await using var command = database.CreateCommand(
            $"SELECT {Columns} FROM alarms WHERE namespace = @namespace AND id = @id");
        command.Parameters.AddWithValue("namespace", @namespace);
        command.Parameters.AddWithValue("id", id);

        return (await ReadAsync(command, cancellationToken)).SingleOrDefault();
    }

    public async Task<AlarmHistoryPage> PageAsync(
        string @namespace,
        string? asset,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AlarmPriority? priority,
        AlarmState? state,
        int offset,
        int limit,
        CancellationToken cancellationToken = default)
    {
        const string filter = "namespace = @namespace AND (@asset::text IS NULL OR asset = @asset) " +
                              "AND (@from::timestamptz IS NULL OR raised_at >= @from) " +
                              "AND (@to::timestamptz IS NULL OR raised_at < @to) " +
                              "AND (@priority::smallint IS NULL OR priority = @priority) " +
                              "AND (@state::text IS NULL OR " +
                              "COALESCE(outcome, CASE WHEN cleared_at IS NULL THEN 'Active' ELSE 'Cleared' END) = @state)";

        await using var count = database.CreateCommand($"SELECT count(*) FROM alarms WHERE {filter}");
        Filter(count, @namespace, asset, from, to, priority, state);
        var total = Convert.ToInt32(await count.ExecuteScalarAsync(cancellationToken));

        await using var page = database.CreateCommand(
            $"SELECT {Columns} FROM alarms WHERE {filter} ORDER BY raised_at DESC, id OFFSET @offset LIMIT @limit");
        Filter(page, @namespace, asset, from, to, priority, state);
        page.Parameters.AddWithValue("offset", offset);
        page.Parameters.AddWithValue("limit", limit);

        return new AlarmHistoryPage(await ReadAsync(page, cancellationToken), total);
    }

    public async Task PurgeAsync(DateTimeOffset endedBefore, CancellationToken cancellationToken = default)
    {
        await using var command = database.CreateCommand("DELETE FROM alarms WHERE ended_at < @cutoff");
        command.Parameters.AddWithValue("cutoff", endedBefore.ToUniversalTime());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void Filter(
        NpgsqlCommand command,
        string @namespace,
        string? asset,
        DateTimeOffset? from,
        DateTimeOffset? to,
        AlarmPriority? priority,
        AlarmState? state)
    {
        command.Parameters.AddWithValue("namespace", @namespace);
        command.Parameters.AddWithValue("asset", NpgsqlTypes.NpgsqlDbType.Text, Nullable(asset));
        command.Parameters.AddWithValue(
            "from",
            NpgsqlTypes.NpgsqlDbType.TimestampTz,
            Nullable(from?.ToUniversalTime()));
        command.Parameters.AddWithValue("to", NpgsqlTypes.NpgsqlDbType.TimestampTz, Nullable(to?.ToUniversalTime()));
        command.Parameters.AddWithValue("priority", NpgsqlTypes.NpgsqlDbType.Smallint, Nullable((short?)priority));
        command.Parameters.AddWithValue("state", NpgsqlTypes.NpgsqlDbType.Text, Nullable(state?.ToString()));
    }

    private static object Nullable(object? value) => value ?? DBNull.Value;

    private static async Task<IReadOnlyList<Alarm>> ReadAsync(
        NpgsqlCommand command,
        CancellationToken cancellationToken)
    {
        var alarms = new List<Alarm>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            alarms.Add(
                Alarm.Restore(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.IsDBNull(13) ? null : reader.GetString(13),
                    string.Empty,
                    reader.GetFieldValue<DateTimeOffset>(5),
                    (AlarmPriority)reader.GetInt16(3),
                    reader.GetString(4),
                    reader.GetInt32(6),
                    reader.IsDBNull(7) ? null : reader.GetFieldValue<DateTimeOffset>(7),
                    reader.IsDBNull(8) ? null : reader.GetGuid(8),
                    reader.IsDBNull(14) ? null : reader.GetString(14),
                    reader.IsDBNull(9) ? null : reader.GetString(9),
                    reader.IsDBNull(10) ? null : reader.GetFieldValue<DateTimeOffset>(10),
                    reader.IsDBNull(11) ? null : reader.GetFieldValue<DateTimeOffset>(11),
                    reader.IsDBNull(12) ? null : Enum.Parse<AlarmOutcome>(reader.GetString(12))));
        }

        return alarms;
    }
}
