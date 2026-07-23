using System.Collections.ObjectModel;

namespace NurSapat.Cad.Foundation.Workspace;

/// <summary>
/// Реализация <see cref="IWorkspaceManager"/>: управляет несколькими workspace-ами.
/// </summary>
public sealed class WorkspaceManager : IWorkspaceManager
{
    private readonly Dictionary<Guid, IWorkspace> _byId = new();
    private readonly ObservableCollection<IWorkspace> _workspaces = new();
    private IWorkspace? _active;

    public IReadOnlyList<IWorkspace> Workspaces => _workspaces;
    public IWorkspace? Active => _active;

    public event EventHandler? ActiveChanged;
    public event EventHandler<IWorkspace>? WorkspaceOpened;
    public event EventHandler<IWorkspace>? WorkspaceClosed;

    public IWorkspace Open(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            displayName = $"Document {_byId.Count + 1}";
        var id = WorkspaceId.New(displayName);
        var workspace = new Workspace(id);
        _byId[id.Id] = workspace;
        _workspaces.Add(workspace);
        Activate(id);
        WorkspaceOpened?.Invoke(this, workspace);
        return workspace;
    }

    public void Close(WorkspaceId id)
    {
        if (!_byId.TryGetValue(id.Id, out var workspace)) return;
        _byId.Remove(id.Id);
        _workspaces.Remove(workspace);
        workspace.Dispose();
        WorkspaceClosed?.Invoke(this, workspace);

        if (_active == workspace)
        {
            _active = _workspaces.Count > 0 ? _workspaces[^1] : null;
            ActiveChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Activate(WorkspaceId id)
    {
        if (!_byId.TryGetValue(id.Id, out var workspace))
            throw new KeyNotFoundException($"Workspace «{id.DisplayName}» не найден.");
        if (_active == workspace) return;
        _active = workspace;
        ActiveChanged?.Invoke(this, EventArgs.Empty);
    }

    public IWorkspace? Find(WorkspaceId id) =>
        _byId.TryGetValue(id.Id, out var ws) ? ws : null;
}
