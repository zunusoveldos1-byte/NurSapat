using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Users;
using NurSapat.App.Services;
using NurSapat.App.Services.Users;
using System;
using System.Threading.Tasks;

namespace NurSapat.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IUsersStore _usersStore;
    private readonly IUserSessionService _sessionService;
    private readonly SettingsService _settings;
    private readonly IWorkspaceWindowService _workspaceWindowService;
    private readonly MainWindowViewModel _mainWindowViewModel;

    // Константные данные для контактов
    public string SupportPhoneDisplay => "+996 (559) 34-00-32";
    public string SupportPhoneTelUri => "tel:+996559340032";
    public string SupportEmail => "zunusoveldos1@gmail.com";
    public string SupportEmailMailtoUri => "mailto:zunusoveldos1@gmail.com";
    public string SupportTelegramUrl => "https://t.me/willofeldos";

    public IRelayCommand OpenTelegramCommand => new RelayCommand(() => OpenUrl(SupportTelegramUrl));
    public IRelayCommand OpenPhoneCommand => new RelayCommand(() => OpenUrl(SupportPhoneTelUri));
    public IRelayCommand OpenEmailCommand => new RelayCommand(() => OpenUrl(SupportEmailMailtoUri));
    
    private void OpenUrl(string url)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Не удалось открыть ссылку: {ex.Message}";
        }
    }

    // Событие для запроса закрытия окна
    public event Action? RequestClose;

    public LoginViewModel(
        IUsersStore usersStore,
        IUserSessionService sessionService,
        SettingsService settings,
        IWorkspaceWindowService workspaceWindowService,
        MainWindowViewModel mainWindowViewModel)
    {
        _usersStore = usersStore;
        _sessionService = sessionService;
        _settings = settings;
        _workspaceWindowService = workspaceWindowService;
        _mainWindowViewModel = mainWindowViewModel;
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

    [ObservableProperty]
    private bool _isContactModalOpen;

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
            await Task.Yield();
            var account = _usersStore.GetByUsername(Username);
            if (account is not null && account.VerifyPassword(Password))
            {
                CompleteLogin(account);
                return;
            }

            ErrorMessage = "Неверное имя пользователя или пароль";
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
    private void GoToRegister() => _mainWindowViewModel.NavigateToRegister();

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private void Help() => ErrorMessage = "Обратитесь к администратору системы.";

    [RelayCommand]
    private void OpenContact() => IsContactModalOpen = true;

    [RelayCommand]
    private void CloseContact() => IsContactModalOpen = false;

    [RelayCommand]
    private void Logout()
    {
        _sessionService.Clear();
        IsLoggedIn = false;
        RequestClose?.Invoke();
    }

    private void CompleteLogin(UserAccountItem account)
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

        _settings.CurrentUserId = account.Id;
        _sessionService.SetCurrentUser(account);
        _workspaceWindowService.OpenWorkspace();
        IsLoggedIn = true;
    }
}
