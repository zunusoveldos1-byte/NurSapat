namespace NurSapat.Cad.Foundation.Commands;

/// <summary>
/// Команда, оборачивающая два делегата (для быстрого создания команд без нового класса).
/// Аналог существующего <c>CadActionCommand</c> в NurSapat.Cad.Core, но обобщённый.
/// </summary>
public sealed class ActionCommand<TContext> : CadCommandBase<TContext>
{
    private readonly Action<TContext> _execute;
    private readonly Action<TContext> _undo;

    public ActionCommand(string name, Action<TContext> execute, Action<TContext> undo)
        : base(name)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _undo = undo ?? throw new ArgumentNullException(nameof(undo));
    }

    protected override void ExecuteCore(TContext context) => _execute(context);
    protected override void UndoCore(TContext context) => _undo(context);
}
