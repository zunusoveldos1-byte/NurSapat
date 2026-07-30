using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NurSapat.Infrastructure.Sync.Diagnostics;
using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Auth;

public sealed class ProtectedSessionCache
{
    private const int PasswordIterations = 210_000;
    private const int PasswordSaltSize = 16;
    private const int PasswordHashSize = 32;
    private static readonly byte[] Entropy =
        SHA256.HashData(Encoding.UTF8.GetBytes("NurSapat/session-cache/v1"));

    private readonly ILogger<ProtectedSessionCache>? _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;

    public ProtectedSessionCache(ILogger<ProtectedSessionCache>? logger = null)
    {
        _logger = logger;
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NurSapat");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "session.dat");
    }

    public string FilePath => _filePath;

    internal async Task<OfflineSessionCache?> ReadAsync(
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
            {
                StartupTrace.Info("[LOG] Проверка файла session.dat: Не существует");
                return null;
            }

            StartupTrace.Info("[LOG] Проверка файла session.dat: Существует");
            var protectedBytes = await File.ReadAllBytesAsync(_filePath, cancellationToken)
                .ConfigureAwait(false);
            var json = ProtectedData.Unprotect(
                protectedBytes,
                Entropy,
                DataProtectionScope.CurrentUser);
            return JsonSerializer.Deserialize<OfflineSessionCache>(json);
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException or IOException)
        {
            _logger?.LogWarning(ex, "Protected session cache could not be read.");
            StartupTrace.Error(
                "[LOG] Проверка файла session.dat: Ошибка расшифровки/чтения",
                ex);
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    internal async Task SaveSuccessfulLoginAsync(
        AuthSessionInfo session,
        string password,
        byte[] databaseSalt,
        byte[] databaseKey,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var passwordSalt = RandomNumberGenerator.GetBytes(PasswordSaltSize);
        var hash = DerivePasswordHash(password, passwordSalt);
        var cache = new OfflineSessionCache
        {
            UserId = session.UserId,
            Email = session.Email.Trim().ToLowerInvariant(),
            PasswordSalt = passwordSalt,
            PasswordHash = hash,
            AccessToken = session.AccessToken,
            RefreshToken = session.RefreshToken,
            DatabaseSalt = databaseSalt,
            DatabaseKey = databaseKey.ToArray(),
            LastOnlineUtc = nowUtc,
            LastLocalAppRunUtc = nowUtc
        };
        await WriteAsync(cache, cancellationToken).ConfigureAwait(false);
    }

    internal async Task SaveRestoredOnlineSessionAsync(
        OfflineSessionCache cache,
        AuthSessionInfo session,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        cache.UserId = session.UserId;
        cache.Email = session.Email.Trim().ToLowerInvariant();
        cache.AccessToken = session.AccessToken;
        cache.RefreshToken = session.RefreshToken;
        cache.LastOnlineUtc = nowUtc;
        cache.LastLocalAppRunUtc = nowUtc;
        await WriteAsync(cache, cancellationToken).ConfigureAwait(false);
    }

    internal async Task TouchAutomaticOfflineSessionAsync(
        OfflineSessionCache cache,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        cache.LastLocalAppRunUtc = nowUtc;
        await WriteAsync(cache, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<OfflineValidationResult> ValidateOfflineAsync(
        string email,
        string password,
        DateTimeOffset nowUtc,
        bool startupTimeWasInvalid,
        CancellationToken cancellationToken = default)
    {
        var metadata = await ValidateOfflineMetadataAsync(
                email,
                nowUtc,
                startupTimeWasInvalid,
                cancellationToken)
            .ConfigureAwait(false);
        if (!metadata.Success || metadata.Cache is null)
        {
            return metadata;
        }

        if (!VerifyPassword(metadata.Cache, password))
        {
            return OfflineValidationResult.Fail("Неверный email или пароль.");
        }

        metadata.Cache.LastLocalAppRunUtc = nowUtc;
        await WriteAsync(metadata.Cache, cancellationToken).ConfigureAwait(false);
        return OfflineValidationResult.Ok(metadata.Cache);
    }

    internal async Task<OfflineValidationResult> ValidateOfflineMetadataAsync(
        string email,
        DateTimeOffset nowUtc,
        bool startupTimeWasInvalid,
        CancellationToken cancellationToken = default)
    {
        var cache = await ReadAsync(cancellationToken).ConfigureAwait(false);
        if (cache is null ||
            !string.Equals(cache.Email, email.Trim(), StringComparison.OrdinalIgnoreCase) ||
            cache.UserId == Guid.Empty)
        {
            return OfflineValidationResult.Fail(
                "Нет сохранённой оффлайн-сессии для этого пользователя.");
        }

        if (startupTimeWasInvalid ||
            nowUtc < cache.LastLocalAppRunUtc ||
            nowUtc < cache.LastOnlineUtc)
        {
            return OfflineValidationResult.Fail(
                "Обнаружено несоответствие системного времени. Войдите в сеть для синхронизации",
                timeTampering: true);
        }

        if (nowUtc > cache.LastOnlineUtc.AddDays(7))
        {
            return OfflineValidationResult.Fail(
                "Срок оффлайн-сессии истек. Подключитесь к сети");
        }

        return OfflineValidationResult.Ok(cache);
    }

    internal static bool VerifyPassword(OfflineSessionCache cache, string password)
    {
        if (string.IsNullOrEmpty(password) ||
            cache.PasswordSalt.Length == 0 ||
            cache.PasswordHash.Length == 0)
        {
            return false;
        }

        var actual = DerivePasswordHash(password, cache.PasswordSalt);
        try
        {
            return CryptographicOperations.FixedTimeEquals(actual, cache.PasswordHash);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(actual);
        }
    }

    internal static string GetEncodedPasswordHash(OfflineSessionCache cache) =>
        $"pbkdf2-sha256${PasswordIterations}$" +
        $"{Convert.ToBase64String(cache.PasswordSalt)}$" +
        Convert.ToBase64String(cache.PasswordHash);

    internal async Task<bool> ValidateAndTouchStartupClockAsync(
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken = default)
    {
        var cache = await ReadAsync(cancellationToken).ConfigureAwait(false);
        if (cache is null)
        {
            return true;
        }

        if (nowUtc < cache.LastLocalAppRunUtc || nowUtc < cache.LastOnlineUtc)
        {
            return false;
        }

        cache.LastLocalAppRunUtc = nowUtc;
        await WriteAsync(cache, cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task DeleteAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(_filePath))
            {
                File.Delete(_filePath);
            }

            var legacyPath = Path.Combine(
                Path.GetDirectoryName(_filePath)!,
                "supabase-session.json");
            if (File.Exists(legacyPath))
            {
                File.Delete(legacyPath);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WriteAsync(
        OfflineSessionCache cache,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(cache);
        var protectedBytes = ProtectedData.Protect(
            json,
            Entropy,
            DataProtectionScope.CurrentUser);
        var tempPath = _filePath + ".tmp";

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await File.WriteAllBytesAsync(tempPath, protectedBytes, cancellationToken)
                .ConfigureAwait(false);
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            _gate.Release();
        }
    }

    private static byte[] DerivePasswordHash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            PasswordIterations,
            HashAlgorithmName.SHA256,
            PasswordHashSize);
}

internal sealed class OfflineSessionCache
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public byte[] PasswordSalt { get; set; } = [];
    public byte[] PasswordHash { get; set; } = [];
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public byte[] DatabaseSalt { get; set; } = [];
    public byte[] DatabaseKey { get; set; } = [];
    public DateTimeOffset LastOnlineUtc { get; set; }
    public DateTimeOffset LastLocalAppRunUtc { get; set; }
}

internal sealed record OfflineValidationResult(
    bool Success,
    string? ErrorMessage,
    bool TimeTampering,
    OfflineSessionCache? Cache)
{
    public static OfflineValidationResult Ok(OfflineSessionCache cache) =>
        new(true, null, false, cache);

    public static OfflineValidationResult Fail(string error, bool timeTampering = false) =>
        new(false, error, timeTampering, null);
}
