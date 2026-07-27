using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Services;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using System.Linq;

namespace NurSapat.Cad.UI.Rendering;

internal static class ConstructionSelectionHelper
{
    private const double BeadHitToleranceMm = 24;

    public static GeometryElement? FindGlazingAtPoint(CadEditorViewModel editor, CadPoint worldPoint)
    {
        foreach (var element in editor.CanvasElements
                     .Where(e => e.Kind == GeometryElementKind.Glazing)
                     .Reverse())
        {
            if (HitTestGlazing(editor, element, worldPoint))
            {
                return element;
            }
        }

        return null;
    }

    public static CadSelectableItem? HitTest(CadEditorViewModel editor, CadPoint worldPoint)
    {
        if (editor.FrameRoot is null && editor.CanvasElements.Count == 0 && editor.Document.Rectangles.Count == 0)
        {
            return null;
        }

        if (SashHardwareHitTest.TryHitTest(editor, worldPoint, out var hardwareHit) && hardwareHit is not null)
        {
            return hardwareHit;
        }

        var canvasElements = editor.CanvasElements.ToList();

        foreach (var element in canvasElements)
        {
            if (element.Kind == GeometryElementKind.Bead && HitTestGeometryElement(element, worldPoint, BeadHitToleranceMm))
            {
                return CadSelectableItem.Bead(element.Id);
            }
        }

        foreach (var element in canvasElements)
        {
            if (element.Kind == GeometryElementKind.Glazing && HitTestGlazing(editor, element, worldPoint))
            {
                return CadSelectableItem.Glazing(element.Id);
            }
        }

        if (editor.FrameRoot is not null)
        {
            foreach (var (leaf, slot) in editor.FrameRoot.EnumerateExtremeImposts())
            {
                if (ExtremeImpostGeometry.ContainsCommittedImpost(
                        editor.FrameRoot,
                        editor.FrameThickness,
                        leaf,
                        slot,
                        worldPoint))
                {
                    return CadSelectableItem.ExtremeImpost(leaf, slot.Id);
                }
            }
        }

        var impost = HitTestImpost(editor, worldPoint);
        if (impost is not null)
        {
            return CadSelectableItem.FromImpostTarget(impost);
        }

        foreach (var element in canvasElements)
        {
            if (element.Kind == GeometryElementKind.Sash &&
                SashGeometry.ContainsSashProfileAtPoint(editor, element, worldPoint))
            {
                return CadSelectableItem.Sash(element.Id);
            }
        }

        // Переходной профиль — после ручки/створки, чтобы фурнитура не перекрывалась объёмом хоста.
        foreach (var element in canvasElements)
        {
            if (element.Kind == GeometryElementKind.TransitionFrame
                && HitTestTransitionProfileBand(editor, element, worldPoint))
            {
                return CadSelectableItem.Frame();
            }
        }

        foreach (var element in canvasElements)
        {
            if (element.Kind == GeometryElementKind.Sill && SillRenderer.TryHitSill(editor, worldPoint))
            {
                return CadSelectableItem.Sill(element.Id);
            }
        }

        if (editor.CurrentToolMode == WindowToolMode.SelectProfile
            && editor.FrameRoot is FrameNode profileFrame
            && TryHitTestFrameProfileRail(
                editor,
                profileFrame,
                worldPoint,
                out var profileRail,
                out var profileSegmentIndex))
        {
            return CadSelectableItem.FrameRail(profileRail, profileSegmentIndex);
        }

        if (editor.FrameRoot is not null && HitTestFrame(editor, worldPoint))
        {
            return CadSelectableItem.Frame();
        }

        return null;
    }

    public static IReadOnlyList<CadSelectableItem> HitTestInRect(CadEditorViewModel editor, CadRect selectionBounds)
    {
        var results = new HashSet<CadSelectableItem>();
        var canvasElements = editor.CanvasElements.ToList();

        foreach (var hardware in SashHardwareHitTest.HitTestInRect(editor, selectionBounds))
        {
            results.Add(hardware);
        }

        foreach (var element in canvasElements)
        {
            if (element.Kind == GeometryElementKind.Bead &&
                TryGetElementBounds(editor, element, out var beadBounds) &&
                selectionBounds.Intersects(beadBounds))
            {
                results.Add(CadSelectableItem.Bead(element.Id));
            }
        }

        foreach (var element in canvasElements)
        {
            if (element.Kind == GeometryElementKind.Glazing && GlazingIntersectsRect(editor, element, selectionBounds))
            {
                results.Add(CadSelectableItem.Glazing(element.Id));
            }
        }

        if (editor.FrameRoot is not null)
        {
            foreach (var (leaf, slot) in editor.FrameRoot.EnumerateExtremeImposts())
            {
                if (TryGetExtremeImpostBounds(editor, leaf, slot) is { } extremeBounds
                    && selectionBounds.Intersects(extremeBounds))
                {
                    results.Add(CadSelectableItem.ExtremeImpost(leaf, slot.Id));
                }
            }

            foreach (var node in editor.FrameRoot.EnumerateInternalNodes())
            {
                if (node.IsUniformSplit)
                {
                    for (var gapIndex = 0; gapIndex < node.UniformCells.Count - 1; gapIndex++)
                    {
                        if (ImpostProfileHitTest.IntersectsRect(editor, node, gapIndex, selectionBounds))
                        {
                            results.Add(CadSelectableItem.Impost(node, gapIndex));
                        }
                    }
                }
                else if (ImpostProfileHitTest.IntersectsRect(editor, node, 0, selectionBounds))
                {
                    results.Add(CadSelectableItem.Impost(node, 0));
                }
            }
        }

        foreach (var element in canvasElements)
        {
            if (element.Kind == GeometryElementKind.Sash &&
                SashGeometry.SashProfileIntersectsRect(editor, element, selectionBounds))
            {
                results.Add(CadSelectableItem.Sash(element.Id));
            }
        }

        if (editor.FrameRoot is not null
            && editor.CurrentToolMode == WindowToolMode.SelectProfile)
        {
            foreach (var rail in new[]
                     {
                         FrameProfileRail.Top,
                         FrameProfileRail.Bottom,
                         FrameProfileRail.Left,
                         FrameProfileRail.Right
                     })
            {
                var band = editor.FrameRoot.GetRailBand(rail, editor.FrameThickness);
                var joints = editor.FrameRoot.GetProfileRailJoints(rail);
                if (joints.Count == 0)
                {
                    if (RailBandIntersectsRect(band, selectionBounds))
                    {
                        results.Add(CadSelectableItem.FrameRail(rail, 0));
                    }

                    continue;
                }

                var outerJoints = joints
                    .Select(editor.FrameRoot.ResolveProfileRailJointOuterPoint)
                    .ToList();
                var segments = FrameProfileRailGeometry.SplitRailBandWithMiters(band, outerJoints);
                for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                {
                    if (RailBandIntersectsRect(segments[segmentIndex], selectionBounds))
                    {
                        results.Add(CadSelectableItem.FrameRail(rail, segmentIndex));
                    }
                }
            }

            return results.ToList();
        }

        if (editor.FrameRoot is not null && FrameProfileIntersectsRect(editor, selectionBounds))
        {
            results.Add(CadSelectableItem.Frame());
        }

        return results.ToList();
    }

    private static bool RailBandIntersectsRect(
        FrameProfileRailGeometry.RailBand band,
        CadRect selectionBounds)
    {
        var minX = Math.Min(
            Math.Min(band.OuterStart.X, band.OuterEnd.X),
            Math.Min(band.InnerStart.X, band.InnerEnd.X));
        var maxX = Math.Max(
            Math.Max(band.OuterStart.X, band.OuterEnd.X),
            Math.Max(band.InnerStart.X, band.InnerEnd.X));
        var minY = Math.Min(
            Math.Min(band.OuterStart.Y, band.OuterEnd.Y),
            Math.Min(band.InnerStart.Y, band.InnerEnd.Y));
        var maxY = Math.Max(
            Math.Max(band.OuterStart.Y, band.OuterEnd.Y),
            Math.Max(band.InnerStart.Y, band.InnerEnd.Y));
        var railBounds = new CadRect(minX, maxY, maxX - minX, maxY - minY);
        return selectionBounds.Intersects(railBounds);
    }

    public static bool TryGetElementBounds(CadEditorViewModel editor, GeometryElement element, out CadRect bounds)
    {
        bounds = default;
        if (element.Kind == GeometryElementKind.Sash)
        {
            var inner = SashGeometry.TryResolveInnerBounds(editor, element, out var liveInner)
                ? liveInner
                : SashGeometry.InnerOpeningBoundsFromElement(element);
            var outer = SashGeometry.ExpandInnerBoundsToOuter(inner, editor);
            if (outer.Width <= 0 || outer.Height <= 0)
            {
                return false;
            }

            bounds = new CadRect(outer.Left, outer.Top, outer.Width, outer.Height);
            return true;
        }

        if (element.Kind == GeometryElementKind.Glazing)
        {
            if (SashGeometry.TryResolveGlassForGlazingElement(editor, element, editor.CanvasElements, out var glass) &&
                glass.Width > 0 && glass.Height > 0)
            {
                bounds = new CadRect(glass.Left, glass.Top, glass.Width, glass.Height);
                return true;
            }
        }

        var halfW = (double)element.WidthMm / 2.0;
        var halfH = (double)element.HeightMm / 2.0;
        if (halfW <= 0 || halfH <= 0)
        {
            return false;
        }

        var cx = (double)element.X;
        var cy = (double)element.Y;
        bounds = new CadRect(cx - halfW, cy + halfH, halfW * 2, halfH * 2);
        return true;
    }

    public static CadRect? GetFrameBounds(CadEditorViewModel editor)
    {
        if (editor.FrameRoot is not FrameNode frame)
        {
            return null;
        }

        var points = frame.GetConstructionRoot().GetOuterContourPoints();
        if (points.Count == 0)
        {
            return null;
        }

        var minX = points.Min(point => point.X);
        var maxX = points.Max(point => point.X);
        var minY = points.Min(point => point.Y);
        var maxY = points.Max(point => point.Y);
        return new CadRect(minX, maxY, maxX - minX, maxY - minY);
    }

    public static CadRect? TryGetImpostBounds(CadEditorViewModel editor, FrameNode node, int gapIndex)
    {
        if (!ImpostProfileHitTest.TryGetFootprint(
                editor,
                node,
                gapIndex,
                out var left,
                out var right,
                out var yBottom,
                out var rectangularTop,
                out var arc))
        {
            return null;
        }

        var top = rectangularTop;
        if (arc is ArcSpringingGeometry archedArc)
        {
            top = Math.Max(rectangularTop, archedArc.Center.Y + archedArc.InnerRadius);
        }
        else if (ImpostProfileHitTest.TryGetHorizontalFootprintPolygon(editor, node, gapIndex, out var horizontalPolygon))
        {
            var maxX = horizontalPolygon.Max(point => point.X);
            var minX = horizontalPolygon.Min(point => point.X);
            var maxY = horizontalPolygon.Max(point => point.Y);
            var minY = horizontalPolygon.Min(point => point.Y);
            return new CadRect(minX, maxY, maxX - minX, maxY - minY);
        }

        return new CadRect(left, top, right - left, top - yBottom);
    }

    public static CadRect? TryGetExtremeImpostBounds(
        CadEditorViewModel editor,
        FrameNode leaf,
        ExtremeImpostSlot slot,
        double? displayThicknessMm = null)
    {
        if (editor.FrameRoot is null)
        {
            return null;
        }

        var cellAxes = SashGeometry.ResolveLeafCellAxisBounds(
            leaf,
            editor.FrameRoot,
            editor.FrameThickness,
            includeExtremeImposts: false);
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
        var thickness = displayThicknessMm ?? slot.ThicknessMm;
        var half = thickness / 2.0;

        if (slot.Orientation == SplitOrientation.Vertical)
        {
            return new CadRect(axis - half, spanEnd, thickness, spanEnd - spanStart);
        }

        return new CadRect(spanStart, axis + half, spanEnd - spanStart, thickness);
    }

    private static bool HitTestFrame(CadEditorViewModel editor, CadPoint worldPoint)
    {
        if (editor.FrameRoot is not FrameNode frame)
        {
            return false;
        }

        // Фиктивный контейнер не имеет собственного профильного объёма — попадание
        // обрабатывается через вложенные Frame / TransitionFrame.
        if (frame.IsFictitiousContainer)
        {
            return false;
        }

        if (frame.Shape == ConstructionShape.Triangle)
        {
            var outer = FrameTriangleGeometry.BuildOuterTriangle(frame.X, frame.Y, frame.Width, frame.Height);
            var inner = FrameTriangleGeometry.BuildInnerTriangle(frame, editor.FrameThickness);
            return ProfileBandHitTest.ContainsPointInTriangularBand(
                outer.Bl,
                outer.Br,
                outer.Top,
                inner.Bl,
                inner.Br,
                inner.Top,
                worldPoint);
        }

        if (TryGetArchedFrameContours(editor, out var outerLoop, out var innerLoop))
        {
            return ProfileBandHitTest.ContainsPoint(outerLoop, innerLoop, worldPoint);
        }

        var inset = FrameProfileGeometry.ResolveInnerInset(
            frame.Width,
            frame.Height,
            editor.FrameThickness);
        return ProfileBandHitTest.ContainsPointInRectangularBand(
            frame.X,
            frame.Y,
            frame.X + frame.Width,
            frame.Y + frame.Height,
            frame.X + inset,
            frame.Y + inset,
            frame.X + frame.Width - inset,
            frame.Y + frame.Height - inset,
            worldPoint);
    }

    private static bool HitTestTransitionProfileBand(
        CadEditorViewModel editor,
        GeometryElement element,
        CadPoint worldPoint)
    {
        if (!TransitionAdjacentFrameGeometry.TryResolveHostGeometry(
                editor,
                element,
                editor.FrameRoot,
                editor.FrameThickness,
                out var host))
        {
            return false;
        }

        return ProfileBandHitTest.ContainsPointInRectangularBand(
            host.OuterLeft,
            host.OuterBottom,
            host.OuterRight,
            host.OuterTop,
            host.InnerLeftFace((host.OuterBottom + host.OuterTop) / 2.0),
            host.InnerBottomFace((host.OuterLeft + host.OuterRight) / 2.0),
            host.InnerRightFace((host.OuterBottom + host.OuterTop) / 2.0),
            host.InnerTopFace((host.OuterLeft + host.OuterRight) / 2.0),
            worldPoint);
    }

    private static bool FrameProfileIntersectsRect(CadEditorViewModel editor, CadRect selection)
    {
        if (editor.FrameRoot is not FrameNode frame)
        {
            return false;
        }

        if (TryGetArchedFrameContours(editor, out var outerLoop, out var innerLoop))
        {
            return ProfileBandHitTest.IntersectsRect(outerLoop, innerLoop, selection);
        }

        var inset = FrameProfileGeometry.ResolveInnerInset(
            frame.Width,
            frame.Height,
            editor.FrameThickness);
        return ProfileBandHitTest.RectIntersectsRectangularBand(
            selection,
            frame.X,
            frame.Y,
            frame.X + frame.Width,
            frame.Y + frame.Height,
            frame.X + inset,
            frame.Y + inset,
            frame.X + frame.Width - inset,
            frame.Y + frame.Height - inset);
    }

    private static bool TryHitTestFrameProfileRail(
        CadEditorViewModel editor,
        FrameNode frame,
        CadPoint worldPoint,
        out FrameProfileRail rail,
        out int segmentIndex)
    {
        rail = default;
        segmentIndex = 0;
        if (frame.IsDummy || frame.IsFictitiousContainer)
        {
            return false;
        }

        if (frame.Shape != ConstructionShape.Rectangle)
        {
            return false;
        }

        if (frame.TopProfile?.SegmentType == SegmentType.Arc
            && frame.TopProfile.ArcHeight > ProfileSegment.MinFlatArcHeightMm + CadMath.Epsilon)
        {
            return false;
        }

        var root = frame.GetConstructionRoot();
        foreach (var candidateRail in new[]
                 {
                     FrameProfileRail.Top,
                     FrameProfileRail.Bottom,
                     FrameProfileRail.Left,
                     FrameProfileRail.Right
                 })
        {
            var band = root.GetRailBand(candidateRail, editor.FrameThickness);
            var outerJoints = root.GetProfileRailJoints(candidateRail)
                .Select(root.ResolveProfileRailJointOuterPoint)
                .ToList();
            var segments = outerJoints.Count == 0
                ? [band]
                : FrameProfileRailGeometry.SplitRailBandWithMiters(band, outerJoints);
            for (var candidateSegmentIndex = 0;
                 candidateSegmentIndex < segments.Count;
                 candidateSegmentIndex++)
            {
                if (!FrameProfileRailGeometry.ContainsPointInBand(
                        segments[candidateSegmentIndex],
                        worldPoint))
                {
                    continue;
                }

                rail = candidateRail;
                segmentIndex = candidateSegmentIndex;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetArchedFrameContours(
        CadEditorViewModel editor,
        out ProfileContourLoop outerLoop,
        out ProfileContourLoop innerLoop)
    {
        outerLoop = null!;
        innerLoop = null!;

        if (editor.FrameRoot is not FrameNode frame ||
            frame.TopProfile?.SegmentType != SegmentType.Arc ||
            frame.TopProfile.ArcHeight <= ProfileSegment.MinFlatArcHeightMm)
        {
            return false;
        }

        try
        {
            var contours = FrameProfileGeometry.BuildArchedFrameContours(
                frame.X,
                frame.Y,
                frame.Width,
                frame.Height,
                frame.TopProfile,
                editor.FrameThickness);
            outerLoop = contours.OuterLoop;
            innerLoop = contours.InnerLoop;
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static bool HitTestGlazing(CadEditorViewModel editor, GeometryElement element, CadPoint worldPoint)
    {
        if (!SashGeometry.TryResolveGlassForGlazingElement(editor, element, editor.CanvasElements, out var glass))
        {
            return HitTestGeometryElement(element, worldPoint, 0);
        }

        if (worldPoint.X < glass.Left - CadMath.Epsilon || worldPoint.X > glass.Right + CadMath.Epsilon ||
            worldPoint.Y < glass.Bottom - CadMath.Epsilon)
        {
            return false;
        }

        return worldPoint.Y <= glass.ResolveTopAtX(worldPoint.X) + CadMath.Epsilon;
    }

    private static bool GlazingIntersectsRect(
        CadEditorViewModel editor,
        GeometryElement element,
        CadRect selection)
    {
        if (!SashGeometry.TryResolveGlassForGlazingElement(editor, element, editor.CanvasElements, out var glass))
        {
            return TryGetElementBounds(editor, element, out var bounds) && selection.Intersects(bounds);
        }

        if (glass.IsArched || glass.IsTriangular)
        {
            return ProfileBandHitTest.RectIntersectsPolygon(glass.BuildOuterContourLoop().Tessellate(), selection);
        }

        return selection.Intersects(new CadRect(glass.Left, glass.Top, glass.Width, glass.Height));
    }

    private static bool HitTestGeometryElement(GeometryElement element, CadPoint worldPoint, double extraTolerance)
    {
        var halfW = (double)element.WidthMm / 2.0 + extraTolerance;
        var halfH = (double)element.HeightMm / 2.0 + extraTolerance;
        var cx = (double)element.X;
        var cy = (double)element.Y;
        return worldPoint.X >= cx - halfW && worldPoint.X <= cx + halfW &&
               worldPoint.Y >= cy - halfH && worldPoint.Y <= cy + halfH;
    }

    private static ImpostTarget? HitTestImpost(CadEditorViewModel editor, CadPoint worldPoint) =>
        editor.HitTestGridLine(worldPoint);
}
