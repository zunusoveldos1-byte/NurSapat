using Avalonia.Controls;
using System;

namespace NurSapat.App.Views.Dialogs;

public partial class ConfirmDeleteDialogWindow : Window
{
    public ConfirmDeleteDialogWindow()
    {
        InitializeComponent();
    }

    private void OnTitleBarClose(object? sender, EventArgs e) =>
        ErpDialogWindowHelper.CloseWithCancel(this);
}
