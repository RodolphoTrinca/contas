using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Contas.PartnerDemo;

internal sealed class MessageHistory(IOptions<ConsumerOptions> options)
{
    private const int PageSize = 25;

    internal async Task Initialize(CancellationToken ct)
    {
        await using var db = await Open(ct);
        await using var command = db.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS messages (
                sequence INTEGER PRIMARY KEY,
                event_id TEXT NOT NULL UNIQUE,
                account_id TEXT NOT NULL,
                event_type TEXT NOT NULL,
                version INTEGER NOT NULL,
                occurred_at TEXT NOT NULL,
                received_at TEXT NOT NULL,
                processed_at TEXT,
                outcome TEXT,
                payload TEXT
            );
            """;
        await command.ExecuteNonQueryAsync(ct);

        command.CommandText = "SELECT 1 FROM pragma_table_info('messages') WHERE name = 'payload'";
        if (await command.ExecuteScalarAsync(ct) is null)
        {
            command.CommandText = "ALTER TABLE messages ADD COLUMN payload TEXT";
            await command.ExecuteNonQueryAsync(ct);
        }
    }

    internal async Task RecordReceipt(AccountNotification message, string payload, CancellationToken ct)
    {
        await using var db = await Open(ct);
        await using var command = db.CreateCommand();
        command.CommandText = """
            INSERT INTO messages (event_id, account_id, event_type, version, occurred_at, received_at, payload)
            VALUES ($eventId, $accountId, $type, $version, $occurredAt, $receivedAt, $payload)
            ON CONFLICT (event_id) DO NOTHING
            """;
        command.Parameters.AddWithValue("$eventId", message.EventId.ToString());
        command.Parameters.AddWithValue("$accountId", message.AccountId.ToString());
        command.Parameters.AddWithValue("$type", message.Type);
        command.Parameters.AddWithValue("$version", message.Version);
        command.Parameters.AddWithValue("$occurredAt", message.OccurredAt.ToUniversalTime().ToString("O"));
        command.Parameters.AddWithValue("$receivedAt", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$payload", payload);
        await command.ExecuteNonQueryAsync(ct);
    }

    internal async Task RecordProcessing(AccountNotification message, CancellationToken ct)
    {
        await using var db = await Open(ct);
        await using var command = db.CreateCommand();
        command.CommandText = """
            UPDATE messages SET processed_at = $processedAt, outcome = $outcome
            WHERE event_id = $eventId AND processed_at IS NULL
            """;
        command.Parameters.AddWithValue("$eventId", message.EventId.ToString());
        command.Parameters.AddWithValue("$processedAt", DateTimeOffset.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$outcome", message.Simulate(options.Value.Consumer));
        await command.ExecuteNonQueryAsync(ct);
    }

    internal async Task<IResult> Read(long? before, CancellationToken ct)
    {
        await using var db = await Open(ct);
        await using var command = db.CreateCommand();
        command.CommandText = """
            SELECT sequence, event_id, account_id, event_type, version, occurred_at, received_at, processed_at, outcome, payload
            FROM messages WHERE $before IS NULL OR sequence < $before
            ORDER BY sequence DESC LIMIT $limit
            """;
        command.Parameters.AddWithValue("$before", (object?)before ?? DBNull.Value);
        command.Parameters.AddWithValue("$limit", PageSize + 1);
        await using var reader = await command.ExecuteReaderAsync(ct);
        var items = new List<MessageHistoryItem>();
        while (await reader.ReadAsync(ct))
            items.Add(new(reader.GetInt64(0), Guid.Parse(reader.GetString(1)), Guid.Parse(reader.GetString(2)),
                reader.GetString(3), reader.GetInt64(4), reader.GetString(5), reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7), reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9)));

        return Results.Ok(new
        {
            consumer = options.Value.Consumer,
            items = items.Take(PageSize),
            nextBefore = items.Count > PageSize ? (long?)items[PageSize - 1].Sequence : null
        });
    }

    private async Task<SqliteConnection> Open(CancellationToken ct)
    {
        var settings = new SqliteConnectionStringBuilder { DataSource = options.Value.DatabasePath };
        var db = new SqliteConnection(settings.ToString());
        try
        {
            await db.OpenAsync(ct);
            return db;
        }
        catch
        {
            await db.DisposeAsync();
            throw;
        }
    }
}
