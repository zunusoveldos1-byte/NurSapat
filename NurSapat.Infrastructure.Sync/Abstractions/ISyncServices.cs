using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Abstractions;

public interface ISupabaseAuthService
{
    AuthSessionInfo? CurrentSession { get; }

    Guid? CurrentUserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>
    /// Returns true only when session.dat exists, can be decrypted for the
    /// current Windows user, and contains enough data for an offline restore.
    /// Does not contact Supabase.
    /// </summary>
    Task<bool> HasRememberedSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores a remembered Supabase session at application start, or restores
    /// the protected local session when the device is within the offline window.
    /// </summary>
    Task<AuthResult> RestoreSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Restores the remembered local session without contacting Supabase.
    /// Used as a defensive startup fallback when DNS/network discovery fails.
    /// </summary>
    Task<AuthResult> TryOfflineLoginFromCacheAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Signs in online through Supabase, or validates the protected seven-day
    /// offline credential when the API cannot be reached.
    /// </summary>
    Task<AuthResult> SignInAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken = default);

    Task<AuthResult> TryOfflineLoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>Sets in-memory current user for offline login (no remote tokens).</summary>
    void EstablishLocalSession(AuthSessionInfo session);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}

public interface IConnectivityMonitor
{
    Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default);
}

public interface INetworkStatusService : IConnectivityMonitor, IAsyncDisposable
{
    NetworkStatusSnapshot Current { get; }

    event EventHandler<NetworkStatusSnapshot>? StatusChanged;

    void Start();

    Task StopAsync();

    Task<NetworkStatusSnapshot> CheckNowAsync(CancellationToken cancellationToken = default);
}

public interface ILocalDbService
{
    bool IsUnlocked { get; }

    Task UnlockAsync(string email, string password, CancellationToken cancellationToken = default);

    Task UnlockWithDerivedKeyAsync(
        string email,
        byte[] derivedKey,
        CancellationToken cancellationToken = default);

    Task LockAsync();

    Task EnsureMirrorSchemaAsync(
        IReadOnlyCollection<RemoteTableSchema> tables,
        CancellationToken cancellationToken = default);

    Task MergeRemoteRowsAsync(
        string tableName,
        IReadOnlyCollection<Dictionary<string, object?>> rows,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingMirrorRow>> GetPendingRowsAsync(
        CancellationToken cancellationToken = default);

    Task UpsertLocalAsync(
        string tableName,
        IReadOnlyDictionary<string, object?> values,
        string operation = "upsert",
        CancellationToken cancellationToken = default);

    Task MarkSyncedAsync(
        string tableName,
        string localKey,
        CancellationToken cancellationToken = default);
}

public interface ISyncService
{
    /// <summary>Push all Pending* rows to Supabase (upsert / soft-delete).</summary>
    Task<SyncCycleResult> PushPendingAsync(CancellationToken cancellationToken = default);

    /// <summary>Initial (or refresh) pull of materials and orders into SQLite.</summary>
    Task PullRemoteAsync(CancellationToken cancellationToken = default);

    /// <summary>Pull then push — typical post-login bootstrap.</summary>
    Task BootstrapAfterLoginAsync(CancellationToken cancellationToken = default);
}

public interface IBackgroundSynchronizer : IAsyncDisposable
{
    bool IsRunning { get; }

    void Start();

    Task StopAsync();

    /// <summary>Force an immediate sync cycle (e.g. when network returns).</summary>
    Task TriggerNowAsync(CancellationToken cancellationToken = default);
}
