using Avalonia;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Services;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using System.Linq;

namespace NurSapat.Cad.UI.Rendering;

public static class ConstructionSelectionRenderer
{
    private static readonly IPen SelectionPen = new Pen(
        FrameVolumeRenderer.SelectionHighlightBrush,
        2.5);

    public static void DrawHighlights(
        DrawingContext context,
        CadEditorViewModel editor,
        IEnumerable<CadSelectableItem> selection)
    {
        if (editor.Viewport == null)
        {
            return;
        }

        foreach (var item in selection.ToList())
        {
            switch (item.Kind)
            {
                case CadSelectableKind.Frame:
                    DrawFrameHighlight(context, editor, item.ProfileRail);
                    break;
                case CadSelectableKind.Impost when item.ImpostNode is not null:
                    DrawImpostHighlight(context, editor, item.ImpostNode, item.ImpostGapIndex);
                    break;
                case CadSelectableKind.ExtremeImpost
                    when item.ImpostNode is not null && item.ElementId is Guid slotId:
                    DrawExtremeImpostHighlight(context, editor, item.ImpostNode, slotId);
                    break;
                case CadSelectableKind.Sash when item.ElementId is Guid sashId:
                    DrawSashProfileHighlight(context, editor, sashId);
                    break;
                case CadSelectableKind.Glazing when item.ElementId is Guid glazingId:
                    DrawGeometryElementHighlight(context, editor, glazingId, GeometryElementKind.Glazing);
                    break;
                case CadSelectableKind.Bead when item.ElementId is Guid beadId:
                    DrawBeadHighlight(context, editor, beadId);
                    break;
                case CadSelectableKind.SashHandle when item.ElementId is Guid handleSashId:
                    DrawHardwareHighlight(context, editor, handleSashId, CadSelectableKind.SashHandle, 0);
                    break;
                case CadSelectableKind.SashHinge when item.ElementId is Guid hingeSashId:
                    DrawHardwareHighlight(context, editor, hingeSashId, CadSelectableKind.SashHinge, item.HingeSlot);
                    break;
            }
        }
    }

    private static void DrawFrameHighlight(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameProfileRail? profileRail = null)
    {
        if (editor.FrameRoot is not FrameNode frame)
        {
            return;
        }

        // Фиктивная коробка: без наложенной рамки и угловых маркеров — только собственный пунктир в отрисовщике рамы.
        if (frame.IsDummy || frame.IsFictitiousContainer)
        {
            return;
        }

        if (frame.Shape == ConstructionShape.Triangle)
        {
            DrawTriangularFrameHighlight(context, editor, frame);
            return;
        }

        if (profileRail is FrameProfileRail rail)
        {
            DrawRectangularFrameRailHighlight(context, editor, frame, rail);
            return;
        }

        if (frame.TopProfile?.SegmentType == SegmentType.Arc &&
            frame.TopProfile.ArcHeight > ProfileSegment.MinFlatArcHeightMm)
        {
            try
            {
                var contours = FrameProfileGeometry.BuildArchedFrameContours(
                    frame.X,
                    frame.Y,
                    frame.Width,
                    frame.Height,
                    frame.TopProfile,
                    editor.FrameThickness);
            StrokeProfileBand(context, editor, contours.OuterLoop, contours.InnerLoop, SelectionPen);
                if (ConstructionSelectionHelper.GetFrameBounds(editor) is { } archedBounds)
                {
                    DrawEdgeResizeHandles(context, editor, archedBounds);
                }

                return;
            }
            catch (InvalidOperationException)
            {
            }
        }

        if (ConstructionSelectionHelper.GetFrameBounds(editor) is not { } bounds)
        {
            return;
        }

        var inset = FrameProfileGeometry.ResolveInnerInset(
            frame.Width,
            frame.Height,
            editor.FrameThickness);
        DrawRectangularBandHighlight(
            context,
            editor,
            frame.X,
            frame.Y,
            frame.X + frame.Width,
            frame.Y + frame.Height,
            frame.X + inset,
            frame.Y + inset,
            frame.X + frame.Width - inset,
            frame.Y + frame.Height - inset,
            SelectionPen);
        DrawEdgeResizeHandles(context, editor, bounds);
    }

    private static void DrawRectangularFrameRailHighlight(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode frame,
        FrameProfileRail rail)
    {
        var band = FrameProfileRailGeometry.ResolveRailBand(
            frame.X,
            frame.Y,
            frame.Width,
            frame.Height,
            editor.FrameThickness,
            rail);
        StrokeRailBandHighlight(context, editor, band, SelectionPen);
    }

    private static void StrokeRailBandHighlight(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameProfileRailGeometry.RailBand band,
        IPen pen)
    {
        StrokePolygon(
            context,
            editor,
            [band.OuterStart, band.OuterEnd, band.InnerEnd, band.InnerStart],
            close: true,
            pen);
    }

    private static void DrawTriangularFrameHighlight(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode frame)
    {
        var outer = FrameTriangleGeometry.BuildOuterTriangle(frame.X, frame.Y, frame.Width, frame.Height);
        var half = FrameProfileGeometry.ResolveProfileThickness(frame.Width, frame.Height, editor.FrameThickness);
        var inner = FrameTriangleGeometry.InsetTriangle(outer, half);

        void Stroke(CadPoint a, CadPoint b)
        {
            context.DrawLine(
                SelectionPen,
                WorldToScreen(editor, a),
                WorldToScreen(editor, b));
        }

        Stroke(outer.Bl, outer.Br);
        Stroke(outer.Br, outer.Top);
        Stroke(outer.Top, outer.Bl);
        Stroke(inner.Bl, inner.Br);
        Stroke(inner.Br, inner.Top);
        Stroke(inner.Top, inner.Bl);
        Stroke(outer.Bl, inner.Bl);
        Stroke(outer.Br, inner.Br);
        Stroke(outer.Top, inner.Top);
    }

    private static void DrawImpostHighlight(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode node,
        int gapIndex)
    {
        if (!ImpostProfileHitTest.TryGetFootprint(
                editor,
                node,
                gapIndex,
                out var left,
                out var right,
                out var yBottom,
                out _,
                out var arc))
        {
            return;
        }

        if (arc is ArcSpringingGeometry archedArc)
        {
            var polygon = ImpostProfileHitTest.BuildArcTrimmedPolygon(archedArc, left, right, yBottom);
            StrokePolygon(context, editor, polygon, close: true, SelectionPen);
            return;
        }

        if (ImpostProfileHitTest.TryGetHorizontalFootprintPolygon(editor, node, gapIndex, out var horizontalPolygon))
        {
            StrokePolygon(context, editor, horizontalPolygon, close: true, SelectionPen);
            return;
        }

        if (ConstructionSelectionHelper.TryGetImpostBounds(editor, node, gapIndex) is { } bounds)
        {
            DrawWorldRectOutline(context, editor, bounds, SelectionPen);
        }
    }

    private static void DrawExtremeImpostHighlight(
        DrawingContext context,
        CadEditorViewModel editor,
        FrameNode leaf,
        Guid slotId)
    {
        var slot = leaf.FindExtremeImpost(slotId);
        if (slot is null)
        {
            return;
        }

        // Подсветка повторяет видимую толщину: у тонких накладных материалов
        // (нащельник, 65 рама НИЗ) обводится узкая полоса, а не полный профиль.
        var drawThickness = ExtremeImpostRenderer.ResolveDrawThickness(editor, slot);
        if (ConstructionSelectionHelper.TryGetExtremeImpostBounds(editor, leaf, slot, drawThickness) is not { } bounds)
        {
            return;
        }

        DrawWorldRectOutline(context, editor, bounds, SelectionPen);
    }

    private static void DrawSashProfileHighlight(DrawingContext context, CadEditorViewModel editor, Guid sashId)
    {
        var element = editor.CanvasElements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
        if (element is null)
        {
            return;
        }

        var inner = SashGeometry.TryResolveInnerBounds(editor, element, out var liveInner)
            ? liveInner
            : SashGeometry.InnerOpeningBoundsFromElement(element);
        var outer = SashGeometry.ExpandInnerBoundsToOuter(inner, editor);
        if (outer.Width <= 0 || outer.Height <= 0)
        {
            return;
        }

        var profileThickness = SashGeometry.SashDrawProfileThicknessMm;
        if (SashGeometry.TryBuildArchedSashContours(
                editor,
                inner,
                outer,
                profileThickness,
                out var outerLoop,
                out var innerLoop,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _,
                out _))
        {
            StrokeProfileBand(context, editor, outerLoop, innerLoop, SelectionPen);
            return;
        }

        var halfThickness = FrameProfileGeometry.ResolveProfileThickness(outer.Width, outer.Height, profileThickness) / 2.0;
        DrawRectangularBandHighlight(
            context,
            editor,
            outer.Left,
            outer.Bottom,
            outer.Right,
            outer.Top,
            outer.Left + halfThickness,
            outer.Bottom + halfThickness,
            outer.Right - halfThickness,
            outer.Top - halfThickness,
            SelectionPen);
    }

    private static void DrawHardwareHighlight(
    DrawingContext context,
    CadEditorViewModel editor,
    Guid sashId,
    CadSelectableKind kind,
    int hingeSlot)
    {
        var sash = editor.CanvasElements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
        if (sash is null)
        {
            return;
        }

        // Создаем или берем дефолтное перо для подсветки фурнитуры (например, полупрозрачное синее или оранжевое)
        // Ты можешь настроить цвет (Brushes.Orange) и толщину (1.5) под свой дизайн
        var highlightPen = new Pen(Brushes.Orange, 1.5);

        foreach (var region in SashHardwareHitTest.EnumerateRegions(editor, sash))
        {
            if (region.Item.Kind != kind)
            {
                continue;
            }

            if (kind == CadSelectableKind.SashHinge && region.Item.HingeSlot != hingeSlot)
            {
                continue;
            }

            // ИСПРАВЛЕНО: Передаем highlightPen четвертым аргументом
            DrawWorldRectOutline(context, editor, region.Bounds, highlightPen);
        }
    }

    private static void DrawBeadHighlight(
        DrawingContext context,
        CadEditorViewModel editor,
        Guid beadId)
    {
        var element = editor.CanvasElements.FirstOrDefault(e => e.Id == beadId && e.Kind == GeometryElementKind.Bead);
        if (element is null)
        {
            return;
        }

        if (element.SourceToolId == "bead"
            && Services.GlazingBead.GlazingBeadBuilder.TryResolveBeadContours(
                element,
                editor,
                out var outerLoop,
                out var innerLoop))
        {
            StrokeContourLoop(context, editor, outerLoop, SelectionPen);
            StrokeContourLoop(context, editor, innerLoop, SelectionPen);
            return;
        }

        DrawGeometryElementHighlight(context, editor, beadId, GeometryElementKind.Bead);
    }

    private static void DrawGeometryElementHighlight(
        DrawingContext context,
        CadEditorViewModel editor,
        Guid elementId,
        GeometryElementKind expectedKind)
    {
        var element = editor.CanvasElements.FirstOrDefault(e => e.Id == elementId && e.Kind == expectedKind);
        if (element is null)
        {
            return;
        }

        if (expectedKind == GeometryElementKind.Glazing &&
            SashGeometry.TryResolveGlassForGlazingElement(editor, element, editor.CanvasElements, out var glass) &&
            (glass.IsArched || glass.IsTriangular))
        {
            StrokeContourLoop(context, editor, glass.BuildOuterContourLoop(), SelectionPen);
            return;
        }

        if (!ConstructionSelectionHelper.TryGetElementBounds(editor, element, out var bounds))
        {
            return;
        }

        DrawWorldRectOutline(context, editor, bounds, SelectionPen);
    }

    private static void StrokeProfileBand(
        DrawingContext context,
        CadEditorViewModel editor,
        ProfileContourLoop outerLoop,
        ProfileContourLoop innerLoop,
        IPen pen)
    {
        StrokeContourLoop(context, editor, outerLoop, pen);
        StrokeContourLoop(context, editor, innerLoop, pen);
    }

    private static void DrawRectangularBandHighlight(
        DrawingContext context,
        CadEditorViewModel editor,
        double outerLeft,
        double outerBottom,
        double outerRight,
        double outerTop,
        double innerLeft,
        double innerBottom,
        double innerRight,
        double innerTop,
        IPen pen)
    {
        DrawWorldRectOutline(context, editor, ToCadRect(outerLeft, outerBottom, outerRight, outerTop), pen);
        DrawWorldRectOutline(context, editor, ToCadRect(innerLeft, innerBottom, innerRight, innerTop), pen);
    }

    private static CadRect ToCadRect(double left, double bottom, double right, double top) =>
        new(left, top, right - left, top - bottom);

    private static void StrokePolygon(
        DrawingContext context,
        CadEditorViewModel editor,
        IReadOnlyList<CadPoint> polygon,
        bool close,
        IPen pen)
    {
        if (polygon.Count < 2)
        {
            return;
        }

        var previous = WorldToScreen(editor, polygon[0]);
        for (var i = 1; i < polygon.Count; i++)
        {
            var current = WorldToScreen(editor, polygon[i]);
            context.DrawLine(pen, previous, current);
            previous = current;
        }

        if (close)
        {
            context.DrawLine(pen, previous, WorldToScreen(editor, polygon[0]));
        }
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

    /// <summary>Маркеры изменения размера по серединам сторон (без угловых грифов).</summary>
    private static void DrawEdgeResizeHandles(DrawingContext context, CadEditorViewModel editor, CadRect bounds)
    {
        if (editor.Viewport is null)
        {
            return;
        }

        const double handleHalfPx = 4.0;
        var fill = new SolidColorBrush(Color.FromArgb(200, 0xAE, 0xAE, 0xAE));
        var pen = new Pen(FrameVolumeRenderer.SelectionHighlightBrush, 1.5);
        CadPoint[] edgeMidpoints =
        [
            new(bounds.X + bounds.Width / 2.0, bounds.Y),
            new(bounds.X + bounds.Width / 2.0, bounds.Y - bounds.Height),
            new(bounds.X, bounds.Y - bounds.Height / 2.0),
            new(bounds.X + bounds.Width, bounds.Y - bounds.Height / 2.0)
        ];

        foreach (var midpoint in edgeMidpoints)
        {
            var screen = editor.Viewport.WorldToScreen(midpoint);
            var handleRect = new Rect(
                screen.X - handleHalfPx,
                screen.Y - handleHalfPx,
                handleHalfPx * 2,
                handleHalfPx * 2);
            context.DrawRectangle(fill, pen, handleRect);
        }
    }

    private static void DrawWorldRectOutline(DrawingContext context, CadEditorViewModel editor, CadRect bounds, IPen pen)
    {
        var topLeft = WorldToScreen(editor, new CadPoint(bounds.X, bounds.Y));
        var topRight = WorldToScreen(editor, new CadPoint(bounds.X + bounds.Width, bounds.Y));
        var bottomLeft = WorldToScreen(editor, new CadPoint(bounds.X, bounds.Y - bounds.Height));
        var bottomRight = WorldToScreen(editor, new CadPoint(bounds.X + bounds.Width, bounds.Y - bounds.Height));
        context.DrawLine(pen, topLeft, topRight);
        context.DrawLine(pen, topRight, bottomRight);
        context.DrawLine(pen, bottomRight, bottomLeft);
        context.DrawLine(pen, bottomLeft, topLeft);
    }

    private static Point WorldToScreen(CadEditorViewModel editor, CadPoint worldPoint)
    {
        var screen = editor.Viewport!.WorldToScreen(worldPoint);
        return new Point(screen.X, screen.Y);
    }
}
