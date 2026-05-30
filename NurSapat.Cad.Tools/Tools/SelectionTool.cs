namespace NurSapat.Cad.Tools.Tools;

public sealed class SelectionTool : CadToolBase
{
    public override string Id => "select";

    public override string DisplayName => "Выбор";

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        context.HitTest(e.WorldPoint);
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        context.HitTest(e.WorldPoint);
    }
}
