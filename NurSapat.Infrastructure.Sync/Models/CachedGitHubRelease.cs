namespace NurSapat.Infrastructure.Sync.Models;

public sealed class CachedGitHubRelease
{
    public string Id { get; set; } = string.Empty;

    public string TagName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset PublishedAt { get; set; }

    public DateTimeOffset FetchedAt { get; set; }
}
