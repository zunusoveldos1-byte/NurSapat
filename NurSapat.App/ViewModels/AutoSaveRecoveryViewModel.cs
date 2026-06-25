using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;

namespace NurSapat.App.ViewModels;

public sealed partial class AutoSaveRecoveryViewModel : ObservableObject
{
    public event EventHandler<YesNoChoice>? CloseRequested;

    [RelayCommand]
    private void Yes() => CloseRequested?.Invoke(this, YesNoChoice.Yes);

    [RelayCommand]
    private void No() => CloseRequested?.Invoke(this, YesNoChoice.No);
}
