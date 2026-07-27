namespace NurSapat.Cad.Foundation.Commands;

/// <summary>
/// Пустая команда (no-op). Используется как заглушка/маркер и для инициализации истории.
/// </summary>
public sealed class EmptyCommand<TContext> : CadCommandBase<TContext>
{
    public EmptyCommand(string name = "(empty)")
        : base(name)
    {
    }

    protected override void ExecuteCore(TContext context) { }
    protected override void UndoCore(TContext context) { }
}
