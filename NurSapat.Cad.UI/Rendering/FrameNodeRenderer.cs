using System.Linq;
using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Services.ImpostDragPlacement;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

public static class FrameNodeRenderer
{
    private static readonly IBrush ImpostFillBrush = Brushes.White;
    private static readonly IPen HighlightPen = new Pen(
        FrameVolumeRenderer.SelectionHighlightBrush,
        FrameVolumeRenderer.ProfileStrokeThickness);
    private static readonly IBrush GhostImpostFillBrush = new SolidColorBrush(Color.Parse("#50FF0404"));
    private static readonly IBrush InvalidPlacementFillBrush = new SolidColorBrush(Color.Parse("#FFCCCC"));
    private static readonly IPen InvalidPlacementPen = new Pen(
        new SolidColorBrush(Color.Parse("#CC2222")),
        FrameVolumeRenderer.ProfileStrokeThickness);
    private static readonly IPen GhostImpostPen = new Pen(
        FrameVolumeRenderer.SelectionHighlightBrush,
        FrameVolumeRenderer.ProfileStrokeThickness,
        new DashStyle(new double[] { 6, 4 }, 0));

    public static void DrawDragPlacementPreview(
        DrawingContext context,
        CadEditorViewModel editor,
        ImpostDragPlacementPreview preview)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        var fill = preview.IsValid ? ImpostFillBrush : InvalidPlacementFillBrush;
        var pen = preview.IsValid
            ? FrameVolumeRenderer.ResolveProfileOutlinePen(preview.LineStyle == ImpostLineStyle.Dashed)
            : InvalidPlacementPen;
        var leaf = preview.Leaf;
        var thickness = preview.ImpostWidthMm;
        var container = leaf.GetRoot();

        if (preview.Orientation == SplitOrientation.Vertical)
        {
            if (!TryResolveVerticalImpostRenderBounds(preview.AxisValue, thickness, out var left, out var right))
            {
                return;
            }

            var yBottom = ResolveVerticalImpostBottom(editor, leaf, preview.AxisValue);
            if (CurvedContainerImpostGeometry.IsRoundContainer(editor, container)
                && CurvedContainerImpostGeometry.TryResolveVerticalImpostSpan(
                    editor,
                    container,
                    leaf,
                    preview.AxisValue,
                    out var bottomY,
                    out var topY))
            {
                DrawVerticalProfileVolumeRect(context, editor, left, right, bottomY, topY, fill, pen);
                return;
            }

            DrawVerticalProfileVolume(context, editor, leaf, left, right, yBottom, fill, pen);
            return;
        }

        if (!TryResolveHorizontalImpostRenderBounds(preview.AxisValue, thickness, out var bottom, out var top))
        {
            return;
        }

        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, container)
            && CurvedContainerImpostGeometry.TryResolveHorizontalImpostSpan(
                editor,
                container,
                leaf,
                preview.AxisValue,
                out var leftX,
                out var rightX))
        {
            DrawHorizontalProfileVolumeRect(context, editor, leftX, rightX, bottom, top, fill, pen);
            return;
        }

        DrawHorizontalProfileVolume(context, editor, leaf, bottom, top, fill, pen);
    }

    public static void DrawImposts(DrawingContext context, CadEditorViewModel editor, FrameNode? root)
    {
        if (editor?.Viewport == null || root == null)
        {
            return;
        }

        foreach (var node in root.EnumerateInternalNodes())
        {
            DrawSplitLine(context, editor, node);
        }

        DrawDraggingVerticalImpostGhost(context, editor);
        DrawDraggingHorizontalImpostGhost(context, editor);
    }

    private static void DrawSplitLine(DrawingContext context, CadEditorViewModel editor, FrameNode node)
    {
        if (IsFictitiousFlatImpost(editor, node))
        {
            DrawFictitiousFlatImpost(context, editor, node);
            return;
        }

        if (node.LineStyle == ImpostLineStyle.Slanted)
        {
            DrawWorldLine(
                context,
                editor,
                ResolveImpostPen(editor, node),
                new CadPoint(node.X, node.Y),
                new CadPoint(node.X + node.Width, node.Y + node.Height));
            return;
        }

        if (node.IsUniformSplit)
        {
            DrawUniformSplitProfiles(context, editor, node);
            return;
        }

        switch (node.SplitOrientation)
        {
            case SplitOrientation.Vertical:
                DrawSingleVerticalProfile(context, editor, node);
                break;
            case SplitOrientation.Horizontal:
                DrawSingleHorizontalProfile(context, editor, node);
                break;
        }
    }

    private static void DrawUniformSplitProfiles(DrawingContext context, CadEditorViewModel editor, FrameNode node)
    {
        var cells = node.UniformCells;
        if (cells.Count < 2)
        {
            return;
        }

        var container = node.GetRoot();
        var isRoundContainer = CurvedContainerImpostGeometry.IsRoundContainer(editor, container);

        if (node.SplitOrientation == SplitOrientation.Vertical)
        {
            for (var i = 0; i < cells.Count - 1; i++)
            {
                if (!TryResolveUniformVerticalImpostBounds(node, i, out var left, out var right))
                {
                    continue;
                }

                var pen = ResolveImpostPen(editor, node, i);
                if (isRoundContainer
                    && node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, i, out var axisX)
                    && CurvedContainerImpostGeometry.TryResolveVerticalImpostSpan(editor, container, node, axisX, out var bottomY, out var topY))
                {
                    DrawVerticalProfileVolumeRect(context, editor, left, right, bottomY, topY, ImpostFillBrush, pen);
                    continue;
                }

                var yBottom = ResolveVerticalImpostBottom(editor, node, (left + right) / 2.0);
                DrawVerticalProfileVolume(context, editor, node, left, right, yBottom, pen);
            }
        }
        else
        {
            for (var i = 0; i < cells.Count - 1; i++)
            {
                if (!TryResolveUniformHorizontalImpostBounds(node, i, out var bottom, out var top))
                {
                    continue;
                }

                var pen = ResolveHorizontalImpostPen(editor, node, i);
                if (isRoundContainer
                    && node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i, out var axisY)
                    && CurvedContainerImpostGeometry.TryResolveHorizontalImpostSpan(editor, container, node, axisY, out var leftX, out var rightX))
                {
                    DrawHorizontalProfileVolumeRect(context, editor, leftX, rightX, bottom, top, ImpostFillBrush, pen);
                    continue;
                }

                DrawHorizontalProfileVolume(
                    context,
                    editor,
                    node,
                    bottom,
                    top,
                    pen);
            }
        }
    }

    private static void DrawSingleVerticalProfile(DrawingContext context, CadEditorViewModel editor, FrameNode node)
    {
        if (!TryResolveVerticalImpostBounds(node, out var left, out var right))
        {
            return;
        }

        var pen = ResolveImpostPen(editor, node, uniformGapIndex: 0);
        var container = node.GetRoot();
        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, container)
            && node.TryGetVerticalImpostAxisWorldX(out var axisX)
            && CurvedContainerImpostGeometry.TryResolveVerticalImpostSpan(editor, container, node, axisX, out var bottomY, out var topY))
        {
            DrawVerticalProfileVolumeRect(context, editor, left, right, bottomY, topY, ImpostFillBrush, pen);
            return;
        }

        var yBottom = ResolveVerticalImpostBottom(editor, node, (left + right) / 2.0);
        DrawVerticalProfileVolume(context, editor, node, left, right, yBottom, ImpostFillBrush, pen);
    }

    private static void DrawSingleHorizontalProfile(DrawingContext context, CadEditorViewModel editor, FrameNode node)
    {
        if (!TryResolveHorizontalImpostDrawBounds(node, out var bottom, out var top, out var renderAxisY))
        {
            return;
        }

        if (editor.IsHorizontalImpostDragging)
        {
            var gapIndex = editor.SelectedMullion?.UniformGapIndex;
            HorizontalImpostDragDiagnostics.Trace(
                "render-real",
                ghostAxisY: editor.DraggingMullionWorldY,
                modelAxisY: HorizontalImpostDragDiagnostics.TryReadModelAxisY(node, gapIndex),
                renderAxisY: renderAxisY,
                splitNode: node,
                frameRoot: editor.FrameRoot);
        }

        var pen = ResolveHorizontalImpostPen(editor, node);
        var container = node.GetRoot();
        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, container)
            && node.TryGetHorizontalImpostAxisWorldY(out var axisY)
            && CurvedContainerImpostGeometry.TryResolveHorizontalImpostSpan(editor, container, node, axisY, out var leftX, out var rightX))
        {
            DrawHorizontalProfileVolumeRect(context, editor, leftX, rightX, bottom, top, ImpostFillBrush, pen);
            return;
        }

        DrawHorizontalProfileVolume(context, editor, node, bottom, top, pen);
    }

    private static bool TryResolveHorizontalImpostDrawBounds(
        FrameNode node,
        out double bottom,
        out double top,
        out double renderAxisY)
    {
        bottom = top = renderAxisY = 0;
        if (!TryResolveHorizontalImpostBounds(node, out bottom, out top))
        {
            return false;
        }

        renderAxisY = (bottom + top) / 2.0;
        return true;
    }

    private static bool IsFictitiousFlatImpost(CadEditorViewModel editor, FrameNode node) =>
        (editor.FrameRoot?.IsFictitiousContainer == true
         || CurvedContainerImpostGeometry.IsRoundContainer(editor))
        && node.LineStyle == ImpostLineStyle.Dashed
        && node.GetResolvedImpostThicknessMm() <= CadMath.Epsilon;

    private static void DrawFictitiousFlatImpost(DrawingContext context, CadEditorViewModel editor, FrameNode node)
    {
        var pen = ResolveImpostPen(editor, node);
        var container = node.GetRoot();

        if (node.IsUniformSplit)
        {
            var cells = node.UniformCells;
            if (cells.Count < 2)
            {
                return;
            }

            if (node.SplitOrientation == SplitOrientation.Vertical)
            {
                for (var i = 0; i < cells.Count - 1; i++)
                {
                    if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, i, out var axisX))
                    {
                        axisX = cells[i].X + cells[i].Width;
                    }

                    DrawCurvedVerticalImpost(context, editor, container, pen, axisX);
                }
            }
            else
            {
                for (var i = 0; i < cells.Count - 1; i++)
                {
                    if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, i, out var axisY))
                    {
                        axisY = cells[i].Y + cells[i].Height;
                    }

                    DrawCurvedHorizontalImpost(context, editor, container, pen, axisY);
                }
            }

            return;
        }

        if (node.ChildA is null || node.ChildB is null)
        {
            return;
        }

        if (node.SplitOrientation == SplitOrientation.Vertical)
        {
            var axisX = node.ChildA.X + node.ChildA.Width;
            DrawCurvedVerticalImpost(context, editor, container, pen, axisX);
        }
        else if (node.SplitOrientation == SplitOrientation.Horizontal)
        {
            if (!node.TryGetHorizontalImpostAxisWorldY(out var axisY))
            {
                axisY = node.ChildA.Y + node.ChildA.Height;
            }

            DrawCurvedHorizontalImpost(context, editor, container, pen, axisY);
        }
    }

    private static void DrawCurvedHorizontalImpost(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode container,
        IPen pen,
        double axisY)
    {
        if (!CurvedContainerImpostGeometry.TryResolveHorizontalSpan(editor, container, axisY, out var leftX, out var rightX))
        {
            return;
        }

        DrawWorldLine(
            context,
            editor,
            pen,
            new CadPoint(leftX, axisY),
            new CadPoint(rightX, axisY));
    }

    private static void DrawCurvedVerticalImpost(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode container,
        IPen pen,
        double axisX)
    {
        if (!CurvedContainerImpostGeometry.TryResolveVerticalSpan(editor, container, axisX, out var bottomY, out var topY))
        {
            return;
        }

        DrawWorldLine(
            context,
            editor,
            pen,
            new CadPoint(axisX, bottomY),
            new CadPoint(axisX, topY));
    }

    /// <summary>
    /// Толщина профиля импоста для отрисовки: тот же источник, что и у модели
    /// (<see cref="FrameNode.GetResolvedImpostThicknessMm"/>).
    /// </summary>
    private static double ResolveImpostThickness(FrameNode node) =>
        node.GetResolvedImpostThicknessMm();

    private static IPen ResolveImpostPen(CadEditorViewModel editor, FrameNode node, int uniformGapIndex = 0)
    {
        if (editor.IsVerticalImpostHighlighted(node, uniformGapIndex))
        {
            return HighlightPen;
        }

        return FrameVolumeRenderer.ResolveProfileOutlinePen(node.LineStyle == ImpostLineStyle.Dashed);
    }

    private static IPen ResolveHorizontalImpostPen(CadEditorViewModel editor, FrameNode node, int uniformGapIndex = 0)
    {
        if (editor.IsHorizontalImpostHighlighted(node, uniformGapIndex))
        {
            return HighlightPen;
        }

        return FrameVolumeRenderer.ResolveProfileOutlinePen(node.LineStyle == ImpostLineStyle.Dashed);
    }

    private static bool TryResolveVerticalImpostBounds(FrameNode node, out double left, out double right)
    {
        left = right = 0;
        if (!node.TryGetVerticalImpostAxisWorldX(out var centerX))
        {
            return false;
        }

        return TryResolveVerticalImpostRenderBounds(centerX, ResolveImpostThickness(node), out left, out right);
    }

    private static bool TryResolveUniformVerticalImpostBounds(
        FrameNode node,
        int gapIndex,
        out double left,
        out double right)
    {
        left = right = 0;
        if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, gapIndex, out var centerX))
        {
            return false;
        }

        return TryResolveVerticalImpostRenderBounds(centerX, ResolveImpostThickness(node), out left, out right);
    }

    /// <summary>
    /// Ось импоста = засечка размера; профиль симметричен относительно оси.
    /// </summary>
    private static bool TryResolveVerticalImpostRenderBounds(
        double centerX,
        double thickness,
        out double left,
        out double right)
    {
        if (thickness <= CadMath.Epsilon)
        {
            left = right = 0;
            return false;
        }

        var half = thickness / 2.0;
        left = CadMath.RoundMm(centerX - half);
        right = CadMath.RoundMm(centerX + half);
        return right - left > CadMath.Epsilon;
    }

    /// <summary>
    /// Горизонтальный импост: ось = <see cref="FrameNode.TryGetHorizontalImpostAxisWorldY"/>
    /// (та же координата, что у размерной сетки и пунктира), профиль симметричен относительно оси.
    /// </summary>
    private static bool TryResolveHorizontalImpostBounds(FrameNode node, out double bottom, out double top)
    {
        bottom = top = 0;
        if (!node.TryGetHorizontalImpostAxisWorldY(out var centerY))
        {
            return false;
        }

        return TryResolveHorizontalImpostRenderBounds(centerY, ResolveImpostThickness(node), out bottom, out top);
    }

    private static bool TryResolveUniformHorizontalImpostBounds(
        FrameNode node,
        int gapIndex,
        out double bottom,
        out double top)
    {
        bottom = top = 0;
        if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var centerY))
        {
            return false;
        }

        return TryResolveHorizontalImpostRenderBounds(centerY, ResolveImpostThickness(node), out bottom, out top);
    }

    /// <summary>
    /// Ось импоста = засечка размера; профиль симметричен относительно оси (как у вертикального).
    /// </summary>
    private static bool TryResolveHorizontalImpostRenderBounds(
        double centerY,
        double thickness,
        out double bottom,
        out double top)
    {
        if (thickness <= CadMath.Epsilon)
        {
            bottom = top = 0;
            return false;
        }

        var half = thickness / 2.0;
        bottom = CadMath.RoundMm(centerY - half);
        top = CadMath.RoundMm(centerY + half);
        return top - bottom > CadMath.Epsilon;
    }

    private static double ResolveVerticalImpostBottom(CadEditorViewModel editor, FrameNode splitNode, double axisX)
    {
        var (bottom, _) = ResolveVerticalImpostSpan(editor, splitNode, axisX);
        return bottom;
    }

    /// <summary>
    /// Верхняя граница узла для импоста: коробка + стрела арки (не только хорда).
    /// </summary>
    private static double ResolveVerticalImpostExtentMaxY(CadEditorViewModel editor, FrameNode splitNode)
    {
        var maxY = splitNode.Y + splitNode.Height;
        var profile = editor.FrameRoot?.TopProfile;
        if (profile?.SegmentType == SegmentType.Arc &&
            profile.ArcHeight > ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            maxY = Math.Max(maxY, splitNode.Y + splitNode.Height + profile.ArcHeight);
        }

        return maxY;
    }

    /// <summary>
    /// Верх импоста — строгое пересечение вертикальных граней профиля с внутренней дугой (R_inner).
    /// </summary>
    private static bool TryResolveVerticalImpostArcTrim(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double centerX,
        double left,
        double right,
        double rectangularTop,
        out ArcSpringingGeometry arc)
    {
        arc = default;

        if (editor.FrameRoot?.TopProfile?.SegmentType != SegmentType.Arc ||
            !FrameProfileGeometry.TryGetArchedFrameInnerArc(editor.FrameRoot, editor.FrameThickness, out arc))
        {
            return false;
        }

        return VerticalImpostReachesArc(editor, splitNode, centerX, left, right, rectangularTop, arc);
    }

    private static double ResolveInnerArcTopYAtX(ArcSpringingGeometry arc, double worldX, double minY) =>
        CadMath.RoundMm(Math.Max(ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, worldX).Y, minY));

    private static double ResolveMinInnerArcTopYBetweenX(
        ArcSpringingGeometry arc,
        double xLeft,
        double xRight,
        double minY)
    {
        var left = Math.Min(xLeft, xRight);
        var right = Math.Max(xLeft, xRight);
        var minTopY = double.MaxValue;
        var segments = Math.Clamp((int)Math.Ceiling((right - left) / 15.0), 2, 16);
        for (var i = 0; i <= segments; i++)
        {
            var x = left + (right - left) * i / segments;
            minTopY = Math.Min(minTopY, ResolveInnerArcTopYAtX(arc, x, minY));
        }

        return minTopY;
    }

    private static bool VerticalImpostReachesArc(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double centerX,
        double left,
        double right,
        double rectangularTop,
        ArcSpringingGeometry arc)
    {
        var inner = SashGeometry.ResolveNodeInnerOpeningBounds(
            splitNode,
            editor.FrameRoot!,
            editor.FrameThickness);

        if (inner.PinsFrameTop)
        {
            return true;
        }

        var chordY = editor.FrameRoot!.Y + editor.FrameRoot.Height;
        if (rectangularTop >= chordY - CadMath.MmPrecision)
        {
            return true;
        }

        var minArcTopY = ResolveMinInnerArcTopYBetweenX(arc, left, right, splitNode.Y);
        return minArcTopY > rectangularTop + CadMath.Epsilon;
    }

    private static double ResolveVerticalImpostRectangularTop(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double left,
        double right,
        double spanTop,
        double yBottom)
    {
        if (editor.FrameRoot is null ||
            !FrameProfileGeometry.TryGetArchedFrameInnerArc(editor.FrameRoot, editor.FrameThickness, out var arc))
        {
            return spanTop;
        }

        var cappedTop = ResolveMinInnerArcTopYBetweenX(arc, left, right, yBottom);
        return Math.Min(spanTop, cappedTop);
    }

    private static (double Bottom, double Top) ResolveVerticalImpostSpan(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisX)
    {
        var container = splitNode.GetRoot();
        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, container))
        {
            var defaultSpan = (splitNode.Y, splitNode.Y + splitNode.Height);
            if (splitNode.TryGetVerticalImpostAxisWorldX(out var axisX)
                || (splitNode.IsUniformSplit
                    && splitNode.SplitOrientation == SplitOrientation.Vertical
                    && splitNode.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, 0, out axisX)))
            {
                if (CurvedContainerImpostGeometry.TryResolveVerticalImpostSpan(editor, container, splitNode, axisX, out var bottomY, out var topY))
                {
                    return (bottomY, topY);
                }
            }

            return defaultSpan;
        }

        if (editor.FrameRoot?.IsFictitiousContainer == true)
        {
            return ResolveFictitiousVerticalImpostSpan(editor, splitNode, impostAxisX);
        }

        if (editor.FrameRoot is null)
        {
            return (splitNode.Y, splitNode.Y + splitNode.Height);
        }

        var (bottom, top) = SashGeometry.ResolveVerticalImpostStructuralSpan(
            splitNode,
            editor.FrameRoot,
            editor.FrameThickness);

        var maxY = ResolveVerticalImpostExtentMaxY(editor, splitNode);
        return (
            CadMath.RoundMm(bottom),
            CadMath.RoundMm(Math.Clamp(top, bottom, maxY)));
    }

    /// <summary>
    /// Вертикальный пролёт в фиктивном контейнере: строго по внутренним граням
    /// светового проёма ячейки (импостные оси + рама), с уточнением у вложенных коробок.
    /// </summary>
    internal static (double Bottom, double Top) ResolveFictitiousVerticalImpostSpan(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisX)
    {
        var boundsLeaf = SashGeometry.TryGetVerticalImpostColumnCell(splitNode, impostAxisX, out var columnCell)
            ? columnCell
            : splitNode;
        var inner = TransitionAdjacentFrameGeometry.ResolveHostInnerOpeningBounds(
            editor,
            boundsLeaf,
            editor.FrameRoot!,
            editor.FrameThickness);

        var bottom = inner.Bottom;
        var top = inner.Top;
        ClipVerticalImpostSpanAtNestedFrames(editor, splitNode, impostAxisX, ref bottom, ref top);
        TransitionAdjacentFrameGeometry.ClipVerticalImpostSpanAtTransitionHosts(
            editor,
            splitNode,
            impostAxisX,
            ref bottom,
            ref top);
        TransitionAdjacentFrameGeometry.ClipVerticalSpanAtTransitionShelves(
            editor,
            splitNode,
            impostAxisX,
            ref bottom,
            ref top);
        PinVerticalImpostTopToNestedFrameInnerShelf(editor, splitNode, impostAxisX, ref top);
        TransitionAdjacentFrameGeometry.PinVerticalImpostTopToTransitionHostInnerShelf(
            editor,
            splitNode,
            impostAxisX,
            ref top);

        return (
            CadMath.RoundMm(bottom),
            CadMath.RoundMm(Math.Max(bottom, top)));
    }

    /// <summary>
    /// Т-образная подрезка вертикального импоста внутри фиктивного контейнера:
    /// концы упираются во внутренние грани горизонтальных профилей вложенной
    /// коробки (высота = чистый световой проём), не перекрывая её угловые резы.
    /// </summary>
    internal static void ClipVerticalImpostSpanAtNestedFrames(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisX,
        ref double bottom,
        ref double top)
    {
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind is not (GeometryElementKind.Frame or GeometryElementKind.ArchFrame))
            {
                continue;
            }

            var width = (double)element.WidthMm;
            var height = (double)element.HeightMm;
            if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
            {
                continue;
            }

            var frameLeft = (double)element.X - width / 2.0;
            var frameRight = frameLeft + width;
            var frameBottom = (double)element.Y - height / 2.0;
            var frameTop = frameBottom + height;

            // Импост должен проходить внутри коробки: ось строго между стойками.
            if (impostAxisX <= frameLeft + CadMath.MmPrecision
                || impostAxisX >= frameRight - CadMath.MmPrecision)
            {
                continue;
            }

            if (frameTop <= bottom + CadMath.Epsilon || frameBottom >= top - CadMath.Epsilon)
            {
                continue;
            }

            var inset = FrameProfileGeometry.ResolveInnerInset(width, height, editor.FrameThickness);
            var frameInnerBottom = CadMath.RoundMm(frameBottom + inset);
            var frameInnerTop = CadMath.RoundMm(frameTop - inset);
            bottom = Math.Max(bottom, frameInnerBottom);

            ApplyNestedFrameVerticalImpostTopJoint(
                splitNode,
                impostAxisX,
                frameTop,
                frameInnerTop,
                ref top);
        }
    }

    /// <summary>
    /// Т-стык верха вертикального импоста с внутренней полкой вложенной коробки:
    /// при касании верхней рамы (в т.ч. смежной ячейкой) — точный snap, иначе — подрезка.
    /// </summary>
    private static void ApplyNestedFrameVerticalImpostTopJoint(
        FrameNode splitNode,
        double impostAxisX,
        double frameTop,
        double frameInnerTop,
        ref double top)
    {
        if (SashGeometry.VerticalImpostColumnTouchesFrameTop(
                splitNode,
                impostAxisX,
                frameTop,
                frameInnerTop,
                top))
        {
            top = frameInnerTop;
            return;
        }

        top = Math.Min(top, frameInnerTop);
    }

    /// <summary>
    /// Финальный snap верха к физической внутренней полке вложенных коробок
    /// (после transition-clipping, который может сузить top через Math.Min).
    /// </summary>
    private static void PinVerticalImpostTopToNestedFrameInnerShelf(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisX,
        ref double top)
    {
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind is not (GeometryElementKind.Frame or GeometryElementKind.ArchFrame))
            {
                continue;
            }

            var width = (double)element.WidthMm;
            var height = (double)element.HeightMm;
            if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
            {
                continue;
            }

            var frameLeft = (double)element.X - width / 2.0;
            var frameRight = frameLeft + width;
            var frameBottom = (double)element.Y - height / 2.0;
            var frameTop = frameBottom + height;

            if (impostAxisX <= frameLeft + CadMath.MmPrecision
                || impostAxisX >= frameRight - CadMath.MmPrecision)
            {
                continue;
            }

            var inset = FrameProfileGeometry.ResolveInnerInset(width, height, editor.FrameThickness);
            var frameInnerTop = CadMath.RoundMm(frameTop - inset);

            if (SashGeometry.VerticalImpostColumnTouchesFrameTop(
                    splitNode,
                    impostAxisX,
                    frameTop,
                    frameInnerTop,
                    top))
            {
                top = frameInnerTop;
            }
        }
    }

    /// <summary>
    /// Т-образная подрезка горизонтального импоста: правый конец упирается
    /// во внутреннюю вертикальную полку вложенной коробки.
    /// </summary>
    internal static void ClipHorizontalImpostSpanAtNestedFrames(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisY,
        ref double left,
        ref double right)
    {
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind is not (GeometryElementKind.Frame or GeometryElementKind.ArchFrame))
            {
                continue;
            }

            var width = (double)element.WidthMm;
            var height = (double)element.HeightMm;
            if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
            {
                continue;
            }

            var frameLeft = (double)element.X - width / 2.0;
            var frameRight = frameLeft + width;
            var frameBottom = (double)element.Y - height / 2.0;
            var frameTop = frameBottom + height;

            if (impostAxisY <= frameBottom + CadMath.MmPrecision
                || impostAxisY >= frameTop - CadMath.MmPrecision)
            {
                continue;
            }

            if (frameRight <= left + CadMath.Epsilon || frameLeft >= right - CadMath.Epsilon)
            {
                continue;
            }

            var inset = FrameProfileGeometry.ResolveInnerInset(width, height, editor.FrameThickness);
            var frameInnerLeft = CadMath.RoundMm(frameLeft + inset);
            var frameInnerRight = CadMath.RoundMm(frameRight - inset);
            left = Math.Max(left, frameInnerLeft);

            ApplyNestedFrameHorizontalImpostRightJoint(
                splitNode,
                impostAxisY,
                frameRight,
                frameInnerRight,
                ref right);
        }
    }

    /// <summary>
    /// Т-стык правого конца горизонтального импоста с внутренней полкой вложенной коробки.
    /// </summary>
    private static void ApplyNestedFrameHorizontalImpostRightJoint(
        FrameNode splitNode,
        double impostAxisY,
        double frameRight,
        double frameInnerRight,
        ref double right)
    {
        if (SashGeometry.HorizontalImpostRowTouchesFrameRight(
                splitNode,
                frameRight,
                frameInnerRight,
                right,
                impostAxisY))
        {
            right = frameInnerRight;
            return;
        }

        right = Math.Min(right, frameInnerRight);
    }

    /// <summary>
    /// Финальный snap правого конца к внутренней полке вложенных коробок.
    /// </summary>
    internal static void PinHorizontalImpostRightToNestedFrameInnerShelf(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisY,
        ref double right)
    {
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind is not (GeometryElementKind.Frame or GeometryElementKind.ArchFrame))
            {
                continue;
            }

            var width = (double)element.WidthMm;
            var height = (double)element.HeightMm;
            if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
            {
                continue;
            }

            var frameLeft = (double)element.X - width / 2.0;
            var frameRight = frameLeft + width;
            var frameBottom = (double)element.Y - height / 2.0;
            var frameTop = frameBottom + height;

            if (impostAxisY <= frameBottom + CadMath.MmPrecision
                || impostAxisY >= frameTop - CadMath.MmPrecision)
            {
                continue;
            }

            var inset = FrameProfileGeometry.ResolveInnerInset(width, height, editor.FrameThickness);
            var frameInnerRight = CadMath.RoundMm(frameRight - inset);

            if (SashGeometry.HorizontalImpostRowTouchesFrameRight(
                    splitNode,
                    frameRight,
                    frameInnerRight,
                    right,
                    impostAxisY))
            {
                right = frameInnerRight;
            }
        }
    }


    private static void DrawVerticalProfileVolume(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode splitNode,
        double left,
        double right,
        double yBottom,
        IPen pen) =>
        DrawVerticalProfileVolume(context, editor, splitNode, left, right, yBottom, ImpostFillBrush, pen);

    private static void DrawVerticalProfileVolume(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode splitNode,
        double left,
        double right,
        double yBottom,
        IBrush fill,
        IPen pen)
    {
        var centerX = (left + right) / 2.0;
        var rectangularTop = ResolveVerticalImpostRectangularTop(
            editor,
            splitNode,
            left,
            right,
            ResolveVerticalImpostSpan(editor, splitNode, centerX).Top,
            yBottom);
        var minY = splitNode.Y;

        if (TryResolveVerticalImpostArcTrim(
                editor,
                splitNode,
                centerX,
                left,
                right,
                rectangularTop,
                out var arc))
        {
            var yTopLeft = ResolveInnerArcTopYAtX(arc, left, minY);
            var yTopRight = ResolveInnerArcTopYAtX(arc, right, minY);
            if (yTopLeft - yBottom <= CadMath.Epsilon && yTopRight - yBottom <= CadMath.Epsilon)
            {
                return;
            }

            DrawVerticalProfileArcTrimmed(context, editor, arc, left, right, yBottom, fill, pen);
            return;
        }

        if (editor.FrameRoot?.Shape == ConstructionShape.Triangle
            && TryResolveVerticalImpostTriangleTrim(editor, left, right, yBottom, out var triTopLeft, out var triTopRight))
        {
            if (triTopLeft - yBottom <= CadMath.Epsilon && triTopRight - yBottom <= CadMath.Epsilon)
            {
                return;
            }

            DrawVerticalProfilePolygon(context, editor, left, right, yBottom, triTopLeft, triTopRight, fill, pen);
            return;
        }

        if (rectangularTop - yBottom <= CadMath.Epsilon)
        {
            return;
        }

        DrawVerticalProfileVolumeRect(context, editor, left, right, yBottom, rectangularTop, fill, pen);
    }

    private static void DrawVerticalProfileVolumeRect(
        DrawingContext context,
        CadEditorViewModel editor,
        double left,
        double right,
        double yBottom,
        double yTop,
        IBrush fill,
        IPen pen)
    {
        var rect = WorldBoundsToScreenRect(editor, left, yBottom, right - left, yTop - yBottom);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        context.FillRectangle(fill, rect);
        context.DrawRectangle(null, pen, rect);
    }

    private static void DrawVerticalProfileArcTrimmed(
        DrawingContext context,
        CadEditorViewModel editor,
        ArcSpringingGeometry arc,
        double left,
        double right,
        double yBottom,
        IBrush fill,
        IPen pen)
    {
        var bottomLeft = WorldToScreenPoint(editor, left, yBottom);
        var bottomRight = WorldToScreenPoint(editor, right, yBottom);
        var arcTopPoints = TessellateInnerArcTopBetweenX(arc, left, right, yBottom);
        if (arcTopPoints.Count < 2)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var gctx = geometry.Open())
        {
            gctx.BeginFigure(bottomLeft, isFilled: true);
            gctx.LineTo(bottomRight);
            for (var i = arcTopPoints.Count - 1; i >= 0; i--)
            {
                var point = arcTopPoints[i];
                gctx.LineTo(WorldToScreenPoint(editor, point.X, point.Y));
            }

            gctx.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    private static List<CadPoint> TessellateInnerArcTopBetweenX(
        ArcSpringingGeometry arc,
        double xLeft,
        double xRight,
        double yBottom)
    {
        var left = Math.Min(xLeft, xRight);
        var right = Math.Max(xLeft, xRight);
        var span = right - left;
        var segments = Math.Clamp((int)Math.Ceiling(span / 15.0), 2, 16);
        var points = new List<CadPoint>(segments + 1);
        for (var i = 0; i <= segments; i++)
        {
            var x = left + span * i / segments;
            var point = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.InnerRadius, x);
            if (point.Y >= yBottom - CadMath.Epsilon)
            {
                points.Add(point);
            }
        }

        return points;
    }

    private static bool TryResolveVerticalImpostTriangleTrim(
        CadEditorViewModel editor,
        double left,
        double right,
        double yBottom,
        out double topLeft,
        out double topRight)
    {
        topLeft = topRight = 0;
        if (editor.FrameRoot?.Shape != ConstructionShape.Triangle)
        {
            return false;
        }

        var innerTri = FrameTriangleGeometry.BuildInnerTriangle(editor.FrameRoot, editor.FrameThickness);
        topLeft = FrameTriangleGeometry.InterpolateTopAtX(innerTri.Bl, innerTri.Br, innerTri.Top, left);
        topRight = FrameTriangleGeometry.InterpolateTopAtX(innerTri.Bl, innerTri.Br, innerTri.Top, right);
        return topLeft > yBottom + CadMath.Epsilon || topRight > yBottom + CadMath.Epsilon;
    }

    private static void DrawVerticalProfilePolygon(
        DrawingContext context,
        CadEditorViewModel editor,
        double left,
        double right,
        double yBottom,
        double yTopLeft,
        double yTopRight,
        IBrush fill,
        IPen pen)
    {
        var bottomLeft = WorldToScreenPoint(editor, left, yBottom);
        var bottomRight = WorldToScreenPoint(editor, right, yBottom);
        var topRight = WorldToScreenPoint(editor, right, yTopRight);
        var topLeft = WorldToScreenPoint(editor, left, yTopLeft);

        var geometry = new StreamGeometry();
        using (var gctx = geometry.Open())
        {
            gctx.BeginFigure(bottomLeft, isFilled: true);
            gctx.LineTo(bottomRight);
            gctx.LineTo(topRight);
            gctx.LineTo(topLeft);
            gctx.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    private static Point WorldToScreenPoint(CadEditorViewModel editor, double worldX, double worldY)
    {
        var screen = editor.Viewport!.WorldToScreen(new CadPoint(worldX, worldY));
        return new Point(screen.X, screen.Y);
    }

    private static void DrawDraggingVerticalImpostGhost(DrawingContext context, CadEditorViewModel editor)
    {
        if (editor.SelectedMullion is not { } target || editor.DraggingMullionWorldX is not double axisX)
        {
            return;
        }

        var node = target.SplitNode;
        if (!TryResolveVerticalImpostRenderBounds(axisX, ResolveImpostThickness(node), out var left, out var right))
        {
            return;
        }

        var yBottom = ResolveVerticalImpostBottom(editor, node, axisX);
        var container = node.GetRoot();
        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, container)
            && CurvedContainerImpostGeometry.TryResolveVerticalImpostSpan(editor, container, node, axisX, out var bottomY, out var topY))
        {
            DrawVerticalProfileVolumeRect(context, editor, left, right, bottomY, topY, GhostImpostFillBrush, GhostImpostPen);
            return;
        }

        DrawVerticalProfileVolume(context, editor, node, left, right, yBottom, GhostImpostFillBrush, GhostImpostPen);
    }

    private static void DrawDraggingHorizontalImpostGhost(DrawingContext context, CadEditorViewModel editor)
    {
        if (editor.SelectedMullion is not { } target || editor.DraggingMullionWorldY is not double axisY)
        {
            return;
        }

        var node = target.SplitNode;
        if (!TryResolveHorizontalImpostRenderBounds(axisY, ResolveImpostThickness(node), out var bottom, out var top))
        {
            return;
        }

        if (editor.IsHorizontalImpostDragging)
        {
            var gapIndex = target.UniformGapIndex;
            HorizontalImpostDragDiagnostics.Trace(
                "render-ghost",
                ghostAxisY: axisY,
                modelAxisY: HorizontalImpostDragDiagnostics.TryReadModelAxisY(node, gapIndex),
                renderAxisY: axisY,
                splitNode: node,
                frameRoot: editor.FrameRoot);
        }

        var container = node.GetRoot();
        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, container)
            && CurvedContainerImpostGeometry.TryResolveHorizontalImpostSpan(editor, container, node, axisY, out var leftX, out var rightX))
        {
            DrawHorizontalProfileVolumeRect(context, editor, leftX, rightX, bottom, top, GhostImpostFillBrush, GhostImpostPen);
            return;
        }

        DrawHorizontalProfileVolume(context, editor, node, bottom, top, GhostImpostFillBrush, GhostImpostPen);
    }

    private static void DrawHorizontalProfileVolume(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode splitNode,
        double bottom,
        double top,
        IPen pen) =>
        DrawHorizontalProfileVolume(context, editor, splitNode, bottom, top, ImpostFillBrush, pen);

    private static void DrawHorizontalProfileVolume(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode splitNode,
        double bottom,
        double top,
        IBrush fill,
        IPen pen)
    {
        var container = splitNode.GetRoot();
        var centerAxisY = (bottom + top) / 2.0;
        if (CurvedContainerImpostGeometry.IsRoundContainer(editor, container)
            && CurvedContainerImpostGeometry.TryResolveHorizontalImpostSpan(editor, container, splitNode, centerAxisY, out var circleLeft, out var circleRight))
        {
            DrawHorizontalProfileVolumeRect(context, editor, circleLeft, circleRight, bottom, top, fill, pen);
            return;
        }

        if (HorizontalImpostGeometry.TryResolveArcAdaptedSpans(
                editor,
                splitNode,
                bottom,
                top,
                out var leftBottom,
                out var rightBottom,
                out var leftTop,
                out var rightTop))
        {
            DrawHorizontalProfilePolygon(
                context,
                editor,
                leftBottom,
                rightBottom,
                leftTop,
                rightTop,
                bottom,
                top,
                fill,
                pen);
            return;
        }

        var (left, right) = HorizontalImpostGeometry.ResolveSpanAtY(editor, splitNode, (bottom + top) / 2.0);
        DrawHorizontalProfileVolumeRect(context, editor, left, right, bottom, top, fill, pen);
    }

    private static void DrawHorizontalProfileVolumeRect(
        DrawingContext context,
        CadEditorViewModel editor,
        double xLeft,
        double xRight,
        double bottom,
        double top,
        IBrush fill,
        IPen pen)
    {
        var rect = WorldBoundsToScreenRect(editor, xLeft, bottom, xRight - xLeft, top - bottom);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        context.FillRectangle(fill, rect);
        context.DrawRectangle(null, pen, rect);
    }

    private static void DrawHorizontalProfilePolygon(
        DrawingContext context,
        CadEditorViewModel editor,
        double leftBottom,
        double rightBottom,
        double leftTop,
        double rightTop,
        double bottom,
        double top,
        IBrush fill,
        IPen pen)
    {
        var polygon = HorizontalImpostGeometry.BuildFootprintPolygon(
            leftBottom,
            rightBottom,
            leftTop,
            rightTop,
            bottom,
            top);
        var screenPoints = polygon
            .Select(point => WorldToScreenPoint(editor, point.X, point.Y))
            .ToList();
        if (screenPoints.Count < 3)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var gctx = geometry.Open())
        {
            gctx.BeginFigure(screenPoints[0], isFilled: true);
            for (var i = 1; i < screenPoints.Count; i++)
            {
                gctx.LineTo(screenPoints[i]);
            }

            gctx.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    private static void DrawWorldLine(DrawingContext context, CadEditorViewModel editor, IPen pen, CadPoint start, CadPoint end)
    {
        var p1 = editor.Viewport!.WorldToScreen(start);
        var p2 = editor.Viewport.WorldToScreen(end);
        context.DrawLine(pen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
    }

    private static Rect WorldBoundsToScreenRect(CadEditorViewModel editor, double originX, double originY, double width, double height)
    {
        var topLeft = editor.Viewport!.WorldToScreen(new CadPoint(originX, originY + height));
        var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(originX + width, originY));
        return new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
    }
}
