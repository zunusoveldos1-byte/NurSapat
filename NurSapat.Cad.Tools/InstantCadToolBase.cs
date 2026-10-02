namespace NurSapat.Cad.Tools;

/// <summary>Базовый класс для одноразовых команд панели инструментов.</summary>
public abstract class InstantCadToolBase : CadToolBase
{
    public sealed override CadToolInteraction Interaction => CadToolInteraction.InstantAction;

    public sealed override void OnActivated(CadToolContext context)
    {
        if (!TryExecute(context))
        {
            return;
        }

        context.Invalidate?.Invoke();
    }

    /// <summary>Выполняет действие. Возвращает <c>false</c>, если операция не выполнена.</summary>
    protected abstract bool TryExecute(CadToolContext context);
}
