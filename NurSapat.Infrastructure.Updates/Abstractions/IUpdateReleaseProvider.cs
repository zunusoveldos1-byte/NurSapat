using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Abstractions;

public interface IUpdateReleaseProvider
{
    Task<UpdateReleaseInfo?> GetLatestReleaseAsync(
        UpdateChannel channel,
        CancellationToken cancellationToken = default);
}
