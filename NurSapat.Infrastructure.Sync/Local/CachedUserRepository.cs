using System.Globalization;
using Microsoft.Data.Sqlite;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Auth;
using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Local;

public sealed class CachedUserRepository : ICachedUserRepository
{
    private readonly SqliteConnectionFactory _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public CachedUserRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await SqliteSchema.EnsureCreatedAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task UpsertSuccessfulLoginAsync(
        Guid userId,
        string email,
        string plainPassword,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var passwordHash = OfflinePasswordHasher.HashPassword(plainPassword);
        var nowUtc = DateTimeOffset.UtcNow;
        await ImportRememberedCredentialAsync(
                userId,
                normalizedEmail,
                passwordHash,
                nowUtc,
                nowUtc,
                cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task ImportRememberedCredentialAsync(
        Guid userId,
        string email,
        string passwordHash,
        DateTimeOffset lastOnlineUtc,
        DateTimeOffset lastLocalAppRunUtc,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var lastOnline = lastOnlineUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
        var lastLocalRun = lastLocalAppRunUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                DELETE FROM cached_users
                WHERE email = $email COLLATE NOCASE
                  AND id <> $id;

                INSERT INTO cached_users (
                    id, email, password_hash, last_login_at,
                    last_online_utc, last_local_app_run_utc)
                VALUES (
                    $id, $email, $password_hash, $last_local_app_run_utc,
                    $last_online_utc, $last_local_app_run_utc)
                ON CONFLICT(id) DO UPDATE SET
                    email = excluded.email,
                    password_hash = excluded.password_hash,
                    last_login_at = excluded.last_login_at,
                    last_online_utc = excluded.last_online_utc,
                    last_local_app_run_utc = excluded.last_local_app_run_utc;
                """;
            command.Parameters.AddWithValue("$id", userId.ToString("D"));
            command.Parameters.AddWithValue("$email", normalizedEmail);
            command.Parameters.AddWithValue("$password_hash", passwordHash);
            command.Parameters.AddWithValue("$last_online_utc", lastOnline);
            command.Parameters.AddWithValue("$last_local_app_run_utc", lastLocalRun);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CachedUser> CreateLocalAccountAsync(
        string email,
        string plainPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrEmpty(plainPassword) || plainPassword.Length < 4)
        {
            throw new ArgumentException("Пароль должен содержать не менее 4 символов.", nameof(plainPassword));
        }

        var existing = await FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Аккаунт с email «{email.Trim()}» уже существует на этом устройстве.");
        }

        var userId = Guid.NewGuid();
        await UpsertSuccessfulLoginAsync(userId, email, plainPassword, cancellationToken).ConfigureAwait(false);

        return new CachedUser
        {
            Id = userId,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = string.Empty,
            LastLoginAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<CachedUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, email, password_hash, last_login_at,
                   last_online_utc, last_local_app_run_utc
            FROM cached_users
            WHERE email = $email COLLATE NOCASE
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$email", email.Trim().ToLowerInvariant());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new CachedUser
        {
            Id = Guid.Parse(reader.GetString(0)),
            Email = reader.GetString(1),
            PasswordHash = reader.GetString(2),
            LastLoginAt = ParseUtc(reader.GetString(3)),
            LastOnlineUtc = ParseUtc(reader.GetString(4)),
            LastLocalAppRunUtc = ParseUtc(reader.GetString(5))
        };
    }

    public async Task<AuthSessionInfo?> TryAuthenticateOfflineAsync(
        string email,
        string plainPassword,
        CancellationToken cancellationToken = default)
    {
        var cached = await FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        if (cached is null)
        {
            return null;
        }

        if (!OfflinePasswordHasher.VerifyPassword(plainPassword, cached.PasswordHash))
        {
            return null;
        }

        await TouchLastLocalAppRunAsync(cached.Id, DateTimeOffset.UtcNow, cancellationToken)
            .ConfigureAwait(false);

        return new AuthSessionInfo
        {
            UserId = cached.Id,
            Email = cached.Email,
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            ExpiresAt = null
        };
    }

    public async Task TouchLastLocalAppRunAsync(
        Guid userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE cached_users
            SET last_login_at = $last_local_app_run_utc,
                last_local_app_run_utc = $last_local_app_run_utc
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", userId.ToString("D"));
        command.Parameters.AddWithValue(
            "$last_local_app_run_utc",
            nowUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM cached_users WHERE email = $email COLLATE NOCASE;";
        command.Parameters.AddWithValue("$email", email.Trim().ToLowerInvariant());
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static DateTimeOffset ParseUtc(string value) =>
        DateTimeOffset.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind).ToUniversalTime();
}
