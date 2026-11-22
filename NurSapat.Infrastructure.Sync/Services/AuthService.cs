using Microsoft.Extensions.Logging;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Auth;
using NurSapat.Infrastructure.Sync.Diagnostics;
using NurSapat.Infrastructure.Sync.Models;
using Supabase.Gotrue;
using System.Net.Sockets;

namespace NurSapat.Infrastructure.Sync.Services;

/// <summary>
/// Supabase authentication with a DPAPI-protected, PBKDF2-verified offline
/// credential that is valid for exactly seven days after the last online login.
/// </summary>
public sealed class AuthService : ISupabaseAuthService
{
    private static readonly TimeSpan StartupNetworkProbeTimeout = TimeSpan.FromSeconds(2);

    private readonly SupabaseClientFactory _clientFactory;
    private readonly INetworkStatusService _network;
    private readonly ProtectedSessionCache _sessionCache;
    private readonly DatabaseSaltStore _databaseSaltStore;
    private readonly ILocalDbService _localDb;
    private readonly ICachedUserRepository _cachedUsers;
    private readonly ILogger<AuthService>? _logger;
    private AuthSessionInfo? _current;
    private bool _startupClockInvalid;

    public AuthService(
        SupabaseClientFactory clientFactory,
        INetworkStatusService network,
        ProtectedSessionCache sessionCache,
        DatabaseSaltStore databaseSaltStore,
        ILocalDbService localDb,
        ICachedUserRepository cachedUsers,
        ILogger<AuthService>? logger = null)
    {
        _clientFactory = clientFactory;
        _network = network;
        _sessionCache = sessionCache;
        _databaseSaltStore = databaseSaltStore;
        _localDb = localDb;
        _cachedUsers = cachedUsers;
        _logger = logger;
    }

    public AuthSessionInfo? CurrentSession => _current;

    public Guid? CurrentUserId => _current?.UserId;

    public bool IsAuthenticated => _current is { UserId: var id } && id != Guid.Empty;

    public async Task<bool> HasRememberedSessionAsync(
        CancellationToken cancellationToken = default)
    {
        var cache = await _sessionCache.ReadAsync(cancellationToken).ConfigureAwait(false);
        var isUsable = IsUsableOfflineCache(cache);
        StartupTrace.Info(
            $"[LOG] Проверка содержимого session.dat: " +
            $"{(isUsable ? "Успех" : "Ошибка: отсутствуют обязательные данные локальной сессии")}");
        return isUsable;
    }

    public async Task<AuthResult> RestoreSessionAsync(
        CancellationToken cancellationToken = default)
    {
        _current = null;
        var cache = await _sessionCache.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (!IsUsableOfflineCache(cache))
        {
            return AuthResult.Fail("Сохранённая сессия отсутствует.");
        }

        var isOnline = await IsNetworkAvailableSafeAsync(cancellationToken).ConfigureAwait(false);
        StartupTrace.Info(
            $"[LOG] Проверка интернет-соединения: {(isOnline ? "Онлайн" : "Оффлайн")}");
        if (isOnline && _clientFactory.IsConfigured &&
            !string.IsNullOrWhiteSpace(cache!.AccessToken) &&
            !string.IsNullOrWhiteSpace(cache.RefreshToken))
        {
            try
            {
                var client = await _clientFactory.GetClientAsync(cancellationToken)
                    .ConfigureAwait(false);
                var restored = await client.Auth.SetSession(
                        cache.AccessToken,
                        cache.RefreshToken,
                        forceAccessTokenRefresh: true)
                    .ConfigureAwait(false);
                restored ??= client.Auth.CurrentSession;
                if (restored?.User?.Id is null)
                {
                    _logger?.LogWarning(
                        "Supabase did not restore the remembered session; using the valid local session.");
                }
                else
                {
                    var session = MapSession(restored);
                    await _localDb.UnlockWithDerivedKeyAsync(
                            cache.Email,
                            cache.DatabaseKey,
                            cancellationToken)
                        .ConfigureAwait(false);
                    var nowUtc = DateTimeOffset.UtcNow;
                    await UpsertRememberedCredentialAsync(
                            cache,
                            lastOnlineUtc: nowUtc,
                            lastLocalAppRunUtc: nowUtc,
                            cancellationToken)
                        .ConfigureAwait(false);
                    await _sessionCache.SaveRestoredOnlineSessionAsync(
                            cache,
                            session,
                            nowUtc,
                            cancellationToken)
                        .ConfigureAwait(false);

                    _startupClockInvalid = false;
                    _current = session;
                    StartupTrace.Info("[LOG] Восстановление сессии Supabase: Успех");
                    return AuthResult.Ok(session);
                }
            }
            catch (Exception ex) when (
                !cancellationToken.IsCancellationRequested &&
                IsNetworkFailure(ex))
            {
                _logger?.LogWarning(
                    ex,
                    "Network dropped while restoring the remembered session; trying offline.");
                StartupTrace.Error(
                    "[LOG] Supabase SDK: сетевой сбой, переход к локальной сессии",
                    ex);
            }
            catch (Exception ex)
            {
                // The protected local session is the authority for offline access.
                // An SDK/server failure must not erase it or route startup to Login.
                _logger?.LogWarning(
                    ex,
                    "Supabase session refresh failed; using the valid local session.");
                StartupTrace.Error(
                    "[LOG] Supabase SDK: ошибка восстановления, переход к локальной сессии",
                    ex);
                await _clientFactory.ResetAsync().ConfigureAwait(false);
            }
        }

        StartupTrace.Info("[LOG] Передача управления RestoreRememberedOfflineAsync...");
        return await RestoreRememberedOfflineAsync(cache!, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AuthResult> TryOfflineLoginFromCacheAsync(
        CancellationToken cancellationToken = default)
    {
        _current = null;
        var cache = await _sessionCache.ReadAsync(cancellationToken).ConfigureAwait(false);
        if (cache is null ||
            cache.UserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(cache.Email))
        {
            return AuthResult.Fail(
                "Сохранённая оффлайн-сессия отсутствует.",
                offline: true);
        }

        return await RestoreRememberedOfflineAsync(cache, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<AuthResult> SignInAsync(
        string email,
        string password,
        bool rememberMe,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Fail("Введите email и пароль.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var isOnline = await IsNetworkAvailableSafeAsync(cancellationToken).ConfigureAwait(false);
        if (!isOnline)
        {
            return await TryOfflineLoginAsync(normalizedEmail, password, cancellationToken)
                .ConfigureAwait(false);
        }

        if (!_clientFactory.IsConfigured)
        {
            return AuthResult.Fail("Supabase не настроен.");
        }

        try
        {
            var client = await _clientFactory.GetClientAsync(cancellationToken).ConfigureAwait(false);
            var remote = await client.Auth.SignInWithPassword(normalizedEmail, password)
                .ConfigureAwait(false);
            if (remote?.User?.Id is null)
            {
                return AuthResult.Fail("Неверный email или пароль.");
            }

            var session = MapSession(remote);
            var databaseSalt = await _databaseSaltStore.GetOrCreateAsync(
                    normalizedEmail,
                    cancellationToken)
                .ConfigureAwait(false);
            var databaseKey = Local.SqliteConnectionFactory.DeriveKey(
                normalizedEmail,
                password,
                databaseSalt);
            try
            {
                await _localDb.UnlockWithDerivedKeyAsync(
                        normalizedEmail,
                        databaseKey,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (rememberMe)
                {
                    var nowUtc = DateTimeOffset.UtcNow;
                    await _cachedUsers.UpsertSuccessfulLoginAsync(
                            session.UserId,
                            normalizedEmail,
                            password,
                            cancellationToken)
                        .ConfigureAwait(false);
                    await _sessionCache.SaveSuccessfulLoginAsync(
                            session,
                            password,
                            databaseSalt,
                            databaseKey,
                            nowUtc,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await _cachedUsers.DeleteByEmailAsync(normalizedEmail, cancellationToken)
                        .ConfigureAwait(false);
                    await _sessionCache.DeleteAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(databaseKey);
            }

            _startupClockInvalid = false;
            _current = session;
            return AuthResult.Ok(session);
        }
        catch (Exception ex) when (
            !cancellationToken.IsCancellationRequested &&
            IsNetworkFailure(ex))
        {
            _logger?.LogWarning(ex, "Online sign-in failed because the network dropped.");
            StartupTrace.Error(
                "[LOG] Онлайн-вход: сетевой сбой, переход к TryOfflineLoginAsync",
                ex);
            return await TryOfflineLoginAsync(normalizedEmail, password, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Sign-in failed for {Email}.", normalizedEmail);
            StartupTrace.Error("[LOG] Онлайн-вход: необработанная ошибка", ex);
            return AuthResult.Fail(SimplifyAuthError(ex.Message));
        }
    }

    public void EstablishLocalSession(AuthSessionInfo session)
    {
        _current = session;
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        Supabase.Client? client = null;
        try
        {
            if (_clientFactory.IsConfigured && _network.Current.IsOnline)
            {
                client = await _clientFactory.GetClientAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Could not prepare remote sign-out.");
            StartupTrace.Error("[LOG] Logout: ошибка подготовки Supabase sign-out", ex);
        }

        _current = null;
        _startupClockInvalid = false;
        await _sessionCache.DeleteAsync(CancellationToken.None).ConfigureAwait(false);
        await _localDb.LockAsync().ConfigureAwait(false);

        try
        {
            if (client is not null)
            {
                await client.Auth.SignOut().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Remote sign-out failed; local secrets are still cleared.");
            StartupTrace.Error(
                "[LOG] Logout: удалённый sign-out завершился ошибкой; локальная сессия очищена",
                ex);
        }
        finally
        {
            await _clientFactory.ResetAsync().ConfigureAwait(false);
        }
    }

    public Task<AuthResult> TryOfflineLoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult(AuthResult.Fail(
                "Неверный логин или пароль",
                offline: true));
        }

        return SignInOfflineCoreAsync(
            email.Trim().ToLowerInvariant(),
            password,
            cancellationToken);
    }

    private async Task<AuthResult> SignInOfflineCoreAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTimeOffset.UtcNow;
        var validation = await _sessionCache.ValidateOfflineMetadataAsync(
                email,
                nowUtc,
                _startupClockInvalid,
                cancellationToken)
            .ConfigureAwait(false);

        if (!validation.Success || validation.Cache is null)
        {
            var message = validation.TimeTampering ||
                          string.Equals(
                              validation.ErrorMessage,
                              "Срок оффлайн-сессии истек. Подключитесь к сети",
                              StringComparison.Ordinal)
                ? validation.ErrorMessage
                : "Неверный логин или пароль";
            return AuthResult.Fail(
                message ?? "Неверный логин или пароль",
                offline: true,
                timeTamperingDetected: validation.TimeTampering);
        }

        // Verify before opening SQLCipher. Otherwise a wrong password could create
        // a new empty database with the wrong key when the local DB was removed.
        if (!ProtectedSessionCache.VerifyPassword(validation.Cache, password))
        {
            return AuthResult.Fail("Неверный логин или пароль", offline: true);
        }

        try
        {
            await _localDb.UnlockAsync(email, password, cancellationToken).ConfigureAwait(false);

            var cachedUser = await _cachedUsers.FindByEmailAsync(email, cancellationToken)
                .ConfigureAwait(false);
            if (cachedUser is null)
            {
                if (!ProtectedSessionCache.VerifyPassword(validation.Cache, password))
                {
                    await _localDb.LockAsync().ConfigureAwait(false);
                    return AuthResult.Fail("Неверный логин или пароль", offline: true);
                }

                await _cachedUsers.ImportRememberedCredentialAsync(
                        validation.Cache.UserId,
                        validation.Cache.Email,
                        ProtectedSessionCache.GetEncodedPasswordHash(validation.Cache),
                        validation.Cache.LastOnlineUtc,
                        validation.Cache.LastLocalAppRunUtc,
                        cancellationToken)
                    .ConfigureAwait(false);
                cachedUser = await _cachedUsers.FindByEmailAsync(email, cancellationToken)
                    .ConfigureAwait(false);
            }

            var localValidationError = ValidateLocalCredential(
                cachedUser,
                validation.Cache,
                password,
                nowUtc,
                _startupClockInvalid);
            if (localValidationError is not null)
            {
                await _localDb.LockAsync().ConfigureAwait(false);
                return AuthResult.Fail(
                    localValidationError.Value.Message,
                    offline: true,
                    timeTamperingDetected: localValidationError.Value.TimeTampering);
            }

            await _cachedUsers.TouchLastLocalAppRunAsync(
                    cachedUser!.Id,
                    nowUtc,
                    cancellationToken)
                .ConfigureAwait(false);
            await _sessionCache.TouchAutomaticOfflineSessionAsync(
                    validation.Cache,
                    nowUtc,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Encrypted local database could not be unlocked.");
            StartupTrace.Error(
                "[LOG] Результат проверки SQLCipher/cached_users: Ошибка",
                ex);
            return AuthResult.Fail(ex.Message, offline: true);
        }

        var session = new AuthSessionInfo
        {
            UserId = validation.Cache.UserId,
            Email = validation.Cache.Email,
            AccessToken = validation.Cache.AccessToken,
            RefreshToken = validation.Cache.RefreshToken
        };
        _current = session;
        return AuthResult.Ok(session, offline: true);
    }

    private async Task<AuthResult> RestoreRememberedOfflineAsync(
        OfflineSessionCache cache,
        CancellationToken cancellationToken)
    {
        StartupTrace.Info("[LOG] Начало RestoreRememberedOfflineAsync...");
        var nowUtc = DateTimeOffset.UtcNow;
        StartupTrace.Info(
            $"[LOG] Проверка 7-дневного лимита: NowUtc={nowUtc:O}, " +
            $"LastOnlineUtc={cache.LastOnlineUtc:O}, " +
            $"LastLocalAppRunUtc={cache.LastLocalAppRunUtc:O}");
        if (nowUtc < cache.LastLocalAppRunUtc || nowUtc < cache.LastOnlineUtc)
        {
            _startupClockInvalid = true;
            StartupTrace.Info(
                "[LOG] Результат проверки SQLCipher/cached_users: " +
                "Ошибка: обнаружено несоответствие системного времени");
            return AuthResult.Fail(
                "Обнаружено несоответствие системного времени. Войдите в сеть для синхронизации",
                offline: true,
                timeTamperingDetected: true);
        }

        if (nowUtc > cache.LastOnlineUtc.AddDays(7))
        {
            StartupTrace.Info(
                "[LOG] Результат проверки SQLCipher/cached_users: " +
                "Ошибка: истёк 7-дневный лимит оффлайн-доступа");
            return AuthResult.Fail(
                "Срок оффлайн-сессии истек. Подключитесь к сети",
                offline: true);
        }

        if (cache.DatabaseKey.Length != 32)
        {
            StartupTrace.Info(
                "[LOG] Результат проверки SQLCipher/cached_users: " +
                "Ошибка: отсутствует корректный 32-байтовый ключ SQLCipher");
            return AuthResult.Fail(
                "Сохранённая сессия создана старой версией приложения. Войдите повторно.",
                offline: true);
        }

        try
        {
            await _localDb.UnlockWithDerivedKeyAsync(
                    cache.Email,
                    cache.DatabaseKey,
                    cancellationToken)
                .ConfigureAwait(false);
            StartupTrace.Info("[LOG] Проверка SQLCipher: Успех");

            var cachedUser = await _cachedUsers.FindByEmailAsync(cache.Email, cancellationToken)
                .ConfigureAwait(false);
            StartupTrace.Info(
                $"[LOG] Поиск пользователя в cached_users: " +
                $"{(cachedUser is null ? "Не найден; импорт из session.dat" : "Найден")}");
            if (cachedUser is null)
            {
                await _cachedUsers.ImportRememberedCredentialAsync(
                        cache.UserId,
                        cache.Email,
                        ProtectedSessionCache.GetEncodedPasswordHash(cache),
                        cache.LastOnlineUtc,
                        cache.LastLocalAppRunUtc,
                        cancellationToken)
                    .ConfigureAwait(false);
                cachedUser = await _cachedUsers.FindByEmailAsync(cache.Email, cancellationToken)
                    .ConfigureAwait(false);
            }

            var localValidationError = ValidateLocalCredential(
                cachedUser,
                cache,
                password: null,
                nowUtc,
                _startupClockInvalid);
            if (localValidationError is not null)
            {
                StartupTrace.Info(
                    $"[LOG] Результат проверки SQLCipher/cached_users: " +
                    $"Ошибка: {localValidationError.Value.Message}");
                await _localDb.LockAsync().ConfigureAwait(false);
                return AuthResult.Fail(
                    localValidationError.Value.Message,
                    offline: true,
                    timeTamperingDetected: localValidationError.Value.TimeTampering);
            }

            await _cachedUsers.TouchLastLocalAppRunAsync(
                    cachedUser!.Id,
                    nowUtc,
                    cancellationToken)
                .ConfigureAwait(false);
            await _sessionCache.TouchAutomaticOfflineSessionAsync(
                    cache,
                    nowUtc,
                    cancellationToken)
                .ConfigureAwait(false);
            StartupTrace.Info("[LOG] Результат проверки SQLCipher/cached_users: Успех");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Remembered local database could not be unlocked.");
            StartupTrace.Error(
                "[LOG] Результат проверки SQLCipher/cached_users: Ошибка",
                ex);
            await _localDb.LockAsync().ConfigureAwait(false);
            return AuthResult.Fail(ex.Message, offline: true);
        }

        var session = new AuthSessionInfo
        {
            UserId = cache.UserId,
            Email = cache.Email,
            AccessToken = cache.AccessToken,
            RefreshToken = cache.RefreshToken
        };
        _startupClockInvalid = false;
        _current = session;
        return AuthResult.Ok(session, offline: true);
    }

    private async Task UpsertRememberedCredentialAsync(
        OfflineSessionCache cache,
        DateTimeOffset lastOnlineUtc,
        DateTimeOffset lastLocalAppRunUtc,
        CancellationToken cancellationToken)
    {
        var existing = await _cachedUsers.FindByEmailAsync(cache.Email, cancellationToken)
            .ConfigureAwait(false);
        var passwordHash = existing?.PasswordHash;
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            passwordHash = ProtectedSessionCache.GetEncodedPasswordHash(cache);
        }

        await _cachedUsers.ImportRememberedCredentialAsync(
                cache.UserId,
                cache.Email,
                passwordHash,
                lastOnlineUtc,
                lastLocalAppRunUtc,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private static (string Message, bool TimeTampering)? ValidateLocalCredential(
        CachedUser? cachedUser,
        OfflineSessionCache sessionCache,
        string? password,
        DateTimeOffset nowUtc,
        bool startupClockInvalid)
    {
        if (cachedUser is null ||
            cachedUser.Id != sessionCache.UserId ||
            !string.Equals(
                cachedUser.Email,
                sessionCache.Email,
                StringComparison.OrdinalIgnoreCase))
        {
            return ("Нет сохранённой оффлайн-сессии для этого пользователя.", false);
        }

        if (startupClockInvalid ||
            nowUtc < cachedUser.LastLocalAppRunUtc ||
            nowUtc < cachedUser.LastOnlineUtc)
        {
            return (
                "Обнаружено несоответствие системного времени. Войдите в сеть для синхронизации",
                true);
        }

        if (nowUtc > cachedUser.LastOnlineUtc.AddDays(7))
        {
            return ("Срок оффлайн-сессии истек. Подключитесь к сети", false);
        }

        if (password is not null &&
            !OfflinePasswordHasher.VerifyPassword(password, cachedUser.PasswordHash))
        {
            return ("Неверный логин или пароль", false);
        }

        return null;
    }

    private static AuthSessionInfo MapSession(Session session)
    {
        var userId = Guid.TryParse(session.User!.Id, out var id) ? id : Guid.Empty;
        DateTimeOffset? expires = null;
        var expiresAt = session.ExpiresAt();
        if (expiresAt != default)
        {
            expires = new DateTimeOffset(DateTime.SpecifyKind(expiresAt, DateTimeKind.Utc));
        }

        return new AuthSessionInfo
        {
            UserId = userId,
            Email = session.User.Email ?? string.Empty,
            AccessToken = session.AccessToken ?? string.Empty,
            RefreshToken = session.RefreshToken ?? string.Empty,
            ExpiresAt = expires
        };
    }

    private async Task<bool> IsNetworkAvailableSafeAsync(CancellationToken cancellationToken)
    {
        using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        probeCts.CancelAfter(StartupNetworkProbeTimeout);
        try
        {
            return (await _network.CheckNowAsync(probeCts.Token).ConfigureAwait(false)).IsOnline;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger?.LogDebug(
                "Startup network probe exceeded {Timeout}; treating the device as offline.",
                StartupNetworkProbeTimeout);
            StartupTrace.Info(
                $"[LOG] Сетевой probe превысил {StartupNetworkProbeTimeout.TotalSeconds:0.#} сек.; " +
                "используется оффлайн-режим");
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // A connectivity probe must never prevent the local authentication path.
            _logger?.LogWarning(ex, "Network probe failed; treating the device as offline.");
            StartupTrace.Error(
                "[LOG] Проверка интернет-соединения завершилась исключением; статус Оффлайн",
                ex);
            return false;
        }
    }

    private static bool IsUsableOfflineCache(OfflineSessionCache? cache) =>
        cache is
        {
            UserId: var userId,
            Email: var email,
            DatabaseKey.Length: 32
        } &&
        userId != Guid.Empty &&
        !string.IsNullOrWhiteSpace(email);

    internal static bool IsNetworkFailure(Exception ex)
    {
        var pending = new Stack<Exception>();
        pending.Push(ex);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (current is HttpRequestException or SocketException or TimeoutException or
                TaskCanceledException)
            {
                return true;
            }

            if (ContainsHostNotFoundMessage(current.Message))
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

    private static bool ContainsHostNotFoundMessage(string? message) =>
        !string.IsNullOrWhiteSpace(message) &&
        (message.Contains("Этот хост неизвестен", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
         message.Contains("Host not found", StringComparison.OrdinalIgnoreCase));

    private static string SimplifyAuthError(string message)
    {
        if (message.Contains("Invalid login", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("invalid_grant", StringComparison.OrdinalIgnoreCase))
        {
            return "Неверный email или пароль.";
        }

        return string.IsNullOrWhiteSpace(message)
            ? "Не удалось выполнить вход."
            : message;
    }
}
