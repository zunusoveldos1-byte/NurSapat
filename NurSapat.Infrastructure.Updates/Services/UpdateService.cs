using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Models;
using NurSapat.Infrastructure.Updates.Options;

namespace NurSapat.Infrastructure.Updates.Services;

public sealed class UpdateService : IUpdateService, IDisposable
{
    private readonly IUpdateReleaseProvider _releaseProvider;
    private readonly IVersionService _versionService;
    private readonly IUpdateSettingsService _settingsService;
    private readonly IUpdateInstaller _installer;
    private readonly IUpdateValidator _validator;
    private readonly IUpdaterLauncher _updaterLauncher;
    private readonly IUpdateLogger _logger;
    private readonly UpdateOptions _options;
    private readonly SemaphoreSlim _checkLock = new(1, 1);
    private Timer? _periodicTimer;
    private bool _started;

    public UpdateService(
        IUpdateReleaseProvider releaseProvider,
        IVersionService versionService,
        IUpdateSettingsService settingsService,
        IUpdateInstaller installer,
        IUpdateValidator validator,
        IUpdaterLauncher updaterLauncher,
        IUpdateLogger logger,
        IOptions<UpdateOptions> options)
    {
        _releaseProvider = releaseProvider;
        _versionService = versionService;
        _settingsService = settingsService;
        _installer = installer;
        _validator = validator;
        _updaterLauncher = updaterLauncher;
        _logger = logger;
        _options = options.Value;
    }

    public event EventHandler<UpdateCheckResult>? UpdateAvailable;

    public Version CurrentVersion => _versionService.GetCurrentVersion();

    public UpdateSettings Settings => _settingsService.Current;

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        await _settingsService.LoadAsync(cancellationToken);

        if (!force && !_settingsService.Current.AutoCheckEnabled)
        {
            return new UpdateCheckResult
            {
                CurrentVersion = CurrentVersion,
                Message = "Автоматическая проверка обновлений отключена."
            };
        }

        await _checkLock.WaitAsync(cancellationToken);
        try
        {
            _logger.Info("Проверка версии.");
            var current = CurrentVersion;

            try
            {
                var release = await _releaseProvider.GetLatestReleaseAsync(
                    _settingsService.Current.Channel,
                    cancellationToken);

                if (release is null)
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = current,
                        IsError = true,
                        Message = "Не удалось получить информацию о релизе."
                    };
                }

                _settingsService.RecordCheck(DateTimeOffset.UtcNow, release.Version);
                await _settingsService.SaveAsync(cancellationToken);

                var skipped = _settingsService.Current.SkippedVersion;
                if (!string.IsNullOrWhiteSpace(skipped)
                    && Version.TryParse(skipped, out var skippedVersion)
                    && skippedVersion >= release.Version)
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = current,
                        LatestVersion = release.Version,
                        Release = release,
                        IsSkippedVersion = true,
                        Message = $"Версия {release.Version} пропущена."
                    };
                }

                var isNewer = _versionService.IsNewer(release.Version, current);
                var result = new UpdateCheckResult
                {
                    CurrentVersion = current,
                    LatestVersion = release.Version,
                    Release = release,
                    IsUpdateAvailable = isNewer,
                    Message = isNewer
                        ? $"Доступна новая версия {release.Version}. Текущая версия: {current}."
                        : $"Установлена актуальная версия {current}."
                };

                if (isNewer)
                {
                    UpdateAvailable?.Invoke(this, result);
                }

                return result;
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.Error("Ошибка проверки обновлений.", ex);
                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    IsError = true,
                    Message = $"Ошибка проверки обновлений: {ex.Message}"
                };
            }
        }
        finally
        {
            _checkLock.Release();
        }
    }

    public async Task<UpdateOperationResult> DownloadAndApplyAsync(
        UpdateReleaseInfo release,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        VersionManifest? manifest = null;
        if (release.VersionManifestAsset is not null)
        {
            manifest = await _validator.FetchManifestAsync(release.VersionManifestAsset, cancellationToken);
        }

        var updaterValidation = await _updaterLauncher.EnsureUpdaterAsync(release, manifest, progress, cancellationToken);
        if (!updaterValidation.Success)
        {
            return updaterValidation;
        }

        var prepareResult = await _installer.PrepareInstallAsync(release, manifest, progress, cancellationToken);
        if (!prepareResult.Success)
        {
            return prepareResult;
        }

        var packagePath = prepareResult.Data
            ?? Path.Combine(_installer.GetUpdatesDirectory(), release.PackageAsset!.Name);

        var backupDirectory = Path.Combine(
            AppContext.BaseDirectory,
            "Backup",
            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));

        var plan = new UpdateInstallPlan
        {
            PackagePath = packagePath,
            TargetDirectory = AppContext.BaseDirectory,
            ApplicationExeName = _options.ApplicationExeName,
            ProcessId = Environment.ProcessId,
            BackupDirectory = backupDirectory,
            TargetVersion = release.Version
        };

        _settingsService.SetPendingSuccessVersion(release.Version);
        await _settingsService.SaveAsync(cancellationToken);

        var launchResult = _updaterLauncher.Launch(plan);
        if (!launchResult.Success)
        {
            return launchResult;
        }

        _logger.Info("Обновление передано Updater.exe. Завершение приложения.");
        return UpdateOperationResult.Ok();
    }

    public void SkipVersion(Version version)
    {
        _settingsService.SetSkippedVersion(version);
        _ = _settingsService.SaveAsync();
        _logger.Info($"Версия {version} пропущена пользователем.");
    }

    public void StartPeriodicChecks()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        var interval = TimeSpan.FromHours(Math.Max(_options.CheckIntervalHours, 1));
        _periodicTimer = new Timer(
            _ => _ = SafePeriodicCheckAsync(),
            null,
            TimeSpan.FromSeconds(15),
            interval);
    }

    public void StopPeriodicChecks()
    {
        _periodicTimer?.Dispose();
        _periodicTimer = null;
        _started = false;
    }

    private async Task SafePeriodicCheckAsync()
    {
        try
        {
            await CheckForUpdatesAsync(force: false);
        }
        catch (Exception ex)
        {
            _logger.Error("Ошибка периодической проверки обновлений.", ex);
        }
    }

    public void Dispose()
    {
        StopPeriodicChecks();
        _checkLock.Dispose();
    }
}
