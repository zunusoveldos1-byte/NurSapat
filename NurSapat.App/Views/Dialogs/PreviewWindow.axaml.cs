using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NurSapat.Cad.UI.Controls;

namespace NurSapat.App.Views.Dialogs;

public partial class PreviewWindow : Window
{
    public PreviewWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(ApplyPreviewViewportFit, DispatcherPriority.Loaded);
    }

    private void ApplyPreviewViewportFit()
    {
        foreach (var canvas in this.GetVisualDescendants().OfType<CadCanvasControl>())
        {
            canvas.ResetThumbnailAutoFit();
            if (canvas.Editor is not { } editor)
            {
                continue;
            }

            var width = canvas.Bounds.Width > 0 ? canvas.Bounds.Width : canvas.DesiredSize.Width;
            var height = canvas.Bounds.Height > 0 ? canvas.Bounds.Height : canvas.DesiredSize.Height;
            if (width <= 0 || height <= 0)
            {
                continue;
            }

            editor.Viewport.Resize(width, height);
            editor.FitPreviewViewport(0.85);
            canvas.InvalidateVisual();
        }
    }

    private void PointerPressed_OnDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
