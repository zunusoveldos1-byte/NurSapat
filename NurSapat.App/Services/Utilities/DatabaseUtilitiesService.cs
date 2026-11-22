using Dapper;
using Microsoft.Data.Sqlite;
using Npgsql;
using NurSapat.Data;

namespace NurSapat.App.Services.Utilities;

public interface IDatabaseUtilitiesService
{
    Task CompressAsync(CancellationToken cancellationToken = default);
}

public sealed class DatabaseUtilitiesService : IDatabaseUtilitiesService
{
    private readonly DatabaseService _databaseService;

    public DatabaseUtilitiesService(DatabaseService databaseService)
    {
        _databaseService = databaseService;
    }

    public async Task CompressAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _databaseService.ConnectionString;

        if (IsSqliteConnection(connectionString))
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "VACUUM;";
            await command.ExecuteNonQueryAsync(cancellationToken);
            return;
        }

        await using var pgConnection = _databaseService.CreateConnection();
        await pgConnection.OpenAsync(cancellationToken);
        await using var pgCommand = new NpgsqlCommand("VACUUM ANALYZE;", pgConnection);
        await pgCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static bool IsSqliteConnection(string connectionString) =>
        connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
        || connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase);
}
