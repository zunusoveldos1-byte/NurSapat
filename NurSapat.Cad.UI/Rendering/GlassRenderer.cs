using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Services.GlazingBead;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>Отрисовка стеклопакетов, связанных со штапиком (ParentBeadId).</summary>
public static class GlassRenderer
{
    private static readonly IBrush GlassFill = new SolidColorBrush(Color.FromArgb(180, 0xB8, 0xD4, 0xE8));
    private static readonly IPen GlassOutlinePen = new Pen(new SolidColorBrush(Color.Parse("#7EB3D4")), 0.5);

    public static void DrawBeadLinkedLayer(
        DrawingContext context,
        CadEditorViewModel editor,
        IEnumerable<GeometryElement> elements)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        var elementList = elements as IReadOnlyList<GeometryElement> ?? elements.ToList();
        foreach (var element in elementList)
        {
            if (element.Kind != GeometryElementKind.Glazing || element.ParentBeadId is null)
            {
                continue;
            }

            try
            {
                DrawBeadLinkedGlass(context, editor, element, elementList);
            }
            catch (Exception ex)
            {
                CadDiagnostics.LogWarning($"GlassRenderer failed for {element.Id}", ex);
            }
        }
    }

    private static void DrawBeadLinkedGlass(
        DrawingContext context,
        CadEditorViewModel editor,
        GeometryElement element,
        IReadOnlyList<GeometryElement> elements)
    {
        if (!GlassBuilder.TryResolveGlassBoundsForBeadGlazing(editor, element, elements, out var glass))
        {
            return;
        }

        if (glass.Width <= 2 || glass.Height <= 2)
        {
            return;
        }

        if (glass.IsArched)
        {
            DrawArchedGlass(context, editor, glass);
            return;
        }

        var outerLoop = glass.BuildOuterContourLoop();
        FillContourLoop(context, editor, outerLoop, GlassFill, GlassOutlinePen);
        DrawReflections(context, editor, glass);
    }

    private static void DrawArchedGlass(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass)
    {
        var outerLoop = glass.BuildOuterContourLoop();
        FillContourLoop(context, editor, outerLoop, GlassFill, GlassOutlinePen);
        DrawReflections(context, editor, glass);
    }

    private static void DrawReflections(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass)
    {
        var rect = ToScreenRect(editor, glass);
        if (rect.Width <= 1 || rect.Height <= 1)
        {
            return;
        }

        var highlight = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255));
        var stripeWidth = Math.Max(2, rect.Width * 0.04);
        var stripe = new Rect(rect.X + rect.Width * 0.08, rect.Y + rect.Height * 0.1, stripeWidth, rect.Height * 0.55);
        context.FillRectangle(highlight, stripe);
    }

    private static void FillContourLoop(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop loop,
        IBrush fill,
        IPen? pen)
    {
        var points = loop.Tessellate();
        if (points.Count < 3)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(WorldToScreen(editor, points[0]), isFilled: true);
            for (var i = 1; i < points.Count; i++)
            {
                ctx.LineTo(WorldToScreen(editor, points[i]));
            }

            ctx.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    private static Rect ToScreenRect(CadEditorViewModel editor, SashGeometry.GlassOpeningBounds glass)
    {
        var topLeft = editor.Viewport!.WorldToScreen(new CadPoint(glass.Left, glass.Top));
        var bottomRight = editor.Viewport.WorldToScreen(new CadPoint(glass.Right, glass.Bottom));
        return new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
    }

    private static Point WorldToScreen(CadEditorViewModel editor, CadPoint worldPoint)
    {
        var screen = editor.Viewport!.WorldToScreen(worldPoint);
        return new Point(screen.X, screen.Y);
    }
}
