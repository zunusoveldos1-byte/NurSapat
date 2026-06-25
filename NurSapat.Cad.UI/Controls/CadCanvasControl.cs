using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Tools;
using NurSapat.Cad.Tools.Tools;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using ToolPointerEvent = NurSapat.Cad.Tools.CadPointerEvent;

namespace NurSapat.Cad.UI.Controls;

public sealed class CadCanvasControl : Control
{
    private static bool IsDragCreateShapeTool(WindowToolMode mode) =>
        mode is WindowToolMode.RealFrame
            or WindowToolMode.FakeFrame
            or WindowToolMode.FakeFrameTriangle
            or WindowToolMode.FakeFrameArch
            or WindowToolMode.FakeFrameCircle;

    private static bool IsImpostSplitTool(WindowToolMode mode) =>
        mode is WindowToolMode.FakeVerticalImpost or WindowToolMode.FakeHorizontalImpost
            or WindowToolMode.RealVerticalImpost or WindowToolMode.RealHorizontalImpost
            or WindowToolMode.DoubleVerticalImpost or WindowToolMode.DoubleHorizontalImpost
            or WindowToolMode.SlantedImpost;

    private static readonly IBrush CanvasBrush = Brushes.White;
    private static readonly IBrush CadStrokeBrush = new SolidColorBrush(Color.Parse("#AEAEAE"));
    private static readonly Pen FramePen = new(CadStrokeBrush, 1.5);
    private static readonly Pen MiterPreviewPen = new(new SolidColorBrush(FrameVolumeRenderer.MiterColor), 1.2);
    private static readonly Pen SelectedFramePen = new(CadStrokeBrush, 2);
    private static readonly Pen FramePreviewPen = new(
        new SolidColorBrush(Color.Parse("#0078D7")),
        1.5,
        new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly Pen MinorGridPen = new(new SolidColorBrush(Color.FromArgb(92, 239, 239, 239)), 1);
    private static readonly Pen MajorGridPen = new(new SolidColorBrush(Color.FromArgb(150, 224, 224, 224)), 1);
    private static readonly IBrush AxisBrush = new SolidColorBrush(Color.Parse("#64748B"));
    private static readonly Pen AxisPen = new(AxisBrush, 1.2);

    private Point? _lastPanPoint;
    private bool _isPanning;
    private Point _startPoint;
    private bool _isDrawingFrame;

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

        var suppressCommittedFrame = Editor.IsShapePlacementActive;

        if (!suppressCommittedFrame && Editor.CanvasElements.Count == 0)
        {
            foreach (var rect in Editor.Document.Rectangles)
            {
                var originY = rect.Y - rect.Height;
                FrameVolumeRenderer.Draw(
                    context,
                    Editor,
                    rect.X,
                    originY,
                    rect.Width,
                    rect.Height,
                    Editor.FrameThickness,
                    dashed: !Editor.HasRealFrameOnCanvas);
                DrawRectangleDimensions(context, rect, isTemporary: false);
            }
        }

        if (!suppressCommittedFrame && Editor.CanvasElements.Count > 0)
        {
            GeometryElementRenderer.DrawAll(context, Editor, Editor.DesignElements);
        }

        if (!suppressCommittedFrame && Editor.FrameRoot != null && Editor.HasFrameOnCanvas)
        {
            FrameNodeRenderer.DrawImposts(context, Editor, Editor.FrameRoot);
        }

        if (!suppressCommittedFrame && Editor.CanvasElements.Count > 0)
        {
            SashElementRenderer.DrawAll(context, Editor, Editor.CanvasElements);
        }

        if (Editor.SashDragPreview is { } sashPreview)
        {
            SashElementRenderer.DrawPreview(context, Editor, sashPreview);
        }

        if (Editor.RealFramePreview is CadRect framePreview && Editor.IsShapePlacementActive)
        {
            if (Editor.CurrentToolMode == WindowToolMode.RealFrame)
            {
                var originY = framePreview.Y - framePreview.Height;
                FrameVolumeRenderer.Draw(
                    context,
                    Editor,
                    framePreview.X,
                    originY,
                    framePreview.Width,
                    framePreview.Height,
                    Editor.FrameThickness,
                    dashed: false);
            }
            else
            {
                GeometryElementRenderer.DrawPlacementPreview(context, Editor, Editor.CurrentToolMode, framePreview);
            }

            DrawRectangleDimensions(context, framePreview, isTemporary: true);
        }

        DrawHandlePreview(context);

        // RectangleTool: призрак + размеры
        if (Editor.ActiveTool is RectangleTool rectTool && rectTool.PreviewRect is CadRect preview)
        {
            DrawRect(context, preview, null, FramePen);
            DrawRectangleDimensions(context, preview, isTemporary: true);
        }

        // Превью одиночного импоста внутри светового проёма
        if (Editor.ActiveTool is MullionPlacementTool mullionTool)
        {
            foreach (var rect in mullionTool.PreviewRects)
            {
                DrawRect(context, rect, null, FramePen);
            }
        }

        if (Editor.ActiveTool is DesignRectPlacementTool designRectTool && designRectTool.PreviewRect is CadRect designPreview)
        {
            DrawRect(context, designPreview, null, FramePen);
        }

        if (Editor.ActiveTool is SelectionTool selectionTool)
        {
            if (selectionTool.SelectionRect is CadRect selectionRect)
            {
                DrawRect(context, selectionRect, null, SelectedFramePen);
            }
        }

        DrawAxisIndicator(context);
    }

    private void DrawHandlePreview(DrawingContext context)
    {
        if (Editor?.HandlePreviewSashId is not Guid sashId)
        {
            return;
        }

        var sash = Editor.CanvasElements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
        if (sash?.HingeSide is not SashHingeSide hingeSide)
        {
            return;
        }

        SashElementRenderer.DrawHandleMarker(context, Editor, sash, hingeSide, preview: true);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (Editor == null)
        {
            base.OnPointerWheelChanged(e);
            return;
        }

        if (Editor.IsViewOnlyMode || e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            var p = e.GetPosition(this);
            Editor.Viewport.ZoomAt(new ScreenPoint(p.X, p.Y), e.Delta.Y > 0 ? 1.12 : 1 / 1.12);
            Editor.UpdateCursor(Editor.Viewport.ScreenToWorld(new ScreenPoint(p.X, p.Y)));
            Editor.RequestSceneRefresh();
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        base.OnPointerWheelChanged(e);
    }

    private void DrawRectangleDimensions(DrawingContext context, CadRect rect, bool isTemporary)
    {
        if (Editor?.Viewport == null || rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        try
        {
            var topLeft = Editor.Viewport.WorldToScreen(new CadPoint(rect.X, rect.Y));
            var topRight = Editor.Viewport.WorldToScreen(new CadPoint(rect.X + rect.Width, rect.Y));
            var bottomLeft = Editor.Viewport.WorldToScreen(new CadPoint(rect.X, rect.Y - rect.Height));
            var bottomRight = Editor.Viewport.WorldToScreen(new CadPoint(rect.X + rect.Width, rect.Y - rect.Height));

            var left = Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X));
            var right = Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X));
            var top = Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y));
            var bottom = Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y));

            GeometryElementRenderer.DrawScreenDimensions(
                context,
                new Rect(left, top, right - left, bottom - top),
                $"{rect.Width:F1}",
                $"{rect.Height:F1}");
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("DrawRectangleDimensions failed", ex);
        }
    }

    private void DrawAxisIndicator(DrawingContext context)
    {
        const double x0 = 14;
        var y0 = Math.Max(14, Bounds.Height - 14);
        const double len = 22;

        context.DrawLine(AxisPen, new Point(x0, y0), new Point(x0 + len, y0));
        context.DrawLine(AxisPen, new Point(x0 + len - 4, y0 - 3), new Point(x0 + len, y0));
        context.DrawLine(AxisPen, new Point(x0 + len - 4, y0 + 3), new Point(x0 + len, y0));

        context.DrawLine(AxisPen, new Point(x0, y0), new Point(x0, y0 - len));
        context.DrawLine(AxisPen, new Point(x0 - 3, y0 - len + 4), new Point(x0, y0 - len));
        context.DrawLine(AxisPen, new Point(x0 + 3, y0 - len + 4), new Point(x0, y0 - len));

        var xLabel = new FormattedText("X", System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, Typeface.Default, 10, AxisBrush);
        var yLabel = new FormattedText("Y", System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, Typeface.Default, 10, AxisBrush);
        context.DrawText(xLabel, new Point(x0 + len + 3, y0 - xLabel.Height / 2));
        context.DrawText(yLabel, new Point(x0 - yLabel.Width / 2, y0 - len - 12));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var point = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;

        if (Editor?.IsViewOnlyMode == true)
        {
            if (properties.IsMiddleButtonPressed
                || properties.IsLeftButtonPressed
                || e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                _isPanning = true;
                _lastPanPoint = point;
                e.Pointer.Capture(this);
            }

            e.Handled = true;
            return;
        }

        if (properties.IsMiddleButtonPressed || e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            _isPanning = true;
            _lastPanPoint = point;
            e.Pointer.Capture(this);
            return;
        }

        if (Editor != null && IsDragCreateShapeTool(Editor.CurrentToolMode))
        {
            if (properties.IsRightButtonPressed)
            {
                e.Handled = true;
                _ = Editor.BeginShapeSizeEntryFromRightClickAsync();
                InvalidateVisual();
                return;
            }

            if (properties.IsLeftButtonPressed)
            {
                _startPoint = point;
                _isDrawingFrame = true;
                var worldPoint = Editor.Viewport.ScreenToWorld(new ScreenPoint(point.X, point.Y));
                Editor.BeginShapePlacement(worldPoint);
                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        var cadEvent = ToCadEvent(point, e.KeyModifiers);
        if (Editor?.TryBeginPointerInteraction(cadEvent.WorldPoint) == true)
        {
            e.Pointer.Capture(this);
            InvalidateVisual();
            return;
        }

        if (Editor != null && IsImpostSplitTool(Editor.CurrentToolMode) && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            _ = HandleImpostClickAsync(cadEvent.WorldPoint);
            return;
        }

        if (Editor?.TryHandleCanvasClick(cadEvent.WorldPoint) == true)
        {
            e.Pointer.Capture(this);
            InvalidateVisual();
            return;
        }

        if (Editor?.DesignPlacement?.Invoke(cadEvent.WorldPoint) == true)
        {
            e.Pointer.Capture(this);
            InvalidateVisual();
            return;
        }

        Editor?.ActiveTool?.OnMouseDown(Editor.CreateToolContext(), cadEvent);
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

        if (_isDrawingFrame)
        {
            Editor.UpdateShapePlacementPreview(worldPoint);
            InvalidateVisual();
            return;
        }

        Editor.UpdatePointerInteraction(worldPoint);

        if (_isPanning && _lastPanPoint is Point last)
        {
            Editor.Viewport.Pan(new CadVector(point.X - last.X, point.Y - last.Y));
            _lastPanPoint = point;
            Editor.RequestSceneRefresh();
            InvalidateVisual();
            return;
        }

        Editor?.ActiveTool?.OnMouseMove(Editor.CreateToolContext(), ToCadEvent(point, e.KeyModifiers));
        Editor?.RefreshSelection();
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _isPanning = false;
        _lastPanPoint = null;

        if (_isDrawingFrame)
        {
            try
            {
                if (e.InitialPressMouseButton != MouseButton.Left)
                {
                    Editor?.CancelShapePlacement();
                    return;
                }

                if (Editor != null)
                {
                    var releasePoint = e.GetPosition(this);
                    var worldPoint = Editor.Viewport.ScreenToWorld(new ScreenPoint(releasePoint.X, releasePoint.Y));
                    Editor.TryCompleteShapeDrag(worldPoint);
                }
            }
            finally
            {
                _isDrawingFrame = false;
                e.Pointer.Capture(null);
                Editor?.RequestSceneRefresh();
                InvalidateVisual();
            }

            return;
        }

        if (Editor != null)
        {
            var releasePoint = e.GetPosition(this);
            var worldPoint = Editor.Viewport.ScreenToWorld(new ScreenPoint(releasePoint.X, releasePoint.Y));
            if (Editor.TryEndPointerInteraction(worldPoint))
            {
                e.Pointer.Capture(null);
                InvalidateVisual();
                return;
            }

            Editor.ActiveTool?.OnMouseUp(Editor.CreateToolContext(), ToCadEvent(releasePoint, e.KeyModifiers));
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

    private async Task HandleImpostClickAsync(CadPoint worldPoint)
    {
        if (Editor == null)
        {
            return;
        }

        await Editor.TryHandleImpostClickAsync(worldPoint);
        InvalidateVisual();
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