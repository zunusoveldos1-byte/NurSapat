using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Tools.Tools;

/// <summary>
/// Drag-прямоугольник внутри рамы (створка, остекление, гофра и т.д.) — аналог Ccalc speedbtnclick + drag.
/// </summary>
public sealed class DesignRectPlacementTool : CadToolBase
{
    private CadPoint? _startPoint;
    private CadRect? _previewRect;
    private bool _wasDragged;

    public CadRect? PreviewRect => _previewRect;

    public override string Id => "design-rect";

    public override string DisplayName => "Область заполнения";
    public override CadToolKind Kind => CadToolKind.None;

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        var snapped = Snap(context, e.WorldPoint);
        if (FrameBoundsHelper.FindFrameAt(context.Document, snapped) is null)
        {
            _startPoint = null;
            return;
        }

        _startPoint = snapped;
        _previewRect = null;
        _wasDragged = false;
    }

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        if (_startPoint is not CadPoint start)
        {
            return;
        }

        var end = Snap(context, e.WorldPoint);
        if ((end - start).Length <= 5)
        {
            return;
        }

        _wasDragged = true;
        var frame = FrameBoundsHelper.FindFrameAt(context.Document, start)
            ?? FrameBoundsHelper.ResolveBounds(context.Document, start);
        var raw = CadRect.FromPoints(start, end);
        _previewRect = FrameBoundsHelper.ClampToFrame(raw, frame);
        context.Invalidate?.Invoke();
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        if (_startPoint is not CadPoint start)
        {
            return;
        }

        if (!_wasDragged)
        {
            var frame = FrameBoundsHelper.FindFrameAt(context.Document, start)
                ?? FrameBoundsHelper.ResolveBounds(context.Document, start);
            var defaultW = Math.Min(500, frame.Width * 0.4);
            var defaultH = Math.Min(700, frame.Height * 0.4);
            _previewRect = FrameBoundsHelper.ClampToFrame(
                new CadRect(start.X - defaultW / 2, start.Y + defaultH / 2, defaultW, defaultH),
                frame);
        }

        if (_previewRect is CadRect rect && rect.Width > 10 && rect.Height > 10)
        {
            var toolId = context.ActiveDesignToolId ?? Id;
            var label = context.ActiveDesignToolLabel ?? DisplayName;
            context.CommitPlacement?.Invoke(new DesignPlacementCommit(
                toolId,
                label,
                DesignPlacementKind.AreaRect,
                new CadPoint(rect.X + rect.Width / 2, rect.Y - rect.Height / 2),
                Rect: rect));
        }

        _startPoint = null;
        _previewRect = null;
        _wasDragged = false;
        context.Invalidate?.Invoke();
    }

    private static CadPoint Snap(CadToolContext context, CadPoint point) =>
        context.SnapPoint?.Invoke(point) ?? point;
}
