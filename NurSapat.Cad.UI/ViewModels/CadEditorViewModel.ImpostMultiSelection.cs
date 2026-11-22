using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Cad.UI.Models;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private const double HorizontalImpostLineSelectionToleranceMm = 0.5;
    private readonly List<ImpostTarget> _selectedImposts = [];
    private readonly List<ImpostDragStart> _impostDragStarts = [];
    private double _impostDragPointerStartAxis;
    private double _impostDragReferenceStartAxis;

    public IReadOnlyList<ImpostTarget> SelectedImposts => _selectedImposts;

    private bool TrySelectImpostForOffset(
        ImpostTarget hit,
        GridLineOrientation targetOrientation,
        bool isMultiSelectPressed,
        double pointerAxis)
    {
        if (hit.Orientation != targetOrientation)
        {
            return false;
        }

        // Horizontal profiles are stored as independent cell segments. For the
        // offset tool Alt explicitly selects their logical Y-line; a regular
        // click keeps the segment local.
        if (targetOrientation == GridLineOrientation.Horizontal)
        {
            return TrySelectHorizontalImpostLine(
                hit,
                pointerAxis,
                selectWholeLine: isMultiSelectPressed);
        }

        RemoveTargetsOfOtherOrientation(targetOrientation);
        var existing = _selectedImposts.FindIndex(target => IsSameImpost(target, hit));

        if (isMultiSelectPressed && existing >= 0)
        {
            _selectedImposts.RemoveAt(existing);
            _selectedMullion = _selectedImposts.FirstOrDefault();
            NotifyImpostMultiSelectionChanged();
            StatusText = _selectedImposts.Count == 0
                ? "Выделение импостов снято"
                : $"Выбрано импостов: {_selectedImposts.Count}";
            return false;
        }

        BeginConstructionDrag();
        TryResolveImpostAxis(hit, targetOrientation, out var hitAxisBeforeAlignment);
        var alignedOnSelection = false;
        if (!isMultiSelectPressed)
        {
            _selectedImposts.Clear();
        }

        if (existing < 0 || !isMultiSelectPressed)
        {
            if (isMultiSelectPressed && _selectedImposts.Count > 0
                && !TryAlignImpostToGroupReference(hit, targetOrientation))
            {
                CancelConstructionDrag();
                StatusText = "Импост нельзя выровнять: достигнута граница ячейки";
                return false;
            }

            alignedOnSelection = isMultiSelectPressed && _selectedImposts.Count > 0;
            _selectedImposts.Add(hit);
        }

        _selectedMullion = hit;
        if (!TryBeginImpostGroupDrag(targetOrientation, pointerAxis))
        {
            if (alignedOnSelection)
            {
                MoveImpost(hit, targetOrientation, hitAxisBeforeAlignment);
                _selectedImposts.RemoveAll(target => IsSameImpost(target, hit));
            }

            CancelConstructionDrag();
            return false;
        }

        NotifyImpostMultiSelectionChanged();
        StatusText = _selectedImposts.Count > 1
            ? $"Выбрано импостов: {_selectedImposts.Count}. Перемещается вся группа"
            : "Импост выделен — перетащите или измените L/H";
        return true;
    }

    private bool TrySelectHorizontalImpostLine(
        ImpostTarget hit,
        double pointerAxis,
        bool selectWholeLine)
    {
        if (!TryResolveImpostAxis(hit, GridLineOrientation.Horizontal, out var targetAxis))
        {
            return false;
        }

        BeginConstructionDrag();
        RebuildGridLines();
        _selectedImposts.Clear();

        if (selectWholeLine)
        {
            foreach (var line in GridLines.OfOrientation(GridLineOrientation.Horizontal))
            {
                if (line.Kind != GridLineKind.Physical
                    || Math.Abs(line.Coordinate - targetAxis) >= HorizontalImpostLineSelectionToleranceMm
                    || line.SourceNodeId is not Guid nodeId
                    || FrameRoot?.FindNodeById(nodeId) is not { } node)
                {
                    continue;
                }

                var target = new ImpostTarget
                {
                    SplitNode = node,
                    UniformGapIndex = line.UniformGapIndex,
                    GridLineId = line.Id,
                    Orientation = GridLineOrientation.Horizontal
                };

                if (!_selectedImposts.Any(selected => IsSameImpost(selected, target)))
                {
                    _selectedImposts.Add(target);
                }
            }
        }

        if (!_selectedImposts.Any(target => IsSameImpost(target, hit)))
        {
            _selectedImposts.Add(hit);
        }

        _selectedMullion = hit;
        if (!TryBeginImpostGroupDrag(GridLineOrientation.Horizontal, pointerAxis))
        {
            CancelConstructionDrag();
            ClearImpostMultiSelection();
            _selectedMullion = null;
            return false;
        }

        NotifyImpostMultiSelectionChanged();
        StatusText = _selectedImposts.Count > 1
            ? $"Выбрана горизонтальная линия: {_selectedImposts.Count} сегмента(ов)"
            : "Выбран один сегмент горизонтального импоста";
        return true;
    }

    private bool TryAlignImpostToGroupReference(
        ImpostTarget target,
        GridLineOrientation orientation)
    {
        var reference = _selectedImposts[0];
        if (!TryResolveImpostAxis(reference, orientation, out var referenceAxis)
            || !IsAxisWithinTargetRange(target, orientation, referenceAxis))
        {
            return false;
        }

        var moved = orientation == GridLineOrientation.Vertical
            ? MoveVerticalImpost(target, referenceAxis)
            : MoveHorizontalImpost(target, referenceAxis);
        return moved
            && TryResolveImpostAxis(target, orientation, out var alignedAxis)
            && CadMath.NearlyEqual(alignedAxis, referenceAxis);
    }

    private bool TryBeginImpostGroupDrag(
        GridLineOrientation orientation,
        double pointerAxis)
    {
        _impostDragStarts.Clear();
        foreach (var target in _selectedImposts)
        {
            if (target.Orientation != orientation
                || !TryResolveImpostAxis(target, orientation, out var axis))
            {
                _impostDragStarts.Clear();
                return false;
            }

            _impostDragStarts.Add(new ImpostDragStart(target, axis));
        }

        _impostDragPointerStartAxis = pointerAxis;
        _impostDragReferenceStartAxis = _selectedMullion is { } selected
            && selected.Orientation == orientation
            && TryResolveImpostAxis(selected, orientation, out var selectedAxis)
                ? selectedAxis
                : _impostDragStarts[0].Axis;
        return _impostDragStarts.Count > 0;
    }

    private bool TryMoveSelectedImpostGroup(
        GridLineOrientation orientation,
        double pointerAxis)
    {
        if (_impostDragStarts.Count == 0
            || _impostDragStarts.Any(item => item.Target.Orientation != orientation))
        {
            return false;
        }

        var delta = pointerAxis - _impostDragPointerStartAxis;
        var sharedAxis = RoundMm(_impostDragReferenceStartAxis + delta);
        var destinations = _impostDragStarts
            .Select(item => (item.Target, Axis: sharedAxis))
            .ToList();

        if (destinations.Any(item => !IsAxisWithinTargetRange(item.Target, orientation, item.Axis)))
        {
            StatusText = "Движение группы ограничено границей одной из ячеек";
            return false;
        }

        var originalAxes = new List<(ImpostTarget Target, double Axis)>(destinations.Count);
        foreach (var destination in destinations)
        {
            if (!TryResolveImpostAxis(destination.Target, orientation, out var originalAxis))
            {
                return false;
            }

            originalAxes.Add((destination.Target, originalAxis));
        }

        for (var i = 0; i < destinations.Count; i++)
        {
            var destination = destinations[i];
            if (!MoveImpost(destination.Target, orientation, destination.Axis))
            {
                for (var rollbackIndex = i; rollbackIndex >= 0; rollbackIndex--)
                {
                    var original = originalAxes[rollbackIndex];
                    MoveImpost(original.Target, orientation, original.Axis);
                }

                return false;
            }
        }

        return true;
    }

    private bool TryMoveSelectedImpostGroupToAxis(
        GridLineOrientation orientation,
        double axis)
    {
        var targets = _selectedImposts
            .Where(target => target.Orientation == orientation)
            .ToList();
        if (targets.Count == 0 && _selectedMullion?.Orientation == orientation)
        {
            targets.Add(_selectedMullion);
        }

        var destinationAxis = RoundMm(axis);
        if (targets.Count == 0
            || targets.Any(target => !IsAxisWithinTargetRange(target, orientation, destinationAxis)))
        {
            StatusText = "Движение группы ограничено границей одной из ячеек";
            return false;
        }

        var originalAxes = new List<(ImpostTarget Target, double Axis)>(targets.Count);
        foreach (var target in targets)
        {
            if (!TryResolveImpostAxis(target, orientation, out var originalAxis))
            {
                return false;
            }

            originalAxes.Add((target, originalAxis));
        }

        for (var i = 0; i < targets.Count; i++)
        {
            if (MoveImpost(targets[i], orientation, destinationAxis))
            {
                continue;
            }

            for (var rollbackIndex = i; rollbackIndex >= 0; rollbackIndex--)
            {
                var original = originalAxes[rollbackIndex];
                MoveImpost(original.Target, orientation, original.Axis);
            }

            return false;
        }

        return true;
    }

    private bool IsSelectedHorizontalImpostLineAtAxis(double axis) =>
        _selectedImposts.Any(
            target => target.Orientation == GridLineOrientation.Horizontal
                && TryResolveImpostAxis(
                    target,
                    GridLineOrientation.Horizontal,
                    out var selectedAxis)
                && Math.Abs(selectedAxis - axis) < HorizontalImpostLineSelectionToleranceMm);

    private bool IsAxisWithinTargetRange(
        ImpostTarget target,
        GridLineOrientation orientation,
        double axis)
    {
        double min;
        double max;
        var resolved = orientation == GridLineOrientation.Vertical
            ? TryResolveVerticalImpostDragAxisRange(target, out min, out max)
            : TryResolveHorizontalImpostDragAxisRange(target, out min, out max);

        return resolved
            && axis >= min - CadMath.MmPrecision
            && axis <= max + CadMath.MmPrecision;
    }

    private bool MoveImpost(
        ImpostTarget target,
        GridLineOrientation orientation,
        double axis) =>
        orientation == GridLineOrientation.Vertical
            ? MoveVerticalImpost(target, axis)
            : MoveHorizontalImpost(target, axis);

    private static bool TryResolveImpostAxis(
        ImpostTarget target,
        GridLineOrientation orientation,
        out double axis)
    {
        axis = 0;
        var node = target.SplitNode;
        if (node.IsUniformSplit && target.UniformGapIndex is int gapIndex)
        {
            var splitOrientation = orientation == GridLineOrientation.Vertical
                ? SplitOrientation.Vertical
                : SplitOrientation.Horizontal;
            return node.TryGetUniformImpostAxisWorld(splitOrientation, gapIndex, out axis);
        }

        return orientation == GridLineOrientation.Vertical
            ? node.TryGetVerticalImpostAxisWorldX(out axis)
            : node.TryGetHorizontalImpostAxisWorldY(out axis);
    }

    private void RemoveTargetsOfOtherOrientation(GridLineOrientation orientation)
    {
        _selectedImposts.RemoveAll(target => target.Orientation != orientation);
    }

    private static bool IsSameImpost(ImpostTarget left, ImpostTarget right) =>
        ReferenceEquals(left.SplitNode, right.SplitNode)
        && left.UniformGapIndex == right.UniformGapIndex
        && left.Orientation == right.Orientation;

    private void NotifyImpostMultiSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedImposts));
        OnPropertyChanged(nameof(SelectedMullion));
        OnPropertyChanged(nameof(IsImpostPanelActive));
        RequestSceneRefresh();
    }

    private void ClearImpostMultiSelection()
    {
        _selectedImposts.Clear();
        _impostDragStarts.Clear();
        OnPropertyChanged(nameof(SelectedImposts));
        RequestSceneRefresh();
    }

    private sealed record ImpostDragStart(ImpostTarget Target, double Axis);
}
