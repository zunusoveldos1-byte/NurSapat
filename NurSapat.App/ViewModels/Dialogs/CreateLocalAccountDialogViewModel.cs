using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.App.ViewModels.Dialogs;

public sealed partial class CreateLocalAccountDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public event Action<(string Email, string Password)?>? RequestClose;

    [RelayCommand]
    private void Confirm()
    {
        ErrorMessage = string.Empty;
        var email = Email.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            ErrorMessage = "Укажите корректный email.";
            return;
        }

        if (string.IsNullOrEmpty(Password) || Password.Length < 4)
        {
            ErrorMessage = "Пароль должен содержать не менее 4 символов.";
            return;
        }

        if (!string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            ErrorMessage = "Пароли не совпадают.";
            return;
        }

        RequestClose?.Invoke((email, Password));
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(null);
}
