namespace NurSapat.Infrastructure.Sync.Models;

/// <summary>Local-only sync state for offline-first records.</summary>
public enum SyncStatus
{
    Synced = 0,
    PendingInsert = 1,
    PendingUpdate = 2,
    PendingDelete = 3
}
