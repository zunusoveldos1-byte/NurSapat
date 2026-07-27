using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Tools;
using NurSapat.Cad.Tools.Tools;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.Services;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using System.Linq;
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
            or WindowToolMode.SlantedImpost
            or WindowToolMode.MullionVertical or WindowToolMode.MullionHorizontal;

    private static bool IsRealImpostDragPlacementTool(WindowToolMode mode) =>
        mode is WindowToolMode.RealVerticalImpost or WindowToolMode.RealHorizontalImpost;

    private static readonly IBrush CanvasBrush = Brushes.White;

    /// <summary>Контур превью/выбранной рамы — общая тема цвета контура чертежа.</summary>
    private static readonly IBrush CadStrokeBrush = CadContourTheme.Brush;
    private static readonly Pen FramePen = new(CadStrokeBrush, 1.5);
    private static readonly Pen MullionPreviewPen = new(
        new SolidColorBrush(Color.FromArgb(200, 255, 4, 4)),
        2);
    private static readonly IBrush MullionPreviewFill = new SolidColorBrush(Color.FromArgb(128, 255, 4, 4));
    private static readonly Pen MiterPreviewPen = new(CadContourTheme.Brush, 1.2);
    private static readonly Pen SelectedFramePen = new(CadStrokeBrush, 2);
    private static readonly Pen DummySelectedFramePen = new(
        new SolidColorBrush(FrameVolumeRenderer.DummySelectionHighlightColor),
        2,
        new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly Pen FramePreviewPen = new(
        new SolidColorBrush(Color.Parse("#0078D7")),
        1.5,
        new DashStyle(new double[] { 4, 3 }, 0));
    private static readonly Pen MinorGridPen = new(new SolidColorBrush(Color.FromArgb(92, 239, 239, 239)), 1);
    private static readonly Pen MajorGridPen = new(new SolidColorBrush(Color.FromArgb(150, 224, 224, 224)), 1);
    private static readonly IBrush AxisBrush = new SolidColorBrush(Color.Parse("#64748B"));
    private static readonly Pen AxisPen = new(AxisBrush, 1.2);
    private static readonly Cursor PanHandCursor = new(StandardCursorType.Hand);
    private static readonly Cursor PanDragCursor = new(StandardCursorType.SizeAll);
    private static readonly Cursor DefaultCanvasCursor = new(StandardCursorType.Arrow);

    private Point? _lastPanPoint;
    private bool _isPanning;
    private Point _startPoint;
    private bool _isDrawingFrame;
    private bool _isImpostDragPlacing;

    public CadEditorViewModel? Editor
    {
        get => GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public static readonly StyledProperty<CadEditorViewModel?> EditorProperty =
        AvaloniaProperty.Register<CadCanvasControl, CadEditorViewModel?>(nameof(Editor));

    public static readonly StyledProperty<bool> AutoFitOnResizeProperty =
        AvaloniaProperty.Register<CadCanvasControl, bool>(nameof(AutoFitOnResize), false);

    public bool AutoFitOnResize
    {
        get => GetValue(AutoFitOnResizeProperty);
        set => SetValue(AutoFitOnResizeProperty, value);
    }

    public static readonly StyledProperty<bool> IsThumbnailPreviewProperty =
        AvaloniaProperty.Register<CadCanvasControl, bool>(nameof(IsThumbnailPreview), false);

    public bool IsThumbnailPreview
    {
        get => GetValue(IsThumbnailPreviewProperty);
        set => SetValue(IsThumbnailPreviewProperty, value);
    }

    private Size _lastAutoFitBounds;
    private bool _invalidateVisualScheduled;

    public void ResetThumbnailAutoFit() => _lastAutoFitBounds = default;

    public CadCanvasControl()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    static CadCanvasControl()
    {
        EditorProperty.Changed.AddClassHandler<CadCanvasControl>((control, e) =>
        {
            if (e.OldValue is CadEditorViewModel oldEditor)
            {
                oldEditor.PropertyChanged -= control.OnEditorPropertyChanged;
                oldEditor.SceneInvalidated -= control.OnSceneInvalidated;
            }

            if (e.NewValue is CadEditorViewModel newEditor)
            {
                newEditor.PropertyChanged += control.OnEditorPropertyChanged;
                newEditor.SceneInvalidated += control.OnSceneInvalidated;
            }

            control._lastAutoFitBounds = default;
            control.UpdateInteractionCursor(control._isPanning);
            control.ScheduleInvalidateVisual();
        });
    }

    private void ScheduleInvalidateVisual()
    {
        if (_invalidateVisualScheduled)
        {
            return;
        }

        _invalidateVisualScheduled = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                _invalidateVisualScheduled = false;
                InvalidateVisual();
            },
            DispatcherPriority.Render);
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CadEditorViewModel.CurrentToolMode))
        {
            _isDrawingFrame = false;
            UpdateInteractionCursor(_isPanning);
        }

        // Призрак створки/фурнитуры и смена открывания — мгновенная перерисовка.
        if (e.PropertyName is nameof(CadEditorViewModel.SashHardwarePreview)
            or nameof(CadEditorViewModel.SashDragPreview)
            or nameof(CadEditorViewModel.HandlePreviewSashId)
            or nameof(CadEditorViewModel.StatusText))
        {
            ScheduleInvalidateVisual();
        }
    }

    private void OnSceneInvalidated(object? sender, EventArgs e)
    {
        _lastAutoFitBounds = default;
        ScheduleInvalidateVisual();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _lastAutoFitBounds = default;
        if (!IsThumbnailPreview && Editor is not null && Bounds.Width > 0 && Bounds.Height > 0)
        {
            Editor.NotifyDrawingCanvasSized(Bounds.Width, Bounds.Height);
        }

        ScheduleInvalidateVisual();
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (AutoFitOnResize)
        {
            _lastAutoFitBounds = default;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (IsThumbnailPreview && Width > 0 && Height > 0)
        {
            var width = double.IsPositiveInfinity(availableSize.Width)
                ? Width
                : Math.Min(Width, availableSize.Width);
            var height = double.IsPositiveInfinity(availableSize.Height)
                ? Height
                : Math.Min(Height, availableSize.Height);
            return new Size(Math.Max(0, width), Math.Max(0, height));
        }

        return base.MeasureOverride(availableSize);
    }

    private void UpdateInteractionCursor(bool isDragging)
    {
        if (Editor?.CurrentToolMode == WindowToolMode.PanView)
        {
            Cursor = isDragging ? PanDragCursor : PanHandCursor;
            return;
        }

        Cursor = DefaultCanvasCursor;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        UpdateInteractionCursor(_isPanning);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Cursor = DefaultCanvasCursor;
        if (!IsThumbnailPreview)
        {
            Editor?.ClearCanvasHoverHint();
        }
    }

    private bool ShouldDrawDimensions => !IsThumbnailPreview && Editor is { ShowDimensions: true };

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (Editor == null) return;

        Editor.ClearDimensionHits();
        if (IsThumbnailPreview)
        {
            Editor.Viewport.Resize(Bounds.Width, Bounds.Height);
        }
        else
        {
            Editor.NotifyDrawingCanvasSized(Bounds.Width, Bounds.Height);
        }

        ScheduleAutoFitOnResize();
        context.FillRectangle(CanvasBrush, Bounds);

        if (Editor.IsGridEnabled) DrawGrid(context);

        var suppressCommittedFrame = Editor.IsShapePlacementActive;
        var canvasElements = Editor.CanvasElements.ToList();
        var constructionSelection = Editor.ConstructionSelection.ToList();
        var documentRectangles = Editor.Document.Rectangles.ToList();

        if (!suppressCommittedFrame && canvasElements.Count == 0)
        {
            foreach (var rect in documentRectangles)
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
                if (ShouldDrawDimensions)
                {
                    DrawRectangleDimensions(context, rect, isTemporary: false);
                }
            }
        }

        if (!suppressCommittedFrame && canvasElements.Count > 0)
        {
            GeometryElementRenderer.DrawAll(context, Editor, canvasElements);
        }

        if (!suppressCommittedFrame && Editor.FrameRoot != null && Editor.HasFrameOnCanvas)
        {
            FrameNodeRenderer.DrawImposts(context, Editor, Editor.FrameRoot);
            ExtremeImpostRenderer.DrawCommittedImposts(context, Editor, Editor.FrameRoot);
            if (Editor.ExtremeImpostHoverPreview is { } extremeHover)
            {
                ExtremeImpostRenderer.DrawHoverPreview(context, Editor, extremeHover);
            }

            if (Editor.TransitionProfileHoverBounds is { } transitionHover)
            {
                DrawTransitionProfileHoverPreview(context, transitionHover);
            }
        }

        if (!suppressCommittedFrame && canvasElements.Count > 0)
        {
            SashElementRenderer.DrawAll(context, Editor, canvasElements);
            // Стекло под штапиком: сначала заливка внутри проёма, затем белый профиль штапика поверх.
            GlassRenderer.DrawBeadLinkedLayer(context, Editor, canvasElements);
            GeometryElementRenderer.DrawGlazingLayer(context, Editor, canvasElements);
            GlazingBeadRenderer.DrawAll(context, Editor, canvasElements);
            DecorativeSpokeRenderer.DrawLayer(context, Editor, canvasElements);
            SashElementRenderer.DrawOpeningIndicators(context, Editor, canvasElements);
            SashElementRenderer.DrawHardwareOverlays(context, Editor, canvasElements);
            SillRenderer.DrawAttachedSill(context, Editor, Editor.FrameRoot);
        }

        if (!suppressCommittedFrame)
        {
            // Чёрный AABB-overlay до красной подсветки профиля, иначе перекрывает внешние грани.
            DrawSelectedRectangleOverlay(context);
        }

        if (!suppressCommittedFrame && constructionSelection.Count > 0)
        {
            // SelectionOverlay поверх основной геометрии и чёрных контуров рамы.
            ConstructionSelectionRenderer.DrawHighlights(context, Editor, constructionSelection);
        }

        if (Editor.SashDragPreview is { } sashPreview)
        {
            SashElementRenderer.DrawPreview(context, Editor, sashPreview);
        }

        if (Editor.SashHardwarePreview is { } hardwarePreview)
        {
            SashElementRenderer.DrawHardwarePreview(context, Editor, hardwarePreview);
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

            if (ShouldDrawDimensions)
            {
                DrawRectangleDimensions(context, framePreview, isTemporary: true);
            }
        }

        DrawHandlePreview(context);

        if (Editor.IsTopProfileHovered && Editor.HoveredTopProfile is { } hoveredTopProfile)
        {
            FrameVolumeRenderer.DrawTopProfileHighlight(context, Editor, hoveredTopProfile);
        }

        if (Editor.ShowArcVertexHandles && Editor.FrameRoot?.TopProfile is { } arcTopProfile)
        {
            FrameVolumeRenderer.DrawArcVertexHandles(context, Editor, arcTopProfile);
        }

        if (Editor.ShowVertexHandles)
        {
            FrameVolumeRenderer.DrawVertexHandles(context, Editor);
        }

        // RectangleTool: призрак + размеры
        if (Editor.ActiveTool is RectangleTool rectTool && rectTool.PreviewRect is CadRect preview)
        {
            DrawRect(context, preview, null, FramePen);
            if (ShouldDrawDimensions)
            {
                DrawRectangleDimensions(context, preview, isTemporary: true);
            }
        }

        // Превью одиночного импоста внутри светового проёма
        if (Editor.ActiveTool is MullionPlacementTool mullionTool)
        {
            foreach (var rect in mullionTool.PreviewRects)
            {
                DrawRect(context, rect, MullionPreviewFill, MullionPreviewPen);
            }
        }

        if (Editor.ImpostDragPlacementPreview is not null)
        {
            ImpostDragPlacementRenderer.Draw(context, Editor);
        }

        if (Editor.ActiveTool is DesignRectPlacementTool designRectTool && designRectTool.PreviewRect is CadRect designPreview)
        {
            DrawRect(context, designPreview, null, FramePen);
        }

        if (Editor.ActiveTool is SelectionTool selectionTool)
        {
            if (selectionTool.SelectionRect is CadRect selectionRect)
            {
                var useDummyStyle = Editor.FrameRoot?.IsDummy == true || Editor.HasFakeFrameOnCanvas;
                var fill = useDummyStyle
                    ? new SolidColorBrush(Color.FromArgb(48, 0x00, 0x78, 0xD7))
                    : new SolidColorBrush(Color.FromArgb(48, 255, 4, 4));
                DrawRect(context, selectionRect, fill, useDummyStyle ? DummySelectedFramePen : SelectedFramePen);
            }
        }

        if (!IsThumbnailPreview)
        {
            DrawAxisIndicator(context);
        }
    }

    private bool _autoFitOnResizeScheduled;

    private void ScheduleAutoFitOnResize()
    {
        if (!AutoFitOnResize || Editor is null)
        {
            return;
        }

        if (_autoFitOnResizeScheduled)
        {
            return;
        }

        _autoFitOnResizeScheduled = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                _autoFitOnResizeScheduled = false;
                TryAutoFitOnResize();
            },
            DispatcherPriority.Background);
    }

    private void TryAutoFitOnResize()
    {
        if (!AutoFitOnResize || Editor is null || Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        var bounds = Bounds.Size;
        if (Math.Abs(bounds.Width - _lastAutoFitBounds.Width) < 0.5 &&
            Math.Abs(bounds.Height - _lastAutoFitBounds.Height) < 0.5)
        {
            return;
        }

        _lastAutoFitBounds = bounds;
        if (IsThumbnailPreview)
        {
            var thumbMargin = Math.Max(12, Math.Min(Bounds.Width, Bounds.Height) * 0.18);
            Editor.FitThumbnailPreview(thumbMargin, boundsPaddingRatio: 0.36);
        }
        else if (Editor.IsViewOnlyMode)
        {
            Editor.FitPreviewViewport(0.85);
        }
        else if (Editor.HasConstructionGeometry())
        {
            Editor.FitDocumentToViewport();
        }
        else
        {
            Editor.CenterViewportOnOrigin();
        }
    }

    private void DrawHandlePreview(DrawingContext context)
    {
        if (Editor?.HandlePreviewSashId is not Guid sashId)
        {
            return;
        }

        var sash = Editor.CanvasElements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
        if (sash is null)
        {
            return;
        }

        var hingeSide = sash.HingeSide ?? SashHingeSide.Left;
        var opening = sash.OpeningType ?? SashOpeningType.TiltTurn;
        if (opening is SashOpeningType.Tilt)
        {
            return;
        }

        var handleSide = CadHostedElementHelper.ResolveDefaultHandleSide(hingeSide, opening);
        SashElementRenderer.DrawHandleMarker(context, Editor, sash, handleSide, preview: true);
    }

    /// <summary>
    /// Фантом переходного профиля: единый контур будущего размещения
    /// (для дверных материалов — сквозной, на всю высоту рамы).
    /// </summary>
    private void DrawTransitionProfileHoverPreview(
        DrawingContext context,
        (double OriginX, double OriginY, double Width, double Height) bounds)
    {
        if (Editor?.Viewport is null)
        {
            return;
        }

        var topLeft = Editor.Viewport.WorldToScreen(new CadPoint(bounds.OriginX, bounds.OriginY + bounds.Height));
        var bottomRight = Editor.Viewport.WorldToScreen(new CadPoint(bounds.OriginX + bounds.Width, bounds.OriginY));
        var rect = new Rect(new Point(topLeft.X, topLeft.Y), new Point(bottomRight.X, bottomRight.Y));
        if (rect.Width <= 0 || rect.Height <= 0)
        {
            return;
        }

        context.DrawRectangle(MullionPreviewFill, MullionPreviewPen, rect);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        if (Editor == null)
        {
            base.OnPointerWheelChanged(e);
            return;
        }

        if (Editor.IsViewOnlyMode)
        {
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
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
                $"{rect.Height:F1}",
                Editor,
                registerHits: !isTemporary && ShouldDrawDimensions);
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

    protected override async void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var point = e.GetPosition(this);
        var properties = e.GetCurrentPoint(this).Properties;

        if (Editor?.IsViewOnlyMode == true)
        {
            e.Handled = true;
            return;
        }

        if (ShouldDrawDimensions
            && Editor != null
            && properties.IsLeftButtonPressed
            && Editor.DismissDimensionEditIfClickOutside(point))
        {
            Focus();
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (ShouldDrawDimensions
            && Editor != null
            && properties.IsLeftButtonPressed
            && Editor.TryBeginDimensionEdit(point))
        {
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (properties.IsMiddleButtonPressed
            || (e.KeyModifiers.HasFlag(KeyModifiers.Control)
                && !ShouldSuppressCtrlPanForMultiSelect(point)))
        {
            _isPanning = true;
            _lastPanPoint = point;
            e.Pointer.Capture(this);
            return;
        }

        if (Editor?.CurrentToolMode == WindowToolMode.PanView && properties.IsLeftButtonPressed)
        {
            _isPanning = true;
            _lastPanPoint = point;
            UpdateInteractionCursor(isDragging: true);
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (Editor?.CurrentToolMode == WindowToolMode.ZoomView)
        {
            var screenPoint = new ScreenPoint(point.X, point.Y);
            if (properties.IsLeftButtonPressed)
            {
                Editor.Viewport.ZoomAt(screenPoint, 1.25);
            }
            else if (properties.IsRightButtonPressed)
            {
                Editor.Viewport.ZoomAt(screenPoint, 1 / 1.25);
            }

            Editor.UpdateCursor(Editor.Viewport.ScreenToWorld(screenPoint));
            Editor.RequestSceneRefresh();
            e.Handled = true;
            InvalidateVisual();
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
                var worldPoint = Editor.Viewport.ScreenToWorld(new ScreenPoint(point.X, point.Y));
                if (!await Editor.TryBeginShapePlacementAsync(worldPoint))
                {
                    e.Handled = true;
                    InvalidateVisual();
                    return;
                }

                _startPoint = point;
                _isDrawingFrame = true;
                e.Pointer.Capture(this);
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        var cadEvent = ToCadEvent(point, e.KeyModifiers);

        if (Editor != null && Editor.CurrentToolMode == WindowToolMode.Hardware && properties.IsLeftButtonPressed)
        {
            if (Editor.TryCycleSashHardware(cadEvent.WorldPoint))
            {
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        if (Editor != null && Editor.CurrentToolMode == WindowToolMode.Handle && properties.IsLeftButtonPressed)
        {
            if (Editor.TryCycleHandleOnSash(cadEvent.WorldPoint))
            {
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        if (Editor != null
            && Editor.CurrentToolMode is WindowToolMode.BeadSystem or WindowToolMode.GlassWithBead
            && properties.IsLeftButtonPressed)
        {
            if (Editor.TryPlaceGlazingBead(cadEvent.WorldPoint, cadEvent.IsAltDown))
            {
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        if (Editor != null
            && CadCreationToolCatalog.IsClickPlacementMode(Editor.CurrentToolMode)
            && properties.IsLeftButtonPressed
            && Editor.CurrentToolMode is not (WindowToolMode.Glassing or WindowToolMode.MosquitoNet
                or WindowToolMode.BeadSystem or WindowToolMode.GlassWithBead))
        {
            if (Editor.TryPlaceCreationToolClick(cadEvent.WorldPoint))
            {
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        if (Editor != null
            && CadCreationToolCatalog.ResolveHardwareToolId(Editor.CurrentToolMode) is not null
            && properties.IsLeftButtonPressed)
        {
            if (Editor.TryPlaceCreationToolClick(cadEvent.WorldPoint))
            {
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        if (Editor != null
            && Editor.CurrentToolMode == WindowToolMode.OffsetImpostVertical
            && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            if (Editor.TryPressVerticalImpostOffset(
                    cadEvent.WorldPoint,
                    e.KeyModifiers.HasFlag(KeyModifiers.Control)))
            {
                e.Pointer.Capture(this);
            }

            InvalidateVisual();
            return;
        }

        if (Editor != null
            && Editor.CurrentToolMode == WindowToolMode.OffsetImpostHorizontal
            && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            if (Editor.TryPressHorizontalImpostOffset(
                    cadEvent.WorldPoint,
                    e.KeyModifiers.HasFlag(KeyModifiers.Control)))
            {
                e.Pointer.Capture(this);
            }

            InvalidateVisual();
            return;
        }

        if (Editor != null
            && Editor.CurrentToolMode is WindowToolMode.RadiusVertex or WindowToolMode.ArcVertex
            && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            var handled = Editor.CurrentToolMode == WindowToolMode.RadiusVertex
                ? Editor.TryPressRadiusVertex(cadEvent.WorldPoint)
                : Editor.TryPressArcVertex(cadEvent.WorldPoint);
            if (handled)
            {
                e.Pointer.Capture(this);
            }

            InvalidateVisual();
            return;
        }

        if (Editor != null
            && Editor.CurrentToolMode == WindowToolMode.SelectVertex
            && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            if (Editor.TryPressVertexDrag(cadEvent.WorldPoint))
            {
                e.Pointer.Capture(this);
            }
            else
            {
                Editor.ActiveTool?.OnMouseDown(Editor.CreateToolContext(), cadEvent);
            }

            InvalidateVisual();
            return;
        }

        if (Editor != null
            && Editor.CurrentToolMode == WindowToolMode.Sash
            && Editor.ActiveDesignToolId == CadCreationToolCatalog.SashFixed
            && properties.IsLeftButtonPressed)
        {
            if (Editor.TryPlaceCreationToolClick(cadEvent.WorldPoint))
            {
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        if (Editor?.TryBeginPointerInteraction(cadEvent.WorldPoint) == true)
        {
            e.Pointer.Capture(this);
            InvalidateVisual();
            return;
        }

        if (Editor != null
            && Editor.CurrentToolMode == WindowToolMode.AdapterProfile
            && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            _ = HandleAdapterProfileClickAsync(cadEvent.WorldPoint);
            return;
        }

        if (Editor != null && IsRealImpostDragPlacementTool(Editor.CurrentToolMode) && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            var pressPoint = e.GetPosition(this);
            if (Editor.TryBeginImpostDragPlacement(cadEvent.WorldPoint, pressPoint.X, pressPoint.Y))
            {
                _isImpostDragPlacing = true;
                e.Pointer.Capture(this);
                InvalidateVisual();
            }

            return;
        }

        if (Editor != null && IsImpostSplitTool(Editor.CurrentToolMode) && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            _ = HandleImpostClickAsync(cadEvent.WorldPoint);
            return;
        }

        if (Editor != null
            && Editor.CurrentToolMode == WindowToolMode.ExtremeImpost
            && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            if (Editor.TryHandleExtremeImpostClick(cadEvent.WorldPoint))
            {
                InvalidateVisual();
            }

            return;
        }

        if (Editor != null
            && Editor.WorkspaceMode == CadEditorWorkspaceMode.Editing
            && properties.IsLeftButtonPressed
            && Editor.TryHandleExtremeImpostClick(cadEvent.WorldPoint))
        {
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (Editor != null
            && Editor.CurrentToolMode == WindowToolMode.DecorativeSpoke
            && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            _ = HandleDecorativeSpokeClickAsync(cadEvent.WorldPoint);
            return;
        }

        if (Editor != null
            && Editor.CurrentToolMode == WindowToolMode.Sill
            && properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            _ = HandleSillClickAsync(cadEvent.WorldPoint);
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

        InvalidateVisual();
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (Editor == null) return;

        var point = e.GetPosition(this);
        var worldPoint = Editor.Viewport.ScreenToWorld(new ScreenPoint(point.X, point.Y));
        Editor.UpdateCursor(worldPoint);
        UpdateInteractionCursor(_isPanning);

        if (_isImpostDragPlacing)
        {
            Editor.UpdateImpostDragPlacement(worldPoint, point.X, point.Y);
            InvalidateVisual();
            return;
        }

        if (_isDrawingFrame)
        {
            Editor.UpdateShapePlacementPreview(worldPoint);
            InvalidateVisual();
            return;
        }

        Editor.UpdatePointerInteraction(worldPoint);
        Editor.UpdateRadiusVertexHover(worldPoint);
        Editor.UpdateVertexHandleHover(worldPoint);
        if (!_isPanning
            && Editor.ShowVertexHandles
            && Editor.TryHitVertexHandle(worldPoint))
        {
            Cursor = PanHandCursor;
        }

        if (Editor.CurrentToolMode == WindowToolMode.ExtremeImpost)
        {
            Editor.UpdateExtremeImpostHover(worldPoint);
        }
        else
        {
            Editor.ClearExtremeImpostHover();
        }

        if (Editor.CurrentToolMode == WindowToolMode.AdapterProfile)
        {
            Editor.UpdateAdapterProfileHover(worldPoint);
        }
        else
        {
            Editor.ClearAdapterProfileHover();
        }

        if (Editor.IsVerticalImpostDragging)
        {
            Editor.UpdateVerticalImpostOffsetDrag(worldPoint);
            InvalidateVisual();
            return;
        }

        if (Editor.IsHorizontalImpostDragging)
        {
            Editor.UpdateHorizontalImpostOffsetDrag(worldPoint);
            InvalidateVisual();
            return;
        }

        if (Editor.IsArcVertexDragging)
        {
            Editor.UpdateArcVertexDrag(worldPoint);
            InvalidateVisual();
            return;
        }

        if (Editor.IsVertexDragging)
        {
            Editor.UpdateVertexDrag(worldPoint, e.KeyModifiers.HasFlag(KeyModifiers.Control));
            InvalidateVisual();
            return;
        }

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

        if (!IsThumbnailPreview && Editor is { IsViewOnlyMode: false })
        {
            Editor.UpdateCanvasHoverHint(point, worldPoint);
        }

        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var wasPanning = _isPanning;
        _isPanning = false;
        _lastPanPoint = null;
        if (wasPanning)
        {
            UpdateInteractionCursor(isDragging: false);
        }

        if (_isImpostDragPlacing && Editor != null)
        {
            if (e.InitialPressMouseButton != MouseButton.Left)
            {
                Editor.CancelImpostDragPlacement();
                _isImpostDragPlacing = false;
                e.Pointer.Capture(null);
                InvalidateVisual();
                return;
            }

            var impostReleasePoint = e.GetPosition(this);
            var impostWorldPoint = Editor.Viewport.ScreenToWorld(new ScreenPoint(impostReleasePoint.X, impostReleasePoint.Y));
            Editor.UpdateImpostDragPlacement(impostWorldPoint, impostReleasePoint.X, impostReleasePoint.Y);
            Editor.CommitImpostDragPlacement();
            _isImpostDragPlacing = false;
            e.Pointer.Capture(null);
            InvalidateVisual();
            return;
        }

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

            if (Editor.IsVerticalImpostDragging)
            {
                Editor.UpdateVerticalImpostOffsetDrag(worldPoint);
                Editor.ReleaseVerticalImpostOffset();
                e.Pointer.Capture(null);
                InvalidateVisual();
                return;
            }

            if (Editor.IsHorizontalImpostDragging)
            {
                Editor.UpdateHorizontalImpostOffsetDrag(worldPoint);
                Editor.ReleaseHorizontalImpostOffset();
                e.Pointer.Capture(null);
                InvalidateVisual();
                return;
            }

            if (Editor.IsArcVertexDragging)
            {
                Editor.UpdateArcVertexDrag(worldPoint);
                Editor.ReleaseArcVertexDrag();
                e.Pointer.Capture(null);
                InvalidateVisual();
                return;
            }

            if (Editor.IsVertexDragging)
            {
                Editor.UpdateVertexDrag(worldPoint, e.KeyModifiers.HasFlag(KeyModifiers.Control));
                Editor.ReleaseVertexDrag();
                e.Pointer.Capture(null);
                InvalidateVisual();
                return;
            }

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

        if (e.Key == Key.Escape && _isImpostDragPlacing)
        {
            Editor.CancelImpostDragPlacement();
            _isImpostDragPlacing = false;
            e.Handled = true;
            InvalidateVisual();
            return;
        }

        if (e.Key == Key.Delete)
        {
            Editor.DeleteSelectedCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            const double nudgeMm = 1.0;
            var delta = e.Key switch
            {
                Key.Left => (-nudgeMm, 0.0),
                Key.Right => (nudgeMm, 0.0),
                Key.Up => (0.0, nudgeMm),
                Key.Down => (0.0, -nudgeMm),
                _ => (0.0, 0.0)
            };
            if ((Math.Abs(delta.Item1) > 0 || Math.Abs(delta.Item2) > 0)
                && Editor.TryNudgeActiveVertex(delta.Item1, delta.Item2))
            {
                e.Handled = true;
                InvalidateVisual();
                return;
            }
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.KeyModifiers.HasFlag(KeyModifiers.Alt) && e.Key == Key.Z)
        {
            Editor.UndoLastElementCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)
            && e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            && e.Key == Key.Z)
        {
            Editor.RedoCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Z)
        {
            Editor.UndoCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) && e.Key == Key.Y)
        {
            Editor.RedoCommand.Execute(null);
            e.Handled = true;
        }
    }

    private bool ShouldSuppressCtrlPanForMultiSelect(Point screenPoint)
    {
        if (Editor?.Viewport is null)
        {
            return false;
        }

        // Ctrl+drag вершины: орто-режим, не pan.
        if (Editor.CurrentToolMode == WindowToolMode.SelectVertex)
        {
            if (Editor.IsVertexDragging)
            {
                return true;
            }

            var world = Editor.Viewport.ScreenToWorld(new ScreenPoint(screenPoint.X, screenPoint.Y));
            return Editor.TryHitVertexHandle(world);
        }

        // Ctrl+клик в режимах выделения: toggle multi-select, не pan.
        if (Editor.CurrentToolMode is WindowToolMode.SelectProfile
            or WindowToolMode.SelectElement
            or WindowToolMode.Select)
        {
            return true;
        }

        return false;
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
        var (majorStep, minorStep) = viewport.ChooseGridSteps();
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

    private void DrawSelectedRectangleOverlay(DrawingContext context)
    {
        // Красная подсветка реек (в т.ч. мультивыбор) заменяет чёрный AABB-контур.
        if (Editor?.ConstructionSelection.Any(item =>
                item.Kind == CadSelectableKind.Frame && item.ProfileRail is not null) == true)
        {
            return;
        }

        if (Editor?.FrameRoot is FrameNode frame
            && frame.Shape == ConstructionShape.Triangle
            && !frame.IsDummy
            && !frame.IsFictitiousContainer)
        {
            ShapedFrameRenderer.DrawTriangularOutline(
                context,
                Editor,
                frame.X,
                frame.Y,
                frame.Width,
                frame.Height,
                SelectedFramePen);
            return;
        }

        if (Editor?.SelectedRectangle is not CadRect selected || selected.Width <= 0 || selected.Height <= 0)
        {
            return;
        }

        var isFictitiousFrame = Editor.FrameRoot?.IsDummy == true
            || Editor.FrameRoot?.IsFictitiousContainer == true
            || Editor.HasFakeFrameOnCanvas;
        if (isFictitiousFrame)
        {
            return;
        }

        // Точный профиль уже подсвечивается ConstructionSelectionRenderer.
        // Старый AABB здесь создавал паразитную «коробку в коробке» после деформации.
        if (Editor.FrameRoot?.HasDeformedOuterContour() == true
            || Editor.FrameRoot?.ProfileRailJoints.Any(joint => Math.Abs(joint.NormalOffsetMm) > CadMath.Epsilon) == true)
        {
            return;
        }

        DrawRect(context, selected, null, SelectedFramePen);
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

    private async Task HandleAdapterProfileClickAsync(CadPoint worldPoint)
    {
        if (Editor == null)
        {
            return;
        }

        await Editor.TryHandleAdapterProfileClickAsync(worldPoint);
        InvalidateVisual();
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

    private async Task HandleDecorativeSpokeClickAsync(CadPoint worldPoint)
    {
        if (Editor == null)
        {
            return;
        }

        await Editor.TryHandleDecorativeSpokeClickAsync(worldPoint);
        InvalidateVisual();
    }

    private async Task HandleSillClickAsync(CadPoint worldPoint)
    {
        if (Editor == null)
        {
            return;
        }

        await Editor.TryHandleSillClickAsync(worldPoint);
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
