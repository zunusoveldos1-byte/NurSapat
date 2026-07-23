namespace NurSapat.Infrastructure.Updates.Models;

public sealed class UpdateCheckResult
{
    public bool IsUpdateAvailable { get; init; }

    public bool IsError { get; init; }

    public string Message { get; init; } = string.Empty;

    public Version? CurrentVersion { get; init; }

    public Version? LatestVersion { get; init; }

    public UpdateReleaseInfo? Release { get; init; }

    public bool IsSkippedVersion { get; init; }
}
