using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Tools;
using NurSapat.Cad.UI.Models;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel
{
    private const double VertexHandleScreenPx = 12;

    private bool _isVertexDragging;
    private VertexDragTarget? _vertexDragTarget;
    private double _vertexDragFixedX;
    private double _vertexDragFixedY;
    private CadPoint _vertexDragStartWorld;
    private bool _vertexDragWasMoved;
    private FrameOuterCornerKind? _hoveredVertexCorner;
    private Guid? _hoveredVertexJointId;

    public bool ShowVertexHandles =>
        (CurrentToolMode == WindowToolMode.SelectVertex
         || ActiveToolId == CadToolMetadata.GetId(CadToolKind.SetVertexCoordinates))
        && FrameRoot is not null
        && HasRealFrameOnCanvas
        && FrameRoot.Shape == ConstructionShape.Rectangle
        && !HasArcTopProfile;

    public bool IsVertexDragging => _isVertexDragging;

    public FrameOuterCornerKind? HoveredVertexCorner => _hoveredVertexCorner;

    public Guid? HoveredVertexJointId => _hoveredVertexJointId;

    public FrameOuterCornerKind? ActiveVertexCorner =>
        _isVertexDragging ? _vertexDragTarget?.Corner : _hoveredVertexCorner;

    public Guid? ActiveVertexJointId =>
        _isVertexDragging ? _vertexDragTarget?.JointId : _hoveredVertexJointId;

    public void UpdateVertexHandleHover(CadPoint worldPoint)
    {
        if (!ShowVertexHandles)
        {
            ClearVertexHandleHover();
            return;
        }

        var hit = ResolveVertexHandleHit(worldPoint);
        var corner = hit?.Corner;
        var jointId = hit?.JointId;
        if (corner == _hoveredVertexCorner && jointId == _hoveredVertexJointId)
        {
            return;
        }

        _hoveredVertexCorner = corner;
        _hoveredVertexJointId = jointId;
        OnPropertyChanged(nameof(HoveredVertexCorner));
        OnPropertyChanged(nameof(HoveredVertexJointId));
        OnPropertyChanged(nameof(ActiveVertexCorner));
        OnPropertyChanged(nameof(ActiveVertexJointId));
    }

    public void ClearVertexHandleHover()
    {
        if (_hoveredVertexCorner is null && _hoveredVertexJointId is null)
        {
            return;
        }

        _hoveredVertexCorner = null;
        _hoveredVertexJointId = null;
        OnPropertyChanged(nameof(HoveredVertexCorner));
        OnPropertyChanged(nameof(HoveredVertexJointId));
        OnPropertyChanged(nameof(ActiveVertexCorner));
        OnPropertyChanged(nameof(ActiveVertexJointId));
    }

    public bool TryHitVertexHandle(CadPoint worldPoint) =>
        ShowVertexHandles && ResolveVertexHandleHit(worldPoint) is not null;

    public bool TryPressVertexDrag(CadPoint worldPoint)
    {
        if (!ShowVertexHandles || FrameRoot is null)
        {
            return false;
        }

        var hit = ResolveVertexHandleHit(worldPoint);
        if (hit is null)
        {
            return false;
        }

        BeginConstructionDrag();
        _isVertexDragging = true;
        _vertexDragTarget = hit;
        _vertexDragFixedX = hit.Point.X;
        _vertexDragFixedY = hit.Point.Y;
        _vertexDragStartWorld = hit.Point;
        _vertexDragWasMoved = false;
        _hoveredVertexCorner = hit.Corner;
        _hoveredVertexJointId = hit.JointId;
        OnPropertyChanged(nameof(IsVertexDragging));
        OnPropertyChanged(nameof(HoveredVertexCorner));
        OnPropertyChanged(nameof(HoveredVertexJointId));
        OnPropertyChanged(nameof(ActiveVertexCorner));
        OnPropertyChanged(nameof(ActiveVertexJointId));
        StatusText = hit.Corner is not null
            ? "Перетаскивание угла (Ctrl — ортогонально)"
            : "Перетаскивание стыка профиля (Ctrl — ортогонально)";
        RequestSceneRefresh();
        return true;
    }

    public void UpdateVertexDrag(CadPoint worldPoint, bool isCtrlDown)
    {
        if (!_isVertexDragging || _vertexDragTarget is null || FrameRoot is null)
        {
            return;
        }

        var target = ApplyOrthoConstraint(worldPoint, isCtrlDown);
        if (Distance(target, _vertexDragStartWorld) <= CadMath.Epsilon)
        {
            return;
        }

        var applied = false;

        if (_vertexDragTarget.Corner is { } corner)
        {
            applied = FrameRoot.TrySetOuterCorner(corner, target, MinEditableOpeningMm);
            if (applied)
            {
                RecalculateFrameGeometry();
            }
        }
        else if (_vertexDragTarget.JointId is { } jointId
                 && _vertexDragTarget.Rail is not null)
        {
            applied = FrameRoot.TrySetProfileRailJointPosition(jointId, target);
            if (applied)
            {
                RequestSceneRefresh();
            }
        }

        if (applied)
        {
            _vertexDragWasMoved = true;
            SyncFrameGeometryToDocument();
            RequestSceneRefresh();
        }
    }

    public void ReleaseVertexDrag()
    {
        if (!_isVertexDragging)
        {
            return;
        }

        var openCoordinateInspector = !_vertexDragWasMoved;
        if (openCoordinateInspector)
        {
            CancelConstructionDrag();
        }
        else
        {
            CommitConstructionDrag("Вершина рамы перемещена");
        }

        _isVertexDragging = false;
        _vertexDragTarget = null;
        OnPropertyChanged(nameof(IsVertexDragging));
        OnPropertyChanged(nameof(ActiveVertexCorner));
        OnPropertyChanged(nameof(ActiveVertexJointId));
        StatusText = openCoordinateInspector
            ? "Введите точные координаты вершины"
            : "Вершина обновлена";
        SyncFrameGeometryToDocument();
        RequestSceneRefresh();
        if (openCoordinateInspector)
        {
            TrySetVertexCoordinatesFromTool(_vertexDragStartWorld);
        }
    }

    public bool TryNudgeActiveVertex(double deltaX, double deltaY)
    {
        if (!ShowVertexHandles || FrameRoot is null)
        {
            return false;
        }

        var applied = false;
        if (_hoveredVertexCorner is { } corner)
        {
            var current = FrameRoot.GetOuterCorner(corner);
            applied = FrameRoot.TrySetOuterCorner(
                corner,
                new CadPoint(
                    CadMath.RoundMm(current.X + deltaX),
                    CadMath.RoundMm(current.Y + deltaY)),
                MinEditableOpeningMm);
            if (applied)
            {
                RecalculateFrameGeometry();
            }
        }
        else if (_hoveredVertexJointId is { } jointId)
        {
            var root = FrameRoot.GetConstructionRoot();
            var joint = root.ProfileRailJoints.FirstOrDefault(item => item.Id == jointId);
            if (joint is null)
            {
                return false;
            }

            var current = root.ResolveProfileRailJointOuterPoint(joint);
            applied = root.TrySetProfileRailJointPosition(
                jointId,
                new CadPoint(
                    CadMath.RoundMm(current.X + deltaX),
                    CadMath.RoundMm(current.Y + deltaY)));
        }

        if (!applied)
        {
            return false;
        }

        SyncFrameGeometryToDocument();
        RequestSceneRefresh();
        StatusText = "Вершина смещена с клавиатуры";
        return true;
    }

    private CadPoint ApplyOrthoConstraint(CadPoint worldPoint, bool isCtrlDown)
    {
        if (!isCtrlDown)
        {
            return new CadPoint(CadMath.RoundMm(worldPoint.X), CadMath.RoundMm(worldPoint.Y));
        }

        var dx = Math.Abs(worldPoint.X - _vertexDragStartWorld.X);
        var dy = Math.Abs(worldPoint.Y - _vertexDragStartWorld.Y);
        return dx > dy
            ? new CadPoint(CadMath.RoundMm(worldPoint.X), _vertexDragFixedY)
            : new CadPoint(_vertexDragFixedX, CadMath.RoundMm(worldPoint.Y));
    }

    private VertexDragTarget? ResolveVertexHandleHit(CadPoint worldPoint)
    {
        if (FrameRoot is null)
        {
            return null;
        }

        var tolerance = Math.Max(VertexHandleScreenPx * MillimetersPerPixel, 8.0);
        VertexDragTarget? best = null;
        var bestDistance = double.MaxValue;

        foreach (var (kind, point) in FrameRoot.EnumerateOuterCorners())
        {
            var distance = Distance(worldPoint, point);
            if (distance > tolerance || distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            best = new VertexDragTarget(point, kind, null, null);
        }

        var root = FrameRoot.GetConstructionRoot();
        foreach (var joint in root.ProfileRailJoints)
        {
            var outer = root.ResolveProfileRailJointOuterPoint(joint);
            var distance = Distance(worldPoint, outer);
            if (distance > tolerance || distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            best = new VertexDragTarget(outer, null, joint.Id, joint.Rail);
        }

        return best;
    }

    private sealed record VertexDragTarget(
        CadPoint Point,
        FrameOuterCornerKind? Corner,
        Guid? JointId,
        FrameProfileRail? Rail);
}
