using Avalonia.Controls;
using NurSapat.App.ViewModels.Utilities;

namespace NurSapat.App.Views.Utilities;

public partial class UtilityProgressWindow : Window
{
    public UtilityProgressWindow()
    {
        InitializeComponent();
    }

    public UtilityProgressWindow(UtilityProgressViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += Close;
    }
}
