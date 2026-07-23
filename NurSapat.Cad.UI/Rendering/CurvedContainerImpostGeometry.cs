using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using System.Linq;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Пересечение горизонтальных/вертикальных импостов с круглой или арочной фиктивной коробкой.
/// </summary>
internal static class CurvedContainerImpostGeometry
{
    private const double FrameMatchToleranceMm = 1.0;

    [ThreadStatic]
    private static int _spanResolutionDepth;

    private const int MaxSpanResolutionDepth = 8;

    public static bool TryResolveInnerCircleGeometry(
        CadEditorViewModel editor,
        FrameNode container,
        out double centerX,
        out double centerY,
        out double innerRadius)
    {
        centerX = centerY = innerRadius = 0;
        if (!TryGetCircleFromContainerBounds(container, out centerX, out centerY, out var outerRadius))
        {
            return false;
        }

        var profileWall = FrameProfileGeometry.ResolveProfileThickness(
            container.Width,
            container.Height,
            editor.FrameThickness);
        innerRadius = Math.Max(outerRadius - profileWall, outerRadius * 0.25);
        return innerRadius > CadMath.Epsilon;
    }

    public static bool TryResolveVerticalImpostSpan(
        CadEditorViewModel editor,
        FrameNode container,
        FrameNode splitNode,
        double axisX,
        out double bottomY,
        out double topY)
    {
        bottomY = topY = 0;
        if (!IsRoundContainer(editor, container)
            || !TryResolveInnerCircleGeometry(editor, container, out var centerX, out var centerY, out var innerRadius))
        {
            return false;
        }

        if (!TryResolveVerticalCircleSpan(
                centerX,
                centerY,
                innerRadius,
                axisX,
                splitNode.Y,
                splitNode.Y + splitNode.Height,
                out bottomY,
                out topY))
        {
            return false;
        }

        (bottomY, topY) = SashGeometry.ClipVerticalImpostSpanAtHorizontalCrossings(splitNode, bottomY, topY);
        return topY - bottomY > CadMath.Epsilon;
    }

    public static bool TryResolveHorizontalImpostSpan(
        CadEditorViewModel editor,
        FrameNode container,
        FrameNode splitNode,
        double axisY,
        out double leftX,
        out double rightX)
    {
        leftX = rightX = 0;
        if (!IsRoundContainer(editor, container)
            || !TryResolveInnerCircleGeometry(editor, container, out var centerX, out var centerY, out var innerRadius))
        {
            return false;
        }

        return TryResolveHorizontalCircleSpan(
            centerX,
            centerY,
            innerRadius,
            axisY,
            splitNode.X,
            splitNode.X + splitNode.Width,
            out leftX,
            out rightX);
    }
    public static bool HasCurvedContainerGeometry(CadEditorViewModel editor) =>
        IsRoundContainer(editor)
        || IsArchedContainer(editor);

    public static bool IsRoundContainer(CadEditorViewModel editor, FrameNode? container = null)
    {
        container ??= editor.FrameRoot;
        if (container is null)
        {
            return false;
        }

        if (!editor.CanvasElements.Any(element => element.Kind == GeometryElementKind.RoundDummyFrame))
        {
            return false;
        }

        if (TryFindRoundFrameElement(editor, container) is not null)
        {
            return true;
        }

        return container.IsFictitiousContainer && ReferenceEquals(container, editor.FrameRoot);
    }

    public static bool TryResolveCircleGeometry(
        CadEditorViewModel editor,
        out double centerX,
        out double centerY,
        out double radius) =>
        TryResolveCircleGeometry(editor, editor.FrameRoot, out centerX, out centerY, out radius);

    /// <summary>
    /// Геометрия круга только по габаритам контейнера — без обхода дерева и CanvasElements.
    /// </summary>
    public static bool TryGetCircleFromContainerBounds(
        double originX,
        double originY,
        double width,
        double height,
        out double centerX,
        out double centerY,
        out double radius)
    {
        centerX = centerY = radius = 0;
        if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
        {
            return false;
        }

        centerX = originX + width / 2.0;
        centerY = originY + height / 2.0;
        radius = width / 2.0;
        return radius > CadMath.Epsilon;
    }

    public static bool TryGetCircleFromContainerBounds(
        FrameNode container,
        out double centerX,
        out double centerY,
        out double radius) =>
        TryGetCircleFromContainerBounds(
            container.X,
            container.Y,
            container.Width,
            container.Height,
            out centerX,
            out centerY,
            out radius);

    public static bool TryResolveCircleGeometry(
        CadEditorViewModel editor,
        FrameNode? container,
        out double centerX,
        out double centerY,
        out double radius)
    {
        centerX = centerY = radius = 0;
        container ??= editor.FrameRoot;
        if (container is null)
        {
            return false;
        }

        if (!IsRoundContainer(editor, container))
        {
            return false;
        }

        return TryGetCircleFromContainerBounds(container, out centerX, out centerY, out radius);
    }

    public static bool TryCalculateImpostLineEndpoints(
        CadEditorViewModel editor,
        FrameNode container,
        SplitOrientation orientation,
        double axisPosition,
        out CadPoint start,
        out CadPoint end)
    {
        start = end = default;
        if (orientation == SplitOrientation.Vertical)
        {
            if (!TryResolveVerticalSpan(editor, container, axisPosition, out var bottomY, out var topY))
            {
                return false;
            }

            start = new CadPoint(axisPosition, bottomY);
            end = new CadPoint(axisPosition, topY);
            return true;
        }

        if (orientation == SplitOrientation.Horizontal)
        {
            if (!TryResolveHorizontalSpan(editor, container, axisPosition, out var leftX, out var rightX))
            {
                return false;
            }

            start = new CadPoint(leftX, axisPosition);
            end = new CadPoint(rightX, axisPosition);
            return true;
        }

        return false;
    }

    public static bool IsArchedContainer(CadEditorViewModel editor) =>
        editor.CanvasElements.Any(element =>
            element.Kind is GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame)
        || editor.FrameRoot?.TopProfile?.SegmentType == SegmentType.Arc;

    public static bool TryResolveHorizontalCircleSpan(
        double centerX,
        double centerY,
        double radius,
        double axisY,
        double clampMinX,
        double clampMaxX,
        out double leftX,
        out double rightX)
    {
        leftX = rightX = 0;
        var deltaY = axisY - centerY;
        var underRoot = radius * radius - deltaY * deltaY;
        if (underRoot < -CadMath.Epsilon)
        {
            return false;
        }

        var halfLength = Math.Sqrt(Math.Max(0, underRoot));
        leftX = CadMath.RoundMm(centerX - halfLength);
        rightX = CadMath.RoundMm(centerX + halfLength);
        if (leftX < clampMinX - CadMath.Epsilon || rightX > clampMaxX + CadMath.Epsilon)
        {
            leftX = CadMath.RoundMm(Math.Max(clampMinX, leftX));
            rightX = CadMath.RoundMm(Math.Min(clampMaxX, rightX));
        }

        return rightX - leftX > CadMath.Epsilon;
    }

    public static bool TryResolveVerticalCircleSpan(
        double centerX,
        double centerY,
        double radius,
        double worldX,
        double clampMinY,
        double clampMaxY,
        out double bottomY,
        out double topY)
    {
        bottomY = topY = 0;
        var dx = worldX - centerX;
        var underRoot = radius * radius - dx * dx;
        if (underRoot < -CadMath.Epsilon)
        {
            return false;
        }

        var halfSpan = Math.Sqrt(Math.Max(0, underRoot));
        bottomY = CadMath.RoundMm(Math.Max(clampMinY, centerY - halfSpan));
        topY = CadMath.RoundMm(Math.Min(clampMaxY, centerY + halfSpan));
        return topY - bottomY > CadMath.Epsilon;
    }

    public static bool TryResolveHorizontalSpan(
        CadEditorViewModel editor,
        FrameNode container,
        double axisY,
        out double leftX,
        out double rightX)
    {
        leftX = container.X;
        rightX = container.X + container.Width;

        if (_spanResolutionDepth >= MaxSpanResolutionDepth)
        {
            return rightX - leftX > CadMath.Epsilon;
        }

        _spanResolutionDepth++;
        try
        {
            if (IsRoundContainer(editor, container)
                && TryResolveInnerCircleGeometry(editor, container, out var centerX, out var centerY, out var innerRadius))
            {
                return TryResolveHorizontalCircleSpan(
                    centerX,
                    centerY,
                    innerRadius,
                    axisY,
                    leftX,
                    rightX,
                    out leftX,
                    out rightX);
            }

            if (!IsArchedContainer(editor))
            {
                return true;
            }

            var rectangular = HorizontalImpostGeometry.ResolveRectangularSpan(editor, container);
            if (HorizontalImpostGeometry.TryResolveArcSpringingGeometry(editor, out var arc))
            {
                if (axisY <= arc.ChordY + CadMath.MmPrecision)
                {
                    leftX = rectangular.Left;
                    rightX = rectangular.Right;
                    return true;
                }

                if (HorizontalImpostGeometry.TryResolveHorizontalSpanOnInnerArcAtY(
                        arc,
                        axisY,
                        rectangular.Left,
                        rectangular.Right,
                        out leftX,
                        out rightX))
                {
                    return true;
                }

                return false;
            }

            leftX = rectangular.Left;
            rightX = rectangular.Right;
            return true;
        }
        finally
        {
            _spanResolutionDepth--;
        }
    }

    /// <summary>
    /// Расчёт горизонтального импоста по чистым габаритам (без обхода FrameNode).
    /// </summary>
    public static bool TryResolveHorizontalSpan(
        double originX,
        double originY,
        double width,
        double height,
        double axisY,
        out double leftX,
        out double rightX)
    {
        leftX = originX;
        rightX = originX + width;

        if (!TryGetCircleFromContainerBounds(originX, originY, width, height, out var centerX, out var centerY, out var radius))
        {
            return rightX - leftX > CadMath.Epsilon;
        }

        return TryResolveHorizontalCircleSpan(
            centerX,
            centerY,
            radius,
            axisY,
            leftX,
            rightX,
            out leftX,
            out rightX);
    }

    public static bool TryResolveVerticalSpan(
        CadEditorViewModel editor,
        FrameNode container,
        double axisX,
        out double bottomY,
        out double topY)
    {
        bottomY = container.Y;
        topY = container.Y + container.Height;

        if (_spanResolutionDepth >= MaxSpanResolutionDepth)
        {
            return topY - bottomY > CadMath.Epsilon;
        }

        _spanResolutionDepth++;
        try
        {
            if (IsRoundContainer(editor, container)
                && TryResolveInnerCircleGeometry(editor, container, out var centerX, out var centerY, out var innerRadius))
            {
                return TryResolveVerticalCircleSpan(
                    centerX,
                    centerY,
                    innerRadius,
                    axisX,
                    bottomY,
                    topY,
                    out bottomY,
                    out topY);
            }

            if (!IsArchedContainer(editor) || editor.FrameRoot is null)
            {
                return true;
            }

            if (FrameProfileGeometry.TryGetArchedFrameInnerArc(editor.FrameRoot, editor.FrameThickness, out var innerArc))
            {
                var arcTopY = ProfileSegment.GetPointOnUpperArc(innerArc.Center, innerArc.InnerRadius, axisX).Y;
                topY = CadMath.RoundMm(Math.Min(topY, Math.Max(bottomY, arcTopY)));
                return topY - bottomY > CadMath.Epsilon;
            }

            if (HorizontalImpostGeometry.TryResolveArcSpringingGeometry(editor, out var springArc))
            {
                var arcTopY = ProfileSegment.GetPointOnUpperArc(springArc.Center, springArc.InnerRadius, axisX).Y;
                if (arcTopY > springArc.ChordY + CadMath.MmPrecision)
                {
                    topY = CadMath.RoundMm(Math.Min(topY, arcTopY));
                }

                return topY - bottomY > CadMath.Epsilon;
            }

            return true;
        }
        finally
        {
            _spanResolutionDepth--;
        }
    }

    /// <summary>
    /// Расчёт вертикального импоста по чистым габаритам (без обхода FrameNode).
    /// </summary>
    public static bool TryResolveVerticalSpan(
        double originX,
        double originY,
        double width,
        double height,
        double axisX,
        out double bottomY,
        out double topY)
    {
        bottomY = originY;
        topY = originY + height;

        if (!TryGetCircleFromContainerBounds(originX, originY, width, height, out var centerX, out var centerY, out var radius))
        {
            return topY - bottomY > CadMath.Epsilon;
        }

        return TryResolveVerticalCircleSpan(
            centerX,
            centerY,
            radius,
            axisX,
            bottomY,
            topY,
            out bottomY,
            out topY);
    }

    public static bool TryResolveMullionSpan(
        CadEditorViewModel editor,
        CadPoint point,
        char orientation,
        out CadPoint start,
        out CadPoint end)
    {
        start = end = default;
        if (!HasCurvedContainerGeometry(editor))
        {
            return false;
        }

        var container = editor.FrameRoot;
        if (container is null)
        {
            var host = FindHostFrameElement(editor, point);
            if (host is null)
            {
                return false;
            }

            var width = (double)host.WidthMm;
            var height = (double)host.HeightMm;
            var originX = (double)host.X - width / 2.0;
            var originY = (double)host.Y - height / 2.0;

            if (orientation == 'h')
            {
                if (!TryResolveHorizontalSpan(originX, originY, width, height, point.Y, out var leftX, out var rightX))
                {
                    return false;
                }

                start = new CadPoint(leftX, point.Y);
                end = new CadPoint(rightX, point.Y);
                return true;
            }

            if (orientation == 'v')
            {
                if (!TryResolveVerticalSpan(originX, originY, width, height, point.X, out var bottomY, out var topY))
                {
                    return false;
                }

                start = new CadPoint(point.X, bottomY);
                end = new CadPoint(point.X, topY);
                return true;
            }

            return false;
        }

        if (orientation == 'h')
        {
            if (!TryResolveHorizontalSpan(editor, container, point.Y, out var leftX, out var rightX))
            {
                return false;
            }

            start = new CadPoint(leftX, point.Y);
            end = new CadPoint(rightX, point.Y);
            return true;
        }

        if (orientation == 'v')
        {
            if (!TryResolveVerticalSpan(editor, container, point.X, out var bottomY, out var topY))
            {
                return false;
            }

            start = new CadPoint(point.X, bottomY);
            end = new CadPoint(point.X, topY);
            return true;
        }

        return false;
    }

    private static GeometryElement? TryFindRoundFrameElement(CadEditorViewModel editor, FrameNode? container)
    {
        if (container is null)
        {
            return null;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.RoundDummyFrame)
            {
                continue;
            }

            if (FrameMatchesContainer(container, element))
            {
                return element;
            }
        }

        return null;
    }

    private static bool FrameMatchesContainer(FrameNode container, GeometryElement element)
    {
        var width = (double)element.WidthMm;
        var height = (double)element.HeightMm;
        var originX = (double)element.X - width / 2.0;
        var originY = (double)element.Y - height / 2.0;
        return Math.Abs(container.X - originX) < FrameMatchToleranceMm
            && Math.Abs(container.Y - originY) < FrameMatchToleranceMm
            && Math.Abs(container.Width - width) < FrameMatchToleranceMm
            && Math.Abs(container.Height - height) < FrameMatchToleranceMm;
    }

    private static GeometryElement? FindHostFrameElement(CadEditorViewModel editor, CadPoint point)
    {
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind is not (GeometryElementKind.RoundDummyFrame
                or GeometryElementKind.ArchDummyFrame
                or GeometryElementKind.ArchFrame))
            {
                continue;
            }

            var halfW = (double)element.WidthMm / 2.0;
            var halfH = (double)element.HeightMm / 2.0;
            var centerX = (double)element.X;
            var centerY = (double)element.Y;
            if (point.X >= centerX - halfW - CadMath.Epsilon
                && point.X <= centerX + halfW + CadMath.Epsilon
                && point.Y >= centerY - halfH - CadMath.Epsilon
                && point.Y <= centerY + halfH + CadMath.Epsilon)
            {
                return element;
            }
        }

        return null;
    }
}
