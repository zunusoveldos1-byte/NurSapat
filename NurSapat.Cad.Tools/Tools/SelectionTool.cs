using NurSapat.Cad.Core.Commands;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Viewport;  // Добавить, чтобы использовать CadViewport

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

    // Публичное свойство для получения выбранного прямоугольника
    public CadRect? SelectionRect => _selectionRect;

    // Публичное свойство для получения активной ручки (нужно Canvas для отрисовки)
    public bool IsResizing => _isResizing;
    public CadRect? ResizingOriginalRect => _originalRect;
    public CadPoint? DragStart => _dragStart;
    public ResizeHandle ActiveHandle => _activeHandle;

    public override string Id => "select";
    public override string DisplayName => "Выбор";

    public override void OnMouseDown(CadToolContext context, CadPointerEvent e)
    {
        _dragStart = e.WorldPoint;
        _selectionRect = null;
        _hasMoved = false;

        var selected = context.Document.SelectedRectangle;
        if (selected != null)
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

        if (_dragStart is not CadPoint start) return;

        if ((e.WorldPoint - start).Length > 5.0)
        {
            _hasMoved = true;
            _selectionRect = CadRect.FromPoints(start, e.WorldPoint);
            context.Invalidate?.Invoke();
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
                undo: doc => doc.ReplaceRectangle(rect, original)
            );
            context.CommandManager.Execute(command);

            _isResizing = false;
            _activeHandle = ResizeHandle.None;
            _dragStart = null;
            _originalRect = null;
            _hasMoved = false;
            context.Invalidate?.Invoke();
            return;
        }

        if (!_hasMoved && _dragStart != null)
        {
            context.HitTest(e.WorldPoint);
        }

        _dragStart = null;
        _selectionRect = null;
        _hasMoved = false;
        context.Invalidate?.Invoke();
    }

    // Вспомогательный метод: возвращает позиции ручек для отрисовки
    public CadPoint[] GetHandlePositions(CadRect rect)
    {
        return new[]
        {
            rect.TopLeft,
            rect.TopRight,
            rect.BottomLeft,
            rect.BottomRight
        };
    }

    // Вспомогательный метод: определяет, находится ли точка в ручке
    public bool IsPointOnHandle(CadPoint point, CadRect rect, double tolerance)
    {
        return GetHandleAtPoint(point, rect) != ResizeHandle.None;
    }

    private ResizeHandle GetHandleAtPoint(CadPoint point, CadRect rect)
    {
        double tol = 10.0;
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

    private CadRect ResizeRect(CadRect original, CadPoint dragStart, CadPoint currentPos, ResizeHandle handle)
    {
        double dx = currentPos.X - dragStart.X;
        double dy = currentPos.Y - dragStart.Y;
        double x = original.X, y = original.Y, w = original.Width, h = original.Height;

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