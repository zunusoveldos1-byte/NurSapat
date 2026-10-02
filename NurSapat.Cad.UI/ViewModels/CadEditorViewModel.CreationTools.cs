using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.Services;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private bool IsSashVariantToolActive =>
        CurrentToolMode is WindowToolMode.Sash or WindowToolMode.SlidingSash
        && CadCreationToolCatalog.IsSashVariantTool(_activeToolKey);

    public bool TryPlaceCreationToolClick(CadPoint worldPoint)
    {
        if (CurrentToolMode == WindowToolMode.BeadSystem)
        {
            return TryPlaceGlazingBead(worldPoint, applyToAllOpenings: false);
        }

        if (CurrentToolMode == WindowToolMode.AdditionalElement)
        {
            return TryPlaceAdditionalElement(worldPoint);
        }

        if (CurrentToolMode == WindowToolMode.Cornice)
        {
            return TryPlaceCornice(worldPoint);
        }

        if (CurrentToolMode is WindowToolMode.BlindVertical or WindowToolMode.BlindHorizontal)
        {
            return TryPlaceBlind(worldPoint);
        }

        if (CurrentToolMode == WindowToolMode.BlindControl)
        {
            return TryPlaceBlindControl(worldPoint);
        }

        if (CadCreationToolCatalog.ResolveHardwareToolId(CurrentToolMode) is { } hardwareToolId)
        {
            return TryPlaceSashHardwareAccessory(worldPoint, hardwareToolId);
        }

        if (IsSashVariantToolActive && _activeToolKey == CadCreationToolCatalog.SashFixed)
        {
            return TryPlaceFixedSash(worldPoint);
        }

        return false;
    }

    private bool TryPlaceFixedSash(CadPoint worldPoint)
    {
        if (FrameRoot is null || !HasRealFrameOnCanvas)
        {
            StatusText = "Сначала создайте коробку (рама)";
            return false;
        }

        var leaf = FrameRoot.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (leaf is null || !leaf.IsLeaf || !IsBillableConstructionLeaf(leaf))
        {
            StatusText = "Кликните внутри свободной ячейки";
            return false;
        }

        if (FindSashInLeaf(leaf) is not null)
        {
            StatusText = "Ячейка уже содержит створку";
            return false;
        }

        var sash = CreateSashElementForTool(leaf, SashHingeSide.Left, 0);
        if (sash is null)
        {
            StatusText = "Не удалось создать створку";
            return false;
        }

        if (!RecordPlacement("Створка глухая создана", () =>
            {
                AddCanvasElement(sash);
                return true;
            }))
        {
            return false;
        }

        StatusText = "Створка глухая создана";
        RequestSceneRefresh();
        return true;
    }

    private GeometryElement? CreateSashElementForTool(FrameNode leaf, SashHingeSide hingeSide, double dragDeltaX)
    {
        var normalizedHinge = CadCreationToolCatalog.NormalizeHingeForTool(_activeToolKey, hingeSide);
        var openingType = CadCreationToolCatalog.ResolveOpeningType(_activeToolKey, normalizedHinge, dragDeltaX);
        var kind = CadCreationToolCatalog.ResolveSashKind(_activeToolKey);

        var (innerX, innerY, innerW, innerH) = ResolveSashOpeningBounds(leaf);
        if (innerW <= 40 || innerH <= 40)
        {
            return null;
        }

        var enableHardware = CadCreationToolCatalog.ShouldEnableHardware(_activeToolKey, openingType);
        var defaultHandleSide = CadHostedElementHelper.ResolveDefaultHandleSide(normalizedHinge, openingType);

        return new GeometryElement
        {
            Kind = kind,
            Label = CadCreationToolCatalog.ResolveSashLabel(_activeToolKey),
            SourceToolId = _activeToolKey,
            X = (decimal)(innerX + innerW / 2.0),
            Y = (decimal)(innerY + innerH / 2.0),
            WidthMm = (decimal)innerW,
            HeightMm = (decimal)innerH,
            HingeSide = normalizedHinge,
            OpeningType = openingType,
            HasHardware = enableHardware,
            HasHandle = enableHardware && openingType != SashOpeningType.Folding,
            ProfileSeriesId = SelectedProfileSeriesItem?.Id,
            MaterialId = SelectedConstructionMaterialOption?.MaterialId
                ?? CadHostedElementHelper.ResolveDefaultMaterialId(kind)
        };
    }

    private void FinalizeSashPlacement(GeometryElement sash)
    {
        if (sash.HasHandle)
        {
            var handleSide = CadHostedElementHelper.ResolveDefaultHandleSide(
                sash.HingeSide ?? SashHingeSide.Left,
                sash.OpeningType ?? SashOpeningType.TiltTurn);
            var handle = CreateHandleElement(sash, handleSide);
            AddCanvasElement(handle);
        }
    }

    private static GeometryElement CreateHandleElement(GeometryElement sash, SashHingeSide handleSide) =>
        new()
        {
            Kind = GeometryElementKind.Handle,
            Label = "Ручка",
            SourceToolId = "handle",
            HostSashId = sash.Id,
            HingeSide = handleSide,
            X = sash.X,
            Y = sash.Y,
            WidthMm = sash.WidthMm,
            HeightMm = sash.HeightMm,
            MaterialId = CadHostedElementHelper.ResolveDefaultMaterialId(GeometryElementKind.Handle)
        };

    public bool TryCycleHandleOnSash(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.Handle)
        {
            return false;
        }

        var sash = FindSashAt(worldPoint);
        if (sash is null)
        {
            StatusText = "Кликните по створке";
            return false;
        }

        var existing = CadHostedElementHelper.FindHosted(CanvasElements, GeometryElementKind.Handle, sash.Id);
        var cycle = new[] { SashHingeSide.Right, SashHingeSide.Left, SashHingeSide.Top, SashHingeSide.Bottom };

        RecordPlacement("Ручка", () =>
        {
            if (existing is null)
            {
                var side = CadHostedElementHelper.ResolveDefaultHandleSide(
                    sash.HingeSide ?? SashHingeSide.Left,
                    sash.OpeningType ?? SashOpeningType.TiltTurn);
                var handle = CreateHandleElement(sash, side);
                AddCanvasElement(handle);
                sash.HasHandle = true;
                StatusText = $"Ручка: {DescribeHandleSide(side)}";
                return true;
            }

            var currentIndex = Array.IndexOf(cycle, existing.HingeSide ?? SashHingeSide.Right);
            var nextIndex = currentIndex + 1;
            if (nextIndex >= cycle.Length)
            {
                CadHostedElementHelper.RemoveHosted(CanvasElements, sash.Id, GeometryElementKind.Handle);
                sash.HasHandle = false;
                StatusText = "Ручка снята";
                return true;
            }

            var nextSide = cycle[nextIndex];
            existing.HingeSide = nextSide;
            sash.HasHandle = true;
            StatusText = $"Ручка: {DescribeHandleSide(nextSide)}";
            return true;
        });

        RequestSceneRefresh();
        return true;
    }

    private static string DescribeHandleSide(SashHingeSide side) => side switch
    {
        SashHingeSide.Left => "слева",
        SashHingeSide.Right => "справа",
        SashHingeSide.Top => "сверху",
        SashHingeSide.Bottom => "снизу",
        _ => "на створке"
    };

    private bool TryPlaceSashHardwareAccessory(CadPoint worldPoint, string toolId)
    {
        var sash = FindSashAt(worldPoint);
        if (sash is null)
        {
            StatusText = "Кликните по створке";
            return false;
        }

        if (sash.OpeningType == SashOpeningType.Fixed && toolId != CadCreationToolCatalog.Espagnolette)
        {
            StatusText = "На глухой створке доступен только шпингалет";
            return false;
        }

        var existing = CadHostedElementHelper.FindHosted(
            CanvasElements,
            GeometryElementKind.Hardware,
            sash.Id,
            toolId);

        if (existing is not null)
        {
            if (!RecordPlacement($"{CadCreationToolCatalog.ResolveHardwareLabel(toolId)} снят", () =>
                {
                    CadHostedElementHelper.RemoveHosted(
                        CanvasElements,
                        sash.Id,
                        GeometryElementKind.Hardware,
                        toolId);
                    return true;
                }))
            {
                return false;
            }

            StatusText = $"{CadCreationToolCatalog.ResolveHardwareLabel(toolId)} снят";
            RequestSceneRefresh();
            return true;
        }

        var hardware = CreateSashHardwareElement(sash, toolId);
        if (!RecordPlacement($"{CadCreationToolCatalog.ResolveHardwareLabel(toolId)} установлен", () =>
            {
                AddCanvasElement(hardware);
                return true;
            }))
        {
            return false;
        }

        StatusText = $"{CadCreationToolCatalog.ResolveHardwareLabel(toolId)} установлен";
        RequestSceneRefresh();
        return true;
    }

    private static GeometryElement CreateSashHardwareElement(GeometryElement sash, string toolId) =>
        new()
        {
            Kind = GeometryElementKind.Hardware,
            Label = CadCreationToolCatalog.ResolveHardwareLabel(toolId),
            SourceToolId = toolId,
            HostSashId = sash.Id,
            HingeSide = sash.HingeSide,
            X = sash.X,
            Y = sash.Y,
            WidthMm = sash.WidthMm,
            HeightMm = sash.HeightMm,
            MaterialId = CadHostedElementHelper.ResolveDefaultMaterialId(GeometryElementKind.Hardware)
        };

    private bool TryPlaceBeadOnGlazing(CadPoint worldPoint)
    {
        var glazing = ConstructionSelectionHelper.FindGlazingAtPoint(this, worldPoint)
            ?? CanvasElements.FirstOrDefault(element =>
                element.Kind == GeometryElementKind.Glazing && ContainsElement(element, worldPoint));

        if (glazing is null)
        {
            StatusText = "Кликните по стеклопакету или заполнению";
            return false;
        }

        if (CanvasElements.Any(element =>
                element.Kind == GeometryElementKind.Bead && element.ParentGlazingId == glazing.Id))
        {
            if (!RecordPlacement("Штапик снят", () =>
                {
                    RemoveBeadForGlazing(glazing.Id);
                    return true;
                }))
            {
                return false;
            }

            StatusText = "Штапик снят";
            RequestSceneRefresh();
            return true;
        }

        var bead = CreateBeadForGlazing(glazing);
        if (bead is null)
        {
            StatusText = "Не удалось построить штапик";
            return false;
        }

        if (!RecordPlacement("Штапик установлен", () =>
            {
                AddCanvasElement(bead);
                return true;
            }))
        {
            return false;
        }

        StatusText = "Штапик установлен";
        RequestSceneRefresh();
        return true;
    }

    private GeometryElement? CreateBeadForGlazing(GeometryElement glazing)
    {
        if (!SashGeometry.TryResolveGlassForGlazingElement(this, glazing, CanvasElements, out var glass))
        {
            return null;
        }

        return new GeometryElement
        {
            Kind = GeometryElementKind.Bead,
            Label = "Штапик",
            SourceToolId = CadCreationToolCatalog.Bead,
            ParentGlazingId = glazing.Id,
            HostSashId = glazing.HostSashId,
            X = glazing.X,
            Y = glazing.Y,
            WidthMm = glazing.WidthMm,
            HeightMm = glazing.HeightMm,
            MaterialId = SelectedConstructionMaterialOption?.MaterialId
                ?? CadHostedElementHelper.ResolveDefaultMaterialId(GeometryElementKind.Bead)
        };
    }

    private void RemoveBeadForGlazing(Guid glazingId)
    {
        for (var i = CanvasElements.Count - 1; i >= 0; i--)
        {
            var element = CanvasElements[i];
            if (element.Kind == GeometryElementKind.Bead && element.ParentGlazingId == glazingId)
            {
                CanvasElements.RemoveAt(i);
            }
        }
    }

    private bool TryPlaceAdditionalElement(CadPoint worldPoint)
    {
        if (FrameRoot is null || !HasRealFrameOnCanvas)
        {
            StatusText = "Сначала создайте коробку (рама)";
            return false;
        }

        var leaf = FrameRoot.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (leaf is null || !leaf.IsLeaf || !IsBillableConstructionLeaf(leaf))
        {
            StatusText = "Кликните внутри проёма";
            return false;
        }

        var (innerX, innerY, innerW, innerH) = SashGeometry.ResolveLeafInnerOpening(
            leaf,
            FrameRoot,
            FrameThickness);
        if (innerW <= 40 || innerH <= 40)
        {
            StatusText = "Проём слишком мал";
            return false;
        }

        var element = new GeometryElement
        {
            Kind = GeometryElementKind.Installation,
            Label = CadCreationToolCatalog.ResolveAccessoryLabel(CadCreationToolCatalog.AdditionalElements),
            SourceToolId = CadCreationToolCatalog.AdditionalElements,
            X = (decimal)(innerX + innerW / 2.0),
            Y = (decimal)(innerY + innerH / 2.0),
            WidthMm = (decimal)innerW,
            HeightMm = (decimal)innerH,
            MaterialId = SelectedConstructionMaterialOption?.MaterialId
                ?? CadHostedElementHelper.ResolveDefaultMaterialId(GeometryElementKind.Installation)
        };

        if (!RecordPlacement("Монтажный элемент установлен", () =>
            {
                AddCanvasElement(element);
                return true;
            }))
        {
            return false;
        }

        StatusText = "Монтажный элемент установлен";
        RequestSceneRefresh();
        return true;
    }

    private bool TryPlaceCornice(CadPoint worldPoint)
    {
        if (FrameRoot is null)
        {
            StatusText = "Сначала создайте коробку";
            return false;
        }

        var frameElement = CanvasElements.FirstOrDefault(element =>
            element.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame);
        if (frameElement is null)
        {
            StatusText = "Коробка не найдена";
            return false;
        }

        var topY = ResolveFrameTopWorldY();
        if (Math.Abs(worldPoint.Y - topY) > Math.Max(80, FrameRoot.Width * 0.05))
        {
            StatusText = "Кликните по верхней стороне конструкции";
            return false;
        }

        var element = new GeometryElement
        {
            Kind = GeometryElementKind.Accessory,
            Label = CadCreationToolCatalog.ResolveAccessoryLabel(CadCreationToolCatalog.Cornice),
            SourceToolId = CadCreationToolCatalog.Cornice,
            X = frameElement.X,
            Y = (decimal)(topY + 25),
            WidthMm = frameElement.WidthMm,
            HeightMm = 40,
            MaterialId = SelectedConstructionMaterialOption?.MaterialId
                ?? CadHostedElementHelper.ResolveDefaultMaterialId(GeometryElementKind.Accessory)
        };

        if (!RecordPlacement("Карниз установлен", () =>
            {
                RemoveCorniceElements();
                AddCanvasElement(element);
                return true;
            }))
        {
            return false;
        }

        StatusText = "Карниз установлен";
        RequestSceneRefresh();
        return true;
    }

    private void RemoveCorniceElements()
    {
        for (var i = CanvasElements.Count - 1; i >= 0; i--)
        {
            if (CanvasElements[i].SourceToolId == CadCreationToolCatalog.Cornice)
            {
                CanvasElements.RemoveAt(i);
            }
        }
    }

    private double ResolveFrameTopWorldY()
    {
        if (FrameRoot?.TopProfile?.SegmentType == SegmentType.Arc && FrameRoot.TopProfile is { } profile)
        {
            return FrameRoot.Y + FrameRoot.Height + profile.ArcHeight;
        }

        return FrameRoot?.Y + FrameRoot?.Height ?? 0;
    }

    private bool TryPlaceBlind(CadPoint worldPoint)
    {
        if (FrameRoot is null || !HasRealFrameOnCanvas)
        {
            StatusText = "Сначала создайте коробку (рама)";
            return false;
        }

        var leaf = FrameRoot.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (leaf is null || !leaf.IsLeaf)
        {
            StatusText = "Кликните внутри проёма";
            return false;
        }

        var toolId = CadCreationToolCatalog.ResolveBlindToolId(CurrentToolMode)
            ?? CadCreationToolCatalog.BlindVertical;
        var existing = FindBlindInLeaf(leaf, toolId);
        if (existing is not null)
        {
            if (!RecordPlacement("Жалюзи сняты", () =>
                {
                    CanvasElements.Remove(existing);
                    return true;
                }))
            {
                return false;
            }

            StatusText = "Жалюзи сняты";
            RequestSceneRefresh();
            return true;
        }

        var (innerX, innerY, innerW, innerH) = SashGeometry.ResolveLeafInnerOpening(
            leaf,
            FrameRoot,
            FrameThickness);
        if (innerW <= 40 || innerH <= 40)
        {
            StatusText = "Проём слишком мал";
            return false;
        }

        var sash = FindSashInLeaf(leaf);
        var element = new GeometryElement
        {
            Kind = GeometryElementKind.Accessory,
            Label = CadCreationToolCatalog.ResolveAccessoryLabel(toolId),
            SourceToolId = toolId,
            HostSashId = sash?.Id,
            X = (decimal)(innerX + innerW / 2.0),
            Y = (decimal)(innerY + innerH / 2.0),
            WidthMm = (decimal)innerW,
            HeightMm = (decimal)innerH,
            MaterialId = SelectedConstructionMaterialOption?.MaterialId
                ?? CadHostedElementHelper.ResolveDefaultMaterialId(GeometryElementKind.Accessory)
        };

        if (!RecordPlacement("Жалюзи установлены", () =>
            {
                AddCanvasElement(element);
                return true;
            }))
        {
            return false;
        }

        StatusText = "Жалюзи установлены";
        RequestSceneRefresh();
        return true;
    }

    private bool TryPlaceBlindControl(CadPoint worldPoint)
    {
        var blind = CanvasElements.FirstOrDefault(element =>
            (element.SourceToolId == CadCreationToolCatalog.BlindVertical
             || element.SourceToolId == CadCreationToolCatalog.BlindHorizontal)
            && ContainsElement(element, worldPoint));

        if (blind is null)
        {
            StatusText = "Кликните по установленным жалюзи";
            return false;
        }

        var existing = CanvasElements.FirstOrDefault(element =>
            element.SourceToolId == CadCreationToolCatalog.BlindControl
            && element.HostSashId == blind.HostSashId);

        if (existing is not null)
        {
            if (!RecordPlacement("Блок управления снят", () =>
                {
                    CanvasElements.Remove(existing);
                    return true;
                }))
            {
                return false;
            }

            StatusText = "Блок управления снят";
            RequestSceneRefresh();
            return true;
        }

        var element = new GeometryElement
        {
            Kind = GeometryElementKind.Accessory,
            Label = CadCreationToolCatalog.ResolveAccessoryLabel(CadCreationToolCatalog.BlindControl),
            SourceToolId = CadCreationToolCatalog.BlindControl,
            HostSashId = blind.HostSashId,
            X = blind.X,
            Y = (decimal)((double)blind.Y - (double)blind.HeightMm / 2.0 + 30),
            WidthMm = 60,
            HeightMm = 40,
            MaterialId = CadHostedElementHelper.ResolveDefaultMaterialId(GeometryElementKind.Accessory)
        };

        if (!RecordPlacement("Блок управления установлен", () =>
            {
                AddCanvasElement(element);
                return true;
            }))
        {
            return false;
        }

        StatusText = "Блок управления установлен";
        RequestSceneRefresh();
        return true;
    }

    private GeometryElement? FindBlindInLeaf(FrameNode leaf, string toolId)
    {
        foreach (var element in CanvasElements)
        {
            if (element.SourceToolId != toolId)
            {
                continue;
            }

            if (ContainsLeaf(element, leaf))
            {
                return element;
            }
        }

        return null;
    }

    private void SyncHostedElementsForSash(GeometryElement sash)
    {
        foreach (var hosted in CadHostedElementHelper.FindAllHosted(CanvasElements, sash.Id))
        {
            hosted.X = sash.X;
            hosted.Y = sash.Y;
            hosted.WidthMm = sash.WidthMm;
            hosted.HeightMm = sash.HeightMm;
            hosted.HingeSide = hosted.Kind == GeometryElementKind.Handle
                ? hosted.HingeSide
                : sash.HingeSide;
        }
    }

    private void PurgeOrphanedHostedElements() =>
        CadHostedElementHelper.PurgeOrphanedHostedElements(CanvasElements, FrameRoot);
}
