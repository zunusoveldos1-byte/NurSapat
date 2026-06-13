using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Tools;
using NurSapat.Cad.Tools.Tools;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.ViewModels;
using ToolPointerEvent = NurSapat.Cad.Tools.CadPointerEvent;

namespace NurSapat.Cad.UI.Controls;

public sealed class CadCanvasControl : Control
{
    private static readonly IBrush CanvasBrush = Brushes.White;
    private static readonly IBrush AccentBrush = new SolidColorBrush(Color.Parse("#1A73E8"));
    private static readonly Pen MinorGridPen = new(new SolidColorBrush(Color.FromArgb(92, 239, 239, 239)), 1);
    private static readonly Pen MajorGridPen = new(new SolidColorBrush(Color.FromArgb(150, 224, 224, 224)), 1);
    private static readonly Pen DimensionPen = new(AccentBrush, 1);

    private Point? _lastPanPoint;
    private bool _isPanning;

    public CadEditorViewModel? Editor
    {
        get => GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public static readonly StyledProperty<CadEditorViewModel?> EditorProperty =
        AvaloniaProperty.Register<CadCanvasControl, CadEditorViewModel?>(nameof(Editor));

    public CadCanvasControl()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Editor == null) return;

        Editor.Viewport.Resize(Bounds.Width, Bounds.Height);
        context.FillRectangle(CanvasBrush, Bounds);

        if (Editor.IsGridEnabled) DrawGrid(context);

        // Отрисовка прямоугольников
        foreach (var rect in Editor.Document.Rectangles)
        {
            var pen = Editor.SelectedRectangle == rect
                ? new Pen(AccentBrush, 2)
                : new Pen(new SolidColorBrush(Color.Parse("#1F2937")), 1);
            DrawRect(context, rect, new SolidColorBrush(Color.FromArgb(42, 26, 115, 232)), pen);
        }

        if (Editor.DesignElements.Count > 0)
        {
            GeometryElementRenderer.DrawAll(context, Editor, Editor.DesignElements);
        }

        // RectangleTool: призрак + размеры
        if (Editor.ActiveTool is RectangleTool rectTool && rectTool.PreviewRect is CadRect preview)
        {
            DrawRect(context, preview, new SolidColorBrush(Color.FromArgb(36, 26, 115, 232)), new Pen(AccentBrush, 1));
            DrawRectangleDimensions(context, preview, isTemporary: true);
        }

        if (Editor.ActiveTool is MullionPlacementTool mullionTool && mullionTool.PreviewLine is CadLine mullionLine)
        {
            DrawLine(context, mullionLine, new Pen(AccentBrush, 2));
        }

        if (Editor.ActiveTool is DesignRectPlacementTool designRectTool && designRectTool.PreviewRect is CadRect designPreview)
        {
            DrawRect(context, designPreview, new SolidColorBrush(Color.FromArgb(48, 37, 99, 235)), new Pen(AccentBrush, 1.5));
        }

        // SelectionTool: рамка выделения + ручки
        if (Editor.ActiveTool is SelectionTool selectionTool)
        {
            if (selectionTool.SelectionRect is CadRect selectionRect)
            {
                DrawRect(context, selectionRect, new SolidColorBrush(Color.FromArgb(42, 26, 115, 232)),
                         new Pen(new SolidColorBrush(Color.Parse("#1A73E8")), 1));
            }

            var selectedRect = Editor.SelectedRectangle;
            if (selectedRect != null)
            {
                DrawResizeHandles(context, selectedRect.Value);
            }
        }
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (Editor == null || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            base.OnPointerWheelChanged(e);
            return;
        }

        var p = e.GetPosition(this);
        Editor.Viewport.ZoomAt(new ScreenPoint(p.X, p.Y), e.Delta.Y > 0 ? 1.12 : 1 / 1.12);
        Editor.UpdateCursor(Editor.Viewport.ScreenToWorld(new ScreenPoint(p.X, p.Y)));
        e.Handled = true;
        this.InvalidateVisual();
    }

    private void DrawRectangleDimensions(DrawingContext context, CadRect rect, bool isTemporary)
    {
        DrawHorizontalDimension(context,
            rect.X, rect.X + rect.Width,
            rect.Y + (isTemporary ? 20 : 15), rect.Y,
            $"{rect.Width:F0} мм", false);

        DrawVerticalDimension(context,
            rect.X - (isTemporary ? 20 : 15),
            rect.Y, rect.Y - rect.Height,
            rect.X,
            $"{rect.Height:F0} мм", false);
    }

    private void DrawResizeHandles(DrawingContext context, CadRect rect)
    {
        if (Editor?.ActiveTool is not SelectionTool selectionTool) return;

        var handles = selectionTool.GetHandlePositions(rect);
        var pen = new Pen(Brushes.White, 2);
        var brush = new SolidColorBrush(Colors.Black);

        foreach (var h in handles)
        {
            var sp = Editor.Viewport.WorldToScreen(h);
            var handleRect = new Rect(sp.X - 4, sp.Y - 4, 8, 8);
            context.DrawRectangle(brush, pen, handleRect);
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var point = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;

        if (properties.IsMiddleButtonPressed || e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _isPanning = true;
            _lastPanPoint = point;
            e.Pointer.Capture(this);
            return;
        }

        var cadEvent = ToCadEvent(point, e.KeyModifiers);
        if (Editor?.DesignPlacement?.Invoke(cadEvent.WorldPoint) == true)
        {
            e.Pointer.Capture(this);
            InvalidateVisual();
            return;
        }

        Editor?.ActiveTool.OnMouseDown(Editor.CreateToolContext(), cadEvent);
        e.Pointer.Capture(this);

        if (Editor?.ActiveTool?.Id == "select")
        {
            var worldPoint = Editor.Viewport.ScreenToWorld(new ScreenPoint(point.X, point.Y));
            var found = false;
            foreach (var rect in Editor.Document.Rectangles)
            {
                if (rect.Contains(worldPoint))
                {
                    Editor.SelectedRectangle = rect;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Editor.SelectedRectangle = null;
            }
        }

        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (Editor == null) return;

        var point = e.GetPosition(this);
        var worldPoint = Editor.Viewport.ScreenToWorld(new ScreenPoint(point.X, point.Y));
        Editor.UpdateCursor(worldPoint);

        if (_isPanning && _lastPanPoint is Point last)
        {
            Editor.Viewport.Pan(new CadVector(point.X - last.X, point.Y - last.Y));
            _lastPanPoint = point;
            InvalidateVisual();
            return;
        }

        Editor.ActiveTool.OnMouseMove(Editor.CreateToolContext(), ToCadEvent(point, e.KeyModifiers));
        Editor.RefreshSelection();
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _isPanning = false;
        _lastPanPoint = null;

        if (Editor != null)
        {
            Editor.ActiveTool.OnMouseUp(Editor.CreateToolContext(), ToCadEvent(e.GetPosition(this), e.KeyModifiers));
            Editor.RefreshSelection();
        }

        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (Editor == null) return;

        if (e.Key == Key.Delete)
        {
            Editor.DeleteSelectedCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Z)
        {
            Editor.UndoCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Y)
        {
            Editor.RedoCommand.Execute(null);
            e.Handled = true;
        }
    }

    private ToolPointerEvent ToCadEvent(Point point, KeyModifiers modifiers)
    {
        var screen = new ScreenPoint(point.X, point.Y);
        return new ToolPointerEvent(
            Editor!.Viewport.ScreenToWorld(screen),
            screen,
            modifiers.HasFlag(KeyModifiers.Control),
            modifiers.HasFlag(KeyModifiers.Shift),
            modifiers.HasFlag(KeyModifiers.Alt));
    }

    private void DrawGrid(DrawingContext context)
    {
        var viewport = Editor!.Viewport;
        var majorStep = viewport.ChooseGridStep();
        var minorStep = majorStep / 5;
        var topLeft = viewport.ScreenToWorld(new ScreenPoint(0, 0));
        var bottomRight = viewport.ScreenToWorld(new ScreenPoint(Bounds.Width, Bounds.Height));
        var minX = Math.Min(topLeft.X, bottomRight.X);
        var maxX = Math.Max(topLeft.X, bottomRight.X);
        var minY = Math.Min(topLeft.Y, bottomRight.Y);
        var maxY = Math.Max(topLeft.Y, bottomRight.Y);

        if (minorStep * viewport.ZoomFactor >= 8)
        {
            DrawGridLines(context, minorStep, minX, maxX, minY, maxY, MinorGridPen);
        }

        DrawGridLines(context, majorStep, minX, maxX, minY, maxY, MajorGridPen);
    }

    private void DrawGridLines(DrawingContext context, double step, double minX, double maxX, double minY, double maxY, Pen pen)
    {
        for (var x = Math.Floor(minX / step) * step; x <= maxX; x += step)
        {
            DrawLine(context, new CadLine(new CadPoint(x, minY), new CadPoint(x, maxY)), pen);
        }

        for (var y = Math.Floor(minY / step) * step; y <= maxY; y += step)
        {
            DrawLine(context, new CadLine(new CadPoint(minX, y), new CadPoint(maxX, y)), pen);
        }
    }

    private void DrawHorizontalDimension(DrawingContext context, double x1, double x2, double y, double extensionToY, string label, bool largeText)
    {
        var p1 = Editor!.Viewport.WorldToScreen(new CadPoint(x1, y));
        var p2 = Editor.Viewport.WorldToScreen(new CadPoint(x2, y));
        var e1 = Editor.Viewport.WorldToScreen(new CadPoint(x1, extensionToY));
        var e2 = Editor.Viewport.WorldToScreen(new CadPoint(x2, extensionToY));

        context.DrawLine(DimensionPen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
        context.DrawLine(DimensionPen, new Point(p1.X, p1.Y - 11), new Point(p1.X, e1.Y));
        context.DrawLine(DimensionPen, new Point(p2.X, p2.Y - 11), new Point(p2.X, e2.Y));
        DrawArrow(context, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
        DrawArrow(context, new Point(p2.X, p2.Y), new Point(p1.X, p1.Y));
        DrawCenteredText(context, label, new Point((p1.X + p2.X) / 2, p1.Y - (largeText ? 18 : 14)), AccentBrush, largeText ? 18 : 15);
    }

    private void DrawVerticalDimension(DrawingContext context, double x, double y1, double y2, double extensionToX, string label, bool largeText)
    {
        var p1 = Editor!.Viewport.WorldToScreen(new CadPoint(x, y1));
        var p2 = Editor.Viewport.WorldToScreen(new CadPoint(x, y2));
        var e1 = Editor.Viewport.WorldToScreen(new CadPoint(extensionToX, y1));
        var e2 = Editor.Viewport.WorldToScreen(new CadPoint(extensionToX, y2));

        context.DrawLine(DimensionPen, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
        context.DrawLine(DimensionPen, new Point(p1.X - 11, p1.Y), new Point(e1.X, p1.Y));
        context.DrawLine(DimensionPen, new Point(p2.X - 11, p2.Y), new Point(e2.X, p2.Y));
        DrawArrow(context, new Point(p1.X, p1.Y), new Point(p2.X, p2.Y));
        DrawArrow(context, new Point(p2.X, p2.Y), new Point(p1.X, p1.Y));
        DrawCenteredText(context, label, new Point(p1.X - (largeText ? 24 : 18), p1.Y - (largeText ? 18 : 14)), AccentBrush, largeText ? 18 : 15);
    }

    private static void DrawArrow(DrawingContext context, Point tip, Point toward)
    {
        var dx = toward.X - tip.X;
        var dy = toward.Y - tip.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 0.1) return;

        var ux = dx / length;
        var uy = dy / length;
        var px = -uy;
        var py = ux;
        var size = 8;
        var a = new Point(tip.X + ux * size + px * size * 0.45, tip.Y + uy * size + py * size * 0.45);
        var b = new Point(tip.X + ux * size - px * size * 0.45, tip.Y + uy * size - py * size * 0.45);
        context.DrawLine(DimensionPen, tip, a);
        context.DrawLine(DimensionPen, tip, b);
    }

    private void DrawRect(DrawingContext context, CadRect rect, IBrush? fill, Pen pen)
    {
        var topLeft = Editor!.Viewport.WorldToScreen(new CadPoint(rect.X, rect.Y));
        var bottomRight = Editor.Viewport.WorldToScreen(new CadPoint(rect.X + rect.Width, rect.Y - rect.Height));
        context.DrawRectangle(fill, pen, new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y)));
    }

    private void DrawLine(DrawingContext context, CadLine line, Pen pen)
    {
        var a = Editor!.Viewport.WorldToScreen(line.Start);
        var b = Editor.Viewport.WorldToScreen(line.End);
        context.DrawLine(pen, new Point(a.X, a.Y), new Point(b.X, b.Y));
    }

    private static void DrawCenteredText(DrawingContext context, string text, Point center, IBrush brush, double size)
    {
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            size,
            brush);
        context.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
    }
}