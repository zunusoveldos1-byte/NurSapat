using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Rendering;

namespace NurSapat.Cad.UI.Services.ImpostDragPlacement;

/// <summary>
/// Параметры сессии размещения импоста (контекст редактора).
/// </summary>
public readonly record struct ImpostDragPlacementSession(
    FrameNode? FrameRoot,
    double FrameThickness,
    WindowToolMode ToolMode,
    double ImpostWidthMm,
    ImpostLineStyle LineStyle,
    Func<CadPoint, CadPoint>? SnapWorldPoint);

/// <summary>
/// Размещение вертикального/горизонтального импоста жестом:
/// MouseDown → короткий жест → предпросмотр профиля → Snap → MouseUp → создание.
/// </summary>
public sealed class ImpostDragPlacementController
{
    public const double GestureActivationThresholdPx = 18.0;

    private enum PlacementPhase
    {
        Idle,
        AwaitingGesture,
        Placing
    }

    private PlacementPhase _phase = PlacementPhase.Idle;
    private ImpostDragPlacementPreview? _preview;
    private FrameNode? _leaf;
    private double _pressScreenX;
    private double _pressScreenY;

    public ImpostDragPlacementPreview? Preview => _preview;

    public bool IsPointerDown => _phase != PlacementPhase.Idle;

    public bool IsPlacing => _phase == PlacementPhase.Placing;

    public bool TryBegin(ImpostDragPlacementSession session, CadPoint worldPoint, double screenX, double screenY)
    {
        if (!IsDragPlacementTool(session.ToolMode) || session.FrameRoot is null)
        {
            return false;
        }

        var snapped = session.SnapWorldPoint?.Invoke(worldPoint) ?? worldPoint;
        var leaf = session.FrameRoot.FindLeafAt(snapped.X, snapped.Y);
        if (leaf is null || !leaf.IsLeaf || !leaf.CanAccept(session.ToolMode, session.ImpostWidthMm))
        {
            return false;
        }

        _phase = PlacementPhase.AwaitingGesture;
        _leaf = leaf;
        _pressScreenX = screenX;
        _pressScreenY = screenY;
        _preview = null;
        return true;
    }

    public void Update(
        ImpostDragPlacementSession session,
        CadPoint worldPoint,
        double screenX,
        double screenY)
    {
        if (_phase == PlacementPhase.Idle || _leaf is null || session.FrameRoot is null)
        {
            return;
        }

        if (_phase == PlacementPhase.AwaitingGesture && !TryActivateGesture(session, screenX, screenY))
        {
            return;
        }

        _preview = BuildPreview(session, _leaf, worldPoint);
    }

    public ImpostDragPlacementPreview? Cancel()
    {
        Reset();
        return null;
    }

    public bool TryGetCommitAxis(out FrameNode leaf, out double axisWorld)
    {
        leaf = null!;
        axisWorld = 0;
        if (_phase != PlacementPhase.Placing || _preview is not { IsValid: true } preview)
        {
            return false;
        }

        leaf = preview.Leaf;
        axisWorld = preview.AxisValue;
        return true;
    }

    public void EndDrag()
    {
        Reset();
    }

    private bool TryActivateGesture(
        ImpostDragPlacementSession session,
        double screenX,
        double screenY)
    {
        var deltaX = screenX - _pressScreenX;
        var deltaY = screenY - _pressScreenY;
        var activated = session.ToolMode == WindowToolMode.RealVerticalImpost
            ? Math.Abs(deltaY) >= GestureActivationThresholdPx
            : Math.Abs(deltaX) >= GestureActivationThresholdPx;

        if (!activated)
        {
            return false;
        }

        _phase = PlacementPhase.Placing;
        return true;
    }

    private void Reset()
    {
        _phase = PlacementPhase.Idle;
        _leaf = null;
        _preview = null;
    }

    private static ImpostDragPlacementPreview? BuildPreview(
        ImpostDragPlacementSession session,
        FrameNode leaf,
        CadPoint cursorWorld)
    {
        if (session.FrameRoot is null)
        {
            return null;
        }

        var inner = SashGeometry.ResolveLeafInnerOpeningBounds(leaf, session.FrameRoot, session.FrameThickness);
        if (inner.Width <= CadMath.Epsilon || inner.Height <= CadMath.Epsilon)
        {
            return null;
        }

        return session.ToolMode == WindowToolMode.RealVerticalImpost
            ? BuildVerticalPreview(session, leaf, inner, ResolveSnappedVerticalAxis(session, leaf, cursorWorld.X))
            : BuildHorizontalPreview(session, leaf, inner, ResolveSnappedHorizontalAxis(session, leaf, cursorWorld.Y));
    }

    private static double ResolveSnappedVerticalAxis(
        ImpostDragPlacementSession session,
        FrameNode leaf,
        double rawAxisX)
    {
        if (session.SnapWorldPoint is not { } snap)
        {
            return rawAxisX;
        }

        var anchorY = leaf.Y + leaf.Height / 2.0;
        return snap(new CadPoint(rawAxisX, anchorY)).X;
    }

    private static double ResolveSnappedHorizontalAxis(
        ImpostDragPlacementSession session,
        FrameNode leaf,
        double rawAxisY)
    {
        if (session.SnapWorldPoint is not { } snap)
        {
            return rawAxisY;
        }

        var anchorX = leaf.X + leaf.Width / 2.0;
        return snap(new CadPoint(anchorX, rawAxisY)).Y;
    }

    private static ImpostDragPlacementPreview BuildVerticalPreview(
        ImpostDragPlacementSession session,
        FrameNode leaf,
        SashGeometry.InnerOpeningBounds inner,
        double rawAxisX)
    {
        var thickness = session.ImpostWidthMm;
        var (minAxis, maxAxis) = ResolveVerticalAxisRange(leaf, thickness);
        var axisX = ImpostDragPlacementSnap.ResolveVerticalAxis(
            session.FrameRoot!,
            leaf,
            inner,
            rawAxisX,
            minAxis,
            maxAxis);

        var localX = axisX - leaf.X;
        var isValid = leaf.CanSplitVerticalAt(localX, session.LineStyle, thickness);

        return new ImpostDragPlacementPreview
        {
            Leaf = leaf,
            Orientation = SplitOrientation.Vertical,
            AxisValue = axisX,
            ImpostWidthMm = thickness,
            LineStyle = session.LineStyle,
            IsValid = isValid
        };
    }

    private static ImpostDragPlacementPreview BuildHorizontalPreview(
        ImpostDragPlacementSession session,
        FrameNode leaf,
        SashGeometry.InnerOpeningBounds inner,
        double rawAxisY)
    {
        var thickness = session.ImpostWidthMm;
        var (minAxis, maxAxis) = ResolveHorizontalAxisRange(leaf, session.FrameRoot!, thickness);
        var axisY = ImpostDragPlacementSnap.ResolveHorizontalAxis(
            session.FrameRoot!,
            leaf,
            inner,
            rawAxisY,
            minAxis,
            maxAxis);

        var localY = axisY - leaf.Y;
        var isValid = leaf.CanSplitHorizontalAt(localY, session.LineStyle, thickness);

        return new ImpostDragPlacementPreview
        {
            Leaf = leaf,
            Orientation = SplitOrientation.Horizontal,
            AxisValue = axisY,
            ImpostWidthMm = thickness,
            LineStyle = session.LineStyle,
            IsValid = isValid
        };
    }

    private static (double Min, double Max) ResolveVerticalAxisRange(FrameNode leaf, double thickness)
    {
        var half = thickness / 2.0;
        const double minCellMm = 40;
        var edge = leaf.ResolveEdgeLightOpeningInsets();
        var min = leaf.X + edge.Left + minCellMm + half;
        var max = leaf.X + leaf.Width - edge.Right - minCellMm - half;
        return (min, Math.Max(min, max));
    }

    private static (double Min, double Max) ResolveHorizontalAxisRange(
        FrameNode leaf,
        FrameNode frameRoot,
        double thickness)
    {
        var half = thickness / 2.0;
        const double minCellMm = 40;
        var edge = leaf.ResolveEdgeLightOpeningInsets();
        var splitExtent = FrameNode.ResolveHorizontalSplitExtentLocal(leaf, frameRoot);
        var min = leaf.Y + edge.Bottom + minCellMm + half;
        var max = leaf.Y + splitExtent - edge.Top - minCellMm - half;
        return (min, Math.Max(min, max));
    }

    public static bool IsDragPlacementTool(WindowToolMode mode) =>
        mode is WindowToolMode.RealVerticalImpost or WindowToolMode.RealHorizontalImpost;
}
