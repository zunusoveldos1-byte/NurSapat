using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.GlazingBead;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services.GlazingBead;

/// <summary>Строит геометрию штапика по световому проёму.</summary>
internal static class GlazingBeadBuilder
{
    public static bool TryBuild(
        CadEditorViewModel editor,
        FrameNode leaf,
        Guid? beadMaterialId,
        Guid? profileSeriesId,
        out GlazingBeadModel? bead)
    {
        bead = null;
        if (editor.FrameRoot is null || !TryResolveLightOpeningBounds(editor, leaf, out var outerBounds))
        {
            return false;
        }

        var beadWidth = ResolveBeadProfileWidth(beadMaterialId);
        if (!outerBounds.TryBuildInnerContourLoop(beadWidth, out var innerLoop))
        {
            return false;
        }

        var outerLoop = outerBounds.BuildOuterContourLoop();
        if (outerBounds.Width < GlazingBeadSettings.MinOpeningSizeMm
            || outerBounds.Height < GlazingBeadSettings.MinOpeningSizeMm)
        {
            return false;
        }

        var sash = FindSashInLeaf(editor, leaf);
        bead = new GlazingBeadModel
        {
            ProfileId = profileSeriesId,
            MaterialId = beadMaterialId,
            Width = beadWidth,
            Height = beadWidth,
            Offset = 0,
            Segments = outerLoop.Edges.ToList(),
            OuterContour = outerLoop,
            InnerContour = innerLoop,
            OwnerId = leaf.NodeId,
            HostSashId = sash?.Id,
            CenterX = (outerBounds.Left + outerBounds.Right) / 2.0,
            CenterY = (outerBounds.Bottom + outerBounds.Top) / 2.0,
            WidthMm = outerBounds.Width,
            HeightMm = outerBounds.Height
        };

        return true;
    }

    public static bool TryRebuild(
        CadEditorViewModel editor,
        GeometryElement beadElement,
        out GlazingBeadModel? bead)
    {
        bead = null;
        if (editor.FrameRoot is null || beadElement.HostFrameNodeId is not Guid leafId)
        {
            return false;
        }

        var leaf = SashGeometry.EnumerateFrameLeaves(editor.FrameRoot)
            .FirstOrDefault(node => node.NodeId == leafId);
        if (leaf is null || !leaf.IsLeaf)
        {
            return false;
        }

        if (!TryBuild(
                editor,
                leaf,
                beadElement.MaterialId,
                beadElement.ProfileSeriesId,
                out var rebuilt)
            || rebuilt is null)
        {
            return false;
        }

        bead = rebuilt;
        bead.Id = beadElement.Id;
        bead.CreatedAt = beadElement.CreatedAt ?? DateTimeOffset.UtcNow;
        return true;
    }

    public static bool TryResolveLightOpeningBounds(
        CadEditorViewModel editor,
        FrameNode leaf,
        out SashGeometry.GlassOpeningBounds bounds)
    {
        bounds = default;
        if (editor.FrameRoot is null)
        {
            return false;
        }

        var sash = FindSashInLeaf(editor, leaf);
        if (sash is not null)
        {
            return TryResolveSashLightOpening(editor, sash, leaf, out bounds);
        }

        return TryResolveFixedLightOpening(editor, leaf, out bounds);
    }

    private static bool TryResolveSashLightOpening(
        CadEditorViewModel editor,
        GeometryElement sash,
        FrameNode leaf,
        out SashGeometry.GlassOpeningBounds bounds)
    {
        bounds = default;
        if (editor.FrameRoot is null)
        {
            return false;
        }

        var inner = SashGeometry.TryResolveInnerBounds(editor, sash, out var liveInner)
            ? liveInner
            : SashGeometry.ResolveLeafInnerOpeningBounds(leaf, editor.FrameRoot, editor.FrameThickness);
        var outer = SashGeometry.ExpandInnerBoundsToOuter(inner, editor);
        SashGeometry.TryResolveGlassOpeningBounds(
            editor,
            inner,
            outer,
            SashGeometry.SashDrawProfileThicknessMm,
            out bounds);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    private static bool TryResolveFixedLightOpening(
        CadEditorViewModel editor,
        FrameNode leaf,
        out SashGeometry.GlassOpeningBounds bounds)
    {
        bounds = default;
        if (editor.FrameRoot is null)
        {
            return false;
        }

        var inner = SashGeometry.ResolveLeafInnerOpeningBounds(leaf, editor.FrameRoot, editor.FrameThickness);
        inner = SashGeometry.UpgradeInnerTopForArcGlazing(editor, inner);
        if (inner.Width <= CadMath.Epsilon || inner.Height <= CadMath.Epsilon)
        {
            return false;
        }

        if (SashGeometry.ShouldUseArchedGlazingTop(editor, inner)
            && TryBuildArchedLightOpening(editor, inner, out bounds))
        {
            return true;
        }

        bounds = SashGeometry.GlassOpeningBounds.FromRect(inner.Left, inner.Bottom, inner.Right, inner.Top);
        return bounds.Width > 0 && bounds.Height > 0;
    }

    private static bool TryBuildArchedLightOpening(
        CadEditorViewModel editor,
        SashGeometry.InnerOpeningBounds inner,
        out SashGeometry.GlassOpeningBounds bounds)
    {
        bounds = default;
        if (editor.FrameRoot?.TopProfile?.SegmentType != SegmentType.Arc)
        {
            return false;
        }

        var chordY = editor.FrameRoot.Y + editor.FrameRoot.Height;
        var profileWall = FrameProfileGeometry.ResolveInnerInset(
            editor.FrameRoot.Width,
            editor.FrameRoot.Height,
            editor.FrameThickness);

        try
        {
            var arc = FrameProfileGeometry.ComputeArcSpringingGeometry(
                editor.FrameRoot.X,
                editor.FrameRoot.Width,
                chordY,
                editor.FrameRoot.TopProfile.ArcHeight,
                profileWall);

            var x0 = inner.Left;
            var x1 = inner.Right;
            var y0 = inner.Bottom;
            var springingLeft = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.OuterRadius, x0);
            var springingRight = ProfileSegment.GetPointOnUpperArc(arc.Center, arc.OuterRadius, x1);
            if (springingLeft.Y - y0 <= CadMath.Epsilon)
            {
                return false;
            }

            bounds = new SashGeometry.GlassOpeningBounds
            {
                Left = x0,
                Bottom = y0,
                Right = x1,
                Top = springingLeft.Y,
                IsArched = true,
                ArcCenter = arc.Center,
                ArcRadius = arc.OuterRadius,
                BottomLeft = new CadPoint(x0, y0),
                BottomRight = new CadPoint(x1, y0),
                SpringingLeft = springingLeft,
                SpringingRight = springingRight
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool TryResolveBeadContours(
        GeometryElement beadElement,
        CadEditorViewModel editor,
        out ProfileContourLoop outerLoop,
        out ProfileContourLoop innerLoop)
    {
        outerLoop = null!;
        innerLoop = null!;
        if (!TryRebuild(editor, beadElement, out var model)
            || model?.OuterContour is null
            || model.InnerContour is null)
        {
            return false;
        }

        outerLoop = model.OuterContour;
        innerLoop = model.InnerContour;
        return true;
    }

    private static double ResolveBeadProfileWidth(Guid? materialId)
    {
        var material = ConstructionMaterialCatalog.FindById(materialId);
        return material?.ProfileWidth ?? GlazingBeadSettings.DefaultBeadWidthMm;
    }

    private static GeometryElement? FindSashInLeaf(CadEditorViewModel editor, FrameNode leaf)
    {
        GeometryElement? best = null;
        var bestOverlap = 0.0;

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.Sash)
            {
                continue;
            }

            if (SashGeometry.TryMeasureLeafSashOverlap(editor, leaf, element, out var overlap)
                && overlap > bestOverlap)
            {
                bestOverlap = overlap;
                best = element;
            }
        }

        return best;
    }
}
