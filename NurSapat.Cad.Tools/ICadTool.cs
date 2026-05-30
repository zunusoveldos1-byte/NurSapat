namespace NurSapat.Cad.Tools;

public interface ICadTool
{
    string Id { get; }

    string DisplayName { get; }

    void OnMouseDown(CadToolContext context, CadPointerEvent e);

    void OnMouseMove(CadToolContext context, CadPointerEvent e);

    void OnMouseUp(CadToolContext context, CadPointerEvent e);
}
