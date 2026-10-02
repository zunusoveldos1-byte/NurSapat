using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

internal static class HorizontalImpostGeometry
{
    public static (double Left, double Right) ResolveRectangularSpan(
        CadEditorViewModel editor,
        FrameNode splitNode)
    {
        if (editor.FrameRoot is null)
        {
            return (splitNode.X, splitNode.X + splitNode.Width);
        }

        var inner = TransitionAdjacentFrameGeometry.ResolveImpostHostInnerOpeningBounds(
            editor,
            splitNode,
            editor.FrameRoot,
            editor.FrameThickness);

        if (inner.Width <= CadMath.Epsilon)
        {
            return (splitNode.X, splitNode.X + splitNode.Width);
        }

        return (
            CadMath.RoundMm(Math.Clamp(inner.Left, splitNode.X, splitNode.X + splitNode.Width)),
            CadMath.RoundMm(Math.Clamp(inner.Right, splitNode.X, splitNode.X + splitNode.Width)));
    }

    public static (double Left, double Right) ResolveSpanAtY(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double worldY)
    {
        var container = splitNode.GetRoot();
        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, container)
            && CurvedContainerImpostGeometry.TryResolveHorizontalSpan(editor, container, worldY, out var circleLeft, out var circleRight))
        {
            return (circleLeft, circleRight);
        }

        var rectangular = ResolveRectangularSpan(editor, splitNode);
        if (!TryResolveArcSpringingGeometry(editor, out var arc))
        {
            return rectangular;
        }

        if (worldY <= arc.ChordY + CadMath.MmPrecision)
        {
            return rectangular;
        }

        if (TryResolveHorizontalSpanOnInnerArcAtY(arc, worldY, rectangular.Left, rectangular.Right, out var leftX, out var rightX))
        {
            return (leftX, rightX);
        }

        return rectangular;
    }

    /// <summary>
    /// Полуширина импоста на внутренней дуге: W_half = sqrt(R² − h²), h = Y − Y_center.
    /// </summary>
    public static bool TryResolveHorizontalSpanOnInnerArcAtY(
        ArcSpringingGeometry arc,
        double worldY,
        double clampMinX,
        double clampMaxX,
        out double leftX,
        out double rightX)
    {
        leftX = rightX = 0;
        if (worldY <= arc.ChordY + CadMath.MmPrecision)
        {
            return false;
        }

        var dy = worldY - arc.Center.Y;
        if (dy < -CadMath.Epsilon)
        {
            return false;
        }

        var underRoot = arc.InnerRadius * arc.InnerRadius - dy * dy;
        if (underRoot < -CadMath.Epsilon)
        {
            return false;
        }

        var halfSpan = Math.Sqrt(Math.Max(0, underRoot));
        leftX = CadMath.RoundMm(Math.Max(clampMinX, arc.Center.X - halfSpan));
        rightX = CadMath.RoundMm(Math.Min(clampMaxX, arc.Center.X + halfSpan));
        return rightX - leftX > CadMath.Epsilon;
    }

    public static bool TryResolveArcSpringingGeometry(
        CadEditorViewModel editor,
        out ArcSpringingGeometry arc)
    {
        arc = default;
        if (editor.FrameRoot is null)
        {
            return false;
        }

        if (FrameProfileGeometry.TryGetArchedFrameInnerArc(
                editor.FrameRoot,
                editor.FrameThickness,
                out arc))
        {
            return true;
        }

        if (!IsArchedFrameEditor(editor))
        {
            return false;
        }

        var root = editor.FrameRoot;
        var arcHeight = ResolveArcSagittaMm(root);
        if (arcHeight <= ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            return false;
        }

        var profileWall = FrameProfileGeometry.ResolveInnerInset(root.Width, root.Height, editor.FrameThickness);
        var chordY = root.Y + root.Height;
        arc = FrameProfileGeometry.ComputeArcSpringingGeometry(root.X, root.Width, chordY, arcHeight, profileWall);
        return true;
    }

    public static double ResolveArcSagittaMm(FrameNode frameRoot)
    {
        if (frameRoot.TopProfile?.SegmentType == SegmentType.Arc &&
            frameRoot.TopProfile.ArcHeight > ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            return frameRoot.TopProfile.ArcHeight;
        }

        return frameRoot.Width / 2.0;
    }

    public static double ResolveTotalFrameHeight(CadEditorViewModel editor, FrameNode splitNode)
    {
        var rectangularTop = splitNode.Y + splitNode.Height;
        if (!TryResolveArcSpringingGeometry(editor, out var arc))
        {
            return rectangularTop;
        }

        var apexY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.OuterRadius, arc.Center.X).Y;
        return Math.Max(rectangularTop, apexY);
    }

    public static bool TryResolveArcAdaptedSpans(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double bottom,
        double top,
        out double leftBottom,
        out double rightBottom,
        out double leftTop,
        out double rightTop)
    {
        leftBottom = rightBottom = leftTop = rightTop = 0;
        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, splitNode.GetRoot()))
        {
            return false;
        }

        var rectangular = ResolveRectangularSpan(editor, splitNode);
        (leftBottom, rightBottom) = ResolveSpanAtY(editor, splitNode, bottom);
        (leftTop, rightTop) = ResolveSpanAtY(editor, splitNode, top);

        var isAdapted =
            Math.Abs(leftBottom - leftTop) > CadMath.Epsilon ||
            Math.Abs(rightBottom - rightTop) > CadMath.Epsilon ||
            leftBottom > rectangular.Left + CadMath.Epsilon ||
            rightBottom < rectangular.Right - CadMath.Epsilon;

        return isAdapted;
    }

    public static List<CadPoint> BuildFootprintPolygon(
        double leftBottom,
        double rightBottom,
        double leftTop,
        double rightTop,
        double bottom,
        double top)
    {
        return
        [
            new(leftBottom, bottom),
            new(rightBottom, bottom),
            new(rightTop, top),
            new(leftTop, top)
        ];
    }

    public static bool ContainsPoint(
        IReadOnlyList<CadPoint> polygon,
        CadPoint point)
    {
        return ProfileBandHitTest.ContainsPointInPolygon(polygon, point);
    }

    public static bool IntersectsRect(IReadOnlyList<CadPoint> polygon, CadRect selection) =>
        ProfileBandHitTest.RectIntersectsPolygon(polygon, selection);

    public static double ResolveNodeExtentMaxY(CadEditorViewModel editor, FrameNode splitNode)
    {
        var maxY = splitNode.Y + splitNode.Height;
        if (!TryResolveArcSpringingGeometry(editor, out var arc))
        {
            return maxY;
        }

        if (!TouchesFrameTopForArchExtent(splitNode, editor.FrameRoot!, arc.ChordY))
        {
            return maxY;
        }

        var apexY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.OuterRadius, arc.Center.X).Y;
        return Math.Max(maxY, apexY);
    }

    private static bool TouchesFrameTopForArchExtent(FrameNode splitNode, FrameNode frameRoot, double chordY) =>
        CadMath.NearlyEqual(splitNode.Y + splitNode.Height, chordY)
        || splitNode.Y + splitNode.Height >= chordY - CadMath.MmPrecision
        || CadMath.NearlyEqual(splitNode.Y + splitNode.Height, frameRoot.Y + frameRoot.Height)
        || splitNode.Y + splitNode.Height >= frameRoot.Y + frameRoot.Height - CadMath.MmPrecision;

    public static double ResolveMaxCenterAxisY(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double halfThickness,
        double dragInsetMm)
    {
        const double minCellMm = 40;
        var extentMaxY = ResolveNodeExtentMaxY(editor, splitNode) - minCellMm - halfThickness;

        if (editor.FrameRoot is null)
        {
            return extentMaxY;
        }

        if (!TryResolveArcSpringingGeometry(editor, out _))
        {
            var inner = SashGeometry.ResolveNodeInnerOpeningBounds(
                splitNode,
                editor.FrameRoot,
                editor.FrameThickness);
            var innerLimit = inner.Top - dragInsetMm - halfThickness;
            return Math.Min(extentMaxY, innerLimit);
        }

        if (ResolveMaxDragAxisY(editor, splitNode, halfThickness) is double arcCenterLimit)
        {
            return Math.Min(extentMaxY, arcCenterLimit);
        }

        return extentMaxY;
    }

    public static double? ResolveMaxDragAxisY(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double halfThickness)
    {
        if (!TryResolveArcSpringingGeometry(editor, out var arc))
        {
            return null;
        }

        var minSpan = Math.Max(splitNode.GetResolvedImpostThicknessMm(), 40.0);
        var minHalfSpan = minSpan / 2.0;
        if (arc.InnerRadius <= minHalfSpan + CadMath.Epsilon)
        {
            return null;
        }

        var underRoot = arc.InnerRadius * arc.InnerRadius - minHalfSpan * minHalfSpan;
        var maxTopY = arc.Center.Y + Math.Sqrt(Math.Max(0, underRoot));
        var apexY = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, arc.Center.X).Y;
        maxTopY = Math.Min(maxTopY, apexY);
        var maxCenterY = maxTopY - halfThickness;

        if (maxCenterY <= arc.ChordY + halfThickness + CadMath.MmPrecision)
        {
            return null;
        }

        return CadMath.RoundMm(maxCenterY);
    }

    private static bool IsArchedFrameEditor(CadEditorViewModel editor) =>
        editor.CanvasElements.Any(element =>
            element.Kind is GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame);
}
