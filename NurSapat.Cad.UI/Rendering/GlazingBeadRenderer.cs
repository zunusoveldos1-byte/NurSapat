using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Отрисовка профиля штапика: серый контур, белое тело.
/// Прямоугольный проём — стык 90° со сквозными горизонталями (без запила 45°).
/// </summary>
public static class GlazingBeadRenderer
{
    private static readonly IBrush ProfileFaceFill = Brushes.White;
    private static readonly IPen ProfileOutlinePen = FrameVolumeRenderer.ProfilePen;

    public static void DrawAll(
        DrawingContext context,
        CadEditorViewModel editor,
        IEnumerable<GeometryElement> elements)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        foreach (var element in elements)
        {
            if (element.Kind != GeometryElementKind.Bead || element.SourceToolId != "bead")
            {
                continue;
            }

            try
            {
                DrawBeadElement(context, editor, element);
            }
            catch (Exception ex)
            {
                CadDiagnostics.LogWarning($"GlazingBeadRenderer failed for {element.Id}", ex);
            }
        }
    }

    private static void DrawBeadElement(
        DrawingContext context,
        CadEditorViewModel editor,
        GeometryElement element)
    {
        if (!Services.GlazingBead.GlazingBeadBuilder.TryRebuild(editor, element, out var model)
            || model?.OuterContour is null
            || model.InnerContour is null)
        {
            return;
        }

        // Прямоугольный световой проём: сквозные горизонтали + вертикали между ними.
        if (!model.OuterContour.Edges.Any(e => e.Kind == ProfileEdgeKind.Arc)
            && TryDrawRectangularButtJointBead(context, editor, model))
        {
            return;
        }

        // Арочные / непрямоугольные: сплошная полоса без запила 45°.
        DrawProfileBand(context, editor, model.OuterContour, model.InnerContour, ProfileFaceFill, null);
        StrokeContourLoop(context, editor, model.OuterContour, ProfileOutlinePen);
        StrokeContourLoop(context, editor, model.InnerContour, ProfileOutlinePen);
    }

    /// <summary>
    /// Штапик как 4 сегмента:
    /// горизонтали (верх/низ) на всю ширину проёма;
    /// вертикали (лево/право) строго между горизонталями;
    /// в углах — только горизонтальные линии стыка 90°.
    /// </summary>
    private static bool TryDrawRectangularButtJointBead(
        DrawingContext context,
        CadEditorViewModel editor,
        GlazingBeadModel model)
    {
        var beadW = model.Width;
        if (beadW <= CadMath.Epsilon
            || model.WidthMm <= beadW * 2 + CadMath.Epsilon
            || model.HeightMm <= beadW * 2 + CadMath.Epsilon)
        {
            return false;
        }

        var left = model.CenterX - model.WidthMm / 2.0;
        var right = model.CenterX + model.WidthMm / 2.0;
        var bottom = model.CenterY - model.HeightMm / 2.0;
        var top = model.CenterY + model.HeightMm / 2.0;

        var innerLeft = left + beadW;
        var innerRight = right - beadW;
        var innerBottom = bottom + beadW;
        var innerTop = top - beadW;

        // 1) Тело профиля: горизонтали сквозные, вертикали укорочены.
        FillWorldRect(context, editor, left, bottom, right, innerBottom);       // низ
        FillWorldRect(context, editor, left, innerTop, right, top);             // верх
        FillWorldRect(context, editor, left, innerBottom, innerLeft, innerTop); // лево
        FillWorldRect(context, editor, innerRight, innerBottom, right, innerTop); // право

        // 2) Внешний контур светового проёма.
        StrokeWorldRect(context, editor, left, bottom, right, top);

        // 3) Внутренний контур (граница стекла).
        StrokeWorldRect(context, editor, innerLeft, innerBottom, innerRight, innerTop);

        // 4) Горизонтальные стыки 90° в углах (только по толщине штапика).
        // Вертикальных засечек вдоль горизонтальных штапиков нет.
        DrawWorldLine(context, editor, left, innerBottom, innerLeft, innerBottom);
        DrawWorldLine(context, editor, innerRight, innerBottom, right, innerBottom);
        DrawWorldLine(context, editor, left, innerTop, innerLeft, innerTop);
        DrawWorldLine(context, editor, innerRight, innerTop, right, innerTop);

        return true;
    }

    private static void FillWorldRect(
        DrawingContext context,
        CadEditorViewModel editor,
        double left,
        double bottom,
        double right,
        double top)
    {
        if (right - left <= CadMath.Epsilon || top - bottom <= CadMath.Epsilon)
        {
            return;
        }

        var topLeft = WorldToScreen(editor, new CadPoint(left, top));
        var bottomRight = WorldToScreen(editor, new CadPoint(right, bottom));
        var rect = new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
        if (rect.Width <= 0.5 || rect.Height <= 0.5)
        {
            return;
        }

        context.FillRectangle(ProfileFaceFill, rect);
    }

    private static void StrokeWorldRect(
        DrawingContext context,
        CadEditorViewModel editor,
        double left,
        double bottom,
        double right,
        double top)
    {
        DrawWorldLine(context, editor, left, bottom, right, bottom);
        DrawWorldLine(context, editor, right, bottom, right, top);
        DrawWorldLine(context, editor, right, top, left, top);
        DrawWorldLine(context, editor, left, top, left, bottom);
    }

    private static void DrawWorldLine(
        DrawingContext context,
        CadEditorViewModel editor,
        double x0,
        double y0,
        double x1,
        double y1) =>
        context.DrawLine(
            ProfileOutlinePen,
            WorldToScreen(editor, new CadPoint(x0, y0)),
            WorldToScreen(editor, new CadPoint(x1, y1)));

    private static void DrawProfileBand(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop outerLoop,
        ProfileContourLoop innerLoop,
        IBrush fill,
        IPen? pen)
    {
        var outerPoints = outerLoop.Tessellate();
        var innerPoints = innerLoop.Tessellate();
        if (outerPoints.Count < 3 || innerPoints.Count < 3)
        {
            return;
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(WorldToScreen(editor, outerPoints[0]), isFilled: true);
            for (var i = 1; i < outerPoints.Count; i++)
            {
                ctx.LineTo(WorldToScreen(editor, outerPoints[i]));
            }

            for (var i = innerPoints.Count - 1; i >= 0; i--)
            {
                ctx.LineTo(WorldToScreen(editor, innerPoints[i]));
            }

            ctx.EndFigure(true);
        }

        context.DrawGeometry(fill, pen, geometry);
    }

    private static void StrokeContourLoop(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop loop,
        IPen pen)
    {
        foreach (var edge in loop.Edges)
        {
            if (edge.Kind == ProfileEdgeKind.Line)
            {
                context.DrawLine(
                    pen,
                    WorldToScreen(editor, edge.Start),
                    WorldToScreen(editor, edge.End));
                continue;
            }

            var previous = WorldToScreen(editor, edge.Start);
            foreach (var worldPoint in CircularArc.TessellateSpringingArc(
                         edge.ArcCenter,
                         edge.ArcRadius,
                         edge.ArcStartAngleRad,
                         edge.ArcEndAngleRad))
            {
                var current = WorldToScreen(editor, worldPoint);
                context.DrawLine(pen, previous, current);
                previous = current;
            }
        }
    }

    private static Point WorldToScreen(CadEditorViewModel editor, CadPoint worldPoint)
    {
        var screen = editor.Viewport!.WorldToScreen(worldPoint);
        return new Point(screen.X, screen.Y);
    }
}
