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

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.NavigateToLogin();
        if (viewModel.CurrentView is LoginViewModel loginViewModel)
        {
            loginViewModel.LoadSavedCredentials();
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        RootBorder.Opacity = 1;
    }
}
