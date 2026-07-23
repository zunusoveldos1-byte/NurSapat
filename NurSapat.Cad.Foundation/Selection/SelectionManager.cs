using NurSapat.Cad.Foundation.Events;
using NurSapat.Cad.Foundation.Scene;

namespace NurSapat.Cad.Foundation.Selection;

/// <summary>
/// Реализация <see cref="ISelectionManager"/>: множественный выбор по ObjectId
/// с синхронизацией флага <c>SceneObject.Selected</c> и публикацией событий.
/// </summary>
public sealed class SelectionManager : ISelectionManager
{
    private readonly HashSet<Guid> _selected = new();
    private readonly IScene _scene;
    private readonly ICadEventBus _eventBus;

    private IReadOnlyList<Guid> _selectedSnapshot = Array.Empty<Guid>();

    public IReadOnlyList<Guid> SelectedIds => _selectedSnapshot;
    public int Count => _selected.Count;
    public bool HasSelection => _selected.Count > 0;

    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

    public SelectionManager(IScene scene, ICadEventBus eventBus)
    {
        _scene = scene ?? throw new ArgumentNullException(nameof(scene));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public void Select(Guid objectId)
    {
        if (_selected.Add(objectId))
        {
            UpdateObjectFlag(objectId, true);
            RaiseChanged(added: objectId);
        }
    }

    public void Select(IEnumerable<Guid> objectIds)
    {
        ArgumentNullException.ThrowIfNull(objectIds);
        var added = new List<Guid>();
        foreach (var id in objectIds)
        {
            if (_selected.Add(id))
            {
                UpdateObjectFlag(id, true);
                added.Add(id);
            }
        }
        if (added.Count > 0)
            RaiseChanged(added: added, deselected: Array.Empty<Guid>());
    }

    public void Deselect(Guid objectId)
    {
        if (_selected.Remove(objectId))
        {
            UpdateObjectFlag(objectId, false);
            RaiseChanged(deselected: objectId);
        }
    }

    public void Toggle(Guid objectId)
    {
        if (IsSelected(objectId))
            Deselect(objectId);
        else
            Select(objectId);
    }

    public void Clear()
    {
        if (_selected.Count == 0) return;
        var deselected = _selected.ToArray();
        foreach (var id in deselected)
            UpdateObjectFlag(id, false);
        _selected.Clear();
        RaiseChanged(added: Array.Empty<Guid>(), deselected: deselected);
    }

    public void SelectAll()
    {
        var added = new List<Guid>();
        foreach (var obj in _scene.Objects.Items)
        {
            if (!obj.Locked && _selected.Add(obj.ObjectId))
            {
                obj.Selected = true;
                added.Add(obj.ObjectId);
            }
        }
        if (added.Count > 0)
            RaiseChanged(added: added, deselected: Array.Empty<Guid>());
    }

    public bool IsSelected(Guid objectId) => _selected.Contains(objectId);

    private void UpdateObjectFlag(Guid objectId, bool selected)
    {
        var obj = _scene.Find(objectId);
        if (obj is not null)
            obj.Selected = selected;
    }

    private void RaiseChanged(Guid? added = null, Guid? deselected = null)
    {
        _selectedSnapshot = _selected.ToArray();
        var addedList = added.HasValue ? new[] { added.Value } : Array.Empty<Guid>();
        var deselectedList = deselected.HasValue ? new[] { deselected.Value } : Array.Empty<Guid>();
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(addedList, deselectedList));
        _eventBus.Publish(new SelectionChangedEvent(addedList, deselectedList));
    }

    private void RaiseChanged(IReadOnlyList<Guid> added, IReadOnlyList<Guid> deselected)
    {
        _selectedSnapshot = _selected.ToArray();
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(added, deselected));
        _eventBus.Publish(new SelectionChangedEvent(added, deselected));
    }
}
