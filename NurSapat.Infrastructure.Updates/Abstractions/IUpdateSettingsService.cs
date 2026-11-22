using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Abstractions;

public interface IUpdateSettingsService
{
    UpdateSettings Current { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);

    void SetSkippedVersion(Version version);

    void ClearSkippedVersion();

    void RecordCheck(DateTimeOffset utc, Version? latestVersion);

    void RecordSuccessfulUpdate(Version version);

    void SetPendingSuccessVersion(Version version);

    void ClearPendingSuccessVersion();
}
