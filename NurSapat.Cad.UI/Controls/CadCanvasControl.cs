using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Parametric;
using NurSapat.Cad.Tools;
using NurSapat.Cad.UI.ViewModels;
using ToolPointerEvent = NurSapat.Cad.Tools.CadPointerEvent;

namespace NurSapat.Cad.UI.Controls;

public sealed class CadCanvasControl : Control
{
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
        if (Editor == null)
        {
            return;
        }

        Editor.Viewport.Resize(Bounds.Width, Bounds.Height);
        context.FillRectangle(Brushes.White, Bounds);

        DrawGrid(context);
        DrawFrame(context);
        DrawSections(context, Editor.Document.Structure.RootSection);

        foreach (var rect in Editor.Document.Rectangles)
        {
            var pen = Editor.SelectedRectangle == rect
                ? new Pen(Brushes.Red, 3)
                : new Pen(Brushes.Black, 1);
            DrawRect(context, rect, new SolidColorBrush(Color.Parse("#3B82F6")), pen);
        }

        if (Editor.ActiveTool is RectangleTool rectTool && rectTool.PreviewRect is CadRect preview)
        {
            var fill = new SolidColorBrush(Color.FromArgb(50, 59, 130, 246));
            var pen = new Pen(Brushes.Blue, 2);
            DrawRect(context, preview, fill, pen);
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
        e.Handled = true;
        InvalidateVisual();
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

        Editor?.ActiveTool.OnMouseDown(Editor.CreateToolContext(), ToCadEvent(point, e.KeyModifiers));
        e.Pointer.Capture(this);

        // Логика выделения прямоугольника (SelectionTool)
        if (Editor?.ActiveTool?.Id == "select")
        {
            var worldPoint = Editor.Viewport.ScreenToWorld(new ScreenPoint(point.X, point.Y));
            var found = false;
            foreach (var rect in Editor.Document.Rectangles)
            {
                // Если в CadRect нет Contains, можно использовать простую проверку:
                if (rect.Contains(worldPoint)) // Убедитесь, что у CadRect есть Contains
                {
                    Editor.SelectedRectangle = rect;
                    found = true;
                    break;
                }
            }
            if (!found) Editor.SelectedRectangle = null;
        }

        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (Editor == null) return;

        var point = e.GetPosition(this);
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
            Editor.PublishOrderState();
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
        else if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.S)
        {
            Editor.SaveTemplateCommand.Execute(null);
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
        var step = viewport.ChooseGridStep();
        var topLeft = viewport.ScreenToWorld(new ScreenPoint(0, 0));
        var bottomRight = viewport.ScreenToWorld(new ScreenPoint(Bounds.Width, Bounds.Height));
        var minX = Math.Floor(Math.Min(topLeft.X, bottomRight.X) / step) * step;
        var maxX = Math.Ceiling(Math.Max(topLeft.X, bottomRight.X) / step) * step;
        var minY = Math.Floor(Math.Min(topLeft.Y, bottomRight.Y) / step) * step;
        var maxY = Math.Ceiling(Math.Max(topLeft.Y, bottomRight.Y) / step) * step;
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#303640")), 1);

        for (var x = minX; x <= maxX; x += step)
        {
            DrawLine(context, new CadLine(new CadPoint(x, minY), new CadPoint(x, maxY)), gridPen);
        }

        for (var y = minY; y <= maxY; y += step)
        {
            DrawLine(context, new CadLine(new CadPoint(minX, y), new CadPoint(maxX, y)), gridPen);
        }

        DrawLine(context, new CadLine(new CadPoint(minX, 0), new CadPoint(maxX, 0)), new Pen(Brushes.IndianRed, 1.5));
        DrawLine(context, new CadLine(new CadPoint(0, minY), new CadPoint(0, maxY)), new Pen(Brushes.SeaGreen, 1.5));
    }

    private void DrawFrame(DrawingContext context)
    {
        var frame = Editor!.Document.Structure.OuterFrame;
        DrawRect(context, frame.OuterBounds, null, new Pen(new SolidColorBrush(Color.Parse("#F3F4F6")), 4));
        DrawRect(context, frame.InnerBounds, null, new Pen(new SolidColorBrush(Color.Parse("#9CA3AF")), 2));
    }

    private void DrawSections(DrawingContext context, WindowSection section)
    {
        if (section.SplitImpost != null)
        {
            var selectedImpost = Editor!.Document.Structure.SelectedImpostOwnerId == section.Id;
            var pen = new Pen(new SolidColorBrush(Color.Parse(selectedImpost ? "#FBBF24" : "#E5E7EB")), section.SplitImpost.ProfileWidth * Editor.Viewport.ZoomFactor);
            DrawLine(context, section.SplitImpost.Axis, pen);
            foreach (var child in section.Children)
            {
                DrawSections(context, child);
            }
            return;
        }

        var selected = Editor!.Document.Structure.SelectedSectionId == section.Id;
        DrawRect(context, section.Bounds, null, new Pen(new SolidColorBrush(selected ? Color.Parse("#38BDF8") : Color.Parse("#4B5563")), selected ? 2 : 1));

        if (section.Fill.Kind == SectionFillKind.Sash)
        {
            DrawRect(context, section.GetSashRect(Editor.Document.Structure.Options), null, new Pen(new SolidColorBrush(Color.Parse("#60A5FA")), 3));
            DrawHardwareMarkers(context, section);
        }

        if (section.Fill.Kind is SectionFillKind.Glazing or SectionFillKind.Sash or SectionFillKind.Corrugation)
        {
            var glassRect = section.GetGlassRect(Editor.Document.Structure.Options);
            var fill = section.Fill.Kind == SectionFillKind.Corrugation
                ? new SolidColorBrush(Color.Parse("#334155"))
                : new SolidColorBrush(Color.Parse("#2238BDF8"));
            DrawRect(context, glassRect, fill, new Pen(new SolidColorBrush(Color.Parse("#93C5FD")), 1));
            DrawCenteredLabel(context, section.GetFactoryLabel(Editor.Document.Structure.Options), glassRect);
        }
    }

    private void DrawHardwareMarkers(DrawingContext context, WindowSection section)
    {
        var sashRect = section.GetSashRect(Editor!.Document.Structure.Options);
        var x = sashRect.X + sashRect.Width - 30;
        var y = sashRect.Y - sashRect.Height / 2;
        var center = Editor.Viewport.WorldToScreen(new CadPoint(x, y));
        context.DrawEllipse(Brushes.Gold, new Pen(Brushes.Black, 1), new Point(center.X, center.Y), 6, 6);

        var offset = 16;
        foreach (var modifier in section.Fill.HardwareModifiers)
        {
            DrawText(context, modifier.ToString(), new Point(center.X + 10, center.Y + offset), Brushes.Gold, 10);
            offset += 14;
        }
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

    private void DrawCenteredLabel(DrawingContext context, string label, CadRect rect)
    {
        var center = Editor!.Viewport.WorldToScreen(new CadPoint(rect.X + rect.Width / 2, rect.Y - rect.Height / 2));
        DrawText(context, label, new Point(center.X - 80, center.Y - 8), Brushes.White, 12);
    }

    private static void DrawText(DrawingContext context, string text, Point origin, IBrush brush, double size)
    {
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            size,
            brush);
        context.DrawText(formatted, origin);
    }
}