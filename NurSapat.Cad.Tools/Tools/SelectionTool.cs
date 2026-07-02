using NurSapat.Cad.Core.Commands;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Viewport;

namespace NurSapat.Cad.Tools.Tools;

public sealed class SelectionTool : CadToolBase
{
    public enum ResizeHandle { None, TopLeft, TopRight, BottomLeft, BottomRight, Left, Right, Top, Bottom }

    private CadPoint? _dragStart;
    private CadRect? _selectionRect;
    private CadRect? _originalRect;
    private bool _isResizing;
    private ResizeHandle _activeHandle = ResizeHandle.None;
    private bool _hasMoved;
    private bool _startedOnElement;

    public CadRect? SelectionRect => _selectionRect;

    public bool IsResizing => _isResizing;
    public CadRect? ResizingOriginalRect => _originalRect;
    public CadPoint? DragStart => _dragStart;
    public ResizeHandle ActiveHandle => _activeHandle;

    public override string Id => "select";
    public override string DisplayName => "Выделение элементов";
    public override CadToolKind Kind => CadToolKind.SelectElements;
    public override bool IsImplemented => true;

    private const double DragThresholdMm = 5.0;

    public override void OnActivated(CadToolContext context)
    {
        context.ActiveSelectionMode = CadSelectionMode.Element;
    }

    public override void OnDeactivated(CadToolContext context)
    {
        if (context.ActiveSelectionMode == CadSelectionMode.Element)
        {
            context.ActiveSelectionMode = CadSelectionMode.None;
        }

        ResetDragState();
    }

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        _dragStart = e.WorldPoint;
        _selectionRect = null;
        _hasMoved = false;
        _startedOnElement = HasElementAtPoint(context, e.WorldPoint);

        var selected = context.Document.SelectedRectangle;
        if (selected != null && !_startedOnElement)
        {
            _activeHandle = GetHandleAtPoint(e.WorldPoint, selected.Value);
            if (_activeHandle != ResizeHandle.None)
            {
                _isResizing = true;
                _originalRect = selected;
            }
        }
    }

    public override void OnMouseMove(CadToolContext context, CadPointerEvent e)
    {
        if (_isResizing && _originalRect != null && _dragStart != null)
        {
            _hasMoved = true;
            var newRect = ResizeRect(_originalRect.Value, _dragStart.Value, e.WorldPoint, _activeHandle);
            context.Document.SelectedRectangle = newRect;
            context.Invalidate?.Invoke();
            return;
        }

        if (_dragStart is not CadPoint start)
        {
            return;
        }

        if ((e.WorldPoint - start).Length > DragThresholdMm)
        {
            _hasMoved = true;
            if (!_startedOnElement)
            {
                _selectionRect = CadRect.FromPoints(start, e.WorldPoint);
                context.Invalidate?.Invoke();
            }
        }
    }

    public override void OnMouseUp(CadToolContext context, CadPointerEvent e)
    {
        if (_isResizing && _originalRect != null && _hasMoved)
        {
            var rect = context.Document.SelectedRectangle!.Value;
            var original = _originalRect.Value;

            var command = new CadActionCommand(
                name: "Изменить размер",
                execute: doc => doc.ReplaceRectangle(original, rect),
                undo: doc => doc.ReplaceRectangle(rect, original));
            context.CommandManager.Execute(command);

            ResetDragState();
            context.Invalidate?.Invoke();
            return;
        }

        if (_hasMoved && _selectionRect is CadRect box && !_startedOnElement)
        {
            if (context.HandleConstructionBoxSelection is { } applyBox)
            {
                applyBox(box, e.IsCtrlDown);
            }
            else
            {
                ApplyLegacyBoxSelection(context, box);
            }
        }
        else if (_dragStart != null)
        {
            if (context.HandleConstructionClickSelection is { } applyClick)
            {
                applyClick(e.WorldPoint, e.IsCtrlDown);
            }
            else
            {
                context.HitTest(e.WorldPoint);
            }
        }

        ResetDragState();
        context.Invalidate?.Invoke();
    }

    private static bool HasElementAtPoint(CadToolContext context, CadPoint worldPoint)
    {
        if (context.HasConstructionElementAtPoint?.Invoke(worldPoint) == true)
        {
            return true;
        }

        return context.HitTest(worldPoint) != null;
    }

    private static void ApplyLegacyBoxSelection(CadToolContext context, CadRect box)
    {
        var hits = context.SelectWithin(box);
        if (hits.Count == 0)
        {
            context.Document.SelectedRectangle = null;
            return;
        }

        context.Document.SelectedRectangle = hits[0];
    }

    private void ResetDragState()
    {
        _isResizing = false;
        _activeHandle = ResizeHandle.None;
        _dragStart = null;
        _originalRect = null;
        _selectionRect = null;
        _hasMoved = false;
        _startedOnElement = false;
    }

    public CadPoint[] GetHandlePositions(CadRect rect) =>
    [
        rect.TopLeft,
        rect.TopRight,
        rect.BottomLeft,
        rect.BottomRight
    ];

    public bool IsPointOnHandle(CadPoint point, CadRect rect, double tolerance) =>
        GetHandleAtPoint(point, rect) != ResizeHandle.None;

    private static ResizeHandle GetHandleAtPoint(CadPoint point, CadRect rect)
    {
        const double tol = 10.0;
        if (point.IsNear(rect.TopLeft, tol)) return ResizeHandle.TopLeft;
        if (point.IsNear(rect.TopRight, tol)) return ResizeHandle.TopRight;
        if (point.IsNear(rect.BottomLeft, tol)) return ResizeHandle.BottomLeft;
        if (point.IsNear(rect.BottomRight, tol)) return ResizeHandle.BottomRight;
        if (Math.Abs(point.Y - rect.Y) < tol && point.X >= rect.X && point.X <= rect.X + rect.Width) return ResizeHandle.Top;
        if (Math.Abs(point.Y - (rect.Y - rect.Height)) < tol && point.X >= rect.X && point.X <= rect.X + rect.Width) return ResizeHandle.Bottom;
        if (Math.Abs(point.X - rect.X) < tol && point.Y <= rect.Y && point.Y >= rect.Y - rect.Height) return ResizeHandle.Left;
        if (Math.Abs(point.X - (rect.X + rect.Width)) < tol && point.Y <= rect.Y && point.Y >= rect.Y - rect.Height) return ResizeHandle.Right;
        return ResizeHandle.None;
    }

    private static CadRect ResizeRect(CadRect original, CadPoint dragStart, CadPoint currentPos, ResizeHandle handle)
    {
        var dx = currentPos.X - dragStart.X;
        var dy = currentPos.Y - dragStart.Y;
        var x = original.X;
        var y = original.Y;
        var w = original.Width;
        var h = original.Height;

        switch (handle)
        {
            case ResizeHandle.Right: w += dx; break;
            case ResizeHandle.Left: x += dx; w -= dx; break;
            case ResizeHandle.Top: y += dy; h -= dy; break;
            case ResizeHandle.Bottom: h += dy; break;
            case ResizeHandle.TopRight: w += dx; y += dy; h -= dy; break;
            case ResizeHandle.TopLeft: x += dx; w -= dx; y += dy; h -= dy; break;
            case ResizeHandle.BottomRight: w += dx; h += dy; break;
            case ResizeHandle.BottomLeft: x += dx; w -= dx; h += dy; break;
        }

        if (w < 5) w = 5;
        if (h < 5) h = 5;
        return new CadRect(x, y, w, h);
    }
}
