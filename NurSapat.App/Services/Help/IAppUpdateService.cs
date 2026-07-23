using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Models;
using InfrastructureUpdateCheckResult = NurSapat.Infrastructure.Updates.Models.UpdateCheckResult;

namespace NurSapat.App.Services.Help;

public sealed class UpdateCheckResult
{
    public bool IsUpdateAvailable { get; init; }

    public string CurrentVersion { get; init; } = string.Empty;

    public string? LatestVersion { get; init; }

    public string? ReleaseNotes { get; init; }

    public string? DownloadUrl { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool IsError { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public long? PackageSizeBytes { get; init; }

    public UpdateReleaseInfo? Release { get; init; }

    public static UpdateCheckResult FromInfrastructure(InfrastructureUpdateCheckResult source, IVersionService versionService) =>
        new()
        {
            IsUpdateAvailable = source.IsUpdateAvailable,
            CurrentVersion = source.CurrentVersion?.ToString(3) ?? versionService.GetCurrentVersionDisplay(),
            LatestVersion = source.LatestVersion?.ToString(3),
            ReleaseNotes = source.Release?.ReleaseNotes,
            DownloadUrl = source.Release?.HtmlUrl,
            Message = source.Message,
            IsError = source.IsError,
            PublishedAt = source.Release?.PublishedAt,
            PackageSizeBytes = source.Release?.PackageAsset?.Size,
            Release = source.Release
        };
}

public interface IAppUpdateService
{
    string CurrentVersion { get; }

    string BuildVersion { get; }

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

    event EventHandler<UpdateCheckResult>? UpdateAvailable;
}
