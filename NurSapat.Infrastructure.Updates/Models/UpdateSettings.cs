namespace NurSapat.Infrastructure.Updates.Models;

public sealed class UpdateSettings
{
    public bool AutoCheckEnabled { get; set; } = true;

    public UpdateChannel Channel { get; set; } = UpdateChannel.Stable;

    public string? SkippedVersion { get; set; }

    public string? LastKnownLatestVersion { get; set; }

    public DateTimeOffset? LastCheckUtc { get; set; }

    public DateTimeOffset? LastUpdateUtc { get; set; }

    public string? PendingSuccessVersion { get; set; }
}
