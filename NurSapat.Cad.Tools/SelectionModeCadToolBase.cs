namespace NurSapat.Cad.Tools;

/// <summary>Базовый класс для режимов выделения с подсветкой при наведении.</summary>
public abstract class SelectionModeCadToolBase : CadToolBase
{
    protected abstract CadSelectionMode SelectionMode { get; }

    public sealed override CadToolInteraction Interaction => CadToolInteraction.Persistent;

    public override void OnActivated(CadToolContext context)
    {
        context.ActiveSelectionMode = SelectionMode;
        context.SetStatus?.Invoke($"{DisplayName}: кликните по элементу конструкции");
    }

    public override void OnDeactivated(CadToolContext context)
    {
        if (context.ActiveSelectionMode == SelectionMode)
        {
            context.ActiveSelectionMode = CadSelectionMode.None;
        }

        context.HighlightedTarget = null;
    }

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        if (!IsImplemented)
        {
            return;
        }

        context.HighlightedTarget = context.HitTestSelection?.Invoke(e.WorldPoint, SelectionMode);
        context.Invalidate?.Invoke();
    }

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        if (!IsImplemented)
        {
            ReportNotImplemented(context, DisplayName);
            return;
        }

        var target = context.HitTestSelection?.Invoke(e.WorldPoint, SelectionMode);
        if (target == null)
        {
            context.SetStatus?.Invoke($"{DisplayName}: объект не найден");
            return;
        }

        context.SelectTarget?.Invoke(target);
        context.HighlightedTarget = target;
        context.Invalidate?.Invoke();
    }
}
