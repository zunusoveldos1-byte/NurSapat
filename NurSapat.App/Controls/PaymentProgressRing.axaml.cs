using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;

namespace NurSapat.App.Controls;

public partial class PaymentProgressRing : UserControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<PaymentProgressRing, double>(nameof(Value));

    private const double RingSize = 56;
    private const double StrokeThickness = 4;

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public PaymentProgressRing()
    {
        InitializeComponent();
        UpdateVisual(Value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ValueProperty)
        {
            UpdateVisual(change.GetNewValue<double>());
        }
    }

    private void UpdateVisual(double value)
    {
        var clamped = Math.Clamp(value, 0, 100);
        PercentText.Text = $"{Math.Round(clamped)}%";
        ProgressArc.Data = CreateArcGeometry(clamped);
    }

    private static Geometry? CreateArcGeometry(double percent)
    {
        if (percent <= 0)
        {
            return null;
        }

        var center = RingSize / 2;
        var radius = center - StrokeThickness;

        if (percent >= 100)
        {
            return new EllipseGeometry(new Rect(StrokeThickness, StrokeThickness, RingSize - StrokeThickness * 2, RingSize - StrokeThickness * 2));
        }

        var startAngle = -90d;
        var sweep = percent / 100d * 360d;
        var start = PolarToPoint(center, radius, startAngle);
        var end = PolarToPoint(center, radius, startAngle + sweep);
        var largeArc = sweep > 180d;

        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(start, false);
        context.ArcTo(
            end,
            new Size(radius, radius),
            0,
            largeArc,
            SweepDirection.Clockwise);
        return geometry;
    }

    private static Point PolarToPoint(double center, double radius, double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180d;
        return new Point(
            center + radius * Math.Cos(radians),
            center + radius * Math.Sin(radians));
    }
}
