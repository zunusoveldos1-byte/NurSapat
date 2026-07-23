using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Abstractions;

public interface IUpdaterLauncher
{
    string GetLocalUpdaterPath();

    Task<UpdateOperationResult> EnsureUpdaterAsync(
        UpdateReleaseInfo release,
        VersionManifest? manifest,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken = default);

    UpdateOperationResult Launch(UpdateInstallPlan plan);
}
