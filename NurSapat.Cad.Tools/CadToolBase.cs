namespace NurSapat.Cad.Tools;

public abstract class CadToolBase : ICadTool
{
    public abstract string Id { get; }

    public abstract string DisplayName { get; }

    public virtual void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
    }

    public virtual void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
    }

    public virtual void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
    }
}
