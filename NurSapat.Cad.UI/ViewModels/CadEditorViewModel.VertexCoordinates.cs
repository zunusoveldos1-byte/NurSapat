using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Tools;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Models;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private enum FrameCornerKind
    {
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight
    }

    private sealed record ParametricVertexHit(
        string ProfileRole,
        string Label,
        CadPoint CurrentPoint,
        FrameCornerKind? Corner = null,
        VerticalImpostTarget? Impost = null,
        bool IsArcSpringing = false);

    private bool TrySetVertexCoordinatesFromTool(CadPoint worldPoint)
    {
        var hit = ResolveParametricVertexHit(worldPoint);
        if (hit is null)
        {
            return false;
        }

        _ = EditVertexCoordinatesAsync(hit);
        return true;
    }

    private CadSelectionTarget? HitTestParametricVertex(CadPoint worldPoint)
    {
        var hit = ResolveParametricVertexHit(worldPoint);
        return hit is null
            ? null
            : new CadSelectionTarget(
                CadSelectionMode.Vertex,
                hit.Label,
                hit.CurrentPoint,
                ProfileRole: hit.ProfileRole);
    }

    private ParametricVertexHit? ResolveParametricVertexHit(CadPoint worldPoint)
    {
        if (FrameRoot is null)
        {
            return null;
        }

        var tolerance = Math.Max(ArcVertexHandleScreenPx * MillimetersPerPixel, 8.0);

        if (HasArcTopProfile && FrameRoot.TopProfile is { SegmentType: SegmentType.Arc } profile)
        {
            var handle = profile.GetArcSpringingHandleWorld();
            if (Distance(worldPoint, handle) <= tolerance)
            {
                return new ParametricVertexHit(
                    "ArcSpringing",
                    "Арочная вершина",
                    handle,
                    IsArcSpringing: true);
            }
        }

        ParametricVertexHit? bestImpost = null;
        var bestImpostDistance = double.MaxValue;

        if (HitTestVerticalImpost(worldPoint) is { } vertical
            && TryReadImpostAxisPoint(vertical, isVertical: true, out var verticalPoint))
        {
            var distance = Math.Abs(worldPoint.X - verticalPoint.X);
            if (distance <= ResolveVerticalImpostHitToleranceMm() && distance < bestImpostDistance)
            {
                bestImpostDistance = distance;
                bestImpost = new ParametricVertexHit(
                    "ImpostVertical",
                    "Ось вертикального импоста",
                    verticalPoint,
                    Impost: vertical);
            }
        }

        if (HitTestHorizontalImpost(worldPoint) is { } horizontal
            && TryReadImpostAxisPoint(horizontal, isVertical: false, out var horizontalPoint))
        {
            var distance = Math.Abs(worldPoint.Y - horizontalPoint.Y);
            if (distance <= ResolveVerticalImpostHitToleranceMm() && distance < bestImpostDistance)
            {
                bestImpost = new ParametricVertexHit(
                    "ImpostHorizontal",
                    "Ось горизонтального импоста",
                    horizontalPoint,
                    Impost: horizontal);
            }
        }

        if (bestImpost is not null)
        {
            return bestImpost;
        }

        ParametricVertexHit? bestCorner = null;
        var bestCornerDistance = double.MaxValue;
        foreach (var (corner, point) in EnumerateFrameCorners(FrameRoot))
        {
            var distance = Distance(worldPoint, point);
            if (distance > tolerance || distance >= bestCornerDistance)
            {
                continue;
            }

            bestCornerDistance = distance;
            bestCorner = new ParametricVertexHit(
                corner switch
                {
                    FrameCornerKind.BottomLeft => "FrameCornerBL",
                    FrameCornerKind.BottomRight => "FrameCornerBR",
                    FrameCornerKind.TopLeft => "FrameCornerTL",
                    _ => "FrameCornerTR"
                },
                "Угол коробки",
                point,
                Corner: corner);
        }

        return bestCorner;
    }

    private static IEnumerable<(FrameCornerKind Corner, CadPoint Point)> EnumerateFrameCorners(FrameNode root)
    {
        yield return (FrameCornerKind.BottomLeft, new CadPoint(root.X, root.Y));
        yield return (FrameCornerKind.BottomRight, new CadPoint(root.X + root.Width, root.Y));
        yield return (FrameCornerKind.TopLeft, new CadPoint(root.X, root.Y + root.Height));
        yield return (FrameCornerKind.TopRight, new CadPoint(root.X + root.Width, root.Y + root.Height));
    }

    private static bool TryReadImpostAxisPoint(
        VerticalImpostTarget target,
        bool isVertical,
        out CadPoint point)
    {
        var node = target.SplitNode;
        if (isVertical)
        {
            if (node.IsUniformSplit && target.UniformGapIndex is int gap)
            {
                if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, gap, out var axisX))
                {
                    point = default;
                    return false;
                }

                point = new CadPoint(axisX, node.Y + node.Height / 2.0);
                return true;
            }

            if (!node.TryGetVerticalImpostAxisWorldX(out var binaryAxisX))
            {
                point = default;
                return false;
            }

            point = new CadPoint(binaryAxisX, node.Y + node.Height / 2.0);
            return true;
        }

        if (node.IsUniformSplit && target.UniformGapIndex is int horizontalGap)
        {
            if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, horizontalGap, out var axisY))
            {
                point = default;
                return false;
            }

            point = new CadPoint(node.X + node.Width / 2.0, axisY);
            return true;
        }

        if (!node.TryGetHorizontalImpostAxisWorldY(out var binaryAxisY))
        {
            point = default;
            return false;
        }

        point = new CadPoint(node.X + node.Width / 2.0, binaryAxisY);
        return true;
    }

    private async Task EditVertexCoordinatesAsync(ParametricVertexHit hit)
    {
        try
        {
            var result = await _dialogService.ShowVertexCoordinateDialogAsync(
                hit.CurrentPoint.X,
                hit.CurrentPoint.Y,
                hit.Label);
            if (result is null)
            {
                StatusText = "Ввод координат отменён";
                return;
            }

            var applied = RecordPlacement(
                "Координаты вершины изменены",
                () => ApplyParametricVertexCoordinates(hit, result.X, result.Y));

            if (!applied)
            {
                RefreshAfterPlacementHistoryChange();
                StatusText = "Невозможно применить координаты без нарушения геометрии";
                return;
            }

            SyncFrameGeometryToDocument();
            RefreshAfterPlacementHistoryChange();
            StatusText = $"{hit.Label}: X={result.X:0}, Y={result.Y:0}";
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("EditVertexCoordinatesAsync failed", ex);
            StatusText = "Не удалось изменить координаты вершины";
        }
    }

    private bool ApplyParametricVertexCoordinates(ParametricVertexHit hit, double x, double y)
    {
        if (FrameRoot is null)
        {
            return false;
        }

        if (hit.IsArcSpringing)
        {
            if (FrameRoot.TopProfile is not { SegmentType: SegmentType.Arc } profile)
            {
                return false;
            }

            var chordY = Math.Min(profile.Start.Y, profile.End.Y);
            var arcHeight = profile.ClampArcHeight(chordY - y);
            return TryApplyTopProfileArcHeight(arcHeight);
        }

        if (hit.Impost is { } impost)
        {
            return string.Equals(hit.ProfileRole, "ImpostVertical", StringComparison.Ordinal)
                ? MoveVerticalImpost(impost, x)
                : MoveHorizontalImpost(impost, y);
        }

        if (hit.Corner is not { } corner)
        {
            return false;
        }

        var root = FrameRoot;
        var left = root.X;
        var bottom = root.Y;
        var right = root.X + root.Width;
        var top = root.Y + root.Height;

        double newLeft = left, newBottom = bottom, newRight = right, newTop = top;
        switch (corner)
        {
            case FrameCornerKind.BottomLeft:
                newLeft = x;
                newBottom = y;
                break;
            case FrameCornerKind.BottomRight:
                newRight = x;
                newBottom = y;
                break;
            case FrameCornerKind.TopLeft:
                newLeft = x;
                newTop = y;
                break;
            case FrameCornerKind.TopRight:
                newRight = x;
                newTop = y;
                break;
        }

        var newWidth = CadMath.RoundMm(newRight - newLeft);
        var newHeight = CadMath.RoundMm(newTop - newBottom);
        if (newWidth < MinEditableOpeningMm || newHeight < MinEditableOpeningMm)
        {
            return false;
        }

        var thickness = root.IsFictitiousContainer ? 0 : FrameThickness;
        if (!root.TryResizeOuter(newWidth, newHeight, thickness))
        {
            return false;
        }

        var deltaX = CadMath.RoundMm(newLeft) - root.X;
        var deltaY = CadMath.RoundMm(newBottom) - root.Y;
        if (Math.Abs(deltaX) > CadMath.Epsilon || Math.Abs(deltaY) > CadMath.Epsilon)
        {
            root.TranslateSubtree(deltaX, deltaY);
        }

        RecalculateFrameGeometry();
        return true;
    }
}
