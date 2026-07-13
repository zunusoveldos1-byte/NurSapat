using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Services.GlazingBead;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private readonly GlazingBeadTool _glazingBeadTool = new();

    public GlazingBeadPlacementOptions ResolveGlazingBeadPlacementOptions() =>
        new(
            SelectedConstructionMaterialOption?.MaterialId,
            SelectedProfileSeriesItem?.Id);

    public bool TryPlaceGlazingBead(CadPoint worldPoint, bool applyToAllOpenings) =>
        _glazingBeadTool.TryPlace(this, worldPoint, applyToAllOpenings);

    public bool RecordGlazingBeadPlacement(string description, GlazingBeadPlacementBatch batch) =>
        RecordPlacement(description, () =>
        {
            for (var i = CanvasElements.Count - 1; i >= 0; i--)
            {
                if (batch.GlazingIdsToRemove.Contains(CanvasElements[i].Id))
                {
                    CanvasElements.RemoveAt(i);
                }
            }

            foreach (var element in batch.ElementsToAdd)
            {
                AddCanvasElement(element);
            }

            return batch.ElementsToAdd.Count > 0;
        });

    private void SyncGlazingBeadTopology()
    {
        if (FrameRoot is null)
        {
            return;
        }

        foreach (var bead in CanvasElements
                     .Where(e => e.Kind == GeometryElementKind.Bead && e.SourceToolId == "bead")
                     .ToList())
        {
            SyncBeadElementToLayout(bead);
        }

        foreach (var glass in CanvasElements
                     .Where(e => e.Kind == GeometryElementKind.Glazing && e.ParentBeadId is not null)
                     .ToList())
        {
            SyncBeadLinkedGlassToLayout(glass);
        }
    }

    private void SyncBeadElementToLayout(GeometryElement beadElement)
    {
        if (!GlazingBeadBuilder.TryRebuild(this, beadElement, out var beadModel) || beadModel is null)
        {
            return;
        }

        var updatedBead = beadModel.ToGeometryElement();
        beadElement.X = updatedBead.X;
        beadElement.Y = updatedBead.Y;
        beadElement.WidthMm = updatedBead.WidthMm;
        beadElement.HeightMm = updatedBead.HeightMm;
    }

    private void SyncBeadLinkedGlassToLayout(GeometryElement glassElement)
    {
        if (glassElement.ParentBeadId is not Guid beadId)
        {
            return;
        }

        var beadElement = CanvasElements.FirstOrDefault(e => e.Id == beadId && e.Kind == GeometryElementKind.Bead);
        if (beadElement is null
            || !GlazingBeadBuilder.TryRebuild(this, beadElement, out var beadModel)
            || beadModel is null
            || !GlassBuilder.TryBuildFromBead(beadModel, out var glassModel, out _)
            || glassModel is null)
        {
            return;
        }

        glassElement.X = (decimal)glassModel.CenterX;
        glassElement.Y = (decimal)glassModel.CenterY;
        glassElement.WidthMm = (decimal)glassModel.WidthMm;
        glassElement.HeightMm = (decimal)glassModel.HeightMm;
    }

    private void RemoveBeadWithLinkedGlass(Guid beadId)
    {
        for (var i = CanvasElements.Count - 1; i >= 0; i--)
        {
            var element = CanvasElements[i];
            if (element.Id == beadId
                || (element.Kind == GeometryElementKind.Glazing && element.ParentBeadId == beadId))
            {
                CanvasElements.RemoveAt(i);
            }
        }
    }
}
