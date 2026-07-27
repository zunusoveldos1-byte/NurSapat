using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NurSapat.App.ViewModels;
using NurSapat.Cad.UI.Controls;

namespace NurSapat.App.Views.OrderSheet;

public partial class ConstructionListPanel : UserControl
{
    public ConstructionListPanel()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
        WireToolbarHintHandlers();
    }

    private void WireToolbarHintHandlers()
    {
        WireHintButton(AddConstructionButton, "Добавить новую конструкцию в лист заказа");
        WireHintButton(DuplicateButton, "Создать копию выбранной конструкции");
        WireHintButton(EditButton, "Открыть выбранную конструкцию на вкладке «Чертёж» для редактирования");
        WireHintButton(DeleteButton, "Удалить выбранную конструкцию из листа заказа");
        WireHintButton(PreviousPageButton, "Показать предыдущие конструкции в списке");
        WireHintButton(NextPageButton, "Показать следующие конструкции в списке");
    }

    private void WireHintButton(Button button, string hint)
    {
        button.PointerEntered += (_, _) =>
        {
            if (DataContext is OrderSheetViewModel vm)
            {
                vm.ShowToolbarHint(hint);
            }
        };

        button.PointerExited += (_, _) =>
        {
            if (DataContext is OrderSheetViewModel vm)
            {
                vm.ResetToolbarHint();
            }
        };
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Dispatcher.UIThread.Post(RefreshThumbnailCanvases, DispatcherPriority.Loaded);
    }

    private void RefreshThumbnailCanvases()
    {
        foreach (var canvas in this.GetVisualDescendants().OfType<CadCanvasControl>())
        {
            if (!canvas.IsThumbnailPreview)
            {
                continue;
            }

            canvas.ResetThumbnailAutoFit();
            if (canvas.Editor is { } editor && canvas.Bounds.Width > 0 && canvas.Bounds.Height > 0)
            {
                editor.Viewport.Resize(canvas.Bounds.Width, canvas.Bounds.Height);
                var margin = Math.Max(12, Math.Min(canvas.Bounds.Width, canvas.Bounds.Height) * 0.18);
                editor.FitThumbnailPreview(margin, boundsPaddingRatio: 0.36);
            }

            canvas.InvalidateVisual();
        }
    }
}
