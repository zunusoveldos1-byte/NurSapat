namespace NurSapat.Cad.Tools;

public abstract class CadToolBase : ICadTool
{
    public abstract CadToolKind Kind { get; }

    public virtual CadToolInteraction Interaction => CadToolMetadata.GetInteraction(Kind);

    public virtual bool IsImplemented => CadToolMetadata.Get(Kind).IsImplemented;

    public abstract string Id { get; }

    public abstract string DisplayName { get; }

    public virtual void OnActivated(CadToolContext context)
    {
    }

    public virtual void OnDeactivated(CadToolContext context)
    {
    }

    public virtual void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
    }

    public virtual void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
    }

    public virtual void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
    }

    protected static void ReportNotImplemented(CadToolContext context, string displayName) =>
        context.SetStatus?.Invoke($"Инструмент «{displayName}» — в разработке");
}
