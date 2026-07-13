using System.Linq;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Rendering;

/// <summary>
/// Виртуальная четверть рамы у ячейки, примыкающей к переходному профилю.
/// </summary>
internal static class TransitionAdjacentFrameGeometry
{
    public static SashGeometry.InnerOpeningBounds ResolveImpostHostInnerOpeningBounds(
        CadEditorViewModel editor,
        FrameNode hostNode,
        FrameNode? frameRoot,
        double frameThickness)
    {
        var inner = SashGeometry.ResolveNodeInnerOpeningBounds(hostNode, frameRoot, frameThickness);
        if (frameRoot?.IsFictitiousContainer != true)
        {
            return inner;
        }

        ApplyAdjacentTransitionQuarterFrame(editor, hostNode, frameRoot, frameThickness, ref inner);
        return inner;
    }

    public static void ApplyAdjacentTransitionQuarterFrame(
        CadEditorViewModel editor,
        FrameNode hostNode,
        FrameNode frameRoot,
        double frameThickness,
        ref SashGeometry.InnerOpeningBounds inner)
    {
        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            var transitionLeaf = frameRoot.FindLeafAt((double)element.X, (double)element.Y);
            if (transitionLeaf is null)
            {
                continue;
            }

            if (!TryResolveTransitionInnerShelves(
                    editor,
                    element,
                    transitionLeaf,
                    frameRoot,
                    frameThickness,
                    out var shelves))
            {
                continue;
            }

            if (IsHostRightOfTransition(hostNode, transitionLeaf))
            {
                var probeY = Math.Clamp(
                    hostNode.Y + hostNode.Height / 2.0,
                    hostNode.Y,
                    hostNode.Y + hostNode.Height);
                inner.Left = Math.Max(inner.Left, ResolveShelfAtY(shelves.InnerRightBottom, shelves.InnerRightTop, probeY));
                inner.PinsFrameLeft = false;
            }

            if (IsHostLeftOfTransition(hostNode, transitionLeaf))
            {
                var probeY = Math.Clamp(
                    hostNode.Y + hostNode.Height / 2.0,
                    hostNode.Y,
                    hostNode.Y + hostNode.Height);
                inner.Right = Math.Min(inner.Right, ResolveShelfAtY(shelves.InnerLeftBottom, shelves.InnerLeftTop, probeY));
                inner.PinsFrameRight = false;
            }

            if (IsHostAboveTransition(hostNode, transitionLeaf))
            {
                var probeX = Math.Clamp(
                    hostNode.X + hostNode.Width / 2.0,
                    hostNode.X,
                    hostNode.X + hostNode.Width);
                inner.Bottom = Math.Max(inner.Bottom, ResolveShelfAtY(shelves.InnerBottomLeft, shelves.InnerBottomRight, probeX, horizontal: true));
                inner.PinsFrameBottom = false;
            }

            if (IsHostBelowTransition(hostNode, transitionLeaf))
            {
                var probeX = Math.Clamp(
                    hostNode.X + hostNode.Width / 2.0,
                    hostNode.X,
                    hostNode.X + hostNode.Width);
                inner.Top = Math.Min(inner.Top, ResolveShelfAtY(shelves.InnerTopLeft, shelves.InnerTopRight, probeX, horizontal: true));
                inner.PinsFrameTop = false;
            }
        }

        inner.Left = CadMath.RoundMm(inner.Left);
        inner.Bottom = CadMath.RoundMm(inner.Bottom);
        inner.Right = CadMath.RoundMm(inner.Right);
        inner.Top = CadMath.RoundMm(inner.Top);
    }

    public static void ClipVerticalSpanAtTransitionShelves(
        CadEditorViewModel editor,
        FrameNode splitNode,
        double impostAxisX,
        ref double bottom,
        ref double top)
    {
        if (editor.FrameRoot?.IsFictitiousContainer != true)
        {
            return;
        }

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.TransitionFrame)
            {
                continue;
            }

            var transitionLeaf = editor.FrameRoot.FindLeafAt((double)element.X, (double)element.Y);
            if (transitionLeaf is null
                || !TryResolveTransitionInnerShelves(
                    editor,
                    element,
                    transitionLeaf,
                    editor.FrameRoot,
                    editor.FrameThickness,
                    out var shelves))
            {
                continue;
            }

            var (originX, originY, width, height) = GetWorldOriginBounds(element);
            var hostRight = originX + width;
            var hostBottom = originY;
            var hostTop = originY + height;

            if (impostAxisX <= originX + CadMath.MmPrecision || impostAxisX >= hostRight - CadMath.MmPrecision)
            {
                continue;
            }

            if (top <= hostBottom + CadMath.Epsilon || bottom >= hostTop - CadMath.Epsilon)
            {
                continue;
            }

            var innerBottom = ResolveShelfAtY(shelves.InnerBottomLeft, shelves.InnerBottomRight, impostAxisX, horizontal: true);
            var innerTop = ResolveShelfAtY(shelves.InnerTopLeft, shelves.InnerTopRight, impostAxisX, horizontal: true);
            bottom = Math.Max(bottom, innerBottom);
            top = Math.Min(top, innerTop);
        }
    }

    private readonly record struct TransitionInnerShelves(
        double InnerLeftBottom,
        double InnerRightBottom,
        double InnerLeftTop,
        double InnerRightTop,
        double InnerBottomLeft,
        double InnerBottomRight,
        double InnerTopLeft,
        double InnerTopRight);

    private static bool TryResolveTransitionInnerShelves(
        CadEditorViewModel editor,
        GeometryElement element,
        FrameNode transitionLeaf,
        FrameNode frameRoot,
        double frameThickness,
        out TransitionInnerShelves shelves)
    {
        shelves = default;
        var (originX, originY, width, height) = GetWorldOriginBounds(element);
        if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
        {
            return false;
        }

        var profileThickness = ResolveTransitionProfileThickness(element);
        var halfThickness = FrameProfileGeometry.ResolveProfileThickness(width, height, profileThickness) / 2.0;
        if (halfThickness <= CadMath.Epsilon)
        {
            return false;
        }

        var outerLeft = originX;
        var outerBottom = originY;
        var outerRight = originX + width;
        var outerTop = originY + height;

        SashGeometry.ApplyExtremeImpostJunctionToTransitionBandOuter(
            transitionLeaf,
            frameRoot,
            frameThickness,
            ref outerLeft,
            ref outerBottom,
            ref outerRight,
            ref outerTop);

        if (outerRight <= outerLeft + CadMath.Epsilon || outerTop <= outerBottom + CadMath.Epsilon)
        {
            return false;
        }

        SashGeometry.ResolveTransitionBandInnerCorners(
            transitionLeaf,
            frameRoot,
            frameThickness,
            outerLeft,
            outerBottom,
            outerRight,
            outerTop,
            halfThickness,
            out var innerLeftBottom,
            out var innerRightBottom,
            out var innerLeftTop,
            out var innerRightTop,
            out var innerBottomLeft,
            out var innerTopLeft,
            out var innerBottomRight,
            out var innerTopRight);

        shelves = new TransitionInnerShelves(
            innerLeftBottom,
            innerRightBottom,
            innerLeftTop,
            innerRightTop,
            innerBottomLeft,
            innerBottomRight,
            innerTopLeft,
            innerTopRight);
        return true;
    }

    private static double ResolveShelfAtY(
        double shelfStart,
        double shelfEnd,
        double probe,
        bool horizontal = false)
    {
        if (horizontal)
        {
            return CadMath.RoundMm(Math.Abs(shelfEnd - shelfStart) <= CadMath.Epsilon
                ? shelfStart
                : shelfStart + (shelfEnd - shelfStart) * Math.Clamp((probe - Math.Min(shelfStart, shelfEnd)) / Math.Abs(shelfEnd - shelfStart), 0, 1));
        }

        return CadMath.RoundMm(Math.Abs(shelfEnd - shelfStart) <= CadMath.Epsilon
            ? shelfStart
            : shelfStart + (shelfEnd - shelfStart) * Math.Clamp((probe - Math.Min(shelfStart, shelfEnd)) / Math.Abs(shelfEnd - shelfStart), 0, 1));
    }

    private static bool IsHostRightOfTransition(FrameNode hostNode, FrameNode transitionLeaf) =>
        CadMath.NearlyEqual(hostNode.X, transitionLeaf.X + transitionLeaf.Width)
        && RangesOverlap(hostNode.Y, hostNode.Y + hostNode.Height, transitionLeaf.Y, transitionLeaf.Y + transitionLeaf.Height);

    private static bool IsHostLeftOfTransition(FrameNode hostNode, FrameNode transitionLeaf) =>
        CadMath.NearlyEqual(hostNode.X + hostNode.Width, transitionLeaf.X)
        && RangesOverlap(hostNode.Y, hostNode.Y + hostNode.Height, transitionLeaf.Y, transitionLeaf.Y + transitionLeaf.Height);

    private static bool IsHostAboveTransition(FrameNode hostNode, FrameNode transitionLeaf) =>
        CadMath.NearlyEqual(hostNode.Y, transitionLeaf.Y + transitionLeaf.Height)
        && RangesOverlap(hostNode.X, hostNode.X + hostNode.Width, transitionLeaf.X, transitionLeaf.X + transitionLeaf.Width);

    private static bool IsHostBelowTransition(FrameNode hostNode, FrameNode transitionLeaf) =>
        CadMath.NearlyEqual(hostNode.Y + hostNode.Height, transitionLeaf.Y)
        && RangesOverlap(hostNode.X, hostNode.X + hostNode.Width, transitionLeaf.X, transitionLeaf.X + transitionLeaf.Width);

    private static bool RangesOverlap(double a0, double a1, double b0, double b1)
    {
        var minA = Math.Min(a0, a1);
        var maxA = Math.Max(a0, a1);
        var minB = Math.Min(b0, b1);
        var maxB = Math.Max(b0, b1);
        return maxA > minB + CadMath.MmPrecision && maxB > minA + CadMath.MmPrecision;
    }

    private static double ResolveTransitionProfileThickness(GeometryElement element)
    {
        var material = ConstructionMaterialCatalog.FindById(element.MaterialId);
        if (material is { ProfileWidth: > 0 })
        {
            return material.ProfileWidth;
        }

        return ConstructionMaterialCatalog.ForKind(GeometryElementKind.TransitionFrame).FirstOrDefault()?.ProfileWidth
            ?? 46;
    }

    private static (double X, double Y, double W, double H) GetWorldOriginBounds(GeometryElement element)
    {
        var w = (double)element.WidthMm;
        var h = (double)element.HeightMm;
        return ((double)element.X - w / 2.0, (double)element.Y - h / 2.0, w, h);
    }
}
