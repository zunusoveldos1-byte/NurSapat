using Dapper;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Models;

namespace NurSapat.Data.Repositories;

public sealed class SystemLogRepository : ISystemLogRepository
{
    private readonly DatabaseService _db;

    public SystemLogRepository(DatabaseService db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SystemLogEntry>> GetLatestAsync(int limit = 200)
    {
        await using var connection = _db.CreateConnection();
        var logs = await connection.QueryAsync<SystemLogEntry>(
            @"SELECT id AS ""Id"",
                     timestamp AS ""Timestamp"",
                     ""user"" AS ""User"",
                     operation_type AS ""OperationType"",
                     action_details AS ""ActionDetails""
              FROM system_logs
              ORDER BY timestamp DESC
              LIMIT @Limit",
            new { Limit = limit });

        return logs.ToList();
    }

    public async Task AddAsync(SystemLogEntry entry)
    {
        await using var connection = _db.CreateConnection();
        await connection.ExecuteAsync(
            @"INSERT INTO system_logs (id, timestamp, ""user"", operation_type, action_details)
              VALUES (@Id, @Timestamp, @User, @OperationType, @ActionDetails)",
            entry);
    }
}
