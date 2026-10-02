namespace NurSapat.Cad.Foundation.Commands;

/// <summary>
/// Базовый класс CAD-команд с общим состоянием: имя, выполнимость, маркер объединения.
/// </summary>
/// <typeparam name="TContext">Тип контекста выполнения.</typeparam>
public abstract class CadCommandBase<TContext> : ICadCommand<TContext>
{
    /// <summary>Имя команды.</summary>
    public string Name { get; }

    /// <summary>Была ли команда уже выполнена.</summary>
    public bool IsExecuted { get; protected set; }

    /// <summary>
    /// Идентификатор группы для объединения соседних команд в истории
    /// (например, серия перемещений мыши сливается в одно Undo). <c>null</c> = без группы.
    /// </summary>
    public string? MergeGroup { get; init; }

    protected CadCommandBase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Имя команды обязательно.", nameof(name));
        Name = name;
    }

    /// <inheritdoc />
    public virtual void Execute(TContext context)
    {
        if (IsExecuted)
            throw new InvalidOperationException(
                $"Команда «{Name}» уже выполнена. Создайте новый экземпляр для повторного выполнения.");
        ExecuteCore(context);
        IsExecuted = true;
    }

    /// <inheritdoc />
    public virtual void Undo(TContext context)
    {
        if (!IsExecuted)
            throw new InvalidOperationException($"Команда «{Name}» не была выполнена — Undo невозможно.");
        UndoCore(context);
        IsExecuted = false;
    }

    /// <summary>Собственно логика применения — переопределяется в наследниках.</summary>
    protected abstract void ExecuteCore(TContext context);

    /// <summary>Собственно логика отката — переопределяется в наследниках.</summary>
    protected abstract void UndoCore(TContext context);
}
