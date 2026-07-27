using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NurSapat.App.ViewModels.Dialogs;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Views.Dialogs;

internal static class ErpDialogWindowHelper
{
    public static void OnTitleBarPointerPressed(Window window, PointerPressedEventArgs e) =>
        BorderlessChromeHelper.OnTitleBarPointerPressed(window, e);

    public static void CloseWithCancel(Window window)
    {
        switch (window.DataContext)
        {
            case OrderSettingsDialogViewModel orderSettings:
                orderSettings.CancelCommand.Execute(null);
                break;
            case DealerSelectDialogViewModel dealerSelect:
                dealerSelect.CancelCommand.Execute(null);
                break;
            case PriceModeDialogViewModel priceMode:
                priceMode.CancelCommand.Execute(null);
                break;
            case ConfirmDeleteDialogViewModel confirmDelete:
                confirmDelete.CancelCommand.Execute(null);
                break;
            default:
                window.Close(false);
                break;
        }
    }

    public static void FocusSearchBox(Control? searchBox)
    {
        if (searchBox is null)
        {
            return;
        }

        searchBox.Focus();
    }
}
