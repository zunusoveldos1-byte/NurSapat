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
        var lastLogin = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture);

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

                INSERT INTO cached_users (id, email, password_hash, last_login_at)
                VALUES ($id, $email, $password_hash, $last_login_at)
                ON CONFLICT(id) DO UPDATE SET
                    email = excluded.email,
                    password_hash = excluded.password_hash,
                    last_login_at = excluded.last_login_at;
                """;
            command.Parameters.AddWithValue("$id", userId.ToString("D"));
            command.Parameters.AddWithValue("$email", normalizedEmail);
            command.Parameters.AddWithValue("$password_hash", passwordHash);
            command.Parameters.AddWithValue("$last_login_at", lastLogin);
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
            SELECT id, email, password_hash, last_login_at
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
            LastLoginAt = DateTimeOffset.Parse(reader.GetString(3), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
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

        // Touch last_login_at without re-hashing.
        await TouchLastLoginAsync(cached.Id, cancellationToken).ConfigureAwait(false);

        return new AuthSessionInfo
        {
            UserId = cached.Id,
            Email = cached.Email,
            AccessToken = string.Empty,
            RefreshToken = string.Empty,
            ExpiresAt = null
        };
    }

    private async Task TouchLastLoginAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE cached_users
            SET last_login_at = $last_login_at
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", userId.ToString("D"));
        command.Parameters.AddWithValue(
            "$last_login_at",
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
