using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Abstractions;

public interface IUpdateService
{
    event EventHandler<UpdateCheckResult>? UpdateAvailable;

    Version CurrentVersion { get; }

    UpdateSettings Settings { get; }

    Task<UpdateCheckResult> CheckForUpdatesAsync(
        bool force = false,
        CancellationToken cancellationToken = default);

    Task<UpdateOperationResult> DownloadAndApplyAsync(
        UpdateReleaseInfo release,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken = default);

    void SkipVersion(Version version);

    void StartPeriodicChecks();

    void StopPeriodicChecks();
}
