using System;
using System.Collections.Generic;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.Tools.Tools;

public enum MullionPlacementMode
{
    Vertical,
    Horizontal,
    Diagonal
}

/// <summary>
/// Инструмент размещения одиночного импоста внутри светового проёма рамы.
/// </summary>
public sealed class MullionPlacementTool : CadToolBase
{
    private const double DefaultMullionWidth = 32.0;
    private const double DefaultFrameThickness = 70.0;

    private CadPoint? _startPoint;
    private CadRect? _activeFrame;

    private readonly List<CadRect> _previewRects = new();

    public MullionPlacementMode Mode { get; }

    public IReadOnlyList<CadRect> PreviewRects => _previewRects;

    public override string Id { get; }
    public override string DisplayName { get; }
    public override CadToolKind Kind => CadToolKind.None;

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
            Reset();
            return;
        }

        _startPoint = snapped;
        BuildVolumeMullionPreview(context, snapped, _activeFrame.Value);
        context.Invalidate?.Invoke();
    }

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        if (_startPoint is not CadPoint || _activeFrame is not CadRect frame)
        {
            return;
        }

        var snapped = Snap(context, e.WorldPoint);
        BuildVolumeMullionPreview(context, snapped, frame);
        context.Invalidate?.Invoke();
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        if (_startPoint is not CadPoint || _activeFrame is not CadRect frame)
        {
            Reset();
            return;
        }

        var snapped = Snap(context, e.WorldPoint);
        var frameThickness = ResolveFrameThickness(context);
        var (innerMinX, innerMaxX, innerMinY, innerMaxY) = GetInnerOpeningBounds(frame, frameThickness);
        BuildVolumeMullionPreview(context, snapped, frame);

        if (_previewRects.Count < 1)
        {
            Reset();
            context.Invalidate?.Invoke();
            return;
        }

        var toolId = context.ActiveDesignToolId ?? Id;
        var label = context.ActiveDesignToolLabel ?? DisplayName;

        CadPoint commitStart;
        CadPoint commitEnd;
        if (Mode == MullionPlacementMode.Vertical)
        {
            commitStart = new CadPoint(snapped.X, innerMinY);
            commitEnd = new CadPoint(snapped.X, innerMaxY);
        }
        else if (Mode == MullionPlacementMode.Horizontal)
        {
            commitStart = new CadPoint(innerMinX, snapped.Y);
            commitEnd = new CadPoint(innerMaxX, snapped.Y);
        }
        else
        {
            commitStart = new CadPoint(innerMinX, innerMinY);
            commitEnd = new CadPoint(innerMaxX, innerMaxY);
        }

        context.CommitPlacement?.Invoke(new DesignPlacementCommit(
            toolId,
            label,
            DesignPlacementKind.MullionLine,
            commitStart,
            commitEnd));

        Reset();
        context.Invalidate?.Invoke();
    }

    private void BuildVolumeMullionPreview(CadToolContext context, CadPoint current, CadRect frame)
    {
        _previewRects.Clear();

        var profileThickness = ResolveFrameThickness(context);
        var mullionWidth = DefaultMullionWidth;
        var (innerMinX, innerMaxX, innerMinY, innerMaxY) = GetInnerOpeningBounds(frame, profileThickness);
        var innerHeight = innerMaxY - innerMinY;
        var innerWidth = innerMaxX - innerMinX;

        if (innerWidth <= mullionWidth || innerHeight <= mullionWidth)
        {
            return;
        }

        if (Mode == MullionPlacementMode.Vertical)
        {
            var centerX = current.X;
            var singleMullion = new CadRect(
                centerX - mullionWidth / 2.0,
                innerMaxY,
                mullionWidth,
                innerHeight);
            _previewRects.Add(singleMullion);
            return;
        }

        if (Mode == MullionPlacementMode.Horizontal)
        {
            var centerY = current.Y;
            var singleMullion = new CadRect(
                innerMinX,
                centerY + mullionWidth / 2.0,
                innerWidth,
                mullionWidth);
            _previewRects.Add(singleMullion);
        }
    }

    private static double ResolveFrameThickness(CadToolContext context) =>
        context.FrameProfileThickness > 0 ? context.FrameProfileThickness : DefaultFrameThickness;

    private static (double InnerMinX, double InnerMaxX, double InnerMinY, double InnerMaxY) GetInnerOpeningBounds(
        CadRect frame,
        double profileThickness)
    {
        var inset = FrameProfileGeometry.ResolveInnerInset(frame.Width, frame.Height, profileThickness);
        var innerMinX = frame.X + inset;
        var innerMaxX = frame.X + frame.Width - inset;
        var innerMinY = frame.Y - frame.Height + inset;
        var innerMaxY = frame.Y - inset;
        return (innerMinX, innerMaxX, innerMinY, innerMaxY);
    }

    private void Reset()
    {
        _startPoint = null;
        _activeFrame = null;
        _previewRects.Clear();
    }

    private static CadPoint Snap(CadToolContext context, CadPoint point) =>
        context.SnapPoint?.Invoke(point) ?? point;
}
