using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Services.GlazingBead;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Единый визуальный стиль стеклопакета для всех инструментов
/// (одиночное остекление и «Остекление + штапик»).
/// Прозрачная голубая заливка + тонкие диагональные линии блика по центру.
/// </summary>
public static class GlassRenderer
{
    /// <summary>Лёгкая прозрачная заливка стекла — общий стандарт для всех инструментов.</summary>
    public static readonly IBrush GlassFill = new SolidColorBrush(Color.FromArgb(24, 0xBF, 0xDB, 0xFE));

    private static readonly IPen ReflectionPen = new Pen(
        new SolidColorBrush(Color.Parse("#4169E1")) { Opacity = 0.9 },
        1);

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

        DrawGlassPane(context, editor, glass);
    }

    /// <summary>
    /// Общая отрисовка стеклопакета: лёгкая заливка + центральные диагональные блики.
    /// Используется и одиночным остеклением, и комбинированным «Остекление + штапик».
    /// </summary>
    internal static void DrawGlassPane(
        DrawingContext context,
        CadEditorViewModel editor,
        SashGeometry.GlassOpeningBounds glass)
    {
        if (glass.Width <= 2 || glass.Height <= 2)
        {
            return;
        }

        if (glass.IsArched)
        {
            var outerLoop = glass.BuildOuterContourLoop();
            FillContourLoop(context, editor, outerLoop, GlassFill);
            DrawDiagonalReflections(context, ToScreenRect(editor, glass));
            return;
        }

        var rect = ToScreenRect(editor, glass);
        if (rect.Width <= 1 || rect.Height <= 1)
        {
            return;
        }

        context.FillRectangle(GlassFill, rect);
        DrawDiagonalReflections(context, rect);
    }

    /// <summary>
    /// Три тонкие параллельные диагонали-блика по центру стеклопакета
    /// (стандартный вид одиночного остекления).
    /// </summary>
    public static void DrawDiagonalReflections(DrawingContext context, Rect glassRect)
    {
        if (glassRect.Width <= 1 || glassRect.Height <= 1)
        {
            return;
        }

        var centerX = glassRect.X + glassRect.Width / 2;
        var centerY = glassRect.Y + glassRect.Height / 2;
        var span = Math.Min(glassRect.Width, glassRect.Height);
        var centerLength = Math.Clamp(span * 0.55, 28, 84);
        var sideLength = Math.Clamp(span * 0.40, 20, 56);
        var spacing = Math.Clamp(span * 0.12, 8, 14);

        const double angle = Math.PI / 4;
        var dirX = Math.Cos(angle);
        var dirY = -Math.Sin(angle);
        var perpX = -dirY;
        var perpY = dirX;

        DrawReflectionLine(context, centerX, centerY, perpX, perpY, -spacing, sideLength, dirX, dirY);
        DrawReflectionLine(context, centerX, centerY, perpX, perpY, 0, centerLength, dirX, dirY);
        DrawReflectionLine(context, centerX, centerY, perpX, perpY, spacing, sideLength, dirX, dirY);
    }

    private static void DrawReflectionLine(
        DrawingContext context,
        double centerX,
        double centerY,
        double perpX,
        double perpY,
        double perpOffset,
        double length,
        double dirX,
        double dirY)
    {
        var originX = centerX + perpX * perpOffset;
        var originY = centerY + perpY * perpOffset;
        var half = length / 2;
        context.DrawLine(
            ReflectionPen,
            new Point(originX - dirX * half, originY - dirY * half),
            new Point(originX + dirX * half, originY + dirY * half));
    }

    private static void FillContourLoop(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop loop,
        IBrush fill)
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

        context.DrawGeometry(fill, null, geometry);
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
