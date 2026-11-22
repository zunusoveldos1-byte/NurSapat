using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Auth;
using NurSapat.Infrastructure.Sync.Local;
using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Services;

/// <summary>
/// Owns the SQLCipher key lifecycle and the generic Supabase mirror. Application
/// repositories may use the same connection factory for strongly typed access.
/// </summary>
public sealed class LocalDbService : ILocalDbService
{
    private static readonly HashSet<string> TrackingColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "_mirror_key", "_remote_json", "is_synced", "_sync_operation",
            "_local_updated_utc"
        };

    private readonly SqliteConnectionFactory _factory;
    private readonly DatabaseSaltStore _saltStore;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public LocalDbService(SqliteConnectionFactory factory, DatabaseSaltStore saltStore)
    {
        _factory = factory;
        _saltStore = saltStore;
    }

    public bool IsUnlocked => _factory.IsUnlocked;

    public async Task UnlockAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var salt = await _saltStore.GetOrCreateAsync(email, cancellationToken)
            .ConfigureAwait(false);
        _factory.Unlock(email, password, salt);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await SqliteSchema.EnsureCreatedAsync(connection, cancellationToken).ConfigureAwait(false);
        await EnsureMetadataAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    public async Task UnlockWithDerivedKeyAsync(
        string email,
        byte[] derivedKey,
        CancellationToken cancellationToken = default)
    {
        _factory.UnlockWithDerivedKey(email, derivedKey);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await SqliteSchema.EnsureCreatedAsync(connection, cancellationToken).ConfigureAwait(false);
        await EnsureMetadataAsync(connection, cancellationToken).ConfigureAwait(false);
    }

    public Task LockAsync()
    {
        _factory.Lock();
        return Task.CompletedTask;
    }

    public async Task EnsureMirrorSchemaAsync(
        IReadOnlyCollection<RemoteTableSchema> tables,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await EnsureMetadataAsync(connection, cancellationToken).ConfigureAwait(false);

            foreach (var table in tables)
            {
                ValidateIdentifier(table.Name);
                var quotedTable = Quote(table.Name);
                var primaryKeyCount = table.Columns.Count(column => column.IsPrimaryKey);
                var definitions = table.Columns
                    .Select(column =>
                    {
                        ValidateIdentifier(column.Name);
                        var primaryKey = primaryKeyCount == 1 && column.IsPrimaryKey
                            ? " PRIMARY KEY"
                            : string.Empty;
                        return $"{Quote(column.Name)} {NormalizeType(column.SqliteType)}{primaryKey}";
                    })
                    .ToList();
                definitions.Add("\"_mirror_key\" TEXT");
                definitions.Add("\"_remote_json\" TEXT NOT NULL DEFAULT '{}'");
                definitions.Add("\"is_synced\" INTEGER NOT NULL DEFAULT 1");
                definitions.Add("\"_sync_operation\" TEXT NOT NULL DEFAULT 'upsert'");
                definitions.Add("\"_local_updated_utc\" TEXT NOT NULL DEFAULT ''");

                await using (var create = connection.CreateCommand())
                {
                    create.CommandText =
                        $"CREATE TABLE IF NOT EXISTS {quotedTable} ({string.Join(", ", definitions)});";
                    await create.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                var existing = await GetColumnNamesAsync(connection, table.Name, cancellationToken)
                    .ConfigureAwait(false);
                foreach (var column in table.Columns)
                {
                    if (!existing.Contains(column.Name))
                    {
                        await AddColumnAsync(
                            connection,
                            table.Name,
                            column.Name,
                            NormalizeType(column.SqliteType),
                            cancellationToken).ConfigureAwait(false);
                    }
                }

                foreach (var tracking in new Dictionary<string, string>
                         {
                             ["_mirror_key"] = "TEXT",
                             ["_remote_json"] = "TEXT NOT NULL DEFAULT '{}'",
                             ["is_synced"] = "INTEGER NOT NULL DEFAULT 1",
                             ["_sync_operation"] = "TEXT NOT NULL DEFAULT 'upsert'",
                             ["_local_updated_utc"] = "TEXT NOT NULL DEFAULT ''"
                         })
                {
                    if (!existing.Contains(tracking.Key))
                    {
                        await AddColumnAsync(
                            connection,
                            table.Name,
                            tracking.Key,
                            tracking.Value,
                            cancellationToken).ConfigureAwait(false);
                    }
                }

                await using var metadata = connection.CreateCommand();
                metadata.CommandText = """
                    INSERT INTO _mirror_tables(table_name)
                    VALUES ($table)
                    ON CONFLICT(table_name) DO NOTHING;
                    """;
                metadata.Parameters.AddWithValue("$table", table.Name);
                await metadata.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await using var index = connection.CreateCommand();
                index.CommandText =
                    $"CREATE UNIQUE INDEX IF NOT EXISTS {Quote($"ix_{table.Name}_mirror_key")} " +
                    $"ON {quotedTable}(\"_mirror_key\");";
                await index.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task MergeRemoteRowsAsync(
        string tableName,
        IReadOnlyCollection<Dictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(tableName);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            await using (var prepareKeys = connection.CreateCommand())
            {
                prepareKeys.Transaction = (SqliteTransaction)transaction;
                prepareKeys.CommandText = """
                    CREATE TEMP TABLE IF NOT EXISTS _incoming_mirror_keys (
                        value TEXT NOT NULL PRIMARY KEY
                    );
                    DELETE FROM _incoming_mirror_keys;
                    """;
                await prepareKeys.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var row in rows)
            {
                var key = BuildKey(row);
                await using (var rememberKey = connection.CreateCommand())
                {
                    rememberKey.Transaction = (SqliteTransaction)transaction;
                    rememberKey.CommandText =
                        "INSERT OR IGNORE INTO _incoming_mirror_keys(value) VALUES ($key);";
                    rememberKey.Parameters.AddWithValue("$key", key);
                    await rememberKey.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }

                await using var command = connection.CreateCommand();
                command.Transaction = (SqliteTransaction)transaction;
                var writable = row.Keys.Where(keyName => !TrackingColumns.Contains(keyName)).ToList();
                foreach (var column in writable)
                {
                    ValidateIdentifier(column);
                }

                var columns = writable.Select(Quote).Concat(
                    ["\"_mirror_key\"", "\"_remote_json\"", "\"is_synced\"",
                     "\"_sync_operation\"", "\"_local_updated_utc\""]).ToList();
                var parameters = writable.Select((_, index) => $"$v{index}").Concat(
                    ["$key", "$json", "1", "'upsert'", "$updated"]).ToList();
                var updates = writable.Select((column, index) =>
                    $"{Quote(column)} = $v{index}").Concat(
                    ["_remote_json = $json", "is_synced = 1",
                     "_sync_operation = 'upsert'", "_local_updated_utc = $updated"]).ToList();

                command.CommandText =
                    $"INSERT INTO {Quote(tableName)} ({string.Join(",", columns)}) " +
                    $"VALUES ({string.Join(",", parameters)}) " +
                    "ON CONFLICT DO UPDATE SET " +
                    $"{string.Join(",", updates)} WHERE is_synced = 1;";

                for (var index = 0; index < writable.Count; index++)
                {
                    command.Parameters.AddWithValue(
                        $"$v{index}",
                        ToSqliteValue(row[writable[index]]));
                }

                command.Parameters.AddWithValue("$key", key);
                command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(row));
                command.Parameters.AddWithValue(
                    "$updated",
                    DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await using (var removeMissing = connection.CreateCommand())
            {
                removeMissing.Transaction = (SqliteTransaction)transaction;
                removeMissing.CommandText =
                    $"DELETE FROM {Quote(tableName)} WHERE is_synced = 1 " +
                    "AND NOT EXISTS (SELECT 1 FROM _incoming_mirror_keys incoming " +
                    $"WHERE incoming.value = {Quote(tableName)}._mirror_key);";
                await removeMissing.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertLocalAsync(
        string tableName,
        IReadOnlyDictionary<string, object?> values,
        string operation = "upsert",
        CancellationToken cancellationToken = default)
    {
        if (operation is not ("upsert" or "delete"))
        {
            throw new ArgumentOutOfRangeException(nameof(operation));
        }

        ValidateIdentifier(tableName);
        var copy = values.ToDictionary(pair => pair.Key, pair => pair.Value);
        var writable = copy.Keys.Where(keyName => !TrackingColumns.Contains(keyName)).ToList();
        foreach (var column in writable)
        {
            ValidateIdentifier(column);
        }

        var key = BuildKey(copy);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken)
                .ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            var columns = writable.Select(Quote).Concat(
                ["\"_mirror_key\"", "\"_remote_json\"", "\"is_synced\"",
                 "\"_sync_operation\"", "\"_local_updated_utc\""]).ToList();
            var parameters = writable.Select((_, index) => $"$v{index}").Concat(
                ["$key", "$json", "0", "$operation", "$updated"]).ToList();
            var updates = writable.Select((column, index) =>
                    $"{Quote(column)} = $v{index}")
                .Concat(
                    ["_remote_json = $json", "is_synced = 0",
                     "_sync_operation = $operation", "_local_updated_utc = $updated"])
                .ToList();

            command.CommandText =
                $"INSERT INTO {Quote(tableName)} ({string.Join(",", columns)}) " +
                $"VALUES ({string.Join(",", parameters)}) " +
                $"ON CONFLICT DO UPDATE SET {string.Join(",", updates)};";
            for (var index = 0; index < writable.Count; index++)
            {
                command.Parameters.AddWithValue(
                    $"$v{index}",
                    ToSqliteValue(copy[writable[index]]));
            }

            command.Parameters.AddWithValue("$key", key);
            command.Parameters.AddWithValue("$json", JsonSerializer.Serialize(copy));
            command.Parameters.AddWithValue("$operation", operation);
            command.Parameters.AddWithValue(
                "$updated",
                DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<PendingMirrorRow>> GetPendingRowsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = new List<PendingMirrorRow>();
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        var tables = new List<string>();
        await using (var tableCommand = connection.CreateCommand())
        {
            tableCommand.CommandText = "SELECT table_name FROM _mirror_tables ORDER BY table_name;";
            await using var reader = await tableCommand.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                tables.Add(reader.GetString(0));
            }
        }

        foreach (var table in tables)
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                $"SELECT _mirror_key, _sync_operation, _remote_json FROM {Quote(table)} " +
                "WHERE is_synced = 0 ORDER BY _local_updated_utc;";
            await using var reader = await command.ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, object?>>(
                                 reader.GetString(2))
                             ?? [];
                result.Add(new PendingMirrorRow(
                    table,
                    reader.GetString(0),
                    reader.GetString(1),
                    values));
            }
        }

        return result;
    }

    public async Task MarkSyncedAsync(
        string tableName,
        string localKey,
        CancellationToken cancellationToken = default)
    {
        ValidateIdentifier(tableName);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"UPDATE {Quote(tableName)} SET is_synced = 1 WHERE _mirror_key = $key;";
        command.Parameters.AddWithValue("$key", localKey);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureMetadataAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS _mirror_tables (
                table_name TEXT NOT NULL PRIMARY KEY
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<HashSet<string>> GetColumnNamesAsync(
        SqliteConnection connection,
        string table,
        CancellationToken cancellationToken)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({Quote(table)});";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            result.Add(reader.GetString(1));
        }

        return result;
    }

    private static async Task AddColumnAsync(
        SqliteConnection connection,
        string table,
        string column,
        string definition,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            $"ALTER TABLE {Quote(table)} ADD COLUMN {Quote(column)} {definition};";
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string BuildKey(IReadOnlyDictionary<string, object?> values)
    {
        if (values.TryGetValue("id", out var id) && id is not null)
        {
            return Convert.ToString(id, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        var canonical = string.Join(
            "\u001f",
            values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={JsonSerializer.Serialize(pair.Value)}"));
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(canonical)));
    }

    private static object ToSqliteValue(object? value)
    {
        if (value is null)
        {
            return DBNull.Value;
        }

        if (value is JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => DBNull.Value,
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number when element.TryGetInt64(out var integer) => integer,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => 1,
                JsonValueKind.False => 0,
                _ => element.GetRawText()
            };
        }

        return value switch
        {
            bool boolean => boolean ? 1 : 0,
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString("O"),
            Guid guid => guid.ToString("D"),
            _ => value
        };
    }

    private static string NormalizeType(string type) => type.ToUpperInvariant() switch
    {
        "INTEGER" => "INTEGER",
        "REAL" => "REAL",
        "BLOB" => "BLOB",
        _ => "TEXT"
    };

    private static void ValidateIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) ||
            identifier.Length > 255 ||
            identifier.Contains('\0'))
        {
            throw new InvalidOperationException($"Unsafe SQL identifier: {identifier}");
        }
    }

    private static string Quote(string identifier)
    {
        ValidateIdentifier(identifier);
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}
