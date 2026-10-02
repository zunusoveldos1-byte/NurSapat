using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.Services;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private ExtremeImpostHoverPreview? _extremeImpostHoverPreview;
    private (FrameNode Leaf, Guid SlotId)? _selectedExtremeImpost;

    public ExtremeImpostHoverPreview? ExtremeImpostHoverPreview => _extremeImpostHoverPreview;

    public bool IsExtremeImpostToolActive => CurrentToolMode == WindowToolMode.ExtremeImpost;

    public bool IsExtremeImpostEditPanelActive =>
        WorkspaceMode == CadEditorWorkspaceMode.Editing && _selectedExtremeImpost is not null;

    public IReadOnlyList<ExtremeImpostPlacementOption> ExtremeImpostPlacementOptions { get; } =
    [
        new(ExtremeImpostPlacementKind.Post, "Стойка (вертикальная)"),
        new(ExtremeImpostPlacementKind.Transom, "Ригель (горизонтальный)")
    ];

    [ObservableProperty]
    private ExtremeImpostPlacementOption? _selectedExtremeImpostPlacementOption;

    [ObservableProperty]
    private double _extremeImpostOffsetMm;

    private bool _isSyncingExtremeImpostPanel;

    /// <summary>
    /// Параметры размещения крайнего импоста с учётом выбранного материала.
    /// «Умные» накладные профили (нащельник) переключаются в магнитный режим
    /// <see cref="ExtremeImpostPlacementKind.Auto"/>: ориентация определяется
    /// автоматически, а тонкая толщина ставит фантом заподлицо с осью.
    /// </summary>
    private (ExtremeImpostPlacementKind Placement, double Thickness, double LightOpeningInsetMm) ResolveExtremeImpostPlacement()
    {
        var material = SelectedConstructionMaterialOption?.Material;

        if (material is { SnapsToConstructionAxes: true })
        {
            var thickness = material.DrawThicknessMm is double draw && draw > CadMath.Epsilon
                ? draw
                : GetImpostProfileWidth(WindowToolMode.RealVerticalImpost);
            var inset = material.LightOpeningInsetMm is double lightInset && lightInset > CadMath.Epsilon
                ? lightInset
                : thickness;

            return (ExtremeImpostPlacementKind.Auto, thickness, inset);
        }

        var manualPlacement = SelectedExtremeImpostPlacementOption?.Kind ?? ExtremeImpostPlacementKind.Post;
        return (manualPlacement, GetImpostProfileWidth(WindowToolMode.RealVerticalImpost), 0);
    }

    public void UpdateExtremeImpostHover(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.ExtremeImpost || FrameRoot is null)
        {
            ClearExtremeImpostHover();
            return;
        }

        var (placement, thickness, lightOpeningInset) = ResolveExtremeImpostPlacement();
        if (!ExtremeImpostGeometry.TryHitTestHover(
                FrameRoot,
                FrameThickness,
                worldPoint,
                placement,
                thickness,
                out var preview))
        {
            ClearExtremeImpostHover();
            return;
        }

        if (_extremeImpostHoverPreview is { } current
            && current.Leaf == preview.Leaf
            && current.Edge == preview.Edge
            && Math.Abs(current.AxisWorld - preview.AxisWorld) < CadMath.MmPrecision
            && Math.Abs(current.SpanStart - preview.SpanStart) < CadMath.MmPrecision
            && Math.Abs(current.SpanEnd - preview.SpanEnd) < CadMath.MmPrecision)
        {
            return;
        }

        _extremeImpostHoverPreview = preview;
        RequestSceneRefresh();
    }

    public void ClearExtremeImpostHover()
    {
        if (_extremeImpostHoverPreview is null)
        {
            return;
        }

        _extremeImpostHoverPreview = null;
        RequestSceneRefresh();
    }

    public bool TryHandleExtremeImpostClick(CadPoint worldPoint)
    {
        if (WorkspaceMode == CadEditorWorkspaceMode.Editing)
        {
            return TrySelectExtremeImpostAt(worldPoint);
        }

        if (WorkspaceMode != CadEditorWorkspaceMode.Creation
            || CurrentToolMode != WindowToolMode.ExtremeImpost
            || FrameRoot is null)
        {
            return false;
        }

        var (placement, thickness, lightOpeningInset) = ResolveExtremeImpostPlacement();
        if (!ExtremeImpostGeometry.TryHitTestHover(
                FrameRoot,
                FrameThickness,
                worldPoint,
                placement,
                thickness,
                out var preview))
        {
            StatusText = "Наведите курсор на секцию конструкции";
            return false;
        }

        var orientation = preview.Orientation;
        return RecordPlacement(
            "Крайний импост установлен",
            () =>
            {
                if (!preview.Leaf.TryUpsertExtremeImpost(
                        orientation,
                        preview.Edge,
                        thickness,
                        spanStartMm: preview.SpanStart,
                        spanEndMm: preview.SpanEnd,
                        lightOpeningInsetMm: lightOpeningInset))
                {
                    return false;
                }

                SyncExtremeImpostCanvasElements();
                SyncFillingsToFrameLayout();
                return true;
            });
    }

    private bool TrySelectExtremeImpostAt(CadPoint worldPoint)
    {
        if (FrameRoot is null)
        {
            return false;
        }

        foreach (var (leaf, slot) in FrameRoot.EnumerateExtremeImposts())
        {
            if (!TryHitExtremeImpost(leaf, slot, worldPoint))
            {
                continue;
            }

            _selectedExtremeImpost = (leaf, slot.Id);
            _isSyncingExtremeImpostPanel = true;
            ExtremeImpostOffsetMm = slot.OffsetMm;
            _isSyncingExtremeImpostPanel = false;
            _constructionSelection.Clear();
            _constructionSelection.Add(CadSelectableItem.ExtremeImpost(leaf, slot.Id));
            OnPropertyChanged(nameof(ConstructionSelection));
            OnPropertyChanged(nameof(IsExtremeImpostEditPanelActive));
            StatusText = $"Крайний импост: отступ {slot.OffsetMm:F0} мм";
            RequestSceneRefresh();
            return true;
        }

        return false;
    }

    private bool TryHitExtremeImpost(FrameNode leaf, ExtremeImpostSlot slot, CadPoint worldPoint) =>
        FrameRoot is not null
        && ExtremeImpostGeometry.ContainsCommittedImpost(FrameRoot, FrameThickness, leaf, slot, worldPoint);

    internal void SyncExtremeImpostSelectionFromConstruction(CadSelectableItem item)
    {
        if (item.Kind != CadSelectableKind.ExtremeImpost
            || item.ImpostNode is not FrameNode leaf
            || item.ElementId is not Guid slotId)
        {
            return;
        }

        var slot = leaf.FindExtremeImpost(slotId);
        if (slot is null)
        {
            return;
        }

        _selectedExtremeImpost = (leaf, slotId);
        _isSyncingExtremeImpostPanel = true;
        ExtremeImpostOffsetMm = slot.OffsetMm;
        _isSyncingExtremeImpostPanel = false;
        OnPropertyChanged(nameof(IsExtremeImpostEditPanelActive));
    }

    partial void OnExtremeImpostOffsetMmChanged(double value)
    {
        if (_isSyncingExtremeImpostPanel || _selectedExtremeImpost is not { } selected)
        {
            return;
        }

        if (value < 0)
        {
            return;
        }

        if (!selected.Leaf.TrySetExtremeImpostOffset(selected.SlotId, value))
        {
            return;
        }

        SyncExtremeImpostCanvasElements();
        SyncFillingsToFrameLayout();
        RequestSceneRefresh();
        StatusText = $"Крайний импост: отступ {value:F0} мм";
    }

    partial void OnSelectedExtremeImpostPlacementOptionChanged(ExtremeImpostPlacementOption? value)
    {
        if (value is null)
        {
            SelectedExtremeImpostPlacementOption = ExtremeImpostPlacementOptions[0];
        }

        ClearExtremeImpostHover();
        OnPropertyChanged(nameof(IsExtremeImpostToolActive));
    }

    internal void SyncExtremeImpostCanvasElements()
    {
        if (FrameRoot is null)
        {
            return;
        }

        var activeSlotIds = new HashSet<Guid>();
        foreach (var (leaf, slot) in FrameRoot.EnumerateExtremeImposts())
        {
            activeSlotIds.Add(slot.Id);
            var element = CanvasElements.FirstOrDefault(e =>
                e.Kind == GeometryElementKind.EdgeMullion && e.ExtremeImpostSlotId == slot.Id);

            if (element is null)
            {
                element = new GeometryElement
                {
                    Kind = GeometryElementKind.EdgeMullion,
                    Label = "Стойка/ригель в крайнем положении",
                    SourceToolId = "mullion-edge",
                    IsExtremeImpost = true,
                    ExtremeImpostSlotId = slot.Id,
                    HostFrameNodeId = leaf.NodeId,
                    MaterialId = SelectedConstructionMaterialOption?.MaterialId
                };
                AddCanvasElement(element);
            }
            else if (SelectedConstructionMaterialOption?.MaterialId is Guid materialId)
            {
                element.MaterialId = materialId;
            }

            UpdateExtremeImpostElementGeometry(leaf, slot, element);
        }

        for (var i = CanvasElements.Count - 1; i >= 0; i--)
        {
            var element = CanvasElements[i];
            if (element.Kind == GeometryElementKind.EdgeMullion
                && element.IsExtremeImpost
                && element.ExtremeImpostSlotId is Guid slotId
                && !activeSlotIds.Contains(slotId))
            {
                CanvasElements.RemoveAt(i);
            }
        }

        RefreshConstructionVolume();
        SyncAllNestedFrameElements();
    }

    private void SyncAllNestedFrameElements()
    {
        // Nested real frames exist only inside a fictitious container; the primary
        // outer frame element must keep tracking FrameRoot after impost splits.
        if (FrameRoot is null || !FrameRoot.IsFictitiousContainer)
        {
            return;
        }

        // После вертикального/горизонтального сплита центр хоста может попасть
        // в щель импоста — ребинд/клонирование по AABB-перекрытию с листьями.
        SyncHostFrameTopologyAfterSplit();

        foreach (var leaf in EnumerateFrameLayoutLeaves(FrameRoot))
        {
            if (FindFrameInLeaf(leaf) is not null)
            {
                SyncNestedElementsInLeaf(leaf);
            }
        }
    }

    /// <summary>
    /// Если рама/переходной профиль после деления импостом внутри своей ячейки
    /// перекрывает несколько дочерних листьев — клонируем только внутри поддерева HostFrameNodeId.
    /// TransitionFrame исключён: нащельник на стыке должен оставаться единым сквозным элементом.
    /// </summary>
    // === CRITICAL GUARDRAIL: DO NOT TOUCH THIS SECTION ===
    // Этот блок отвечает за генерацию ЕДИНОГО сквозного нащельника на стыке секций.
    // Любое изменение логики фильтрации или ребинда хостов ломает отрисовку линии стыка!
    // ======================================================
    private void SyncHostFrameTopologyAfterSplit()
    {
        if (FrameRoot is null || !FrameRoot.IsFictitiousContainer)
        {
            return;
        }

        foreach (var host in CanvasElements
                     .Where(e => ConstructionHostResolver.IsHostFrameElement(e)
                                 && e.Kind != GeometryElementKind.TransitionFrame)
                     .ToList())
        {
            if (host.HostFrameNodeId is not Guid boundId)
            {
                continue;
            }

            var boundNode = FrameRoot.FindNodeById(boundId);
            if (boundNode is null)
            {
                continue;
            }

            // Хост, привязанный к корневому контейнеру, нельзя клонировать во все листья.
            if (boundId == FrameRoot.NodeId)
            {
                var targetLeaf = SashGeometry.ResolveSashPlacementLeafAtPoint(this, (double)host.X, (double)host.Y)
                    ?? EnumerateFrameLayoutLeaves(boundNode).FirstOrDefault();
                if (targetLeaf is not null)
                {
                    RebindHostElementToLeaf(host, targetLeaf);
                }

                continue;
            }

            if (boundNode.IsTerminalLeaf)
            {
                RebindHostElementToLeaf(host, boundNode);
                continue;
            }

            var descendantLeaves = EnumerateFrameLayoutLeaves(boundNode).ToList();
            if (descendantLeaves.Count == 0)
            {
                continue;
            }

            if (descendantLeaves.Count == 1)
            {
                RebindHostElementToLeaf(host, descendantLeaves[0]);
                continue;
            }

            CanvasElements.Remove(host);
            foreach (var leaf in descendantLeaves)
            {
                if (CanvasElements.Any(e => ConstructionHostResolver.IsHostFrameElement(e)
                                            && e.HostFrameNodeId == leaf.NodeId))
                {
                    continue;
                }

                var clone = ConstructionMemento.CloneElement(host);
                clone.Id = Guid.NewGuid();
                RebindHostElementToLeaf(clone, leaf);
                AddCanvasElement(clone);
            }
        }
    }
    // === END CRITICAL GUARDRAIL ===

    private void RebindHostElementToLeaf(GeometryElement host, FrameNode leaf)
    {
        if (FrameRoot is null)
        {
            return;
        }

        var probeX = leaf.X + leaf.Width / 2.0;
        var probeY = leaf.Y + leaf.Height / 2.0;
        var material = ConstructionMaterialCatalog.FindById(host.MaterialId);
        var (originX, originY, width, height) = host.Kind == GeometryElementKind.TransitionFrame
            ? ResolveTransitionPlacementBounds(leaf, material, probeX, probeY)
            : Rendering.SashGeometry.ResolvePlacementBoundsAtPoint(
                leaf,
                FrameRoot,
                FrameThickness,
                probeX,
                probeY);

        if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
        {
            return;
        }

        host.X = (decimal)RoundMm(originX + width / 2.0);
        host.Y = (decimal)RoundMm(originY + height / 2.0);
        host.WidthMm = (decimal)width;
        host.HeightMm = (decimal)height;
        host.HostFrameNodeId = leaf.NodeId;
    }

    private static IEnumerable<FrameNode> EnumerateFrameLayoutLeaves(FrameNode node)
    {
        if (node.IsTerminalLeaf)
        {
            yield return node;
            yield break;
        }

        if (node.IsUniformSplit)
        {
            foreach (var cell in node.UniformCells)
            {
                foreach (var leaf in EnumerateFrameLayoutLeaves(cell))
                {
                    yield return leaf;
                }
            }

            yield break;
        }

        if (node.ChildA is not null)
        {
            foreach (var leaf in EnumerateFrameLayoutLeaves(node.ChildA))
            {
                yield return leaf;
            }
        }

        if (node.ChildB is not null)
        {
            foreach (var leaf in EnumerateFrameLayoutLeaves(node.ChildB))
            {
                yield return leaf;
            }
        }
    }

    private void SyncNestedElementsInLeaf(FrameNode leaf)
    {
        var frame = FindFrameInLeaf(leaf);
        if (frame is null)
        {
            return;
        }

        var probeX = (double)frame.X;
        var probeY = (double)frame.Y;
        var (originX, originY, width, height) = Rendering.SashGeometry.ResolvePlacementBoundsAtPoint(
            leaf,
            FrameRoot,
            FrameThickness,
            probeX,
            probeY);

        if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
        {
            return;
        }

        frame.X = (decimal)RoundMm(originX + width / 2.0);
        frame.Y = (decimal)RoundMm(originY + height / 2.0);
        frame.WidthMm = (decimal)width;
        frame.HeightMm = (decimal)height;
    }

    private void UpdateExtremeImpostElementGeometry(FrameNode leaf, ExtremeImpostSlot slot, GeometryElement element)
    {
        if (FrameRoot is null)
        {
            return;
        }

        var cellAxes = Rendering.SashGeometry.ResolveLeafCellAxisBounds(leaf, FrameRoot, FrameThickness, includeExtremeImposts: false);
        var axis = ExtremeImpostGeometry.ResolveAxisWorld(cellAxes, slot.EdgeAlignment, slot.ThicknessMm, slot.OffsetMm);
        ExtremeImpostGeometry.ResolveSpan(
            leaf,
            FrameRoot,
            FrameThickness,
            cellAxes,
            slot.EdgeAlignment,
            slot.ThicknessMm,
            out var spanStart,
            out var spanEnd,
            slot);
        element.EdgeAlignment = slot.EdgeAlignment;
        element.ExtremeImpostOffsetMm = slot.OffsetMm;
        element.HostFrameNodeId = leaf.NodeId;

        if (slot.Orientation == SplitOrientation.Vertical)
        {
            var length = spanEnd - spanStart;
            element.X = (decimal)axis;
            element.Y = (decimal)((spanStart + spanEnd) / 2.0);
            element.WidthMm = (decimal)slot.ThicknessMm;
            element.HeightMm = (decimal)length;
            element.EndX = (decimal)axis;
            element.EndY = (decimal)spanEnd;
            return;
        }

        var horizontalLength = spanEnd - spanStart;
        element.X = (decimal)((spanStart + spanEnd) / 2.0);
        element.Y = (decimal)axis;
        element.WidthMm = (decimal)horizontalLength;
        element.HeightMm = (decimal)slot.ThicknessMm;
        element.EndX = (decimal)spanEnd;
        element.EndY = (decimal)axis;
    }

    private void InitializeExtremeImpostTool()
    {
        SelectedExtremeImpostPlacementOption ??= ExtremeImpostPlacementOptions[0];
        ClearExtremeImpostHover();
        OnPropertyChanged(nameof(IsExtremeImpostToolActive));
    }

    private void ClearExtremeImpostSelection()
    {
        _selectedExtremeImpost = null;
        _isSyncingExtremeImpostPanel = true;
        ExtremeImpostOffsetMm = 0;
        _isSyncingExtremeImpostPanel = false;
        OnPropertyChanged(nameof(IsExtremeImpostEditPanelActive));
    }
}

public sealed record ExtremeImpostPlacementOption(ExtremeImpostPlacementKind Kind, string Label);
