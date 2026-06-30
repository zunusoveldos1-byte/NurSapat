using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Snapping;
using NurSapat.Cad.Core.Viewport;
using NurSapat.Cad.UI.ViewModels;
using System.ComponentModel;

namespace NurSapat.Cad.UI.Controls;

public sealed class RulerControl : Control
{
    private static readonly IBrush DefaultBackground = new SolidColorBrush(Color.Parse("#F8FAFC"));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#64748B"));
    private static readonly Pen BorderPen = new(new SolidColorBrush(Color.Parse("#E2E8F0")), 1);
    private static readonly Pen MajorTickPen = new(new SolidColorBrush(Color.Parse("#94A3B8")), 1);
    private static readonly Pen MinorTickPen = new(new SolidColorBrush(Color.Parse("#CBD5E1")), 1);

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<RulerControl, IBrush?>(nameof(Background));

    public static readonly StyledProperty<CadEditorViewModel?> EditorProperty =
        AvaloniaProperty.Register<RulerControl, CadEditorViewModel?>(nameof(Editor));

    public static readonly StyledProperty<RulerOrientation> OrientationProperty =
        AvaloniaProperty.Register<RulerControl, RulerOrientation>(nameof(Orientation));

    public static readonly StyledProperty<HorizontalTickEdge> HorizontalTickEdgeProperty =
        AvaloniaProperty.Register<RulerControl, HorizontalTickEdge>(
            nameof(HorizontalTickEdge),
            HorizontalTickEdge.Top);

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public CadEditorViewModel? Editor
    {
        get => GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public RulerOrientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public HorizontalTickEdge HorizontalTickEdge
    {
        get => GetValue(HorizontalTickEdgeProperty);
        set => SetValue(HorizontalTickEdgeProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == EditorProperty)
        {
            if (change.OldValue is CadEditorViewModel oldEditor)
            {
                oldEditor.Viewport.PropertyChanged -= OnViewportPropertyChanged;
                oldEditor.SceneInvalidated -= OnSceneInvalidated;
                oldEditor.PropertyChanged -= OnEditorPropertyChanged;
            }

            if (change.NewValue is CadEditorViewModel newEditor)
            {
                newEditor.Viewport.PropertyChanged += OnViewportPropertyChanged;
                newEditor.SceneInvalidated += OnSceneInvalidated;
                newEditor.PropertyChanged += OnEditorPropertyChanged;
            }
        }

        base.OnPropertyChanged(change);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(Background ?? DefaultBackground, Bounds);

        var viewport = Editor?.Viewport;
        if (viewport == null)
        {
            return;
        }

        var majorStep = viewport.ChooseRulerStep();
        var minorStep = viewport.ChooseMinorRulerStep(majorStep);

        if (Orientation == RulerOrientation.Horizontal)
        {
            RenderHorizontal(context, viewport, majorStep, minorStep);
        }
        else
        {
            RenderVertical(context, viewport, majorStep, minorStep);
        }
    }

    private void OnViewportPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CadViewport.ZoomFactor) or nameof(CadViewport.PanOffset) or nameof(CadViewport.Width) or nameof(CadViewport.Height))
        {
            InvalidateVisual();
        }
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CadEditorViewModel.MillimetersPerPixel))
        {
            InvalidateVisual();
        }
    }

    private void OnSceneInvalidated(object? sender, EventArgs e) => InvalidateVisual();

    private void RenderHorizontal(DrawingContext context, CadViewport viewport, double majorStep, double minorStep)
    {
        var left = viewport.ScreenToWorld(new ScreenPoint(0, 0)).X;
        var right = viewport.ScreenToWorld(new ScreenPoint(Bounds.Width, 0)).X;
        var ticksFromTop = HorizontalTickEdge == HorizontalTickEdge.Top;

        if (minorStep * viewport.ZoomFactor >= 6)
        {
            for (var x = Math.Floor(left / minorStep) * minorStep; x <= right; x += minorStep)
            {
                var screen = viewport.WorldToScreen(new CadPoint(x, 0));
                if (ticksFromTop)
                {
                    context.DrawLine(MinorTickPen, new Point(screen.X, 0), new Point(screen.X, 5));
                }
                else
                {
                    context.DrawLine(MinorTickPen, new Point(screen.X, Bounds.Height), new Point(screen.X, Bounds.Height - 5));
                }
            }
        }

        for (var x = Math.Floor(left / majorStep) * majorStep; x <= right; x += majorStep)
        {
            var screen = viewport.WorldToScreen(new CadPoint(x, 0));
            if (ticksFromTop)
            {
                context.DrawLine(MajorTickPen, new Point(screen.X, 0), new Point(screen.X, 10));
                DrawCenteredText(context, FormatRulerLabel(x, majorStep), new Point(screen.X, 20));
            }
            else
            {
                context.DrawLine(MajorTickPen, new Point(screen.X, Bounds.Height), new Point(screen.X, Bounds.Height - 10));
                DrawCenteredText(context, FormatRulerLabel(x, majorStep), new Point(screen.X, Bounds.Height - 18));
            }
        }

        var borderY = ticksFromTop ? 0.5 : Bounds.Height - 0.5;
        context.DrawLine(BorderPen, new Point(0, borderY), new Point(Bounds.Width, borderY));
    }

    private void RenderVertical(DrawingContext context, CadViewport viewport, double majorStep, double minorStep)
    {
        var top = viewport.ScreenToWorld(new ScreenPoint(0, 0)).Y;
        var bottom = viewport.ScreenToWorld(new ScreenPoint(0, Bounds.Height)).Y;
        var min = Math.Min(bottom, top);
        var max = Math.Max(bottom, top);

        if (minorStep * viewport.ZoomFactor >= 6)
        {
            for (var y = Math.Floor(min / minorStep) * minorStep; y <= max; y += minorStep)
            {
                var screen = viewport.WorldToScreen(new CadPoint(0, y));
                context.DrawLine(MinorTickPen, new Point(Bounds.Width, screen.Y), new Point(Bounds.Width - 5, screen.Y));
            }
        }

        for (var y = Math.Floor(min / majorStep) * majorStep; y <= max; y += majorStep)
        {
            var screen = viewport.WorldToScreen(new CadPoint(0, y));
            context.DrawLine(MajorTickPen, new Point(Bounds.Width, screen.Y), new Point(Bounds.Width - 10, screen.Y));
            DrawRightAlignedText(context, FormatRulerLabel(y, majorStep), new Point(Bounds.Width - 12, screen.Y));
        }

        context.DrawLine(BorderPen, new Point(Bounds.Width - 0.5, 0), new Point(Bounds.Width - 0.5, Bounds.Height));
    }

    private static string FormatRulerLabel(double valueMm, double majorStep) =>
        majorStep >= 1000 ? $"{valueMm / 1000:0.#}k" : valueMm.ToString("0");

    private static void DrawCenteredText(DrawingContext context, string text, Point center)
    {
        var formatted = CreateText(text);
        context.DrawText(formatted, new Point(center.X - formatted.Width / 2, center.Y - formatted.Height / 2));
    }

    private static void DrawRightAlignedText(DrawingContext context, string text, Point centerRight)
    {
        var formatted = CreateText(text);
        context.DrawText(formatted, new Point(centerRight.X - formatted.Width, centerRight.Y - formatted.Height / 2));
    }

    private static FormattedText CreateText(string text) =>
        new(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            9,
            TextBrush);
}
