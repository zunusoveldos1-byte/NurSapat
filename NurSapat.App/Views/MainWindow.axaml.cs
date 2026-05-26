using Avalonia.Controls;
using NurSapat.App.ViewModels;

namespace NurSapat.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()  
    {
        InitializeComponent();
    }

    public MainWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}