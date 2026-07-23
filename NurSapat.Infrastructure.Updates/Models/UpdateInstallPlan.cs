namespace NurSapat.Infrastructure.Updates.Models;

public sealed class UpdateInstallPlan
{
    public required string PackagePath { get; init; }

    public required string TargetDirectory { get; init; }

    public required string ApplicationExeName { get; init; }

    public required int ProcessId { get; init; }

    public required string BackupDirectory { get; init; }

    public required Version TargetVersion { get; init; }
}
