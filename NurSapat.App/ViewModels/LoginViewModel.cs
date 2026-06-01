using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;
using NurSapat.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace NurSapat.App.ViewModels;

public partial class LoginViewModel(IUserRepository userRepo, SettingsService settings, IWorkspaceWindowService workspaceWindowService) : ObservableObject
{
    private readonly IUserRepository _userRepo = userRepo;
    private readonly SettingsService _settings = settings;
    private readonly IWorkspaceWindowService _workspaceWindowService = workspaceWindowService;

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
                _settings.CurrentUserId = user.Id;
                _workspaceWindowService.OpenWorkspace();
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
}