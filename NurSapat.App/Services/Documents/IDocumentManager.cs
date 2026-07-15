using NurSapat.Core.Models.Project;

namespace NurSapat.App.Services.Documents;

public interface IDocumentManager
{
    bool HasOpenDocument { get; }

    bool IsModified { get; }

    string? FilePath { get; }

    string WindowTitle { get; }

    event EventHandler? DocumentStateChanged;

    ProjectDocumentData CreateSnapshot();

    Task<bool> NewOrderAsync(CancellationToken cancellationToken = default);

    Task<bool> OpenOrderAsync(string? filePath = null, CancellationToken cancellationToken = default);

    /// <summary>Opens an order from the persistent store (PostgreSQL) when no .nsp file is required.</summary>
    Task<bool> OpenOrderFromDatabaseAsync(Guid orderId, CancellationToken cancellationToken = default);

    Task<bool> SaveOrderAsync(CancellationToken cancellationToken = default);

    Task<bool> SaveOrderToPathAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> SaveAsCopyAsync(CancellationToken cancellationToken = default);

    Task<bool> CloseOrderAsync(CancellationToken cancellationToken = default);

    Task<bool> ExitToLoginAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms discard/save of unsaved changes before the main window closes.
    /// Returns true when the window may close.
    /// </summary>
    Task<bool> ConfirmCloseAsync(CancellationToken cancellationToken = default);

    Task<bool> TryRecoverAutoSaveAsync(CancellationToken cancellationToken = default);

    void PrepareWorkspace();

    void MarkModified();

    void StartAutoSave();

    void StopAutoSave();
}
