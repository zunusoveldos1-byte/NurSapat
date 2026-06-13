using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Tools.Tools;

public enum MullionPlacementMode
{
    Vertical,
    Horizontal,
    Diagonal
}

/// <summary>
/// Размещение импоста по логике Ccalc: V/H — позиция по drag со snap к сетке внутри рамы,
/// D — линия от точки к точке.
/// </summary>
public sealed class MullionPlacementTool : CadToolBase
{
    private CadPoint? _startPoint;
    private CadLine? _previewLine;
    private CadRect? _activeFrame;

    public MullionPlacementMode Mode { get; }

    public CadLine? PreviewLine => _previewLine;

    public override string Id { get; }

    public override string DisplayName { get; }

    public MullionPlacementTool(string id, string displayName, MullionPlacementMode mode)
    {
        Id = id;
        DisplayName = displayName;
        Mode = mode;
    }

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        var snapped = Snap(context, e.WorldPoint);
        _activeFrame = FrameBoundsHelper.FindFrameAt(context.Document, snapped);
        if (_activeFrame is null)
        {
            _startPoint = null;
            _previewLine = null;
            return;
        }

        _startPoint = snapped;
        _previewLine = BuildPreviewLine(context, snapped, snapped);
        context.Invalidate?.Invoke();
    }

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        if (_startPoint is not CadPoint start || _activeFrame is not CadRect frame)
        {
            return;
        }

        var snapped = Snap(context, e.WorldPoint);
        _previewLine = BuildPreviewLine(context, start, snapped, frame);
        context.Invalidate?.Invoke();
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        if (_startPoint is not CadPoint start || _activeFrame is not CadRect frame)
        {
            Reset();
            return;
        }

        var snapped = Snap(context, e.WorldPoint);
        var line = BuildPreviewLine(context, start, snapped, frame);
        if (line.Length < 1)
        {
            Reset();
            context.Invalidate?.Invoke();
            return;
        }

        var toolId = context.ActiveDesignToolId ?? Id;
        var label = context.ActiveDesignToolLabel ?? DisplayName;
        context.CommitPlacement?.Invoke(new DesignPlacementCommit(
            toolId,
            label,
            DesignPlacementKind.MullionLine,
            line.Start,
            line.End));

        Reset();
        context.Invalidate?.Invoke();
    }

    private CadLine BuildPreviewLine(CadToolContext context, CadPoint start, CadPoint current, CadRect? frame = null)
    {
        var bounds = frame ?? _activeFrame ?? FrameBoundsHelper.FindFrameAt(context.Document, start)
            ?? FrameBoundsHelper.ResolveBounds(context.Document, start);
        var minX = bounds.X;
        var maxX = bounds.X + bounds.Width;
        var minY = bounds.Y - bounds.Height;
        var maxY = bounds.Y;

        return Mode switch
        {
            MullionPlacementMode.Vertical => new CadLine(
                new CadPoint(current.X, minY),
                new CadPoint(current.X, maxY)),
            MullionPlacementMode.Horizontal => new CadLine(
                new CadPoint(minX, current.Y),
                new CadPoint(maxX, current.Y)),
            _ => new CadLine(start, current)
        };
    }

    private void Reset()
    {
        _startPoint = null;
        _previewLine = null;
        _activeFrame = null;
    }

    private static CadPoint Snap(CadToolContext context, CadPoint point) =>
        context.SnapPoint?.Invoke(point) ?? point;
}
