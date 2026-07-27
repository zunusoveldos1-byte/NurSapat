using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Abstractions;

public interface IGitHubReleaseCache
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(IEnumerable<CachedGitHubRelease> releases, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CachedGitHubRelease>> LoadAsync(CancellationToken cancellationToken = default);
}
