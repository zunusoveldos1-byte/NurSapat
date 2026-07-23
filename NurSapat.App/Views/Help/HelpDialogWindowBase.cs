using Avalonia.Controls;
using NurSapat.App.ViewModels.Help;

namespace NurSapat.App.Views.Help;

public class HelpDialogWindowBase : Window
{
    // Parameterless ctor required by Avalonia XAML runtime loader (AVLN3001).
    // Windows are created at runtime via HelpWindowService + DI, not through the loader.
    protected HelpDialogWindowBase()
    {
    }

    protected HelpDialogWindowBase(HelpDialogViewModelBase viewModel)
        : this()
    {
        AttachViewModel(viewModel);
    }

    protected void AttachViewModel(HelpDialogViewModelBase viewModel)
    {
        DataContext = viewModel;
        viewModel.OwnerWindow = this;
        viewModel.RequestClose += OnRequestClose;
        Closed += (_, _) => viewModel.RequestClose -= OnRequestClose;
    }

    private void OnRequestClose()
    {
        if (IsVisible)
        {
            Close();
        }
    }
}
