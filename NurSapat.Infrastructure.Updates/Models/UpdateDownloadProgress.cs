namespace NurSapat.Infrastructure.Updates.Models;

public sealed class UpdateDownloadProgress
{
    public long BytesReceived { get; init; }

    public long? TotalBytes { get; init; }

    public double PercentComplete { get; init; }

    public double BytesPerSecond { get; init; }

    public TimeSpan? EstimatedTimeRemaining { get; init; }

    public string Status { get; init; } = string.Empty;
}
