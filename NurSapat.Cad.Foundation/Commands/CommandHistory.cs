namespace NurSapat.Cad.Foundation.Commands;

/// <summary>Действие, выполненное над историей команд.</summary>
public enum CommandHistoryAction
{
    /// <summary>Команда выполнена через Execute или Redo.</summary>
    Execute,

    /// <summary>Команда откатана через Undo.</summary>
    Undo,

    /// <summary>Команда повторена через Redo.</summary>
    Redo,

    /// <summary>История очищена через Clear.</summary>
    Cleared
}

/// <summary>
/// Обобщённая история команд (Undo/Redo) с ограничением размера.
/// </summary>
/// <typeparam name="TContext">Тип контекста, в котором выполняются команды.</typeparam>
/// <remarks>
/// Базовый класс для существующего <c>NurSapat.Cad.Core.CadCommandManager</c>.
/// Стеки объявлены как <see cref="protected"/> и есть виртуальный хук
/// <see cref="OnCommandExecuted"/> — это позволяет наследникам реагировать на любые
/// изменения, не дублируя логику выполнения.
/// </remarks>
public class CommandHistory<TContext>
{
    /// <summary>Undo-стек (верх = последняя выполненная команда). Доступен наследникам.</summary>
    protected readonly Stack<ICadCommand<TContext>> _undo = new();

    /// <summary>Redo-стек (верх = последняя откатанная команда). Доступен наследникам.</summary>
    protected readonly Stack<ICadCommand<TContext>> _redo = new();

    private int _maxSize = DefaultMaxSize;

    /// <summary>Размер истории по умолчанию (количество команд в undo-стеке).</summary>
    public const int DefaultMaxSize = 200;

    public CommandHistory() { }

    /// <summary>Создать историю с указанным максимальным размером.</summary>
    public CommandHistory(int maxSize) => MaxSize = maxSize;

    /// <summary>
    /// Максимальное количество команд в undo-стеке. По умолчанию <see cref="DefaultMaxSize"/>.
    /// При превышении самые старые команды удаляются. Допустимо 1..100000.
    /// </summary>
    public int MaxSize
    {
        get => _maxSize;
        set => _maxSize = Math.Clamp(value, 1, 100_000);
    }

    /// <summary>Можно ли выполнить Undo.</summary>
    public virtual bool CanUndo => _undo.Count > 0;

    /// <summary>Можно ли выполнить Redo.</summary>
    public virtual bool CanRedo => _redo.Count > 0;

    /// <summary>Текущее число команд в undo-стеке.</summary>
    public int UndoCount => _undo.Count;

    /// <summary>Текущее число команд в redo-стеке.</summary>
    public int RedoCount => _redo.Count;

    /// <summary>Поднимается при любом изменении стеков (execute/undo/redo/clear/trim).</summary>
    public event EventHandler? HistoryChanged;

    /// <summary>Выполнить команду и поместить её в undo-стек. Redo-стек очищается.</summary>
    public virtual void Execute(ICadCommand<TContext> command, TContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Execute(context);
        _undo.Push(command);
        _redo.Clear();
        TrimUndo();
        OnCommandExecuted(command, CommandHistoryAction.Execute);
        OnHistoryChanged();
    }

    /// <summary>Откатить последнюю команду (undo-стек → redo-стек).</summary>
    public virtual void Undo(TContext context)
    {
        if (!_undo.TryPop(out var command))
            return;
        command.Undo(context);
        _redo.Push(command);
        OnCommandExecuted(command, CommandHistoryAction.Undo);
        OnHistoryChanged();
    }

    /// <summary>Повторить последнюю откатанную команду (redo-стек → undo-стек).</summary>
    public virtual void Redo(TContext context)
    {
        if (!_redo.TryPop(out var command))
            return;
        command.Execute(context);
        _undo.Push(command);
        TrimUndo();
        OnCommandExecuted(command, CommandHistoryAction.Redo);
        OnHistoryChanged();
    }

    /// <summary>Очистить оба стека без выполнения undo/redo.</summary>
    public virtual void Clear()
    {
        if (_undo.Count == 0 && _redo.Count == 0)
            return;
        _undo.Clear();
        _redo.Clear();
        OnCommandExecuted(null, CommandHistoryAction.Cleared);
        OnHistoryChanged();
    }

    /// <summary>Получить снимок имён команд в undo-стеке (для UI истории), сверху вниз.</summary>
    public IReadOnlyList<string> GetUndoNames() => _undo.Select(c => c.Name).ToArray();

    /// <summary>Получить снимок имён команд в redo-стеке (для UI истории), сверху вниз.</summary>
    public IReadOnlyList<string> GetRedoNames() => _redo.Select(c => c.Name).ToArray();

    /// <summary>
    /// Виртуальный хук: вызывается после Execute/Undo/Redo/Clear.
    /// Наследники могут переопределить его, чтобы поднять собственные события
    /// (например, <c>CadCommandManager.CommandExecuted</c>).
    /// </summary>
    /// <param name="command">Команда; <c>null</c> при очистке истории.</param>
    /// <param name="action">Какое действие было выполнено.</param>
    protected virtual void OnCommandExecuted(ICadCommand<TContext>? command, CommandHistoryAction action) { }

    /// <summary>Поднимает <see cref="HistoryChanged"/>.</summary>
    protected virtual void OnHistoryChanged() => HistoryChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Обрезать undo-стек до <see cref="MaxSize"/>, удаляя самые старые команды.
    /// </summary>
    protected void TrimUndo()
    {
        while (_undo.Count > _maxSize)
        {
            var asArray = _undo.ToArray();     // Stack.ToArray() — сверху вниз
            _undo.Clear();
            // оставляем последние maxSize элементов, восстанавливая порядок
            for (var i = Math.Min(_maxSize, asArray.Length) - 1; i >= 0; i--)
                _undo.Push(asArray[i]);
        }
    }
}
