using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Services;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

public static class ExtremeImpostRenderer
{
    private static readonly IPen PreviewPen = new Pen(
        new SolidColorBrush(Color.Parse("#CC22C55E")),
        3);

    private static readonly IBrush ProfileFillBrush = Brushes.White;
    private static readonly IPen ProfilePen = new Pen(Brushes.Black, FrameVolumeRenderer.ProfileStrokeThickness);

    public static void DrawHoverPreview(DrawingContext context, CadEditorViewModel editor, ExtremeImpostHoverPreview preview)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        var half = preview.ThicknessMm / 2.0;
        var edgeWorld = preview.IsVertical
            ? preview.AxisWorld - half
            : preview.AxisWorld - half;

        if (preview.IsVertical)
        {
            var start = WorldToScreen(editor, new CadPoint(edgeWorld, preview.SpanStart));
            var end = WorldToScreen(editor, new CadPoint(edgeWorld, preview.SpanEnd));
            context.DrawLine(PreviewPen, start, end);
            return;
        }

        var lineStart = WorldToScreen(editor, new CadPoint(preview.SpanStart, edgeWorld));
        var lineEnd = WorldToScreen(editor, new CadPoint(preview.SpanEnd, edgeWorld));
        context.DrawLine(PreviewPen, lineStart, lineEnd);
    }

    public static void DrawCommittedImposts(DrawingContext context, CadEditorViewModel editor, FrameNode? root)
    {
        if (editor.Viewport is null || root is null)
        {
            return;
        }

        foreach (var (leaf, slot) in root.EnumerateExtremeImposts())
        {
            DrawCommittedImpost(context, editor, leaf, slot);
        }
    }

    private static void DrawCommittedImpost(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode leaf,
        ExtremeImpostSlot slot)
    {
        if (editor.FrameRoot is null)
        {
            return;
        }

        var inner = SashGeometry.ResolveLeafInnerOpeningBounds(leaf, editor.FrameRoot, editor.FrameThickness, includeExtremeImposts: false);
        var axis = ExtremeImpostGeometry.ResolveAxisWorld(inner, slot.EdgeAlignment, slot.ThicknessMm, slot.OffsetMm);
        ExtremeImpostGeometry.ResolveSpan(inner, slot.EdgeAlignment, out var spanStart, out var spanEnd);
        var half = slot.ThicknessMm / 2.0;

        if (slot.Orientation == SplitOrientation.Vertical)
        {
            DrawWorldRect(
                context,
                editor,
                axis - half,
                spanStart,
                slot.ThicknessMm,
                spanEnd - spanStart,
                ProfileFillBrush,
                ProfilePen);
            return;
        }

        DrawWorldRect(
            context,
            editor,
            spanStart,
            axis - half,
            spanEnd - spanStart,
            slot.ThicknessMm,
            ProfileFillBrush,
            ProfilePen);
    }

    private static Point WorldToScreen(CadEditorViewModel editor, CadPoint worldPoint)
    {
        var screen = editor.Viewport!.WorldToScreen(worldPoint);
        return new Point(screen.X, screen.Y);
    }

    private static void DrawWorldRect(
        DrawingContext context,
        CadEditorViewModel editor,
        double worldX,
        double worldY,
        double worldWidth,
        double worldHeight,
        IBrush? fill,
        IPen pen)
    {
        if (worldWidth <= CadMath.Epsilon || worldHeight <= CadMath.Epsilon)
        {
            return;
        }

        var topLeft = editor.Viewport!.WorldToScreen(new CadPoint(worldX, worldY + worldHeight));
        var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(worldX + worldWidth, worldY));
        var rect = new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
        if (fill is not null)
        {
            context.DrawRectangle(fill, pen, rect);
        }
        else
        {
            context.DrawRectangle(pen, rect);
        }
    }
}
