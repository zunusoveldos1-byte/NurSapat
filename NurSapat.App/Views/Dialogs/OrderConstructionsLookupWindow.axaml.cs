using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NurSapat.Cad.UI.Controls;

namespace NurSapat.App.Views.Dialogs;

public partial class OrderConstructionsLookupWindow : Window
{
    public OrderConstructionsLookupWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(InvalidatePreviewCanvases, DispatcherPriority.Loaded);
    }

    private void InvalidatePreviewCanvases()
    {
        foreach (var canvas in this.GetVisualDescendants().OfType<CadCanvasControl>())
        {
            canvas.ResetThumbnailAutoFit();
            canvas.InvalidateVisual();
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}
