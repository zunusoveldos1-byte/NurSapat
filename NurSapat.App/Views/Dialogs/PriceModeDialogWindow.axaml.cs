using Avalonia.Controls;
using System;

namespace NurSapat.App.Views.Dialogs;

public partial class PriceModeDialogWindow : Window
{
    public PriceModeDialogWindow()
    {
        InitializeComponent();
    }

    private void OnTitleBarClose(object? sender, EventArgs e) =>
        ErpDialogWindowHelper.CloseWithCancel(this);
}
