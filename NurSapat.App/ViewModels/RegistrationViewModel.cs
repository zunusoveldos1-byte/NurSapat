using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Users;
using NurSapat.App.Services.Users;

namespace NurSapat.App.ViewModels;

public partial class RegistrationViewModel(IUsersStore usersStore, MainWindowViewModel mainViewModel) : ObservableObject
{
    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _confirmPassword = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private bool _hasAcceptedTerms;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError), nameof(HasFormError))]
    private string _errorMessage = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError), nameof(HasUsernameError))]
    private string _usernameError = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError), nameof(HasPasswordError))]
    private string _passwordError = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError), nameof(HasConfirmPasswordError))]
    private string _confirmPasswordError = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RegisterButtonLabel))]
    [NotifyCanExecuteChangedFor(nameof(BackToLoginCommand))]
    [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
    private bool _isBusy;

    public string RegisterButtonLabel => IsBusy ? "Выполняется регистрация…" : "Зарегистрироваться";
    public bool HasFormError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasUsernameError => !string.IsNullOrWhiteSpace(UsernameError);
    public bool HasPasswordError => !string.IsNullOrWhiteSpace(PasswordError);
    public bool HasConfirmPasswordError => !string.IsNullOrWhiteSpace(ConfirmPasswordError);
    public bool HasError => HasFormError || HasUsernameError || HasPasswordError || HasConfirmPasswordError;

    partial void OnUsernameChanged(string value) => UsernameError = "";

    partial void OnPasswordChanged(string value)
    {
        PasswordError = "";
        ConfirmPasswordError = "";
    }

    partial void OnConfirmPasswordChanged(string value) => ConfirmPasswordError = "";

    private bool CanRegister() => HasAcceptedTerms && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanRegister))]
    private async Task RegisterAsync()
    {
        if (!CanRegister())
        {
            return;
        }

        ErrorMessage = "";
        UsernameError = string.IsNullOrWhiteSpace(Username) ? "Введите логин" : "";
        PasswordError = string.IsNullOrWhiteSpace(Password) ? "Введите пароль" : "";
        ConfirmPasswordError = string.IsNullOrWhiteSpace(ConfirmPassword)
            ? "Повторите пароль"
            : Password != ConfirmPassword ? "Пароли не совпадают" : "";
        if (HasUsernameError || HasPasswordError || HasConfirmPasswordError)
        {
            return;
        }

        IsBusy = true;
        var username = Username.Trim();
        var password = Password;
        try
        {
            // The shared users store and BCrypt hashing are synchronous.
            var created = await Task.Run(() =>
            {
                if (usersStore.GetByUsername(username) is not null)
                {
                    return false;
                }

                usersStore.Add(UserAccountItem.Create(username, password, 2, UserPermissions.CreateOperator()));
                return true;
            });
            if (!created)
            {
                UsernameError = "Пользователь с таким именем уже существует";
                return;
            }

            if (ReferenceEquals(mainViewModel.CurrentView, this))
            {
                mainViewModel.NavigateToLogin();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка регистрации: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanBackToLogin() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanBackToLogin))]
    private void BackToLogin() => mainViewModel.NavigateToLogin();
}
