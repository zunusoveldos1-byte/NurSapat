using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private bool IsArcVertexToolActive =>
        CurrentToolMode is WindowToolMode.ArcVertex or WindowToolMode.RadiusVertex;

    public bool ShowArcVertexHandles =>
        HasArcTopProfile && IsArcVertexToolActive;

    public bool TryPressArcVertex(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.ArcVertex || FrameRoot is null)
        {
            return false;
        }

        if (TryPressArcVertexDrag(worldPoint))
        {
            return true;
        }

        if (!HitTestTopProfileSegment(worldPoint))
        {
            return false;
        }

        FrameRoot.EnsureTopProfile();
        var segment = FrameRoot.TopProfile;
        if (segment is null)
        {
            return false;
        }

        if (segment.SegmentType == SegmentType.Arc)
        {
            StatusText = "Перетащите маркер стрелы или вершины дуги";
            return true;
        }

        if (!RecordPlacement("Добавлена дуговая вершина", () => TryAddArcVertexAtTopProfile()))
        {
            return false;
        }

        RequestSceneRefresh();
        StatusText = $"Дуговая вершина добавлена: R={segment.Radius:F0} мм, H={segment.ArcHeight:F0} мм";
        return true;
    }

    private bool TryAddArcVertexAtTopProfile()
    {
        if (FrameRoot is null)
        {
            return false;
        }

        FrameRoot.EnsureTopProfile();
        var profile = FrameRoot.TopProfile;
        if (profile is null || profile.SegmentType != SegmentType.Line)
        {
            return false;
        }

        var defaultRadius = Math.Max(profile.ChordWidth / 2.0, ProfileSegment.DefaultInitRadiusMm);
        profile.ConvertToArc(defaultRadius);
        AttachTopProfileWatcher();
        NotifyTopProfileUiProperties();
        SyncArchFrameElementAfterProfileChange();
        RecalculateFrameGeometry();
        return true;
    }

    private bool TryRemoveArcVertexFromTool()
    {
        if (FrameRoot?.TopProfile is not { SegmentType: SegmentType.Arc })
        {
            StatusText = "Арочный профиль не найден";
            return false;
        }

        if (!RecordPlacement("Дуговая вершина удалена", TryRemoveTopProfileArc))
        {
            return false;
        }

        RefreshAfterPlacementHistoryChange();
        StatusText = "Верхний профиль возвращён к прямой линии";
        return true;
    }

    private bool TryRemoveTopProfileArc()
    {
        if (FrameRoot?.TopProfile is not { } profile)
        {
            return false;
        }

        profile.SyncTopRailGeometry(FrameRoot.X, FrameRoot.Y, FrameRoot.Width, FrameRoot.Height);
        profile.ConvertToLine();
        NotifyTopProfileUiProperties();
        SyncArchFrameElementAfterProfileChange();
        RecalculateFrameGeometry();
        return true;
    }

    private bool TrySplitArcAtPointFromTool(CadPoint worldPoint)
    {
        if (FrameRoot is null || FrameRoot.TopProfile?.SegmentType != SegmentType.Arc)
        {
            StatusText = "Разбиение доступно только для арочного профиля";
            return false;
        }

        if (!HitTestTopProfileSegment(worldPoint))
        {
            return false;
        }

        var leaf = FrameRoot.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (leaf is null || !leaf.IsLeaf)
        {
            StatusText = "Кликните внутри ячейки под аркой";
            return false;
        }

        var splitLocalX = worldPoint.X - leaf.X;
        if (splitLocalX <= MinEditableOpeningMm
            || splitLocalX >= leaf.Width - MinEditableOpeningMm)
        {
            StatusText = $"Точка разделения должна оставлять проём не менее {MinEditableOpeningMm:0} мм";
            return false;
        }

        var impostWidth = ResolveImpostWidthForSplit();
        if (!RecordPlacement(
                "Арка разделена",
                () =>
                {
                    if (!leaf.TrySplitVerticalAt(splitLocalX, ImpostLineStyle.Solid, impostWidth))
                    {
                        return false;
                    }

                    SyncFillingsToFrameLayout();
                    return true;
                }))
        {
            StatusText = "Невозможно разделить арку в выбранной точке";
            return false;
        }

        RefreshAfterPlacementHistoryChange();
        TrackImpostSplit(leaf);
        StatusText = "Арочная конструкция разделена вертикальным импостом";
        return true;
    }

    private double ResolveImpostWidthForSplit() =>
        FrameRoot?.GetResolvedImpostThicknessMm() > CadMath.Epsilon
            ? FrameRoot.GetResolvedImpostThicknessMm()
            : SingleMullionPlacementWidthMm;

    private bool TrySetArcRadiusFromTool()
    {
        if (FrameRoot?.TopProfile is not { SegmentType: SegmentType.Arc } profile)
        {
            StatusText = "Выберите конструкцию с арочным профилем";
            return false;
        }

        _ = SetArcRadiusAsync(profile.Radius);
        return true;
    }

    private async Task SetArcRadiusAsync(double currentRadius)
    {
        try
        {
            var radius = await _dialogService.ShowRadiusInputAsync(currentRadius);
            if (radius is null)
            {
                StatusText = "Задание радиуса отменено";
                return;
            }

            if (!RecordPlacement("Радиус арки изменён", () =>
                {
                    if (!TryApplyTopProfileRadius(radius.Value))
                    {
                        return false;
                    }

                    SyncArchFrameElementAfterProfileChange();
                    RecalculateFrameGeometry();
                    return true;
                }))
            {
                StatusText = "Невозможно применить радиус";
                return;
            }

            NotifyTopProfileUiProperties();
            SyncArchFrameElementAfterProfileChange();
            RefreshAfterPlacementHistoryChange();
            StatusText = $"Радиус арки: {radius.Value:F0} мм";
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("SetArcRadiusAsync failed", ex);
            StatusText = "Не удалось задать радиус";
        }
    }

    private void SyncArchFrameElementAfterProfileChange()
    {
        if (FrameRoot is null)
        {
            return;
        }

        var archElement = CanvasElements.FirstOrDefault(element =>
            element.Kind is GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame);
        if (archElement is null && FrameRoot.TopProfile?.SegmentType != SegmentType.Arc)
        {
            return;
        }

        if (archElement is not null && FrameRoot.TopProfile is { SegmentType: SegmentType.Arc } profile)
        {
            var totalHeight = ArchFrameDrawingHelper.GetTotalHeight(FrameRoot.Height, profile);
            archElement.HeightMm = (decimal)totalHeight;
        }

        SyncFrameGeometryToDocument();
    }
}
