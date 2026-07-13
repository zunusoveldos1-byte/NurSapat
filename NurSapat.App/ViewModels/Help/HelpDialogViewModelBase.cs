using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.ViewModels;

namespace NurSapat.App.ViewModels.Help;

public abstract class HelpDialogViewModelBase : ViewModelBase
{
    protected HelpDialogViewModelBase()
    {
        CloseCommand = new RelayCommand(() => RequestClose?.Invoke());
    }

    public IRelayCommand CloseCommand { get; }

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;
}
