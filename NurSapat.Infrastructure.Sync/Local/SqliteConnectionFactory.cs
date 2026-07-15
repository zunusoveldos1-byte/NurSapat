using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Sync.Options;

namespace NurSapat.Infrastructure.Sync.Local;

public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;
    private int _initialized;

    public SqliteConnectionFactory(IOptions<SyncOptions> options)
    {
        var dbPath = ResolveDatabasePath(options.Value.SqlitePath);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public string DatabasePath =>
        new SqliteConnectionStringBuilder(_connectionString).DataSource;

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        if (Interlocked.Exchange(ref _initialized, 1) == 0)
        {
            await using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";
            await pragma.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return connection;
    }

    /// <summary>
    /// Builds an absolute path under %AppData%/NurSapat from a file name in config
    /// (e.g. "offline-cache.db"). Absolute paths are accepted as-is for tests/overrides.
    /// </summary>
    internal static string ResolveDatabasePath(string? sqlitePathFromConfig)
    {
        var fileName = string.IsNullOrWhiteSpace(sqlitePathFromConfig)
            ? "offline-cache.db"
            : sqlitePathFromConfig.Trim();

        // Allow absolute override (tests / advanced configs).
        if (Path.IsPathRooted(fileName))
        {
            var absoluteDirectory = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(absoluteDirectory) && !Directory.Exists(absoluteDirectory))
            {
                Directory.CreateDirectory(absoluteDirectory);
            }

            return fileName;
        }

        // Guard against accidental "NurSapat/offline-cache.db" or path separators in config.
        fileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "offline-cache.db";
        }

        var localFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NurSapat");

        if (!Directory.Exists(localFolder))
        {
            Directory.CreateDirectory(localFolder);
        }

        return Path.Combine(localFolder, fileName);
    }
}
