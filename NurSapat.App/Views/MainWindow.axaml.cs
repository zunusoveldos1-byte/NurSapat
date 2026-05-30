using Avalonia.Controls;
using NurSapat.App.ViewModels;

namespace NurSapat.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(LoginViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.LoginSucceeded += (_, _) => Close();
    }
}
