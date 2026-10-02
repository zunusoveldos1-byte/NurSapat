using Avalonia.Controls;
using Avalonia.Threading;

namespace NurSapat.App.Views.OrderSheet;

public partial class OrderPreviewCard : UserControl
{
    public OrderPreviewCard()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
            Dispatcher.UIThread.Post(RefreshConstructionPreview, DispatcherPriority.Loaded);
    }

    private void RefreshConstructionPreview()
    {
        if (ConstructionPreviewCanvas?.Editor is { } editor
            && ConstructionPreviewCanvas.Bounds.Width > 0
            && ConstructionPreviewCanvas.Bounds.Height > 0)
        {
            editor.Viewport.Resize(ConstructionPreviewCanvas.Bounds.Width, ConstructionPreviewCanvas.Bounds.Height);
            editor.FitDocumentToViewport();
            ConstructionPreviewCanvas.InvalidateVisual();
        }
    }
}
