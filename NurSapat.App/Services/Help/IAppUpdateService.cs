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
}

public interface IAppUpdateService
{
    string CurrentVersion { get; }

    string BuildVersion { get; }

    Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default);
}
