using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Users;
using NurSapat.App.Services;
using NurSapat.App.Services.Users;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Models;
using System;
using System.Threading.Tasks;

namespace NurSapat.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ISupabaseAuthService _authService;
    private readonly ICachedUserRepository _cachedUsers;
    private readonly ISyncService _syncService;
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
        ISupabaseAuthService authService,
        ICachedUserRepository cachedUsers,
        ISyncService syncService,
        IUserSessionService sessionService,
        SettingsService settings,
        IWorkspaceWindowService workspaceWindowService,
        MainWindowViewModel mainWindowViewModel)
    {
        _authService = authService;
        _cachedUsers = cachedUsers;
        _syncService = syncService;
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
        var email = Username.Trim();
        var password = Password;

        try
        {
            var result = await _authService.SignInAsync(email, password).ConfigureAwait(true);

            if (result.Success && result.Session is not null)
            {
                await _cachedUsers.UpsertSuccessfulLoginAsync(
                    result.Session.UserId,
                    result.Session.Email.Length > 0 ? result.Session.Email : email,
                    password).ConfigureAwait(true);

                CompleteLogin(result.Session, bootstrapSync: true);
                return;
            }

            if (result.IsOffline)
            {
                var offlineSession = await _cachedUsers
                    .TryAuthenticateOfflineAsync(email, password)
                    .ConfigureAwait(true);

                if (offlineSession is not null)
                {
                    _authService.EstablishLocalSession(offlineSession);
                    CompleteLogin(offlineSession, bootstrapSync: false);
                    return;
                }

                ErrorMessage = "Нет сети. Неверный email/пароль или этот пользователь ещё не входил онлайн на этом устройстве.";
                return;
            }

            ErrorMessage = result.ErrorMessage ?? "Неверный email или пароль";
        }
        catch (Exception ex)
        {
            // Network failures bubbling as exceptions → same offline fallback.
            var offlineSession = await _cachedUsers
                .TryAuthenticateOfflineAsync(email, password)
                .ConfigureAwait(true);

            if (offlineSession is not null)
            {
                _authService.EstablishLocalSession(offlineSession);
                CompleteLogin(offlineSession, bootstrapSync: false);
                return;
            }

            ErrorMessage = $"Ошибка входа: {ex.Message}";
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
    private async Task LogoutAsync()
    {
        try
        {
            await _authService.SignOutAsync().ConfigureAwait(true);
        }
        catch
        {
            // Local session still cleared below.
        }

        _sessionService.Clear();
        IsLoggedIn = false;
        RequestClose?.Invoke();
    }

    private void CompleteLogin(AuthSessionInfo session, bool bootstrapSync)
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

        var account = new UserAccountItem
        {
            Id = session.UserId,
            Username = string.IsNullOrWhiteSpace(session.Email) ? Username.Trim() : session.Email,
            GroupNumber = 1,
            Permissions = UserPermissions.CreateAdmin()
        };

        _settings.CurrentUserId = account.Id;
        _sessionService.SetCurrentUser(account);
        _workspaceWindowService.OpenWorkspace();
        IsLoggedIn = true;

        if (bootstrapSync)
        {
            _ = BootstrapSyncSafeAsync();
        }
    }

    private async Task BootstrapSyncSafeAsync()
    {
        try
        {
            await _syncService.BootstrapAfterLoginAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Bootstrap sync after login: {ex.Message}");
        }
    }
}
