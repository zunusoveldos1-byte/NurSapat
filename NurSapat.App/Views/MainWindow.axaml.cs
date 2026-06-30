using Avalonia.Controls;
using NurSapat.App.ViewModels;

namespace NurSapat.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    public MainWindow(LoginViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        RootBorder.Opacity = 1;
    }
}
