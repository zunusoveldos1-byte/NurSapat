namespace NurSapat.Cad.Tools;

public interface ICadTool
{
    CadToolKind Kind { get; }

    CadToolInteraction Interaction { get; }

    /// <summary><c>true</c>, если инструмент уже выполняет заявленную логику (не заглушка).</summary>
    bool IsImplemented { get; }

    string Id { get; }

    string DisplayName { get; }

    void OnActivated(CadToolContext context);

    void OnDeactivated(CadToolContext context);

    void OnMouseDown(CadToolContext context, CadPointerEvent e);

    void OnMouseMove(CadToolContext context, CadPointerEvent e);

    void OnMouseUp(CadToolContext context, CadPointerEvent e);
}
