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

    Task<bool> SaveOrderAsync(CancellationToken cancellationToken = default);

    Task<bool> SaveOrderToPathAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> SaveAsCopyAsync(CancellationToken cancellationToken = default);

    Task<bool> CloseOrderAsync(CancellationToken cancellationToken = default);

    Task<bool> ExitToLoginAsync(CancellationToken cancellationToken = default);

    Task<bool> TryRecoverAutoSaveAsync(CancellationToken cancellationToken = default);

    void PrepareWorkspace();

    void MarkModified();

    void StartAutoSave();

    void StopAutoSave();
}
