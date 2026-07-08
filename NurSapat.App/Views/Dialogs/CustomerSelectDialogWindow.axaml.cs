using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace NurSapat.App.Views.Dialogs;

public partial class CustomerSelectDialogWindow : Window
{
    public Func<Task>? AddCustomerHandler { get; set; }

    public CustomerSelectDialogWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    private void OnOpened(object? sender, EventArgs e) =>
        ErpDialogWindowHelper.FocusSearchBox(SearchTextBox);

    private void OnTitleBarClose(object? sender, EventArgs e) =>
        ErpDialogWindowHelper.CloseWithCancel(this);

    private async void OnAddCustomerClick(object? sender, RoutedEventArgs e)
    {
        if (AddCustomerHandler is not null)
        {
            await AddCustomerHandler();
        }
    }
}
