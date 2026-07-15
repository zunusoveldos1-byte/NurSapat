using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Services;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private bool TryShowObjectPropertiesFromTool()
    {
        if (_constructionSelection.Count != 1
            && _selectedMullion is null
            && _selectedExtremeImpost is null
            && SelectedElement is null)
        {
            StatusText = "Выберите один объект для свойств";
            return false;
        }

        if (_constructionSelection.Count > 1)
        {
            StatusText = "Свойства доступны только для одного выбранного объекта";
            return false;
        }

        _ = ShowObjectPropertiesAsync();
        return true;
    }

    private bool TryShowConstructionDetailsFromTool()
    {
        if (CanvasElements.Count == 0)
        {
            StatusText = "Нет элементов для просмотра";
            return false;
        }

        _ = ShowConstructionDetailsAsync();
        return true;
    }

    private async Task ShowConstructionDetailsAsync()
    {
        try
        {
            var summary = CalculateConstructionVolume();
            await _dialogService.ShowConstructionDetailsDialogAsync(summary, CanvasElements.ToList());
            StatusText = "Перечень элементов";
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("ShowConstructionDetailsAsync failed", ex);
            StatusText = "Не удалось открыть перечень элементов";
        }
    }

    private async Task ShowObjectPropertiesAsync()
    {
        try
        {
            var model = BuildObjectPropertiesModel();
            if (model is null)
            {
                StatusText = "Не удалось определить выбранный объект";
                return;
            }

            var result = await _dialogService.ShowObjectPropertiesDialogAsync(model);
            if (result is null)
            {
                StatusText = "Изменение свойств отменено";
                return;
            }

            var applied = RecordPlacement(
                "Свойства объекта изменены",
                () => ApplyObjectProperties(model.SelectableKind, result));

            if (!applied)
            {
                RefreshAfterPlacementHistoryChange();
                StatusText = "Невозможно применить свойства без нарушения геометрии";
                return;
            }

            SyncFrameGeometryToDocument();
            RefreshAfterPlacementHistoryChange();
            SyncLegacySelectionFromConstruction();
            StatusText = "Свойства объекта применены";
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("ShowObjectPropertiesAsync failed", ex);
            StatusText = "Не удалось открыть свойства объекта";
        }
    }

    private ObjectPropertiesDialogModel? BuildObjectPropertiesModel()
    {
        var seriesOptions = ProfileSeriesCatalog.All
            .Select(series => new NamedGuidOption(series.Id, series.Name))
            .Prepend(new NamedGuidOption(null, "—"))
            .ToList();

        if (_constructionSelection.Count == 1)
        {
            var item = _constructionSelection.First();
            return item.Kind switch
            {
                CadSelectableKind.Frame => BuildFramePropertiesModel(seriesOptions),
                CadSelectableKind.Impost => BuildImpostPropertiesModel(item, seriesOptions),
                CadSelectableKind.ExtremeImpost => BuildExtremeImpostPropertiesModel(item, seriesOptions),
                CadSelectableKind.Sash => BuildElementPropertiesModel(item, seriesOptions, showSash: true),
                CadSelectableKind.Glazing => BuildElementPropertiesModel(item, seriesOptions, showGlazing: true),
                CadSelectableKind.Bead or CadSelectableKind.SashHandle or CadSelectableKind.SashHinge =>
                    BuildElementPropertiesModel(item, seriesOptions),
                CadSelectableKind.Sill => BuildSillPropertiesModel(seriesOptions),
                _ => null
            };
        }

        if (_selectedMullion is not null)
        {
            return BuildImpostPropertiesModel(
                CadSelectableItem.FromImpostTarget(_selectedMullion),
                seriesOptions);
        }

        if (_selectedExtremeImpost is { } extreme)
        {
            return BuildExtremeImpostPropertiesModel(
                CadSelectableItem.ExtremeImpost(extreme.Leaf, extreme.SlotId),
                seriesOptions);
        }

        if (SelectedElement is FrameNode)
        {
            return BuildFramePropertiesModel(seriesOptions);
        }

        return null;
    }

    private ObjectPropertiesDialogModel BuildFramePropertiesModel(IReadOnlyList<NamedGuidOption> seriesOptions)
    {
        var root = FrameRoot!;
        var primary = CanvasElements.FirstOrDefault(e =>
            e.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame
                or GeometryElementKind.FictitiousFrame or GeometryElementKind.DummyFrame
                or GeometryElementKind.ArchDummyFrame or GeometryElementKind.RoundDummyFrame
                or GeometryElementKind.TriangleDummyFrame);
        var materials = BuildMaterialOptions(primary?.Kind ?? GeometryElementKind.Frame);

        return new ObjectPropertiesDialogModel(
            ObjectKind: "Рама / коробка",
            SelectableKind: CadSelectableKind.Frame,
            Label: primary?.Label ?? "Коробка",
            X: root.X,
            Y: root.Y,
            Width: root.Width,
            Height: root.Height,
            SizeSummary: $"Габарит {root.Width:0}×{root.Height:0} мм",
            ShowSizeFields: true,
            ShowPositionFields: true,
            ShowCatalogFields: true,
            ShowSashFields: false,
            ShowGlazingFields: false,
            ShowImpostAxisField: false,
            ShowExtremeFields: false,
            ShowArcField: HasArcTopProfile,
            ShowSillFields: false,
            MaterialId: primary?.MaterialId,
            ProfileSeriesId: primary?.ProfileSeriesId,
            MaterialOptions: materials,
            SeriesOptions: seriesOptions,
            ArcHeightMm: HasArcTopProfile ? root.TopProfile?.ArcHeight : null);
    }

    private ObjectPropertiesDialogModel? BuildImpostPropertiesModel(
        CadSelectableItem item,
        IReadOnlyList<NamedGuidOption> seriesOptions)
    {
        var node = item.ImpostNode ?? _selectedMullion?.SplitNode;
        if (node is null)
        {
            return null;
        }

        var gap = item.Kind == CadSelectableKind.Impost && node.IsUniformSplit
            ? item.ImpostGapIndex
            : _selectedMullion?.UniformGapIndex ?? 0;
        var isVertical = node.SplitOrientation == SplitOrientation.Vertical;
        double axis;
        if (node.IsUniformSplit)
        {
            if (!node.TryGetUniformImpostAxisWorld(
                    isVertical ? SplitOrientation.Vertical : SplitOrientation.Horizontal,
                    gap,
                    out axis))
            {
                return null;
            }
        }
        else if (isVertical)
        {
            if (!node.TryGetVerticalImpostAxisWorldX(out axis))
            {
                return null;
            }
        }
        else if (!node.TryGetHorizontalImpostAxisWorldY(out axis))
        {
            return null;
        }

        return new ObjectPropertiesDialogModel(
            ObjectKind: isVertical ? "Вертикальный импост" : "Горизонтальный импост",
            SelectableKind: CadSelectableKind.Impost,
            Label: "Импост",
            X: node.X,
            Y: node.Y,
            Width: node.Width,
            Height: node.Height,
            SizeSummary: $"Толщина {node.GetResolvedImpostThicknessMm():0} мм; ось {axis:0} мм",
            ShowSizeFields: false,
            ShowPositionFields: false,
            ShowCatalogFields: false,
            ShowSashFields: false,
            ShowGlazingFields: false,
            ShowImpostAxisField: true,
            ShowExtremeFields: false,
            ShowArcField: false,
            ShowSillFields: false,
            MaterialId: null,
            ProfileSeriesId: null,
            MaterialOptions: [new NamedGuidOption(null, "—")],
            SeriesOptions: seriesOptions,
            AxisPosition: axis);
    }

    private ObjectPropertiesDialogModel? BuildExtremeImpostPropertiesModel(
        CadSelectableItem item,
        IReadOnlyList<NamedGuidOption> seriesOptions)
    {
        var leaf = item.ImpostNode;
        var slotId = item.ElementId;
        if (leaf is null || slotId is null)
        {
            return null;
        }

        var slot = leaf.FindExtremeImpost(slotId.Value);
        if (slot is null)
        {
            return null;
        }

        var element = CanvasElements.FirstOrDefault(e =>
            e.Kind == GeometryElementKind.EdgeMullion && e.ExtremeImpostSlotId == slot.Id);
        var materials = BuildMaterialOptions(GeometryElementKind.EdgeMullion);

        return new ObjectPropertiesDialogModel(
            ObjectKind: "Крайний импост",
            SelectableKind: CadSelectableKind.ExtremeImpost,
            Label: element?.Label ?? "Крайний импост",
            X: element is null ? leaf.X : (double)element.X,
            Y: element is null ? leaf.Y : (double)element.Y,
            Width: element is null ? slot.ThicknessMm : (double)element.WidthMm,
            Height: element is null ? leaf.Height : (double)element.HeightMm,
            SizeSummary: $"Толщина {slot.ThicknessMm:0} мм; смещение {slot.OffsetMm:0} мм",
            ShowSizeFields: false,
            ShowPositionFields: false,
            ShowCatalogFields: true,
            ShowSashFields: false,
            ShowGlazingFields: false,
            ShowImpostAxisField: false,
            ShowExtremeFields: true,
            ShowArcField: false,
            ShowSillFields: false,
            MaterialId: element?.MaterialId,
            ProfileSeriesId: element?.ProfileSeriesId,
            MaterialOptions: materials,
            SeriesOptions: seriesOptions,
            ExtremeOffsetMm: slot.OffsetMm,
            EdgeAlignment: slot.EdgeAlignment);
    }

    private ObjectPropertiesDialogModel? BuildElementPropertiesModel(
        CadSelectableItem item,
        IReadOnlyList<NamedGuidOption> seriesOptions,
        bool showSash = false,
        bool showGlazing = false)
    {
        if (item.ElementId is not Guid elementId)
        {
            return null;
        }

        var element = CanvasElements.FirstOrDefault(e => e.Id == elementId);
        if (element is null)
        {
            return null;
        }

        var kindLabel = item.Kind switch
        {
            CadSelectableKind.Sash => "Створка",
            CadSelectableKind.Glazing => "Остекление",
            CadSelectableKind.Bead => "Штапик / фурнитура",
            CadSelectableKind.SashHandle => "Ручка",
            CadSelectableKind.SashHinge => "Петля",
            _ => element.Kind.ToString()
        };

        return new ObjectPropertiesDialogModel(
            ObjectKind: kindLabel,
            SelectableKind: item.Kind,
            Label: element.Label,
            X: (double)element.X,
            Y: (double)element.Y,
            Width: (double)element.WidthMm,
            Height: (double)element.HeightMm,
            SizeSummary: $"Размер {element.WidthMm:0}×{element.HeightMm:0} мм (вычисляемый)",
            ShowSizeFields: false,
            ShowPositionFields: false,
            ShowCatalogFields: true,
            ShowSashFields: showSash,
            ShowGlazingFields: showGlazing,
            ShowImpostAxisField: false,
            ShowExtremeFields: false,
            ShowArcField: false,
            ShowSillFields: false,
            MaterialId: element.MaterialId,
            ProfileSeriesId: element.ProfileSeriesId,
            MaterialOptions: BuildMaterialOptions(element.Kind),
            SeriesOptions: seriesOptions,
            OpeningType: element.OpeningType,
            HingeSide: element.HingeSide,
            HasHardware: element.HasHardware,
            HasHandle: element.HasHandle,
            HasMosquitoNet: element.HasMosquitoNet,
            FillType: element.FillType);
    }

    private ObjectPropertiesDialogModel? BuildSillPropertiesModel(IReadOnlyList<NamedGuidOption> seriesOptions)
    {
        var root = FrameRoot?.GetConstructionRoot();
        var sill = root?.AttachedSill;
        var element = CanvasElements.FirstOrDefault(e => e.Kind == GeometryElementKind.Sill);
        if (sill is null && element is null)
        {
            return null;
        }

        return new ObjectPropertiesDialogModel(
            ObjectKind: "Подоконник",
            SelectableKind: CadSelectableKind.Sill,
            Label: element?.Label ?? "Подоконник",
            X: element is null ? 0 : (double)element.X,
            Y: element is null ? 0 : (double)element.Y,
            Width: sill?.WidthMm ?? (double)(element?.WidthMm ?? 0),
            Height: sill?.ThicknessMm ?? (double)(element?.HeightMm ?? 0),
            SizeSummary: $"Ширина {sill?.WidthMm ?? 0:0} мм; толщина {sill?.ThicknessMm ?? 0:0} мм",
            ShowSizeFields: false,
            ShowPositionFields: false,
            ShowCatalogFields: true,
            ShowSashFields: false,
            ShowGlazingFields: false,
            ShowImpostAxisField: false,
            ShowExtremeFields: false,
            ShowArcField: false,
            ShowSillFields: true,
            MaterialId: sill?.MaterialId ?? element?.MaterialId,
            ProfileSeriesId: element?.ProfileSeriesId,
            MaterialOptions: BuildMaterialOptions(GeometryElementKind.Sill),
            SeriesOptions: seriesOptions,
            SillThicknessMm: sill?.ThicknessMm,
            SillWidthMm: sill?.WidthMm,
            SillLeftOverhangMm: sill?.LeftOverhangMm,
            SillRightOverhangMm: sill?.RightOverhangMm);
    }

    private static IReadOnlyList<NamedGuidOption> BuildMaterialOptions(GeometryElementKind kind) =>
        ConstructionMaterialCatalog.ForKind(kind)
            .OrderBy(material => material.Name)
            .Select(material => new NamedGuidOption(material.Id, material.Name))
            .Prepend(new NamedGuidOption(null, "—"))
            .ToList();

    private bool ApplyObjectProperties(CadSelectableKind kind, ObjectPropertiesDialogResult result)
    {
        return kind switch
        {
            CadSelectableKind.Frame => ApplyFrameProperties(result),
            CadSelectableKind.Impost => ApplyImpostProperties(result),
            CadSelectableKind.ExtremeImpost => ApplyExtremeImpostProperties(result),
            CadSelectableKind.Sash or CadSelectableKind.Glazing or CadSelectableKind.Bead
                or CadSelectableKind.SashHandle or CadSelectableKind.SashHinge =>
                ApplyElementProperties(kind, result),
            CadSelectableKind.Sill => ApplySillProperties(result),
            _ => false
        };
    }

    private bool ApplyFrameProperties(ObjectPropertiesDialogResult result)
    {
        if (FrameRoot is null)
        {
            return false;
        }

        var thickness = FrameRoot.IsFictitiousContainer ? 0 : FrameThickness;
        var sizeChanged = Math.Abs(result.Width - FrameRoot.Width) > CadMath.Epsilon
                          || Math.Abs(result.Height - FrameRoot.Height) > CadMath.Epsilon;
        if (sizeChanged && !FrameRoot.TryResizeOuter(result.Width, result.Height, thickness))
        {
            return false;
        }

        var deltaX = result.X - FrameRoot.X;
        var deltaY = result.Y - FrameRoot.Y;
        if (Math.Abs(deltaX) > CadMath.Epsilon || Math.Abs(deltaY) > CadMath.Epsilon)
        {
            FrameRoot.TranslateSubtree(deltaX, deltaY);
        }

        if (result.ArcHeightMm is double arcHeight && HasArcTopProfile)
        {
            if (!TryApplyTopProfileArcHeight(arcHeight))
            {
                return false;
            }
        }

        var primary = CanvasElements.FirstOrDefault(e =>
            e.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame
                or GeometryElementKind.FictitiousFrame or GeometryElementKind.DummyFrame
                or GeometryElementKind.ArchDummyFrame or GeometryElementKind.RoundDummyFrame
                or GeometryElementKind.TriangleDummyFrame);
        if (primary is not null)
        {
            primary.Label = result.Label;
            primary.MaterialId = result.MaterialId;
            primary.ProfileSeriesId = result.ProfileSeriesId;
        }

        RecalculateFrameGeometry();
        return true;
    }

    private bool ApplyImpostProperties(ObjectPropertiesDialogResult result)
    {
        var target = _selectedMullion
            ?? _constructionSelection
                .Where(item => item.Kind == CadSelectableKind.Impost && item.ImpostNode is not null)
                .Select(item => new ImpostTarget
                {
                    SplitNode = item.ImpostNode!,
                    UniformGapIndex = item.ImpostNode!.IsUniformSplit ? item.ImpostGapIndex : null
                })
                .FirstOrDefault();
        if (target?.SplitNode is null || result.AxisPosition is not double axis)
        {
            return false;
        }

        return target.SplitNode.SplitOrientation == SplitOrientation.Vertical
            ? MoveVerticalImpost(target, axis)
            : MoveHorizontalImpost(target, axis);
    }

    private bool ApplyExtremeImpostProperties(ObjectPropertiesDialogResult result)
    {
        var item = _constructionSelection.FirstOrDefault(i => i.Kind == CadSelectableKind.ExtremeImpost);
        FrameNode? leaf = item?.ImpostNode ?? _selectedExtremeImpost?.Leaf;
        Guid? slotId = item?.ElementId ?? _selectedExtremeImpost?.SlotId;
        if (leaf is null || slotId is null)
        {
            return false;
        }

        var slot = leaf.FindExtremeImpost(slotId.Value);
        if (slot is null)
        {
            return false;
        }

        if (result.ExtremeOffsetMm is double offset
            && !leaf.TrySetExtremeImpostOffset(slotId.Value, offset))
        {
            return false;
        }

        var element = CanvasElements.FirstOrDefault(e =>
            e.Kind == GeometryElementKind.EdgeMullion && e.ExtremeImpostSlotId == slotId);
        if (element is not null)
        {
            element.Label = result.Label;
            element.MaterialId = result.MaterialId;
            element.ProfileSeriesId = result.ProfileSeriesId;
        }

        SyncExtremeImpostCanvasElements();
        SyncFillingsToFrameLayout();
        return true;
    }

    private bool ApplyElementProperties(CadSelectableKind kind, ObjectPropertiesDialogResult result)
    {
        var item = _constructionSelection.FirstOrDefault(i => i.Kind == kind);
        if (item?.ElementId is not Guid elementId)
        {
            return false;
        }

        var element = CanvasElements.FirstOrDefault(e => e.Id == elementId);
        if (element is null)
        {
            return false;
        }

        element.Label = result.Label;
        element.MaterialId = result.MaterialId;
        element.ProfileSeriesId = result.ProfileSeriesId;

        if (kind == CadSelectableKind.Sash)
        {
            if (result.OpeningType is { } opening)
            {
                SashOpeningState.ApplyOpeningTypeChange(
                    element,
                    opening,
                    result.HingeSide);
            }
            else
            {
                element.OpeningType = result.OpeningType;
                element.HingeSide = result.HingeSide;
            }

            element.HasMosquitoNet = result.HasMosquitoNet;

            if (result.HasHardware
                && result.OpeningType is { } openingForHardware
                && openingForHardware != SashOpeningType.Fixed)
            {
                ApplySashOpeningAccessories(
                    element,
                    openingForHardware,
                    element.HingeSide ?? SashHingeSide.Left);
                // Ручка — только по явному флагу диалога, не вместе с петлями.
                if (result.HasHandle)
                {
                    element.HasHandle = true;
                    var handleSide = CadHostedElementHelper.ResolveDefaultHandleSide(
                        element.HingeSide ?? SashHingeSide.Left,
                        openingForHardware);
                    AddCanvasElement(CreateHandleElement(element, handleSide));
                }
            }
            else
            {
                ClearSashOpeningAccessories(element);
                if (result.HasHandle && result.OpeningType is { } handleOpening)
                {
                    element.HasHandle = true;
                    var handleSide = CadHostedElementHelper.ResolveDefaultHandleSide(
                        element.HingeSide ?? SashHingeSide.Left,
                        handleOpening);
                    AddCanvasElement(CreateHandleElement(element, handleSide));
                }
            }
        }

        if (kind == CadSelectableKind.Glazing)
        {
            element.FillType = result.FillType;
        }

        SyncFillingsToFrameLayout();
        RefreshConstructionVolume();
        return true;
    }

    private bool ApplySillProperties(ObjectPropertiesDialogResult result)
    {
        var root = FrameRoot?.GetConstructionRoot();
        if (root is null)
        {
            return false;
        }

        root.AttachedSill ??= new SillElement();
        var sill = root.AttachedSill;
        if (result.SillThicknessMm is double thickness)
        {
            sill.ThicknessMm = thickness;
        }

        if (result.SillWidthMm is double width)
        {
            sill.WidthMm = width;
        }

        if (result.SillLeftOverhangMm is double left)
        {
            sill.LeftOverhangMm = left;
        }

        if (result.SillRightOverhangMm is double right)
        {
            sill.RightOverhangMm = right;
        }

        if (result.MaterialId is Guid materialId)
        {
            sill.MaterialId = materialId;
        }

        var element = CanvasElements.FirstOrDefault(e => e.Kind == GeometryElementKind.Sill);
        if (element is not null)
        {
            element.Label = result.Label;
            element.MaterialId = result.MaterialId;
            element.ProfileSeriesId = result.ProfileSeriesId;
        }

        SyncSillCanvasElement();
        RefreshConstructionVolume();
        return true;
    }
}
