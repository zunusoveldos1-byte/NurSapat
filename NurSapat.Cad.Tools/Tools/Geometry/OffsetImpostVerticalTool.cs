using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.Tools.Tools.Geometry;

/// <summary>
/// Смещение вертикального импоста по оси X (перетаскивание мышью).
/// </summary>
public sealed class OffsetImpostVerticalTool : CadToolBase
{
    private bool _isDragging;

    public override CadToolKind Kind => CadToolKind.OffsetImpostVertical;
    public override string Id => CadToolMetadata.GetId(Kind);
    public override string DisplayName => CadToolMetadata.GetDisplayName(Kind);
    public override bool IsImplemented => true;

    public override void OnActivated(CadToolContext context) =>
        context.SetStatus?.Invoke($"{DisplayName}: наведите на импост и перетащите по оси X");

    public override void OnDeactivated(CadToolContext context)
    {
        if (_isDragging)
        {
            context.EndVerticalImpostDrag?.Invoke();
            _isDragging = false;
        }

        context.UpdateVerticalImpostHover?.Invoke(new CadPoint(double.NaN, double.NaN));
    }

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        _isDragging = context.TryBeginVerticalImpostDrag?.Invoke(e.WorldPoint) == true;
        if (!_isDragging)
        {
            context.SetStatus?.Invoke("Кликните по вертикальному импосту");
        }
    }

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        if (_isDragging)
        {
            context.UpdateVerticalImpostDrag?.Invoke(e.WorldPoint);
            return;
        }

        context.UpdateVerticalImpostHover?.Invoke(e.WorldPoint);
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        if (!_isDragging)
        {
            return;
        }

        context.UpdateVerticalImpostDrag?.Invoke(e.WorldPoint);
        context.EndVerticalImpostDrag?.Invoke();
        _isDragging = false;
    }
}
