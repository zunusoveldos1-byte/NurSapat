using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Abstractions;

public interface IUpdateValidator
{
    Task<UpdateOperationResult> ValidatePackageAsync(
        string packagePath,
        VersionManifest? manifest,
        CancellationToken cancellationToken = default);

    Task<UpdateOperationResult> ValidateUpdaterAsync(
        string updaterPath,
        VersionManifest? manifest,
        CancellationToken cancellationToken = default);

    Task<VersionManifest?> LoadManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken = default);

    Task<VersionManifest?> FetchManifestAsync(
        UpdateAssetInfo manifestAsset,
        CancellationToken cancellationToken = default);
}
