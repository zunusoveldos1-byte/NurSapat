namespace NurSapat.Cad.Foundation.Commands;

/// <summary>
/// Необобщённый фасад истории команд: только чтение состояния + Undo/Redo/Clear.
/// </summary>
/// <remarks>
/// Нужен там, где потребитель не знает (или не должен знать) тип контекста <c>TContext</c> —
/// например, в сервисном слое или ViewModel, которой достаточно «можно ли Undo».
/// Полная история реализует <see cref="CommandHistory{TContext}"/>.
/// </remarks>
public interface ICommandHistory
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    int UndoCount { get; }
    int RedoCount { get; }
    int MaxSize { get; set; }

    void Undo();
    void Redo();
    void Clear();

    IReadOnlyList<string> GetUndoNames();
    IReadOnlyList<string> GetRedoNames();

    event EventHandler HistoryChanged;
}
