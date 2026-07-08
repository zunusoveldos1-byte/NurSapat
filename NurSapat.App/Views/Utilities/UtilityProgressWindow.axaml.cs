using Avalonia.Controls;
using NurSapat.App.ViewModels.Utilities;

namespace NurSapat.App.Views.Utilities;

public partial class UtilityProgressWindow : Window
{
    public UtilityProgressWindow(UtilityProgressViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += Close;
    }
}
