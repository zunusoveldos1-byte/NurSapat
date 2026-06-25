using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.Cad.UI.Rendering;

public static class FrameNodeRenderer
{
    private static readonly IBrush ImpostFillBrush = Brushes.White;
    private static readonly IBrush FrameStrokeBrush = new SolidColorBrush(FrameVolumeRenderer.ProfileStrokeColor);
    private static readonly IPen DashedPen = new Pen(FrameStrokeBrush, 1.5, new DashStyle(new double[] { 5, 3 }, 0));
    private static readonly IPen SolidPen = new Pen(FrameStrokeBrush, 1.5);

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
    }

    private static void DrawSplitLine(DrawingContext context, CadEditorViewModel editor, FrameNode node)
    {
        if (node.LineStyle == ImpostLineStyle.Slanted)
        {
            DrawWorldLine(
                context,
                editor,
                SolidPen,
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

        var pen = node.LineStyle == ImpostLineStyle.Dashed ? DashedPen : SolidPen;
        var frameInset = ResolveFrameProfileInset(editor, node);
        var thickness = ResolveImpostThickness(node);

        if (node.SplitOrientation == SplitOrientation.Vertical)
        {
            var (yBottom, yTop) = ResolveVerticalSpan(node, frameInset);
            for (var i = 0; i < cells.Count - 1; i++)
            {
                if (!TryResolveUniformVerticalImpostBounds(node, i, out var left, out var right))
                {
                    continue;
                }

                DrawVerticalProfileVolume(context, editor, left, right, yBottom, yTop, pen);
            }
        }
        else
        {
            var (xLeft, xRight) = ResolveHorizontalSpan(node, frameInset);
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

                DrawHorizontalProfileVolume(context, editor, xLeft, xRight, bottom, top, pen);
            }
        }
    }

    private static void DrawSingleVerticalProfile(DrawingContext context, CadEditorViewModel editor, FrameNode node)
    {
        if (!TryResolveVerticalImpostBounds(node, out var left, out var right))
        {
            return;
        }

        var pen = node.LineStyle == ImpostLineStyle.Dashed ? DashedPen : SolidPen;
        var frameInset = ResolveFrameProfileInset(editor, node);
        var (yBottom, yTop) = ResolveVerticalSpan(node, frameInset);
        DrawVerticalProfileVolume(context, editor, left, right, yBottom, yTop, pen);
    }

    private static void DrawSingleHorizontalProfile(DrawingContext context, CadEditorViewModel editor, FrameNode node)
    {
        if (!TryResolveHorizontalImpostBounds(node, out var bottom, out var top))
        {
            return;
        }

        var pen = node.LineStyle == ImpostLineStyle.Dashed ? DashedPen : SolidPen;
        var frameInset = ResolveFrameProfileInset(editor, node);
        var (xLeft, xRight) = ResolveHorizontalSpan(node, frameInset);
        DrawHorizontalProfileVolume(context, editor, xLeft, xRight, bottom, top, pen);
    }

    private static double ResolveImpostThickness(FrameNode node) =>
        node.ImpostThicknessMm > CadMath.Epsilon
            ? node.ImpostThicknessMm
            : FrameNode.DefaultImpostProfileMm;

    /// <summary>
    /// Ось импоста = засечка размера; визуально расширен вправо под нахлест створки.
    /// </summary>
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
        right = CadMath.RoundMm(centerX + thickness);
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

    private static (double Bottom, double Top) ResolveVerticalSpan(FrameNode node, double frameInset)
    {
        var yBottom = node.Y + frameInset;
        var yTop = node.Y + node.Height - frameInset;
        if (yTop <= yBottom)
        {
            yBottom = node.Y;
            yTop = node.Y + node.Height;
        }

        return (yBottom, yTop);
    }

    private static (double Left, double Right) ResolveHorizontalSpan(FrameNode node, double frameInset)
    {
        var xLeft = node.X + frameInset;
        var xRight = node.X + node.Width - frameInset;
        if (xRight <= xLeft)
        {
            xLeft = node.X;
            xRight = node.X + node.Width;
        }

        return (xLeft, xRight);
    }

    private static double ResolveFrameProfileInset(CadEditorViewModel editor, FrameNode node)
    {
        if (node.Parent != null)
        {
            return 0;
        }

        return FrameProfileGeometry.ResolveInnerInset(node.Width, node.Height, editor.FrameThickness);
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
        var rect = WorldBoundsToScreenRect(editor, left, yBottom, right - left, yTop - yBottom);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        context.FillRectangle(ImpostFillBrush, rect);
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
        var rect = WorldBoundsToScreenRect(editor, xLeft, bottom, xRight - xLeft, top - bottom);
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        context.FillRectangle(ImpostFillBrush, rect);
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
