namespace NurSapat.Cad.UI.Services;

/// <summary>
/// LIFO-история размещения элементов конструкции (undo / redo стеки).
/// </summary>
public sealed class ConstructionPlacementHistory
{
    private readonly Stack<PlacementEntry> _undo = new();
    private readonly Stack<PlacementEntry> _redo = new();

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public event EventHandler? Changed;

    public void Record(ConstructionMemento before, ConstructionMemento after, string description)
    {
        _undo.Push(new PlacementEntry(before, after, description));
        _redo.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool TryUndo(Action<ConstructionMemento> apply, out string? description)
    {
        if (!_undo.TryPop(out var entry))
        {
            description = null;
            return false;
        }

        apply(entry.Before);
        _redo.Push(entry);
        description = entry.Description;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public bool TryRedo(Action<ConstructionMemento> apply, out string? description)
    {
        if (!_redo.TryPop(out var entry))
        {
            description = null;
            return false;
        }

        apply(entry.After);
        _undo.Push(entry);
        description = entry.Description;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Clear()
    {
        if (_undo.Count == 0 && _redo.Count == 0)
        {
            return;
        }

        _undo.Clear();
        _redo.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private sealed record PlacementEntry(
        ConstructionMemento Before,
        ConstructionMemento After,
        string Description);
}
