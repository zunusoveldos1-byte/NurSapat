using Avalonia.Controls;
using System;

namespace NurSapat.App.Views.Dialogs;

public partial class DealerSelectDialogWindow : Window
{
    public DealerSelectDialogWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e) =>
        ErpDialogWindowHelper.FocusSearchBox(SearchTextBox);

    private void OnTitleBarClose(object? sender, EventArgs e) =>
        ErpDialogWindowHelper.CloseWithCancel(this);
}
