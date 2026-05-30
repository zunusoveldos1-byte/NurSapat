using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;
using NurSapat.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace NurSapat.App.ViewModels;

public sealed partial class LoginViewModel(
    IUserRepository userRepo,
    SettingsService settings,
    IWorkspaceWindowService workspaceWindowService) : ObservableObject
{
    private readonly IUserRepository _userRepo = userRepo;
    private readonly SettingsService _settings = settings;
    private readonly IWorkspaceWindowService _workspaceWindowService = workspaceWindowService;

    public event EventHandler? LoginSucceeded;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isBusy;

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
            // Ищем пользователя в базе
            var user = await _userRepo.GetByUsernameAsync(Username);

            if (user is not null && user.VerifyPassword(Password))
            {
                _settings.CurrentUserId = user.Id;
                CompleteLogin();
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
    private void GoToRegister()
    {
        ErrorMessage = "Регистрация будет подключена на этапе управления пользователями.";
    }

    private void CompleteLogin()
    {
        // Сервис сам откроет окно и закроет текущее.
        _workspaceWindowService.OpenWorkspace();
        LoginSucceeded?.Invoke(this, EventArgs.Empty); // Можно оставить, но сервис уже всё сделал
    }
}