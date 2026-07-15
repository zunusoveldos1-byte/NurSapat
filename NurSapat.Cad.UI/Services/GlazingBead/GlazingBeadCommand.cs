using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services.GlazingBead;

/// <summary>Одна операция установки штапика (и опционально стеклопакета) — единый Undo/Redo.</summary>
internal sealed class GlazingBeadCommand
{
    public bool TryPrepare(
        CadEditorViewModel editor,
        IReadOnlyList<LightOpeningCandidate> openings,
        GlazingBeadPlacementOptions options,
        out GlazingBeadPlacementBatch batch)
    {
        batch = new GlazingBeadPlacementBatch();
        foreach (var opening in openings)
        {
            if (!TryPrepareSingle(editor, opening, options, batch))
            {
                return false;
            }
        }

        return batch.ElementsToAdd.Count > 0;
    }

    private static bool TryPrepareSingle(
        CadEditorViewModel editor,
        LightOpeningCandidate opening,
        GlazingBeadPlacementOptions options,
        GlazingBeadPlacementBatch batch)
    {
        if (OpeningDetector.FindBeadInLeaf(editor, opening.Leaf) is not null)
        {
            return false;
        }

        if (!GlazingBeadBuilder.TryBuild(
                editor,
                opening.Leaf,
                options.BeadMaterialId,
                options.ProfileSeriesId,
                out var beadModel)
            || beadModel is null)
        {
            return false;
        }

        RemoveStandaloneGlazingInLeaf(editor, opening.Leaf, batch);

        var beadElement = beadModel.ToGeometryElement();
        batch.ElementsToAdd.Add(beadElement);

        if (!options.IncludeGlass)
        {
            return true;
        }

        // Стеклопакет строго внутри штапика (ParentBeadId → одна транзакция с штапиком).
        if (!GlassBuilder.TryBuildFromBead(beadModel, out var glassModel, out _)
            || glassModel is null)
        {
            return false;
        }

        batch.ElementsToAdd.Add(glassModel.ToGeometryElement());
        return true;
    }

    /// <summary>Удаляет остекление без штапика — в проёме остаётся пустое место под будущий стеклопакет.</summary>
    private static void RemoveStandaloneGlazingInLeaf(
        CadEditorViewModel editor,
        FrameNode leaf,
        GlazingBeadPlacementBatch batch)
    {
        for (var i = editor.CanvasElements.Count - 1; i >= 0; i--)
        {
            var element = editor.CanvasElements[i];
            if (element.Kind != GeometryElementKind.Glazing || element.ParentBeadId is not null)
            {
                continue;
            }

            if (!IsGlazingInLeaf(editor, leaf, element))
            {
                continue;
            }

            batch.GlazingIdsToRemove.Add(element.Id);
        }
    }

    private static bool IsGlazingInLeaf(CadEditorViewModel editor, FrameNode leaf, GeometryElement glazing)
    {
        if (glazing.HostSashId is Guid sashId)
        {
            return editor.CanvasElements.Any(e =>
                e.Id == sashId && e.Kind == GeometryElementKind.Sash
                && SashGeometry.TryMeasureLeafSashOverlap(editor, leaf, e, out var overlap)
                && overlap > 100);
        }

        return SashGeometry.TryMeasureLeafGlazingOverlap(editor, leaf, glazing, out var glazingOverlap)
               && glazingOverlap > 100;
    }
}

public sealed class GlazingBeadPlacementBatch
{
    public List<GeometryElement> ElementsToAdd { get; } = [];

    public List<Guid> GlazingIdsToRemove { get; } = [];
}

public sealed record GlazingBeadPlacementOptions(
    Guid? BeadMaterialId,
    Guid? ProfileSeriesId,
    bool IncludeGlass = false);
