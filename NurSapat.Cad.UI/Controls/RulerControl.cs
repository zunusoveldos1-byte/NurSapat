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
    private static readonly IBrush DefaultBackground = new SolidColorBrush(Color.Parse("#F8F9FA"));
    private static readonly IBrush TextBrush = new SolidColorBrush(Color.Parse("#666666"));
    private static readonly Pen BorderPen = new(new SolidColorBrush(Color.Parse("#DCDCDC")), 1);
    private static readonly Pen MajorTickPen = new(new SolidColorBrush(Color.Parse("#B8B8B8")), 1);
    private static readonly Pen MinorTickPen = new(new SolidColorBrush(Color.Parse("#D8D8D8")), 1);
    private static readonly Pen SnapTickPen = new(Brushes.IndianRed, 2);

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<RulerControl, IBrush?>(nameof(Background));

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public static readonly StyledProperty<CadEditorViewModel?> EditorProperty =
        AvaloniaProperty.Register<RulerControl, CadEditorViewModel?>(nameof(Editor));

    public CadEditorViewModel? Editor
    {
        get => GetValue(EditorProperty);
        set => SetValue(EditorProperty, value);
    }

    public static readonly StyledProperty<RulerOrientation> OrientationProperty =
        AvaloniaProperty.Register<RulerControl, RulerOrientation>(nameof(Orientation));

    public RulerOrientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == EditorProperty)
        {
            if (change.OldValue is CadEditorViewModel oldEditor)
            {
                oldEditor.Viewport.PropertyChanged -= OnViewportPropertyChanged;
                oldEditor.SceneInvalidated -= OnSceneInvalidated;
            }

            if (change.NewValue is CadEditorViewModel newEditor)
            {
                newEditor.Viewport.PropertyChanged += OnViewportPropertyChanged;
                newEditor.SceneInvalidated += OnSceneInvalidated;
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

        var majorStep = viewport.ChooseGridStep();
        var minorStep = majorStep / 5;

        if (Orientation == RulerOrientation.Horizontal)
        {
            RenderHorizontal(context, viewport, majorStep, minorStep);
            // ÈÇÌÅÍÅÍÈÅ: Êðàñíûå ëèíèè óáðàíû
            // RenderSnapTicks(context, viewport, true);
        }
        else
        {
            RenderVertical(context, viewport, majorStep, minorStep);
            // ÈÇÌÅÍÅÍÈÅ: Êðàñíûå ëèíèè óáðàíû
            // RenderSnapTicks(context, viewport, false);
        }
    }

    private void OnViewportPropertyChanged(object? sender, PropertyChangedEventArgs e) => InvalidateVisual();

    private void OnSceneInvalidated(object? sender, EventArgs e) => InvalidateVisual();

    private void RenderHorizontal(DrawingContext context, CadViewport viewport, double majorStep, double minorStep)
    {
        var left = viewport.ScreenToWorld(new ScreenPoint(0, 0)).X;
        var right = viewport.ScreenToWorld(new ScreenPoint(Bounds.Width, 0)).X;

        if (minorStep * viewport.ZoomFactor >= 8)
        {
            for (var x = Math.Floor(left / minorStep) * minorStep; x <= right; x += minorStep)
            {
                var screen = viewport.WorldToScreen(new CadPoint(x, 0));
                context.DrawLine(MinorTickPen, new Point(screen.X, Bounds.Height), new Point(screen.X, Bounds.Height - 4));
            }
        }

        for (var x = Math.Floor(left / majorStep) * majorStep; x <= right; x += majorStep)
        {
            var screen = viewport.WorldToScreen(new CadPoint(x, 0));
            context.DrawLine(MajorTickPen, new Point(screen.X, Bounds.Height), new Point(screen.X, Bounds.Height - 9));
            DrawCenteredText(context, x.ToString("0"), new Point(screen.X, 7));
        }

        context.DrawLine(BorderPen, new Point(0, Bounds.Height - 0.5), new Point(Bounds.Width, Bounds.Height - 0.5));
    }

    private void RenderVertical(DrawingContext context, CadViewport viewport, double majorStep, double minorStep)
    {
        var top = viewport.ScreenToWorld(new ScreenPoint(0, 0)).Y;
        var bottom = viewport.ScreenToWorld(new ScreenPoint(0, Bounds.Height)).Y;
        var min = Math.Min(bottom, top);
        var max = Math.Max(bottom, top);

        if (minorStep * viewport.ZoomFactor >= 8)
        {
            for (var y = Math.Floor(min / minorStep) * minorStep; y <= max; y += minorStep)
            {
                var screen = viewport.WorldToScreen(new CadPoint(0, y));
                context.DrawLine(MinorTickPen, new Point(Bounds.Width, screen.Y), new Point(Bounds.Width - 4, screen.Y));
            }
        }

        for (var y = Math.Floor(min / majorStep) * majorStep; y <= max; y += majorStep)
        {
            var screen = viewport.WorldToScreen(new CadPoint(0, y));
            context.DrawLine(MajorTickPen, new Point(Bounds.Width, screen.Y), new Point(Bounds.Width - 9, screen.Y));
            DrawRightAlignedText(context, y.ToString("0"), new Point(Bounds.Width - 12, screen.Y));
        }

        context.DrawLine(BorderPen, new Point(Bounds.Width - 0.5, 0), new Point(Bounds.Width - 0.5, Bounds.Height));
    }

    //private void RenderSnapTicks(DrawingContext context, CadViewport viewport, bool isHorizontal)
    //{
    //    if (Editor == null)
    //    {
    //        return;
    //    }

    //    var axis = isHorizontal ? CadSnapTickAxis.X : CadSnapTickAxis.Y;
    //    var ticks = Editor.SnapProvider.GetSnapTicks(Editor.Document)
    //        .Where(tick => tick.Axis == axis)
    //        .Select(tick => tick.Coordinate)
    //        .Distinct()
    //        .ToArray();

    //    foreach (var coordinate in ticks)
    //    {
    //        if (isHorizontal)
    //        {
    //            var screen = viewport.WorldToScreen(new CadPoint(coordinate, 0));
    //            context.DrawLine(SnapTickPen, new Point(screen.X, 0), new Point(screen.X, Bounds.Height));
    //        }
    //        else
    //        {
    //            var screen = viewport.WorldToScreen(new CadPoint(0, coordinate));
    //            context.DrawLine(SnapTickPen, new Point(0, screen.Y), new Point(Bounds.Width, screen.Y));
    //        }
    //    }
    //}

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
            10,
            TextBrush);
}