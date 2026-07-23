using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.App.ViewModels.Dialogs;

public sealed partial class ConfirmDeleteDialogViewModel : ViewModelBase
{
    public ConfirmDeleteDialogViewModel(string confirmationMessage, string title = "Удаление конструкции")
    {
        ConfirmationMessage = confirmationMessage;
        Title = title;
    }

    public string Title { get; }

    public string ConfirmationMessage { get; }

    public event Action<bool>? CloseRequested;

    [RelayCommand]
    private void Confirm() => CloseRequested?.Invoke(true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
