using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace NurSapat.App.Views.Dialogs;

public partial class OrderSettingsDialogWindow : Window
{
    public OrderSettingsDialogWindow()
    {
        InitializeComponent();
    }

    private void OnTitleBarClose(object? sender, EventArgs e) =>
        ErpDialogWindowHelper.CloseWithCancel(this);
}
