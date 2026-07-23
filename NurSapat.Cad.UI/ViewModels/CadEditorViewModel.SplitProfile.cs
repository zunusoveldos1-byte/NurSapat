using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Rendering;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private enum SplitProfileMode
    {
        SingleLeaf,
        ImpostAdjacentLeaves
    }

    private readonly record struct SplitProfileOperation(
        SplitProfileMode Mode,
        FrameNode TargetLeaf,
        FrameNode? SecondaryLeaf,
        SplitOrientation Orientation,
        bool RequiresArchCheck,
        FrameProfileRail? SourceRail);

    /// <summary>Точка входа инструмента «Разбить профиль пополам».</summary>
    private bool TrySplitProfileFromTool() => SplitSelectedProfileInHalf();

    /// <summary>
    /// «Разбить профиль пополам»: делит выбранный профиль рамы (или ячейку/импост)
    /// перпендикулярным разрезом через середину.
    /// </summary>
    private bool SplitSelectedProfileInHalf()
    {
        try
        {
            if (FrameRoot is null)
            {
                StatusText = "Сначала создайте конструкцию";
                CadDiagnostics.LogInfo("SplitProfile: FrameRoot is null");
                return false;
            }

            // Поднимаем ConstructionAssembly из FrameRoot, иначе RecordPlacement
            // падает в ValidateConstructionOwnershipOrThrow и на холсте «ничего не происходит».
            EnsureConstructionAssemblyFromFrameRoot();

            if (!TryResolveSplitProfileOperation(out var operation))
            {
                StatusText = "Выберите профиль рамы, ячейку или импост для разбиения пополам";
                CadDiagnostics.LogInfo(
                    $"SplitProfile: no target. selection={_constructionSelection.Count}, " +
                    $"SelectedElement leaf={SelectedElement?.IsLeaf}, FrameRoot leaf={FrameRoot.IsLeaf}");
                return false;
            }

            CadDiagnostics.LogInfo(
                $"SplitProfile: resolve ok mode={operation.Mode} orient={operation.Orientation} " +
                $"rail={operation.SourceRail} leaf={operation.TargetLeaf.Width:0}x{operation.TargetLeaf.Height:0} " +
                $"isLeaf={operation.TargetLeaf.IsLeaf}");

            if (operation.RequiresArchCheck && HasArcTopProfile)
            {
                StatusText = "Разбиение профиля недоступно для арочной верхней кромки — используйте «Разбить арку»";
                return false;
            }

            EnsureArchFrameTopProfileForImpost();
            var impostWidth = SingleMullionPlacementWidthMm;
            FrameNode? newImpostNode = null;
            var splitSucceeded = false;

            if (!RecordPlacement(
                    "Профиль разделён пополам",
                    () =>
                    {
                        splitSucceeded = ApplySplitProfileOperation(operation, impostWidth);
                        CadDiagnostics.LogInfo($"SplitProfile: TrySplit returned {splitSucceeded}");
                        if (!splitSucceeded)
                        {
                            return false;
                        }

                        newImpostNode = operation.TargetLeaf;
                        SyncFillingsToFrameLayout();
                        return true;
                    }))
            {
                RefreshAfterPlacementHistoryChange();
                StatusText = splitSucceeded
                    ? "Не удалось зафиксировать разбиение в истории"
                    : $"Невозможно разделить профиль (мин. проём {MinEditableOpeningMm:0} мм / MinSplit 40 мм, ширина импоста {impostWidth:0} мм)";
                CadDiagnostics.LogWarning(
                    $"SplitProfile: RecordPlacement failed. trySplit={splitSucceeded}, " +
                    $"leaf={operation.TargetLeaf.Width:0}x{operation.TargetLeaf.Height:0}, impost={impostWidth:0}");
                return false;
            }

            // Полное обновление геометрии / сетки / холста (как после установки импоста).
            RebuildGridLines();
            RefreshAfterPlacementHistoryChange();
            SelectNewImpostAfterSplit(newImpostNode);
            OnPropertyChanged(nameof(FrameRoot));
            OnPropertyChanged(nameof(SelectedElement));
            RequestSceneRefresh();

            StatusText = "Профиль разделён пополам";
            CadDiagnostics.LogInfo(
                $"SplitProfile: success. FrameRoot.IsLeaf={FrameRoot.IsLeaf}, " +
                $"children={(FrameRoot.ChildA is null ? 0 : 1) + (FrameRoot.ChildB is null ? 0 : 1)}");
            return true;
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("SplitProfile failed", ex);
            StatusText = $"Разбиение профиля: {ex.Message}";
            return false;
        }
    }

    private void SelectNewImpostAfterSplit(FrameNode? impostNode)
    {
        _constructionSelection.Clear();
        if (impostNode is { IsLeaf: false })
        {
            _constructionSelection.Add(CadSelectableItem.Impost(impostNode));
        }

        SyncLegacySelectionFromConstruction();
        if (impostNode is { IsLeaf: false })
        {
            SelectedElement = impostNode;
            return;
        }

        if (FrameRoot is not null)
        {
            SelectedElement = FrameRoot;
        }
    }

    private bool TryResolveSplitProfileOperation(out SplitProfileOperation operation)
    {
        operation = default;
        if (FrameRoot is null)
        {
            return false;
        }

        // 1) Явно выделенная рейка коробки → лист, к которому относится середина рейки.
        if (TryGetSelectedFrameProfileRail(out var profileRail)
            && TryResolveLeafForFrameProfileRail(profileRail, out var railLeaf))
        {
            operation = CreateRailSplitOperation(railLeaf, profileRail);
            return true;
        }

        var impostNode = _selectedMullion?.SplitNode
            ?? _constructionSelection
                .FirstOrDefault(item => item.Kind == CadSelectableKind.Impost)
                ?.ImpostNode;

        if (impostNode is
            {
                IsLeaf: false,
                IsUniformSplit: false,
                ChildA: { IsLeaf: true } childA,
                ChildB: { IsLeaf: true } childB
            })
        {
            operation = new SplitProfileOperation(
                SplitProfileMode.ImpostAdjacentLeaves,
                childA,
                childB,
                impostNode.SplitOrientation == SplitOrientation.Vertical
                    ? SplitOrientation.Horizontal
                    : SplitOrientation.Vertical,
                RequiresArchCheck: impostNode.SplitOrientation == SplitOrientation.Horizontal,
                SourceRail: null);
            return true;
        }

        if (SelectedElement is FrameNode { IsLeaf: true } selectedLeaf)
        {
            operation = CreateLeafSplitOperation(selectedLeaf);
            return true;
        }

        if (_constructionSelection.Count == 1
            && TryResolveLeafFromSelection(_constructionSelection.First(), out var leaf))
        {
            operation = CreateLeafSplitOperation(leaf);
            return true;
        }

        if (FrameRoot.IsLeaf)
        {
            operation = CreateLeafSplitOperation(FrameRoot);
            return true;
        }

        return false;
    }

    private bool TryGetSelectedFrameProfileRail(out FrameProfileRail rail)
    {
        rail = default;
        var selected = _constructionSelection.FirstOrDefault(item =>
            item.Kind == CadSelectableKind.Frame && item.ProfileRail is not null);
        if (selected?.ProfileRail is not FrameProfileRail profileRail)
        {
            return false;
        }

        rail = profileRail;
        return true;
    }

    /// <summary>
    /// Находит листовой <see cref="FrameNode"/> для выбранной рейки:
    /// середина M рейки → FindLeafAt / FindLeafAtForOwner.
    /// </summary>
    private bool TryResolveLeafForFrameProfileRail(FrameProfileRail rail, out FrameNode leaf)
    {
        leaf = null!;
        if (FrameRoot is null)
        {
            return false;
        }

        if (FrameRoot.IsLeaf)
        {
            leaf = FrameRoot;
            return true;
        }

        var mid = ResolveFrameProfileRailMidpoint(FrameRoot, rail);
        var resolved = FindLeafAtForOwner(mid.X, mid.Y)
                       ?? FrameRoot.FindLeafAt(mid.X, mid.Y);
        if (resolved is { IsLeaf: true })
        {
            leaf = resolved;
            return true;
        }

        // Запасной путь: если оболочка FrameRoot — лист-обёртка, а дерево живёт в SubFrame.
        if (ConstructionAssembly is { SubFrames.Count: 1 } assembly
            && assembly.SubFrames[0].Root is { IsLeaf: true } subRoot)
        {
            leaf = subRoot;
            return true;
        }

        return false;
    }

    private CadPoint ResolveFrameProfileRailMidpoint(FrameNode frame, FrameProfileRail rail)
    {
        var band = FrameProfileRailGeometry.ResolveRailBand(
            frame.X,
            frame.Y,
            frame.Width,
            frame.Height,
            FrameThickness,
            rail);
        return new CadPoint(
            (band.OuterStart.X + band.OuterEnd.X) / 2.0,
            (band.OuterStart.Y + band.OuterEnd.Y) / 2.0);
    }

    /// <summary>
    /// Разрез по выбранной рейке: Midpoint M профиля задаёт ось нового импоста.
    /// </summary>
    private static SplitProfileOperation CreateRailSplitOperation(FrameNode leaf, FrameProfileRail rail)
    {
        // Горизонтальный профиль (Top/Bottom): Start→End по X → импост вертикально в Xm.
        // Вертикальный профиль (Left/Right): Start→End по Y → импост горизонтально в Ym.
        var orientation = rail is FrameProfileRail.Top or FrameProfileRail.Bottom
            ? SplitOrientation.Vertical
            : SplitOrientation.Horizontal;

        return new SplitProfileOperation(
            SplitProfileMode.SingleLeaf,
            leaf,
            null,
            orientation,
            RequiresArchCheck: true,
            SourceRail: rail);
    }

    private SplitProfileOperation CreateLeafSplitOperation(FrameNode leaf) =>
        new(
            SplitProfileMode.SingleLeaf,
            leaf,
            null,
            leaf.Width >= leaf.Height ? SplitOrientation.Vertical : SplitOrientation.Horizontal,
            RequiresArchCheck: true,
            SourceRail: null);

    private bool TryResolveLeafFromSelection(CadSelectableItem item, out FrameNode leaf)
    {
        leaf = null!;
        if (FrameRoot is null)
        {
            return false;
        }

        if (item.Kind == CadSelectableKind.Impost
            && item.ImpostNode is { ChildA: { IsLeaf: true } impostChild })
        {
            leaf = impostChild;
            return true;
        }

        FrameNode? resolved = item.Kind switch
        {
            CadSelectableKind.Glazing when item.ElementId is Guid glazingId
                => CanvasElements.FirstOrDefault(e => e.Id == glazingId) is { } glazing
                    ? SashGeometry.FindLeafForGlazing(this, glazing)
                    : null,
            CadSelectableKind.Sash when item.ElementId is Guid sashId
                => CanvasElements.FirstOrDefault(e => e.Id == sashId) is { } sash
                    ? SashGeometry.FindLeafForSash(this, sash)
                    : null,
            _ => item.ElementId is Guid elementId
                && CanvasElements.FirstOrDefault(e => e.Id == elementId) is { } element
                ? FrameRoot.FindLeafAt((double)element.X, (double)element.Y)
                : null
        };

        if (resolved is { IsLeaf: true })
        {
            leaf = resolved;
            return true;
        }

        return false;
    }

    private bool ApplySplitProfileOperation(SplitProfileOperation operation, double impostWidth) =>
        operation.Mode switch
        {
            SplitProfileMode.SingleLeaf =>
                ApplyLeafHalfSplit(operation.TargetLeaf, operation.Orientation, impostWidth),
            SplitProfileMode.ImpostAdjacentLeaves when operation.SecondaryLeaf is { } secondary =>
                ApplyLeafHalfSplit(operation.TargetLeaf, operation.Orientation, impostWidth)
                && ApplyLeafHalfSplit(secondary, operation.Orientation, impostWidth),
            _ => false
        };

    /// <summary>
    /// Вставляет импост в геометрическом центре листа (SplitRelativeCenter = 0.5).
    /// </summary>
    private bool ApplyLeafHalfSplit(FrameNode leaf, SplitOrientation orientation, double impostWidth)
    {
        if (!leaf.IsLeaf)
        {
            CadDiagnostics.LogWarning($"SplitProfile: leaf is not leaf (already split)");
            return false;
        }

        var canSplit = orientation == SplitOrientation.Vertical
            ? leaf.CanSplitVerticalAt(
                leaf.ResolveInsetAdjustedVerticalSplitCenterLocal(),
                ImpostLineStyle.Solid,
                impostWidth)
            : leaf.CanSplitHorizontalAt(
                leaf.ResolveInsetAdjustedHorizontalSplitCenterLocal(
                    FrameNode.ResolveHorizontalSplitExtentLocal(leaf, leaf.GetRoot())),
                ImpostLineStyle.Solid,
                impostWidth);

        CadDiagnostics.LogInfo(
            $"SplitProfile: CanSplit={canSplit} orient={orientation} " +
            $"size={leaf.Width:0}x{leaf.Height:0} impostWidth={impostWidth:0}");

        var split = orientation == SplitOrientation.Vertical
            ? leaf.TrySplitVerticalHalf(ImpostLineStyle.Solid, impostWidth)
            : leaf.TrySplitHorizontalHalf(ImpostLineStyle.Solid, impostWidth);
        if (!split)
        {
            CadDiagnostics.LogWarning(
                $"SplitProfile: TrySplit*Half returned false (MinSplitSizeMm=40, size={leaf.Width:0}x{leaf.Height:0})");
            return false;
        }

        TrackImpostSplit(leaf);
        return true;
    }
}
