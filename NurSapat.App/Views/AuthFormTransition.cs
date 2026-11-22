using Avalonia;
using Avalonia.Animation;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;

namespace NurSapat.App.Views;

internal sealed class AuthFormTransition(CancellationToken windowClosed) : IPageTransition
{
    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (from is null || to is null)
        {
            return;
        }

        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, windowClosed);
        var direction = forward ? 1 : -1;
        var outgoingOpacity = from.Opacity;
        to.Opacity = 0;
        if (from is InputElement outgoing)
        {
            outgoing.IsHitTestVisible = false;
            outgoing.IsEnabled = false;
        }
        if (to is InputElement incoming)
        {
            incoming.IsHitTestVisible = false;
            incoming.IsEnabled = true;
        }

        try
        {
            await Task.WhenAll(
                CreateAnimation(250, Frame(0, outgoingOpacity, 0), Frame(0.5, 0, -14 * direction), Frame(1, 0, -14 * direction))
                    .RunAsync(from, lifetime.Token),
                CreateAnimation(250, Frame(0, 0, 14 * direction), Frame(0.5, 0, 14 * direction), Frame(1, 1, 0))
                    .RunAsync(to, lifetime.Token));
            if (!lifetime.IsCancellationRequested)
            {
                from.IsVisible = false;
                if (to is InputElement currentPage)
                {
                    currentPage.IsHitTestVisible = true;
                }
            }
        }
        catch (OperationCanceledException) when (lifetime.IsCancellationRequested)
        {
        }
    }

    public static Task OpenAsync(Visual form, CancellationToken cancellationToken) =>
        CreateAnimation(300, Frame(0, 0, 0, 10), Frame(1, 1, 0)).RunAsync(form, cancellationToken);

    private static Animation CreateAnimation(int milliseconds, params KeyFrame[] frames)
    {
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(milliseconds),
            // Hold the outgoing page at zero opacity until the presenter is hidden.
            FillMode = FillMode.Forward
        };

        foreach (var frame in frames)
        {
            animation.Children.Add(frame);
        }

        return animation;
    }

    private static KeyFrame Frame(double cue, double opacity, double x, double y = 0) =>
        new()
        {
            Cue = new Cue(cue),
            KeySpline = new KeySpline(0.22, 0.61, 0.36, 1),
            Setters =
            {
                new Setter(Visual.OpacityProperty, opacity),
                new Setter(TranslateTransform.XProperty, x),
                new Setter(TranslateTransform.YProperty, y)
            }
        };
}
