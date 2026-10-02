using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

internal static class SashHardwareHitTest
{
    private const double HandleBaseWidthMm = 24.0;
    private const double HandleBaseHeightMm = 65.0;
    private const double HandleLeverThicknessMm = 16.0;
    private const double HandleLeverLengthMm = 110.0;
    private const double HandleGlassInsetMm = 4.0;
    private const double HingeWidthMm = 19.0;
    private const double HingeHeightMm = 65.0;
    private const double HingeEdgeOffsetMm = 60.0;
    private const double HingeFrameOverlapMm = 6.0;

    public readonly record struct HardwareRegion(CadSelectableItem Item, CadRect Bounds);

    public static IEnumerable<HardwareRegion> EnumerateRegions(CadEditorViewModel editor, GeometryElement sash)
    {
        if (sash.Kind != GeometryElementKind.Sash)
        {
            yield break;
        }

        var inner = SashGeometry.TryResolveInnerBounds(editor, sash, out var liveInner)
            ? liveInner
            : SashGeometry.InnerOpeningBoundsFromElement(sash);
        var outer = SashGeometry.ExpandInnerBoundsToOuter(inner, editor);
        if (outer.Width <= 0 || outer.Height <= 0)
        {
            yield break;
        }

        if (!SashGeometry.TryResolveGlassOpeningBounds(
                editor,
                inner,
                outer,
                SashGeometry.SashDrawProfileThicknessMm,
                out var glass))
        {
            yield break;
        }

        var hinge = sash.HingeSide ?? SashHingeSide.Left;
        var opening = sash.OpeningType ?? SashOpeningType.TiltTurn;

        if (sash.HasHandle)
        {
            foreach (var rect in ResolveHandleRects(editor, inner, glass, outer, hinge))
            {
                yield return new HardwareRegion(CadSelectableItem.SashHandle(sash.Id), rect);
            }
        }

        if (sash.HasHardware)
        {
            foreach (var region in ResolveHingeRegions(editor, inner, outer, sash.Id, hinge, opening))
            {
                yield return region;
            }
        }
    }

    public static bool TryHitTest(CadEditorViewModel editor, CadPoint point, out CadSelectableItem? hit)
    {
        hit = null;
        HardwareRegion? best = null;
        var bestArea = double.MaxValue;

        foreach (var sash in editor.CanvasElements.Where(e => e.Kind == GeometryElementKind.Sash))
        {
            foreach (var region in EnumerateRegions(editor, sash))
            {
                if (!region.Bounds.Contains(point))
                {
                    continue;
                }

                var area = region.Bounds.Width * region.Bounds.Height;
                if (area < bestArea)
                {
                    bestArea = area;
                    best = region;
                }
            }
        }

        if (best is null)
        {
            return false;
        }

        hit = best.Value.Item;
        return true;
    }

    public static IEnumerable<CadSelectableItem> HitTestInRect(CadEditorViewModel editor, CadRect selection)
    {
        var results = new HashSet<CadSelectableItem>();
        foreach (var sash in editor.CanvasElements.Where(e => e.Kind == GeometryElementKind.Sash))
        {
            foreach (var region in EnumerateRegions(editor, sash))
            {
                if (selection.Intersects(region.Bounds))
                {
                    results.Add(region.Item);
                }
            }
        }

        return results;
    }

    private static IEnumerable<CadRect> ResolveHandleRects(
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashGeometry.GlassOpeningBounds glass,
        SashGeometry.OuterBounds outer,
        SashHingeSide hingeSide)
    {
        switch (hingeSide)
        {
            case SashHingeSide.Right:
                yield return BuildVerticalHandleRect(editor, inner, glass, outer, handleOnLeft: true);
                break;
            case SashHingeSide.Left:
                yield return BuildVerticalHandleRect(editor, inner, glass, outer, handleOnLeft: false);
                break;
            case SashHingeSide.Top:
                yield return BuildHorizontalHandleRect(editor, outer, glass, handleOnBottom: true);
                break;
            case SashHingeSide.Bottom:
                yield return BuildHorizontalHandleRect(editor, outer, glass, handleOnBottom: false);
                break;
        }
    }

    private static CadRect BuildVerticalHandleRect(
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashGeometry.GlassOpeningBounds glass,
        SashGeometry.OuterBounds outer,
        bool handleOnLeft)
    {
        var profileLeft = handleOnLeft ? outer.Left : glass.Right;
        var profileRight = handleOnLeft ? glass.Left : outer.Right;
        var axisX = (profileLeft + profileRight) / 2.0;
        var baseHalfH = Math.Min(HandleBaseHeightMm / 2.0, glass.Height * 0.45);
        var centerY = SashGeometry.ClampHandleCenterY(
            editor, inner, outer, glass, axisX, (glass.Bottom + glass.Top) / 2.0, baseHalfH);
        var baseHalfW = HandleBaseWidthMm / 2.0;

        var left = axisX - baseHalfW;
        var right = axisX + baseHalfW;
        var bottom = centerY - baseHalfH;
        var top = centerY + baseHalfH;

        var leverHalf = HandleLeverThicknessMm / 2.0;
        if (handleOnLeft)
        {
            var leverLength = Math.Min(HandleLeverLengthMm, Math.Max(0, glass.Right - axisX - HandleGlassInsetMm));
            right = Math.Max(right, axisX + leverLength);
            bottom = Math.Min(bottom, centerY - leverHalf);
            top = Math.Max(top, centerY + leverHalf);
        }
        else
        {
            var leverLength = Math.Min(HandleLeverLengthMm, Math.Max(0, axisX - glass.Left - HandleGlassInsetMm));
            left = Math.Min(left, axisX - leverLength);
            bottom = Math.Min(bottom, centerY - leverHalf);
            top = Math.Max(top, centerY + leverHalf);
        }

        return ToCadRect(left, bottom, right, top);
    }

    private static CadRect BuildHorizontalHandleRect(
        CadEditorViewModel editor,
        SashGeometry.OuterBounds outer,
        SashGeometry.GlassOpeningBounds glass,
        bool handleOnBottom)
    {
        _ = editor;
        var profileBottom = handleOnBottom ? outer.Bottom : glass.Top;
        var profileTop = handleOnBottom ? glass.Bottom : outer.Top;
        var axisY = (profileBottom + profileTop) / 2.0;
        var centerX = (glass.Left + glass.Right) / 2.0;
        var baseHalfSpan = Math.Min(HandleBaseHeightMm / 2.0, glass.Width * 0.45);
        var baseHalfDepth = HandleBaseWidthMm / 2.0;
        var leverHalf = HandleLeverThicknessMm / 2.0;

        var left = centerX - baseHalfSpan;
        var right = centerX + baseHalfSpan;
        var bottom = axisY - baseHalfDepth;
        var top = axisY + baseHalfDepth;

        if (handleOnBottom)
        {
            var leverLength = Math.Min(HandleLeverLengthMm, Math.Max(0, glass.ResolveTopAtX(centerX) - axisY - HandleGlassInsetMm));
            top = Math.Max(top, axisY + leverLength);
            left = Math.Min(left, centerX - leverHalf);
            right = Math.Max(right, centerX + leverHalf);
        }
        else
        {
            var leverLength = Math.Min(HandleLeverLengthMm, Math.Max(0, axisY - glass.Bottom - HandleGlassInsetMm));
            bottom = Math.Min(bottom, axisY - leverLength);
            left = Math.Min(left, centerX - leverHalf);
            right = Math.Max(right, centerX + leverHalf);
        }

        return ToCadRect(left, bottom, right, top);
    }

    private static IEnumerable<HardwareRegion> ResolveHingeRegions(
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashGeometry.OuterBounds outer,
        Guid sashId,
        SashHingeSide hingeSide,
        SashOpeningType openingType)
    {
        _ = openingType;
        switch (hingeSide)
        {
            case SashHingeSide.Left:
            case SashHingeSide.Right:
                foreach (var region in ResolveVerticalHingeRegions(editor, inner, outer, sashId, hingeSide))
                {
                    yield return region;
                }

                break;
            case SashHingeSide.Top:
            case SashHingeSide.Bottom:
                foreach (var region in ResolveHorizontalHingeRegions(editor, inner, outer, sashId, hingeSide == SashHingeSide.Top))
                {
                    yield return region;
                }

                break;
        }
    }

    private static IEnumerable<HardwareRegion> ResolveVerticalHingeRegions(
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashGeometry.OuterBounds outer,
        Guid sashId,
        SashHingeSide hingeSide)
    {
        var offset = ResolveHingeEdgeOffset(outer.Height);
        var hingeHeight = Math.Min(HingeHeightMm, Math.Max(HingeHeightMm * 0.85, outer.Height - offset * 2 - 10));
        if (hingeHeight <= CadMath.Epsilon)
        {
            yield break;
        }

        var straightTop = SashGeometry.ResolveStraightVerticalZoneTopY(editor, inner, outer);
        var topHingeBottom = Math.Min(outer.Top - offset, straightTop);
        var topHingeTop = topHingeBottom - hingeHeight;
        var bottomHingeBottom = outer.Bottom + offset;
        var bottomHingeTop = bottomHingeBottom + hingeHeight;
        if (topHingeTop < outer.Bottom + offset || bottomHingeTop >= topHingeTop - CadMath.Epsilon)
        {
            yield break;
        }

        if (hingeSide == SashHingeSide.Right)
        {
            var innerX = outer.Right - HingeWidthMm;
            var outerX = outer.Right + HingeFrameOverlapMm;
            yield return new HardwareRegion(
                CadSelectableItem.SashHinge(sashId, 0),
                ToCadRect(innerX, topHingeTop, outerX, topHingeBottom));
            yield return new HardwareRegion(
                CadSelectableItem.SashHinge(sashId, 1),
                ToCadRect(innerX, bottomHingeBottom, outerX, bottomHingeTop));
        }
        else
        {
            var innerX = outer.Left + HingeWidthMm;
            var outerX = outer.Left - HingeFrameOverlapMm;
            yield return new HardwareRegion(
                CadSelectableItem.SashHinge(sashId, 0),
                ToCadRect(outerX, topHingeTop, innerX, topHingeBottom));
            yield return new HardwareRegion(
                CadSelectableItem.SashHinge(sashId, 1),
                ToCadRect(outerX, bottomHingeBottom, innerX, bottomHingeTop));
        }
    }

    private static IEnumerable<HardwareRegion> ResolveHorizontalHingeRegions(
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        SashGeometry.OuterBounds outer,
        Guid sashId,
        bool mountOnTop)
    {
        var hingeAlongEdge = HingeWidthMm;
        var hingeIntoFrame = HingeHeightMm;
        var centerX = (outer.Left + outer.Right) / 2.0;
        var gap = 16.0;
        var leftInner = centerX - gap - hingeAlongEdge;
        var leftOuter = centerX - gap;
        var rightInner = centerX + gap;
        var rightOuter = centerX + gap + hingeAlongEdge;

        if (mountOnTop)
        {
            var mountY = SashGeometry.ShouldDrawArchedSash(editor, inner)
                ? SashGeometry.ResolveStraightVerticalZoneTopY(editor, inner, outer)
                : outer.Top;
            var innerY = mountY - HingeWidthMm;
            var outerY = mountY + hingeIntoFrame;
            yield return new HardwareRegion(CadSelectableItem.SashHinge(sashId, 0), ToCadRect(leftInner, innerY, leftOuter, outerY));
            yield return new HardwareRegion(CadSelectableItem.SashHinge(sashId, 1), ToCadRect(rightInner, innerY, rightOuter, outerY));
        }
        else
        {
            var innerY = outer.Bottom + HingeWidthMm;
            var outerY = outer.Bottom - hingeIntoFrame;
            yield return new HardwareRegion(CadSelectableItem.SashHinge(sashId, 0), ToCadRect(leftInner, outerY, leftOuter, innerY));
            yield return new HardwareRegion(CadSelectableItem.SashHinge(sashId, 1), ToCadRect(rightInner, outerY, rightOuter, innerY));
        }
    }

    private static double ResolveHingeEdgeOffset(double sashHeight) =>
        Math.Clamp(HingeEdgeOffsetMm, 40.0, Math.Max(40.0, sashHeight * 0.18));

    private static CadRect ToCadRect(double left, double bottom, double right, double top) =>
        new(left, top, right - left, top - bottom);
}
