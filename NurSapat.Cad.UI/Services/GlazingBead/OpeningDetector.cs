using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services.GlazingBead;

/// <summary>Кандидат светового проёма для установки штапика.</summary>
public sealed record LightOpeningCandidate(
    FrameNode Leaf,
    GeometryElement? Sash,
    bool HasExistingGlazing);

/// <summary>
/// Находит свободные световые проёмы конструкции (без штапика).
/// </summary>
internal static class OpeningDetector
{
    public static IReadOnlyList<LightOpeningCandidate> FindFreeLightOpenings(CadEditorViewModel editor)
    {
        if (editor.FrameRoot is null || !ConstructionHostResolver.HasConstructionHostOnCanvas(editor))
        {
            return [];
        }

        var result = new List<LightOpeningCandidate>();
        foreach (var leaf in SashGeometry.EnumerateFrameLeaves(editor.FrameRoot))
        {
            if (!leaf.IsTerminalLeaf || !ConstructionHostResolver.CanHostOpeningFillings(editor, leaf))
            {
                continue;
            }

            if (HasCorrugation(editor, leaf))
            {
                continue;
            }

            if (FindBeadInLeaf(editor, leaf) is not null)
            {
                continue;
            }

            if (!TryResolveLightOpeningSize(editor, leaf, out var width, out var height)
                || width < 40 || height < 40)
            {
                continue;
            }

            var sash = FindSashInLeaf(editor, leaf);
            var glazing = FindGlazingInLeaf(editor, leaf);
            result.Add(new LightOpeningCandidate(leaf, sash, glazing is not null));
        }

        return result;
    }

    public static LightOpeningCandidate? FindOpeningAtPoint(CadEditorViewModel editor, CadPoint worldPoint)
    {
        if (editor.FrameRoot is null)
        {
            return null;
        }

        var leaf = editor.FrameRoot.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (leaf is null || !leaf.IsTerminalLeaf || !ConstructionHostResolver.CanHostOpeningFillings(editor, leaf))
        {
            return null;
        }

        if (HasCorrugation(editor, leaf))
        {
            return null;
        }

        if (FindBeadInLeaf(editor, leaf) is not null)
        {
            return null;
        }

        if (!TryResolveLightOpeningSize(editor, leaf, out var width, out var height)
            || width < 40 || height < 40)
        {
            return null;
        }

        return new LightOpeningCandidate(leaf, FindSashInLeaf(editor, leaf), FindGlazingInLeaf(editor, leaf) is not null);
    }

    public static GeometryElement? FindBeadInLeaf(CadEditorViewModel editor, FrameNode leaf) =>
        editor.CanvasElements.FirstOrDefault(element =>
            element.Kind == GeometryElementKind.Bead
            && element.HostFrameNodeId == leaf.NodeId
            && element.SourceToolId == "bead");

    public static GeometryElement? FindBeadAtPoint(CadEditorViewModel editor, CadPoint worldPoint)
    {
        if (editor.FrameRoot is null)
        {
            return null;
        }

        var leaf = editor.FrameRoot.FindLeafAt(worldPoint.X, worldPoint.Y);
        return leaf is null ? null : FindBeadInLeaf(editor, leaf);
    }

    private static bool IsBillableLeaf(CadEditorViewModel editor, FrameNode leaf) =>
        ConstructionHostResolver.CanHostOpeningFillings(editor, leaf);

    private static bool HasCorrugation(CadEditorViewModel editor, FrameNode leaf) =>
        editor.CanvasElements.Any(e =>
            e.Kind == GeometryElementKind.Corrugation && leaf.Contains((double)e.X, (double)e.Y));

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

    private static GeometryElement? FindGlazingInLeaf(CadEditorViewModel editor, FrameNode leaf)
    {
        GeometryElement? best = null;
        var bestOverlap = 0.0;

        foreach (var element in editor.CanvasElements)
        {
            if (element.Kind != GeometryElementKind.Glazing)
            {
                continue;
            }

            if (element.HostSashId is Guid sashId)
            {
                if (FindSashInLeaf(editor, leaf)?.Id == sashId)
                {
                    return element;
                }

                continue;
            }

            if (!SashGeometry.TryMeasureLeafGlazingOverlap(editor, leaf, element, out var overlap))
            {
                continue;
            }

            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
                best = element;
            }
        }

        return best;
    }

    private static bool TryResolveLightOpeningSize(
        CadEditorViewModel editor,
        FrameNode leaf,
        out double width,
        out double height)
    {
        width = 0;
        height = 0;

        if (!GlazingBeadBuilder.TryResolveLightOpeningBounds(editor, leaf, out var bounds))
        {
            return false;
        }

        width = bounds.Width;
        height = bounds.Height;
        return true;
    }
}
