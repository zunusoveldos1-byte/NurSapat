using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>Отрисовка профиля штапика: серый контур, белое тело профиля.</summary>
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
        if (!Services.GlazingBead.GlazingBeadBuilder.TryResolveBeadContours(
                element,
                editor,
                out var outerLoop,
                out var innerLoop))
        {
            return;
        }

        DrawProfileBand(context, editor, outerLoop, innerLoop, ProfileFaceFill, null);
        StrokeContourLoop(context, editor, outerLoop, ProfileOutlinePen);
        StrokeContourLoop(context, editor, innerLoop, ProfileOutlinePen);

        for (var i = 0; i < outerLoop.Edges.Count && i < innerLoop.Edges.Count; i++)
        {
            FrameVolumeRenderer.DrawProfileMiter(
                context,
                editor,
                outerLoop.Edges[i].Start,
                innerLoop.Edges[i].Start);
        }
    }

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
