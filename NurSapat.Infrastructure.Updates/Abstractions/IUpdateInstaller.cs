using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Abstractions;

public interface IUpdateInstaller
{
    string GetUpdatesDirectory();

    Task<UpdateOperationResult> PrepareInstallAsync(
        UpdateReleaseInfo release,
        VersionManifest? manifest,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken = default);
}
