using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Abstractions;

public interface IOfflineMaterialRepository
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalMaterial>> GetAllAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<LocalMaterial?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Insert locally with client-generated Guid and PendingInsert.</summary>
    Task<LocalMaterial> InsertAsync(LocalMaterial material, CancellationToken cancellationToken = default);

    /// <summary>Update locally and set PendingUpdate (or keep PendingInsert).</summary>
    Task UpdateAsync(LocalMaterial material, CancellationToken cancellationToken = default);

    /// <summary>Soft-delete: is_deleted = true, SyncStatus = PendingDelete.</summary>
    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalMaterial>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task MarkSyncedAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkSyncErrorAsync(Guid id, string error, CancellationToken cancellationToken = default);

    /// <summary>Upsert remote pull payload into SQLite as Synced (does not overwrite Pending*).</summary>
    Task MergeFromRemoteAsync(IEnumerable<LocalMaterial> remoteRows, CancellationToken cancellationToken = default);
}

public interface IOfflineOrderRepository
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalOrder>> GetAllAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<LocalOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<LocalOrder> InsertAsync(LocalOrder order, CancellationToken cancellationToken = default);

    Task UpdateAsync(LocalOrder order, CancellationToken cancellationToken = default);

    Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LocalOrder>> GetPendingAsync(CancellationToken cancellationToken = default);

    Task MarkSyncedAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkSyncErrorAsync(Guid id, string error, CancellationToken cancellationToken = default);

    Task MergeFromRemoteAsync(IEnumerable<LocalOrder> remoteRows, CancellationToken cancellationToken = default);
}
