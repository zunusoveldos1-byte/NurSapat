namespace NurSapat.Cad.Foundation.Events;

/// <summary>Событие: история команд изменилась (execute/undo/redo/clear).</summary>
public sealed class HistoryChangedEvent : CadEventBase
{
    public int UndoCount { get; }
    public int RedoCount { get; }
    public bool CanUndo { get; }
    public bool CanRedo { get; }

    public HistoryChangedEvent(int undoCount, int redoCount, bool canUndo, bool canRedo)
    {
        UndoCount = undoCount;
        RedoCount = redoCount;
        CanUndo = canUndo;
        CanRedo = canRedo;
    }
}
