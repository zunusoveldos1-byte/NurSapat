using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Sync.Diagnostics;
using NurSapat.Infrastructure.Sync.Options;

namespace NurSapat.Infrastructure.Sync.Local;

/// <summary>Creates authenticated SQLCipher connections for the active account.</summary>
public sealed class SqliteConnectionFactory
{
    private const int KeyIterations = 310_000;
    private readonly string _configuredPath;
    private readonly object _gate = new();
    private byte[]? _key;
    private string? _databasePath;

    static SqliteConnectionFactory()
    {
        SQLitePCL.Batteries_V2.Init();
    }

    public SqliteConnectionFactory(IOptions<SyncOptions> options)
    {
        _configuredPath = ResolveDatabasePath(options.Value.SqlitePath);
    }

    public bool IsUnlocked
    {
        get
        {
            lock (_gate)
            {
                return _key is not null;
            }
        }
    }

    public string DatabasePath
    {
        get
        {
            lock (_gate)
            {
                return _databasePath
                    ?? throw new InvalidOperationException("The encrypted local database is locked.");
            }
        }
    }

    public void Unlock(string email, string password, byte[] salt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentNullException.ThrowIfNull(salt);

        var key = DeriveKey(email, password, salt);
        UnlockWithDerivedKey(email, key);
        CryptographicOperations.ZeroMemory(key);
    }

    public void UnlockWithDerivedKey(string email, byte[] derivedKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentNullException.ThrowIfNull(derivedKey);
        if (derivedKey.Length != 32)
        {
            throw new CryptographicException("Invalid SQLCipher key length.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var accountSuffix = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(normalizedEmail)))[..16].ToLowerInvariant();
        var directory = Path.GetDirectoryName(_configuredPath)!;
        var name = Path.GetFileNameWithoutExtension(_configuredPath);
        var extension = Path.GetExtension(_configuredPath);
        var accountPath = Path.Combine(directory, $"{name}-{accountSuffix}{extension}");

        lock (_gate)
        {
            if (_key is not null)
            {
                CryptographicOperations.ZeroMemory(_key);
            }

            _key = derivedKey.ToArray();
            _databasePath = accountPath;
        }
    }

    public static byte[] DeriveKey(string email, string password, byte[] salt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentNullException.ThrowIfNull(salt);

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var material = $"{password}\0{normalizedEmail}";
        return Rfc2898DeriveBytes.Pbkdf2(
            material,
            salt,
            KeyIterations,
            HashAlgorithmName.SHA256,
            32);
    }

    public void Lock()
    {
        lock (_gate)
        {
            if (_key is not null)
            {
                CryptographicOperations.ZeroMemory(_key);
            }

            _key = null;
            _databasePath = null;
        }
    }

    public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        byte[] key;
        string path;
        lock (_gate)
        {
            if (_key is null || _databasePath is null)
            {
                throw new InvalidOperationException(
                    "The encrypted local database is locked. Authenticate first.");
            }

            key = _key.ToArray();
            path = _databasePath;
        }

        try
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = path,
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                Pooling = false
            }.ToString();

            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await using var pragma = connection.CreateCommand();
                pragma.CommandText = $"""
                    PRAGMA key = "x'{Convert.ToHexString(key)}'";
                    PRAGMA cipher_memory_security = ON;
                    PRAGMA foreign_keys = ON;
                    PRAGMA journal_mode = WAL;
                    """;
                await pragma.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                await using var verify = connection.CreateCommand();
                verify.CommandText = "SELECT count(*) FROM sqlite_master;";
                await verify.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                return connection;
            }
            catch (Exception ex)
            {
                StartupTrace.Error(
                    "[LOG] Проверка SQLCipher: ошибка открытия или проверки ключа",
                    ex);
                await connection.DisposeAsync().ConfigureAwait(false);
                throw new CryptographicException(
                    "Не удалось открыть локальную зашифрованную базу. Проверьте пароль.",
                    ex);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    internal static string ResolveDatabasePath(string? sqlitePathFromConfig)
    {
        var fileName = string.IsNullOrWhiteSpace(sqlitePathFromConfig)
            ? "offline-cache.db"
            : sqlitePathFromConfig.Trim();

        if (Path.IsPathRooted(fileName))
        {
            var absoluteDirectory = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(absoluteDirectory))
            {
                Directory.CreateDirectory(absoluteDirectory);
            }

            return fileName;
        }

        fileName = Path.GetFileName(fileName);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "offline-cache.db";
        }

        var localFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NurSapat");
        Directory.CreateDirectory(localFolder);
        return Path.Combine(localFolder, fileName);
    }
}
