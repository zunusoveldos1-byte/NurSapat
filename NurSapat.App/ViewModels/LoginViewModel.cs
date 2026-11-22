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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NurSapat.Infrastructure.Persistence;
using NurSapat.Infrastructure.Persistence.Auth;
using System.Text.Json;

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
    private readonly IConfiguration _configuration;
    private readonly IServiceProvider _services;

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
        IApplicationPersistenceInitializer persistenceInitializer,
        IConfiguration configuration,
        IServiceProvider services)
    {
        _authService = authService;
        _networkStatus = networkStatus;
        _syncService = syncService;
        _sessionService = sessionService;
        _settings = settings;
        _workspaceWindowService = workspaceWindowService;
        _mainWindowViewModel = mainWindowViewModel;
        _persistenceInitializer = persistenceInitializer;
        _configuration = configuration;
        _services = services;
        ApplyNetworkStatus(_networkStatus.Current);
    }

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _password = "";

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
    [NotifyPropertyChangedFor(nameof(LoginButtonLabel))]
    [NotifyCanExecuteChangedFor(nameof(GoToRegisterCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isRefreshingNetwork;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PasswordVisibilityLabel))]
    private bool _isPasswordVisible;

    [ObservableProperty]
    private bool _rememberMe;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NetworkStatusDisplay), nameof(HasGoodConnection), nameof(HasSlowConnection), nameof(IsServerOffline))]
    private NetworkQuality _connectionQuality = NetworkQuality.Checking;

    public string PasswordVisibilityLabel => IsPasswordVisible ? "Скрыть пароль" : "Показать пароль";
    public string LoginButtonLabel => IsBusy ? "Выполняется вход…" : "Войти";
    public bool HasFormError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasUsernameError => !string.IsNullOrWhiteSpace(UsernameError);
    public bool HasPasswordError => !string.IsNullOrWhiteSpace(PasswordError);
    public bool HasError => HasFormError || HasUsernameError || HasPasswordError;
    public bool HasGoodConnection => ConnectionQuality == NetworkQuality.Excellent;
    public bool HasSlowConnection => ConnectionQuality == NetworkQuality.Poor;
    public bool IsServerOffline => ConnectionQuality == NetworkQuality.Offline;
    public string NetworkStatusDisplay => ConnectionQuality switch
    {
        NetworkQuality.Excellent => "Сервер доступен",
        NetworkQuality.Poor => "Нестабильное подключение",
        NetworkQuality.Offline => "Нет подключения к серверу",
        _ => "Проверка подключения…"
    };

    private bool UsesLocalAuthentication => _configuration["Authentication:Mode"] == "LocalPostgreSQL";

    partial void OnUsernameChanged(string value)
    {
        UsernameError = "";
        PasswordError = "";
    }

    partial void OnPasswordChanged(string value) => PasswordError = "";

    public void LoadSavedCredentials()
    {
        Username = "";
        Password = "";
        RememberMe = false;
    }

    public void Activate()
    {
        if (UsesLocalAuthentication)
        {
            RefreshNetworkStatusCommand.Execute(null);
            return;
        }

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
        ErrorMessage = "";
        UsernameError = string.IsNullOrWhiteSpace(Username) ? "Введите логин" : "";
        PasswordError = string.IsNullOrWhiteSpace(Password) ? "Введите пароль" : "";
        if (HasUsernameError || HasPasswordError)
        {
            return;
        }

        IsBusy = true;
        var email = Username.Trim();
        var password = Password;

        try
        {
            if (UsesLocalAuthentication)
            {
                await _persistenceInitializer.EnsureReadyAsync();
                var user = await _services.GetRequiredService<LocalAuthenticationService>()
                    .SignInAsync(email, password);
                if (user is null)
                {
                    PasswordError = "Неверный логин или пароль";
                    return;
                }

                var account = new UserAccountItem
                {
                    Id = user.Id,
                    Username = user.Username,
                    PasswordHash = user.PasswordHash,
                    GroupNumber = user.GroupNumber,
                    Permissions = JsonSerializer.Deserialize<UserPermissions>(user.PermissionsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new UserPermissions()
                };
                _settings.CurrentUserId = account.Id;
                _sessionService.SetCurrentUser(account, isOffline: false);
                _workspaceWindowService.OpenWorkspace();
                IsLoggedIn = true;
                return;
            }

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

            if (result.ErrorMessage?.TrimEnd('.') is null or "" or "Неверный email или пароль" or "Неверный логин или пароль")
            {
                PasswordError = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "Неверный email или пароль"
                    : result.ErrorMessage;
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (PostgresException ex) when (UsesLocalAuthentication && ex.SqlState == PostgresErrorCodes.InvalidPassword)
        {
            StartupTrace.Error("[LOG] LoginViewModel.LoginAsync", ex);
            ErrorMessage = "PostgreSQL отклонил пароль подключения. Запустите NurSapat через Start-NurSapat-Local.cmd или проверьте настройки базы.";
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

    private bool CanGoToRegister() => !IsBusy;

    [RelayCommand(CanExecute = nameof(CanGoToRegister))]
    private void GoToRegister()
    {
        if (UsesLocalAuthentication)
        {
            _mainWindowViewModel.NavigateToRegister();
            return;
        }

        ErrorMessage = "Для регистрации обратитесь к администратору системы.";
    }

    [RelayCommand]
    private async Task RefreshNetworkStatusAsync()
    {
        ConnectionQuality = NetworkQuality.Checking;
        IsRefreshingNetwork = true;
        try
        {
            if (!UsesLocalAuthentication)
            {
                ApplyNetworkStatus(await _networkStatus.CheckNowAsync());
                return;
            }

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            var factory = _services.GetRequiredService<IDbContextFactory<NurSapatDbContext>>();
            await using var database = await factory.CreateDbContextAsync(timeout.Token);
            ConnectionQuality = await database.Database.CanConnectAsync(timeout.Token)
                ? NetworkQuality.Excellent
                : NetworkQuality.Offline;
        }
        catch (Exception ex)
        {
            StartupTrace.Error("[LOG] Проверка подключения к серверу", ex);
            ConnectionQuality = NetworkQuality.Offline;
        }
        finally
        {
            IsRefreshingNetwork = false;
        }
    }

    [RelayCommand]
    private void TogglePasswordVisibility() => IsPasswordVisible = !IsPasswordVisible;

    [RelayCommand]
    private void Help() => ErrorMessage = "Обратитесь к администратору системы.";

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

    private void ApplyNetworkStatus(NetworkStatusSnapshot status) => ConnectionQuality = status.Quality;

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
