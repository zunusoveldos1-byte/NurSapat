using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;
using NurSapat.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace NurSapat.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IUserRepository _userRepo;
    private readonly SettingsService _settings;
    private readonly MainWindowViewModel _mainVM;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isBusy;

    public LoginViewModel(IUserRepository userRepo, SettingsService settings, MainWindowViewModel mainVM)
    {
        _userRepo = userRepo;
        _settings = settings;
        _mainVM = mainVM;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        ErrorMessage = "";
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Заполните все поля";
            return;
        }

        IsBusy = true;
        try
        {
            var user = await _userRepo.GetByUsernameAsync(Username);
            if (user != null && user.VerifyPassword(Password))
            {
                _settings.CurrentUserId = user.Id;
                _mainVM.NavigateToMain();
            }
            else
            {
                ErrorMessage = "Неверное имя пользователя или пароль";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка подключения: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void GoToRegister() => _mainVM.NavigateToRegister();
}