using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Services;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

public static class ExtremeImpostRenderer
{
    private static readonly IBrush PreviewFillBrush = new SolidColorBrush(Color.FromArgb(128, 255, 4, 4));
    private static readonly IPen PreviewPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 255, 4, 4)), 2);

    private static readonly IBrush ProfileFillBrush = Brushes.White;
    private static readonly IPen ProfilePen = new Pen(Brushes.Black, FrameVolumeRenderer.ProfileStrokeThickness);

    public static void DrawHoverPreview(DrawingContext context, CadEditorViewModel editor, ExtremeImpostHoverPreview preview)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        var half = preview.ThicknessMm / 2.0;
        var edgeWorld = preview.AxisWorld - half;

        if (preview.IsVertical)
        {
            DrawWorldRect(
                context,
                editor,
                edgeWorld,
                preview.SpanStart,
                preview.ThicknessMm,
                preview.SpanEnd - preview.SpanStart,
                PreviewFillBrush,
                PreviewPen);
            return;
        }

        DrawWorldRect(
            context,
            editor,
            preview.SpanStart,
            edgeWorld,
            preview.SpanEnd - preview.SpanStart,
            preview.ThicknessMm,
            PreviewFillBrush,
            PreviewPen);
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

        var cellAxes = SashGeometry.ResolveLeafCellAxisBounds(leaf, editor.FrameRoot, editor.FrameThickness, includeExtremeImposts: false);
        var axis = ExtremeImpostGeometry.ResolveAxisWorld(cellAxes, slot.EdgeAlignment, slot.ThicknessMm, slot.OffsetMm);
        ExtremeImpostGeometry.ResolveSpan(
            leaf,
            editor.FrameRoot,
            editor.FrameThickness,
            cellAxes,
            slot.EdgeAlignment,
            slot.ThicknessMm,
            out var spanStart,
            out var spanEnd,
            slot);

        // Тонкие накладные материалы (нащельник, 65 рама НИЗ) рисуются узкой
        // полосой по оси профиля; геометрия размещения при этом не меняется.
        var drawThickness = ResolveDrawThickness(editor, slot);
        var half = drawThickness / 2.0;

        if (slot.Orientation == SplitOrientation.Vertical)
        {
            DrawWorldRect(
                context,
                editor,
                axis - half,
                spanStart,
                drawThickness,
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
            drawThickness,
            ProfileFillBrush,
            ProfilePen);
    }

    /// <summary>
    /// Толщина отрисовки крайнего импоста: у тонких накладных материалов
    /// (нащельник, 65 рама НИЗ) берётся DrawThicknessMm материала, иначе —
    /// реальная толщина профиля из слота.
    /// </summary>
    internal static double ResolveDrawThickness(CadEditorViewModel editor, ExtremeImpostSlot slot)
    {
        var element = editor.CanvasElements.FirstOrDefault(e =>
            e.Kind == GeometryElementKind.EdgeMullion && e.ExtremeImpostSlotId == slot.Id);
        var material = ConstructionMaterialCatalog.FindById(element?.MaterialId);
        return material?.DrawThicknessMm ?? slot.ThicknessMm;
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
