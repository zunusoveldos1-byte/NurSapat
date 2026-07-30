using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Abstractions;

public interface ICachedUserRepository
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);

    Task UpsertSuccessfulLoginAsync(
        Guid userId,
        string email,
        string plainPassword,
        CancellationToken cancellationToken = default);

    Task ImportRememberedCredentialAsync(
        Guid userId,
        string email,
        string passwordHash,
        DateTimeOffset lastOnlineUtc,
        DateTimeOffset lastLocalAppRunUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a brand-new local-only profile (Guid generated on device).
    /// Throws if email already exists in the cache.
    /// </summary>
    Task<CachedUser> CreateLocalAccountAsync(
        string email,
        string plainPassword,
        CancellationToken cancellationToken = default);

    Task<CachedUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task TouchLastLocalAppRunAsync(
        Guid userId,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default);

    Task DeleteByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies email/password against the local cache.
    /// Returns a session on success; null when email missing or password mismatch.
    /// </summary>
    Task<AuthSessionInfo?> TryAuthenticateOfflineAsync(
        string email,
        string plainPassword,
        CancellationToken cancellationToken = default);
}
