using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class ConfirmDialogViewModel : ObservableObject
{
    public ConfirmDialogViewModel(string message)
    {
        Message = message;
    }

    public string Message { get; }

    public bool IsConfirmed { get; private set; }

    public event EventHandler<bool>? CloseRequested;

    [RelayCommand]
    private void Confirm()
    {
        IsConfirmed = true;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        IsConfirmed = false;
        CloseRequested?.Invoke(this, false);
    }
}
