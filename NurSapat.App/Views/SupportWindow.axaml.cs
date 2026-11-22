using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NurSapat.App.Views;

public partial class SupportWindow : Window
{
    public SupportWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        CloseContactButton.Focus();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
