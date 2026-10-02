using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Users;
using NurSapat.App.Services.Users;
using System;
using System.Threading.Tasks;

namespace NurSapat.App.ViewModels;

public partial class RegistrationViewModel : ObservableObject
{
    private readonly IUsersStore _usersStore;
    private readonly MainWindowViewModel _mainVM;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _confirmPassword = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isBusy;

    public RegistrationViewModel(IUsersStore usersStore, MainWindowViewModel mainVM)
    {
        _usersStore = usersStore;
        _mainVM = mainVM;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        ErrorMessage = "";
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Заполните все поля";
            return;
        }
        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Пароли не совпадают";
            return;
        }

        IsBusy = true;
        try
        {
            await Task.Yield();
            var existing = _usersStore.GetByUsername(Username);
            if (existing != null)
            {
                ErrorMessage = "Пользователь с таким именем уже существует";
                return;
            }

            var newUser = UserAccountItem.Create(Username, Password, 2, UserPermissions.CreateOperator());
            _usersStore.Add(newUser);
            _mainVM.NavigateToLogin();
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

    [RelayCommand]
    private void BackToLogin() => _mainVM.NavigateToLogin();
}
