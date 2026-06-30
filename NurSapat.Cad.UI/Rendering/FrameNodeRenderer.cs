using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Rendering;

public static class FrameNodeRenderer
{
    private static readonly IBrush ImpostFillBrush = Brushes.White;
    private static readonly IPen HighlightPen = new Pen(
        new SolidColorBrush(Color.Parse("#FF0404")),
        FrameVolumeRenderer.ProfileStrokeThickness);
    private static readonly IBrush GhostImpostFillBrush = new SolidColorBrush(Color.Parse("#50FF0404"));
    private static readonly IPen GhostImpostPen = new Pen(
        new SolidColorBrush(Color.Parse("#FF0404")),
        FrameVolumeRenderer.ProfileStrokeThickness,
        new DashStyle(new double[] { 6, 4 }, 0));

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

        var thickness = ResolveImpostThickness(node);

        if (node.SplitOrientation == SplitOrientation.Vertical)
        {
            var (yBottom, yTop) = ResolveVerticalImpostSpan(editor, node);
            for (var i = 0; i < cells.Count - 1; i++)
            {
                if (!TryResolveUniformVerticalImpostBounds(node, i, out var left, out var right))
                {
                    continue;
                }

                DrawVerticalProfileVolume(context, editor, left, right, yBottom, yTop, ResolveImpostPen(editor, node, i));
            }
        }
        else
        {
            var (xLeft, xRight) = ResolveHorizontalImpostSpan(editor, node);
            for (var i = 0; i < cells.Count - 1; i++)
            {
                if (!TryResolveHorizontalImpostBounds(
                        cells[i].Y + cells[i].Height,
                        cells[i + 1].Y,
                        thickness,
                        out var bottom,
                        out var top))
                {
                    continue;
                }

                DrawHorizontalProfileVolume(
                    context,
                    editor,
                    xLeft,
                    xRight,
                    bottom,
                    top,
                    ResolveHorizontalImpostPen(editor, node, i));
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
        var (yBottom, yTop) = ResolveVerticalImpostSpan(editor, node);
        DrawVerticalProfileVolume(context, editor, left, right, yBottom, yTop, pen);
    }

    private static void DrawSingleHorizontalProfile(DrawingContext context, CadEditorViewModel editor, FrameNode node)
    {
        if (!TryResolveHorizontalImpostBounds(node, out var bottom, out var top))
        {
            return;
        }

        var pen = ResolveHorizontalImpostPen(editor, node);
        var (xLeft, xRight) = ResolveHorizontalImpostSpan(editor, node);
        DrawHorizontalProfileVolume(context, editor, xLeft, xRight, bottom, top, pen);
    }

    private static double ResolveImpostThickness(FrameNode node) =>
        node.ImpostThicknessMm > CadMath.Epsilon
            ? node.ImpostThicknessMm
            : FrameNode.DefaultImpostProfileMm;

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

    private static bool TryResolveHorizontalImpostBounds(FrameNode node, out double bottom, out double top)
    {
        if (node.ChildA is null || node.ChildB is null)
        {
            bottom = top = 0;
            return false;
        }

        return TryResolveHorizontalImpostBounds(
            node.ChildA.Y + node.ChildA.Height,
            node.ChildB.Y,
            ResolveImpostThickness(node),
            out bottom,
            out top);
    }

    private static bool TryResolveHorizontalImpostBounds(
        double gapBottom,
        double gapTop,
        double thickness,
        out double bottom,
        out double top)
    {
        if (gapTop - gapBottom <= CadMath.Epsilon || thickness <= CadMath.Epsilon)
        {
            bottom = top = 0;
            return false;
        }

        var centerY = gapBottom + (gapTop - gapBottom) / 2.0;
        var half = thickness / 2.0;
        bottom = centerY - half;
        top = centerY + half;
        return top - bottom > CadMath.Epsilon;
    }

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

    private static (double Bottom, double Top) ResolveVerticalImpostSpan(CadEditorViewModel editor, FrameNode splitNode)
    {
        if (editor.FrameRoot is null)
        {
            return (splitNode.Y, splitNode.Y + splitNode.Height);
        }

        var inner = SashGeometry.ResolveNodeInnerOpeningBounds(
            splitNode,
            editor.FrameRoot,
            editor.FrameThickness);

        if (inner.Height <= CadMath.Epsilon)
        {
            return (splitNode.Y, splitNode.Y + splitNode.Height);
        }

        return (
            CadMath.RoundMm(Math.Clamp(inner.Bottom, splitNode.Y, splitNode.Y + splitNode.Height)),
            CadMath.RoundMm(Math.Clamp(inner.Top, splitNode.Y, splitNode.Y + splitNode.Height)));
    }

    private static (double Left, double Right) ResolveHorizontalImpostSpan(CadEditorViewModel editor, FrameNode splitNode)
    {
        if (editor.FrameRoot is null)
        {
            return (splitNode.X, splitNode.X + splitNode.Width);
        }

        var inner = SashGeometry.ResolveNodeInnerOpeningBounds(
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

    private static void DrawVerticalProfileVolume(
        DrawingContext context,
        CadEditorViewModel editor,
        double left,
        double right,
        double yBottom,
        double yTop,
        IPen pen)
    {
        DrawVerticalProfileVolume(context, editor, left, right, yBottom, yTop, ImpostFillBrush, pen);
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

        var (yBottom, yTop) = ResolveVerticalImpostSpan(editor, node);
        DrawVerticalProfileVolume(context, editor, left, right, yBottom, yTop, GhostImpostFillBrush, GhostImpostPen);
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

        var (xLeft, xRight) = ResolveHorizontalImpostSpan(editor, node);
        DrawHorizontalProfileVolume(context, editor, xLeft, xRight, bottom, top, GhostImpostFillBrush, GhostImpostPen);
    }

    private static void DrawVerticalProfileVolume(
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

    private static void DrawHorizontalProfileVolume(
        DrawingContext context,
        CadEditorViewModel editor,
        double xLeft,
        double xRight,
        double bottom,
        double top,
        IPen pen)
    {
        DrawHorizontalProfileVolume(context, editor, xLeft, xRight, bottom, top, ImpostFillBrush, pen);
    }

    private static void DrawHorizontalProfileVolume(
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
