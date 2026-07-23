using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.App.Services.Help;

public sealed class AppUpdateService : IAppUpdateService
{
    private readonly IUpdateService _updateService;
    private readonly IVersionService _versionService;

    public AppUpdateService(IUpdateService updateService, IVersionService versionService)
    {
        _updateService = updateService;
        _versionService = versionService;
        _updateService.UpdateAvailable += (_, result) =>
            UpdateAvailable?.Invoke(this, UpdateCheckResult.FromInfrastructure(result, _versionService));
    }

    public event EventHandler<UpdateCheckResult>? UpdateAvailable;

    public string CurrentVersion => _versionService.GetCurrentVersionDisplay();

    public string BuildVersion => _versionService.GetBuildVersion();

    public UpdateSettings Settings => _updateService.Settings;

    public Task<UpdateCheckResult> CheckForUpdatesAsync(
        bool force = false,
        CancellationToken cancellationToken = default) =>
        CheckInternalAsync(force, cancellationToken);

    public Task<UpdateOperationResult> DownloadAndApplyAsync(
        UpdateReleaseInfo release,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken = default) =>
        _updateService.DownloadAndApplyAsync(release, progress, cancellationToken);

    public void SkipVersion(Version version) => _updateService.SkipVersion(version);

    public void StartPeriodicChecks() => _updateService.StartPeriodicChecks();

    public void StopPeriodicChecks() => _updateService.StopPeriodicChecks();

    private async Task<UpdateCheckResult> CheckInternalAsync(bool force, CancellationToken cancellationToken)
    {
        var result = await _updateService.CheckForUpdatesAsync(force, cancellationToken);
        return UpdateCheckResult.FromInfrastructure(result, _versionService);
    }
}
