namespace NurSapat.Infrastructure.Updates.Abstractions;

public interface IRollbackService
{
    Task RestoreFromBackupAsync(string backupDirectory, string targetDirectory, CancellationToken cancellationToken = default);
}
