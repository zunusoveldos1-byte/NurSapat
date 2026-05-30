using System.IO.Compression;
using System.Text.Json;
using Dapper;
using NurSapat.Core.Models;

namespace NurSapat.Data;

public sealed class DataMigrationService
{
    private const string BackupEntryName = "nursapat-backup.json";
    private readonly DatabaseService _db;

    public DataMigrationService(DatabaseService db)
    {
        _db = db;
    }

    public async Task ExportDataAsync(string targetFilePath)
    {
        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();

        var snapshot = new CommercialDataSnapshot
        {
            ExportedAt = DateTimeOffset.UtcNow,
            Materials = (await connection.QueryAsync<JsonRow>("SELECT to_jsonb(m.*)::text AS data FROM materials m ORDER BY article")).ToList(),
            Customers = (await connection.QueryAsync<JsonRow>("SELECT to_jsonb(c.*)::text AS data FROM customers c ORDER BY name")).ToList(),
            Orders = (await connection.QueryAsync<JsonRow>("SELECT to_jsonb(o.*)::text AS data FROM orders o ORDER BY created_at")).ToList(),
            OrderItems = (await connection.QueryAsync<JsonRow>("SELECT to_jsonb(oi.*)::text AS data FROM order_items oi ORDER BY created_at")).ToList()
        };

        var json = JsonSerializer.Serialize(snapshot, JsonOptions);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(targetFilePath))!);

        if (Path.GetExtension(targetFilePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
        {
            await WriteZipAsync(targetFilePath, json);
        }
        else
        {
            await File.WriteAllTextAsync(targetFilePath, json);
        }

        await LogAsync(connection, "system", "ExportData", $"Commercial export created: {targetFilePath}");
    }

    public async Task ImportDataAsync(string sourceFilePath)
    {
        var json = Path.GetExtension(sourceFilePath).Equals(".zip", StringComparison.OrdinalIgnoreCase)
            ? await ReadZipAsync(sourceFilePath)
            : await File.ReadAllTextAsync(sourceFilePath);

        var snapshot = JsonSerializer.Deserialize<CommercialDataSnapshot>(json, JsonOptions)
            ?? throw new InvalidDataException("Backup file has invalid NurSapat data.");

        await using var connection = _db.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var incomingArticles = snapshot.Materials
            .Select(row => ReadRequiredString(row.Data, "article"))
            .Where(article => !string.IsNullOrWhiteSpace(article))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (incomingArticles.Length > 0)
        {
            var conflicts = (await connection.QueryAsync<string>(
                "SELECT article FROM materials WHERE article = ANY(@Articles)",
                new { Articles = incomingArticles },
                transaction)).ToArray();

            if (conflicts.Length > 0)
            {
                throw new InvalidOperationException(
                    $"Material article conflicts detected: {string.Join(", ", conflicts)}");
            }
        }

        foreach (var customer in snapshot.Customers)
        {
            await InsertJsonAsync(connection, transaction, "customers", customer.Data);
        }

        foreach (var material in snapshot.Materials)
        {
            await InsertJsonAsync(connection, transaction, "materials", material.Data);
        }

        foreach (var order in snapshot.Orders)
        {
            await InsertJsonAsync(connection, transaction, "orders", order.Data);
        }

        foreach (var item in snapshot.OrderItems)
        {
            await InsertJsonAsync(connection, transaction, "order_items", item.Data);
        }

        await LogAsync(connection, "system", "ImportData", $"Commercial import completed: {sourceFilePath}", transaction);
        await transaction.CommitAsync();
    }

    private static async Task InsertJsonAsync(
        Npgsql.NpgsqlConnection connection,
        Npgsql.NpgsqlTransaction transaction,
        string table,
        string json)
    {
        var sql = $"INSERT INTO {table} SELECT * FROM jsonb_populate_record(NULL::{table}, @Data::jsonb)";
        await connection.ExecuteAsync(sql, new { Data = json }, transaction);
    }

    private static async Task WriteZipAsync(string targetFilePath, string json)
    {
        await using var stream = File.Create(targetFilePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        var entry = archive.CreateEntry(BackupEntryName, CompressionLevel.SmallestSize);
        await using var entryStream = entry.Open();
        await using var writer = new StreamWriter(entryStream);
        await writer.WriteAsync(json);
    }

    private static async Task<string> ReadZipAsync(string sourceFilePath)
    {
        await using var stream = File.OpenRead(sourceFilePath);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        var entry = archive.GetEntry(BackupEntryName)
            ?? throw new InvalidDataException($"Backup archive does not contain {BackupEntryName}.");
        using var entryStream = entry.Open();
        using var reader = new StreamReader(entryStream);
        return await reader.ReadToEndAsync();
    }

    private static string ReadRequiredString(string json, string propertyName)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.TryGetProperty(propertyName, out var property)
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static Task LogAsync(
        Npgsql.NpgsqlConnection connection,
        string user,
        string operationType,
        string details,
        Npgsql.NpgsqlTransaction? transaction = null)
    {
        return connection.ExecuteAsync(
            @"INSERT INTO system_logs (id, timestamp, ""user"", operation_type, action_details)
              VALUES (@Id, @Timestamp, @User, @OperationType, @ActionDetails)",
            new
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTimeOffset.UtcNow,
                User = user,
                OperationType = operationType,
                ActionDetails = details
            },
            transaction);
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private sealed class CommercialDataSnapshot
    {
        public string Format { get; init; } = "NurSapat.CommercialDataSnapshot.v1";

        public DateTimeOffset ExportedAt { get; init; }

        public List<JsonRow> Materials { get; init; } = new();

        public List<JsonRow> Customers { get; init; } = new();

        public List<JsonRow> Orders { get; init; } = new();

        public List<JsonRow> OrderItems { get; init; } = new();
    }

    private sealed class JsonRow
    {
        public string Data { get; init; } = "{}";
    }
}
