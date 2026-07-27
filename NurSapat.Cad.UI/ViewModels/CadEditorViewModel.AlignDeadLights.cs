using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private bool TryAlignDeadLightsFromTool()
    {
        if (FrameRoot is null)
        {
            StatusText = "Сначала создайте конструкцию";
            return false;
        }

        if (HasArcTopProfile)
        {
            StatusText = "Выравнивание глухих просветов недоступно для арочной конструкции";
            return false;
        }

        var leaves = ResolveSelectedDeadLightLeaves();
        if (leaves.Count < 2)
        {
            StatusText = "Выберите не менее двух глухих просветов";
            return false;
        }

        var measurements = leaves
            .Select(leaf =>
            {
                var (_, _, width, height) = leaf.ResolveNestedOpeningBounds();
                return new LeafSpanMeasurement(leaf, width, height);
            })
            .Where(item => item.MeasuredWidth > CadMath.Epsilon && item.MeasuredHeight > CadMath.Epsilon)
            .ToList();

        if (measurements.Count < 2)
        {
            StatusText = "Не удалось определить размеры выбранных глухих просветов";
            return false;
        }

        if (HaveEqualDimension(measurements.Select(item => item.MeasuredWidth))
            && HaveEqualDimension(measurements.Select(item => item.MeasuredHeight)))
        {
            StatusText = "Выбранные глухие просветы уже имеют одинаковый размер";
            return true;
        }

        var plans = BuildLeafSpanAlignmentPlans(measurements, useStructuralCellSpan: false);
        if (plans.Count == 0)
        {
            StatusText = "Выбранные глухие просветы не разделены общими импостами";
            return false;
        }

        var before = CaptureConstructionMemento();
        var aligned = RecordPlacement(
            "Глухие просветы выровнены",
            () =>
            {
                foreach (var plan in plans)
                {
                    if (!ApplySubdivisionAlignmentPlan(plan))
                    {
                        ApplyConstructionMemento(before);
                        return false;
                    }
                }

                SyncFillingsToFrameLayout();
                var updated = leaves
                    .Select(leaf =>
                    {
                        var (_, _, width, height) = leaf.ResolveNestedOpeningBounds();
                        return new LeafSpanMeasurement(leaf, width, height);
                    })
                    .ToList();
                if (!HaveEqualDimension(updated.Select(item => item.MeasuredWidth))
                    || !HaveEqualDimension(updated.Select(item => item.MeasuredHeight)))
                {
                    ApplyConstructionMemento(before);
                    return false;
                }

                return true;
            });

        if (!aligned)
        {
            RefreshAfterPlacementHistoryChange();
            StatusText = "Невозможно выровнять выбранные глухие просветы без нарушения геометрии";
            return false;
        }

        RefreshAfterPlacementHistoryChange();
        SyncLegacySelectionFromConstruction();
        StatusText = $"Выровнено глухих просветов: {measurements.Count}";
        return true;
    }

    private List<FrameNode> ResolveSelectedDeadLightLeaves()
    {
        var leaves = new HashSet<FrameNode>();

        foreach (var item in _constructionSelection)
        {
            if (item.Kind != CadSelectableKind.Glazing || item.ElementId is not Guid elementId)
            {
                continue;
            }

            var element = CanvasElements.FirstOrDefault(e => e.Id == elementId);
            if (element is null)
            {
                continue;
            }

            FrameNode? leaf = element.Kind switch
            {
                GeometryElementKind.Glazing when element.HostSashId is null =>
                    SashGeometry.FindLeafForGlazing(this, element),
                GeometryElementKind.Corrugation =>
                    FrameRoot?.FindLeafAt((double)element.X, (double)element.Y),
                _ => null
            };

            if (leaf is { IsLeaf: true } && !SashGeometry.HasSashInLeaf(this, leaf))
            {
                leaves.Add(leaf);
            }
        }

        // Standalone corrugation is not a CadSelectableKind; resolve by overlapping selected glazing leaves
        // and also by direct element id match when present in selection.
        foreach (var corrugation in CanvasElements.Where(e => e.Kind == GeometryElementKind.Corrugation))
        {
            if (FrameRoot?.FindLeafAt((double)corrugation.X, (double)corrugation.Y) is not { IsLeaf: true } leaf)
            {
                continue;
            }

            if (SashGeometry.HasSashInLeaf(this, leaf))
            {
                continue;
            }

            if (_constructionSelection.Any(item => item.ElementId == corrugation.Id) || leaves.Contains(leaf))
            {
                leaves.Add(leaf);
            }
        }

        return leaves.ToList();
    }
}
