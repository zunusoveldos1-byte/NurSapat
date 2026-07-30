using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Users;
using NurSapat.App.Services;
using NurSapat.App.Services.Users;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Diagnostics;
using NurSapat.Infrastructure.Sync.Models;
using NurSapat.App.Services.Persistence;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NurSapat.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly ISupabaseAuthService _authService;
    private readonly INetworkStatusService _networkStatus;
    private readonly ISyncService _syncService;
    private readonly IUserSessionService _sessionService;
    private readonly SettingsService _settings;
    private readonly IWorkspaceWindowService _workspaceWindowService;
    private readonly MainWindowViewModel _mainWindowViewModel;
    private readonly IApplicationPersistenceInitializer _persistenceInitializer;

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
            StartupTrace.Error("[LOG] LoginViewModel.OpenUrl", ex);
            ErrorMessage = $"Не удалось открыть ссылку: {ex.Message}";
        }
    }

    // Событие для запроса закрытия окна
    public event Action? RequestClose;

    public LoginViewModel(
        ISupabaseAuthService authService,
        INetworkStatusService networkStatus,
        ISyncService syncService,
        IUserSessionService sessionService,
        SettingsService settings,
        IWorkspaceWindowService workspaceWindowService,
        MainWindowViewModel mainWindowViewModel,
        IApplicationPersistenceInitializer persistenceInitializer)
    {
        _authService = authService;
        _networkStatus = networkStatus;
        _syncService = syncService;
        _sessionService = sessionService;
        _settings = settings;
        _workspaceWindowService = workspaceWindowService;
        _mainWindowViewModel = mainWindowViewModel;
        _persistenceInitializer = persistenceInitializer;
        ApplyNetworkStatus(_networkStatus.Current);
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

    [ObservableProperty]
    private string _networkStatusDisplay = "⚪ Проверка сети…";

    public char PasswordChar => IsPasswordVisible ? '\0' : '*';

    public void LoadSavedCredentials()
    {
        Username = "";
        Password = "";
        RememberMe = false;
    }

    public void Activate()
    {
        _networkStatus.StatusChanged -= OnNetworkStatusChanged;
        _networkStatus.StatusChanged += OnNetworkStatusChanged;
        _networkStatus.Start();
        ApplyNetworkStatus(_networkStatus.Current);
    }

    public void Deactivate()
    {
        _networkStatus.StatusChanged -= OnNetworkStatusChanged;
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
            AuthResult result;
            try
            {
                result = await _authService
                    .SignInAsync(email, password, RememberMe)
                    .ConfigureAwait(true);
            }
            catch (Exception ex) when (IsNetworkFailure(ex))
            {
                StartupTrace.Error(
                    "[LOG] LoginViewModel: сетевой сбой online sign-in; переход к offline login",
                    ex);
                // Defense in depth: even an SDK-specific DNS wrapper that escaped
                // AuthService must be converted to local credential validation.
                result = await _authService
                    .TryOfflineLoginAsync(email, password)
                    .ConfigureAwait(true);
            }

            if (result.Success && result.Session is not null)
            {
                await CompleteLoginAsync(
                    result.Session,
                    isOffline: result.IsOffline).ConfigureAwait(true);
                return;
            }

            ErrorMessage = result.ErrorMessage ?? "Неверный email или пароль";
        }
        catch (Exception ex)
        {
            StartupTrace.Error("[LOG] LoginViewModel.LoginAsync", ex);
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
        catch (Exception ex)
        {
            StartupTrace.Error("[LOG] LoginViewModel.LogoutAsync", ex);
            // Local session is still cleared below.
        }

        _sessionService.Clear();
        IsLoggedIn = false;
        RequestClose?.Invoke();
    }

    private async Task CompleteLoginAsync(AuthSessionInfo session, bool isOffline)
    {
        var bootstrapSync = !isOffline;
        if (bootstrapSync)
        {
            try
            {
                await _persistenceInitializer.EnsureReadyAsync().ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    $"Вход Supabase выполнен, но база ERP недоступна: {GetDatabaseError(ex)}";
                return;
            }
        }

        var account = new UserAccountItem
        {
            Id = session.UserId,
            Username = string.IsNullOrWhiteSpace(session.Email) ? Username.Trim() : session.Email,
            GroupNumber = 1,
            Permissions = UserPermissions.CreateAdmin()
        };

        _settings.CurrentUserId = account.Id;
        _sessionService.SetCurrentUser(account, isOffline);
        _workspaceWindowService.OpenWorkspace();
        IsLoggedIn = true;

        if (bootstrapSync)
        {
            _ = BootstrapSyncSafeAsync();
        }
    }

    private static string GetDatabaseError(Exception exception)
    {
        var current = exception;
        while (current.InnerException is not null)
        {
            current = current.InnerException;
        }

        return current.Message;
    }

    private async Task BootstrapSyncSafeAsync()
    {
        try
        {
            await _syncService.BootstrapAfterLoginAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            StartupTrace.Error("[LOG] LoginViewModel: bootstrap sync after login", ex);
        }
    }

    private void OnNetworkStatusChanged(object? sender, NetworkStatusSnapshot status)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() => ApplyNetworkStatus(status));
    }

    private void ApplyNetworkStatus(NetworkStatusSnapshot status)
    {
        var indicator = status.Quality switch
        {
            NetworkQuality.Excellent => "🟢",
            NetworkQuality.Poor => "🟡",
            NetworkQuality.Offline => "🟠",
            _ => "⚪"
        };
        NetworkStatusDisplay = $"{indicator} {status.DisplayText}";
    }

    private static bool IsNetworkFailure(Exception ex)
    {
        var pending = new System.Collections.Generic.Stack<Exception>();
        pending.Push(ex);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (current is HttpRequestException or SocketException or
                TaskCanceledException or TimeoutException)
            {
                return true;
            }

            var message = current.Message;
            if (!string.IsNullOrWhiteSpace(message) &&
                (message.Contains("Этот хост неизвестен", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
                 message.Contains("Host not found", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    pending.Push(inner);
                }
            }
            else if (current.InnerException is not null)
            {
                pending.Push(current.InnerException);
            }
        }

        return false;
    }
}
