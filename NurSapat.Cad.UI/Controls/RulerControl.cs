using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Viewport;

namespace NurSapat.Cad.UI.Controls;

public sealed class RulerControl : Control
{
    // ДОБАВЛЕНО: Свойство Background для корректной работы в XAML
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<RulerControl, IBrush?>(nameof(Background));

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public CadViewport? Viewport
    {
        get => GetValue(ViewportProperty);
        set => SetValue(ViewportProperty, value);
    }

    public static readonly StyledProperty<CadViewport?> ViewportProperty =
        AvaloniaProperty.Register<RulerControl, CadViewport?>(nameof(Viewport));

    public RulerOrientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public static readonly StyledProperty<RulerOrientation> OrientationProperty =
        AvaloniaProperty.Register<RulerControl, RulerOrientation>(nameof(Orientation));

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // ИЗМЕНЕНИЕ: Используем свойство Background, если оно задано
        if (Background != null)
        {
            context.FillRectangle(Background, Bounds);
        }
        else
        {
            // Если фон не задан, закрашиваем белым по умолчанию
            context.FillRectangle(Brushes.White, Bounds);
        }

        if (Viewport == null)
        {
            return;
        }

        var step = Viewport.ChooseGridStep();
        var textBrush = new SolidColorBrush(Color.Parse("#333333"));
        var tickPen = new Pen(new SolidColorBrush(Color.Parse("#A0A0A0")), 1);

        if (Orientation == RulerOrientation.Horizontal)
        {
            var left = Viewport.ScreenToWorld(new ScreenPoint(0, 0)).X;
            var right = Viewport.ScreenToWorld(new ScreenPoint(Bounds.Width, 0)).X;
            for (var x = Math.Floor(left / step) * step; x <= right; x += step)
            {
                var screen = Viewport.WorldToScreen(new CadPoint(x, 0));
                context.DrawLine(tickPen, new Point(screen.X, Bounds.Height), new Point(screen.X, Bounds.Height - 8));
                DrawText(context, x.ToString("0"), new Point(screen.X + 3, 4), textBrush);
            }
        }
        else
        {
            var top = Viewport.ScreenToWorld(new ScreenPoint(0, 0)).Y;
            var bottom = Viewport.ScreenToWorld(new ScreenPoint(0, Bounds.Height)).Y;
            for (var y = Math.Floor(Math.Min(bottom, top) / step) * step; y <= Math.Max(bottom, top); y += step)
            {
                var screen = Viewport.WorldToScreen(new CadPoint(0, y));
                context.DrawLine(tickPen, new Point(Bounds.Width, screen.Y), new Point(Bounds.Width - 8, screen.Y));
                DrawText(context, y.ToString("0"), new Point(4, screen.Y + 2), textBrush);
            }
        }
    }

    private static void DrawText(DrawingContext context, string text, Point origin, IBrush brush)
    {
        var formatted = new FormattedText(
            text,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            10,
            brush);
        context.DrawText(formatted, origin);
    }
}