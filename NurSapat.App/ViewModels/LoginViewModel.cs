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
    private readonly IWorkspaceWindowService _workspaceWindowService;

    // Событие для запроса закрытия окна
    public event Action? RequestClose;

    public LoginViewModel(IUserRepository userRepo, SettingsService settings, IWorkspaceWindowService workspaceWindowService)
    {
        _userRepo = userRepo;
        _settings = settings;
        _workspaceWindowService = workspaceWindowService;
    }

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordChar))]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _rememberMe;

    // ✅ ДОБАВЛЕНО: свойство для управления видимостью кнопки "Выйти"
    [ObservableProperty]
    private bool _isLoggedIn;

    public char PasswordChar => IsPasswordVisible ? '\0' : '*';

    public void LoadSavedCredentials()
    {
        if (_settings.GetValue<bool>("RememberMe", false))
        {
            Username = _settings.GetValue<string>("SavedUsername", "") ?? "";
            Password = _settings.GetValue<string>("SavedPassword", "") ?? "";
            RememberMe = true;
        }
        else
        {
            Username = "";
            Password = "";
            RememberMe = false;
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Заполните все поля";
            return;
        }

        IsBusy = true;
        ErrorMessage = "";
        try
        {
            var user = await _userRepo.GetByUsernameAsync(Username);
            if (user != null && user.VerifyPassword(Password))
            {
                if (RememberMe)
                {
                    _settings.SetValue("RememberMe", true);
                    _settings.SetValue("SavedUsername", Username);
                    _settings.SetValue("SavedPassword", Password);
                }
                else
                {
                    _settings.SetValue("RememberMe", false);
                    _settings.SetValue("SavedUsername", "");
                    _settings.SetValue("SavedPassword", "");
                }

                _settings.CurrentUserId = user.Id;
                _workspaceWindowService.OpenWorkspace();

                // ✅ Теперь это свойство существует
                IsLoggedIn = true;
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
    private void GoToRegister() => ErrorMessage = "Регистрация временно недоступна.";

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private void Help() => ErrorMessage = "Обратитесь к администратору системы.";

    [RelayCommand]
    private void Contact() => ErrorMessage = "Контактная форма будет подключена позже.";

    // ✅ ДОБАВЛЕНО: команда для кнопки "Выйти"
    [RelayCommand]
    private void Logout()
    {
        IsLoggedIn = false;
        RequestClose?.Invoke();
    }
}