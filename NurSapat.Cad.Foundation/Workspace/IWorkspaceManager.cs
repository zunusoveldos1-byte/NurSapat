namespace NurSapat.Cad.Foundation.Workspace;

/// <summary>
/// Контракт менеджера рабочих пространств: поддержка нескольких одновременно
/// открытых документов.
/// </summary>
public interface IWorkspaceManager
{
    /// <summary>Все открытые workspace-ы.</summary>
    IReadOnlyList<IWorkspace> Workspaces { get; }

    /// <summary>Текущий активный workspace (с которым работает UI).</summary>
    IWorkspace? Active { get; }

    /// <summary>Поднимается при смене активного workspace.</summary>
    event EventHandler? ActiveChanged;

    /// <summary>Поднимается при открытии нового workspace.</summary>
    event EventHandler<IWorkspace>? WorkspaceOpened;

    /// <summary>Поднимается при закрытии workspace.</summary>
    event EventHandler<IWorkspace>? WorkspaceClosed;

    /// <summary>Открыть новый workspace (новый документ).</summary>
    IWorkspace Open(string displayName);

    /// <summary>Закрыть workspace.</summary>
    void Close(WorkspaceId id);

    /// <summary>Сделать workspace активным.</summary>
    void Activate(WorkspaceId id);

    /// <summary>Найти workspace по идентификатору.</summary>
    IWorkspace? Find(WorkspaceId id);
}
