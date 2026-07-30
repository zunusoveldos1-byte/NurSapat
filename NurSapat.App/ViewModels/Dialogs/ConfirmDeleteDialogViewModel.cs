using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.App.ViewModels.Dialogs;

public sealed partial class ConfirmDeleteDialogViewModel : ViewModelBase
{
    public ConfirmDeleteDialogViewModel(
        string confirmationMessage,
        string title = "Удаление конструкции",
        string confirmText = "Да, удалить",
        string confirmIcon = "🗑",
        bool isDanger = true)
    {
        ConfirmationMessage = confirmationMessage;
        Title = title;
        ConfirmText = confirmText;
        ConfirmIcon = confirmIcon;
        IsDanger = isDanger;
    }

    public string Title { get; }

    public string ConfirmationMessage { get; }

    public string ConfirmText { get; }

    public string ConfirmIcon { get; }

    public bool IsDanger { get; }

    public event Action<bool>? CloseRequested;

    [RelayCommand]
    private void Confirm() => CloseRequested?.Invoke(true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);
}
