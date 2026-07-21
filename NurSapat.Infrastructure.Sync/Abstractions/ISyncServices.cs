using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Abstractions;

public interface ISupabaseAuthService
{
    AuthSessionInfo? CurrentSession { get; }

    Guid? CurrentUserId { get; }

    bool IsAuthenticated { get; }

    /// <summary>Restore session from disk (works offline if tokens were saved).</summary>
    Task<AuthResult> RestoreSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>Email/password sign-in via Supabase Auth. Persists session locally.</summary>
    Task<AuthResult> SignInAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>Sets in-memory current user for offline login (no remote tokens).</summary>
    void EstablishLocalSession(AuthSessionInfo session);

    Task SignOutAsync(CancellationToken cancellationToken = default);
}

public interface IConnectivityMonitor
{
    Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default);
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
