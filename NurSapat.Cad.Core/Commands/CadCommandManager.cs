using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Foundation.Commands;

namespace NurSapat.Cad.Core.Commands;

/// <summary>
/// Менеджер истории команд редактора, привязанный к <see cref="CadDocument"/>.
/// </summary>
/// <remarks>
/// <b>Мост с NurSapat.Cad.Foundation</b>: наследует обобщённую историю
/// <see cref="CommandHistory{TContext}"/> с <c>TContext = CadDocument</c>.
/// Тем самым получает <see cref="CommandHistory{TContext}.Clear"/>,
/// <see cref="CommandHistory{TContext}.MaxSize"/>, событие
/// <see cref="CommandHistory{TContext}.HistoryChanged"/> и методы <c>GetUndoNames/GetRedoNames</c>.
/// Существующий публичный API (<see cref="Execute"/>, <see cref="Undo"/>, <see cref="Redo"/>,
/// <see cref="CanUndo"/>, <see cref="CanRedo"/>, <see cref="CommandExecuted"/>) сохранён.
/// </remarks>
public sealed class CadCommandManager : CommandHistory<CadDocument>
{
    private readonly CadDocument _document;

    /// <summary>Поднимается после выполнения или повторения команды (payload — сама команда).</summary>
    public event EventHandler<ICadCommand>? CommandExecuted;

    public CadCommandManager(CadDocument document)
    {
        _document = document;
    }

    /// <summary>Выполнить команду над документом и поместить её в undo-стек.</summary>
    public void Execute(ICadCommand command) => Execute(command, _document);

    /// <summary>Откатить последнюю команду.</summary>
    public void Undo() => Undo(_document);

    /// <summary>Повторить последнюю откатанную команду.</summary>
    public void Redo() => Redo(_document);

    /// <inheritdoc />
    /// <remarks>Поднимает <see cref="CommandExecuted"/> при Execute/Redo (существующий контракт).</remarks>
    protected override void OnCommandExecuted(ICadCommand<CadDocument>? command, CommandHistoryAction action)
    {
        if (action is CommandHistoryAction.Execute or CommandHistoryAction.Redo && command is ICadCommand cmd)
        {
            CommandExecuted?.Invoke(this, cmd);
        }
    }
}
