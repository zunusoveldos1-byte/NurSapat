using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.App.ViewModels.Dialogs;

public sealed partial class CreateProfileDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _profileName = string.Empty;

    public event Action<string?>? RequestClose;

    [RelayCommand]
    private void Confirm()
    {
        var trimmed = ProfileName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        RequestClose?.Invoke(trimmed);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(null);
}
