using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.GlazingBead;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services.GlazingBead;

/// <summary>Строит стеклопакет строго внутри контура штапика.</summary>
internal static class GlassBuilder
{
    public static bool TryBuildFromBead(
        GlazingBeadModel bead,
        out GlassPanelModel? glass,
        out SashGeometry.GlassOpeningBounds glassBounds)
    {
        glass = null;
        glassBounds = default;

        if (bead.InnerContour is null)
        {
            return false;
        }

        if (!TryConvertInnerContourToGlassBounds(bead.InnerContour, out var beadInnerBounds))
        {
            return false;
        }

        // Стекло = внутренняя грань штапика минус технологический зазор.
        glassBounds = ShrinkBounds(beadInnerBounds, GlazingBeadSettings.TechnologyGapMm);
        if (glassBounds.Width < 20 || glassBounds.Height < 20)
        {
            return false;
        }

        glass = new GlassPanelModel
        {
            ParentBeadId = bead.Id,
            HostSashId = bead.HostSashId,
            CenterX = (glassBounds.Left + glassBounds.Right) / 2.0,
            CenterY = (glassBounds.Bottom + glassBounds.Top) / 2.0,
            WidthMm = glassBounds.Width,
            HeightMm = glassBounds.Height
        };

        return true;
    }

    public static bool TryResolveGlassBoundsForBeadGlazing(
        CadEditorViewModel editor,
        GeometryElement glazing,
        IReadOnlyList<GeometryElement> elements,
        out SashGeometry.GlassOpeningBounds bounds)
    {
        bounds = default;
        if (glazing.ParentBeadId is not Guid beadId)
        {
            return false;
        }

        var beadElement = elements.FirstOrDefault(e => e.Id == beadId && e.Kind == GeometryElementKind.Bead);
        if (beadElement is null)
        {
            return false;
        }

        if (!GlazingBeadBuilder.TryRebuild(editor, beadElement, out var beadModel) || beadModel is null)
        {
            return false;
        }

        // Тот же прямоугольник, что у GlazingBeadRenderer (внутренняя грань штапика).
        if (TryResolveRectangularBeadInner(beadModel, out var beadInner))
        {
            bounds = ShrinkBounds(beadInner, GlazingBeadSettings.TechnologyGapMm);
            return bounds.Width > 0 && bounds.Height > 0;
        }

        if (beadModel.InnerContour is null
            || !TryConvertInnerContourToGlassBounds(beadModel.InnerContour, out var contourInner))
        {
            return false;
        }

        bounds = ShrinkBounds(contourInner, GlazingBeadSettings.TechnologyGapMm);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    /// <summary>Внутренний прямоугольник штапика — как в отрисовке с стыком 90°.</summary>
    private static bool TryResolveRectangularBeadInner(
        GlazingBeadModel model,
        out SashGeometry.GlassOpeningBounds bounds)
    {
        bounds = default;
        var beadW = model.Width;
        if (beadW <= CadMath.Epsilon
            || model.WidthMm <= beadW * 2 + CadMath.Epsilon
            || model.HeightMm <= beadW * 2 + CadMath.Epsilon)
        {
            return false;
        }

        if (model.OuterContour?.Edges.Any(e => e.Kind == ProfileEdgeKind.Arc) == true)
        {
            return false;
        }

        var left = model.CenterX - model.WidthMm / 2.0 + beadW;
        var right = model.CenterX + model.WidthMm / 2.0 - beadW;
        var bottom = model.CenterY - model.HeightMm / 2.0 + beadW;
        var top = model.CenterY + model.HeightMm / 2.0 - beadW;
        if (right <= left || top <= bottom)
        {
            return false;
        }

        bounds = SashGeometry.GlassOpeningBounds.FromRect(left, bottom, right, top);
        return true;
    }

    private static SashGeometry.GlassOpeningBounds ShrinkBounds(
        SashGeometry.GlassOpeningBounds source,
        double insetMm)
    {
        if (source.IsArched)
        {
            var x0 = source.Left + insetMm;
            var x1 = source.Right - insetMm;
            var y0 = source.Bottom + insetMm;
            var rInner = Math.Max(source.ArcRadius - insetMm, CadMath.Epsilon);
            var springingLeft = ProfileSegment.GetPointOnUpperArc(source.ArcCenter, rInner, x0);
            var springingRight = ProfileSegment.GetPointOnUpperArc(source.ArcCenter, rInner, x1);
            return new SashGeometry.GlassOpeningBounds
            {
                Left = x0,
                Bottom = y0,
                Right = x1,
                Top = springingLeft.Y,
                IsArched = true,
                ArcCenter = source.ArcCenter,
                ArcRadius = rInner,
                BottomLeft = new CadPoint(x0, y0),
                BottomRight = new CadPoint(x1, y0),
                SpringingLeft = springingLeft,
                SpringingRight = springingRight
            };
        }

        return SashGeometry.GlassOpeningBounds.FromRect(
            source.Left + insetMm,
            source.Bottom + insetMm,
            source.Right - insetMm,
            source.Top - insetMm);
    }

    private static bool TryConvertInnerContourToGlassBounds(
        ProfileContourLoop innerContour,
        out SashGeometry.GlassOpeningBounds bounds)
    {
        bounds = default;
        var points = innerContour.Tessellate();
        if (points.Count < 3)
        {
            return false;
        }

        var minX = points.Min(p => p.X);
        var maxX = points.Max(p => p.X);
        var minY = points.Min(p => p.Y);
        var maxY = points.Max(p => p.Y);

        var arcEdge = innerContour.Edges.FirstOrDefault(e => e.Kind == ProfileEdgeKind.Arc);
        if (arcEdge.Kind != ProfileEdgeKind.Arc)
        {
            bounds = SashGeometry.GlassOpeningBounds.FromRect(minX, minY, maxX, maxY);
            return true;
        }

        bounds = new SashGeometry.GlassOpeningBounds
        {
            Left = minX,
            Bottom = minY,
            Right = maxX,
            Top = maxY,
            IsArched = true,
            ArcCenter = arcEdge.ArcCenter,
            ArcRadius = arcEdge.ArcRadius,
            BottomLeft = new CadPoint(minX, minY),
            BottomRight = new CadPoint(maxX, minY),
            SpringingLeft = arcEdge.Start,
            SpringingRight = arcEdge.End
        };
        return true;
    }
}
