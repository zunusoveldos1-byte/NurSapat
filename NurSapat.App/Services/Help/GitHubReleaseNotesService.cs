using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NurSapat.App.Models.Help;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Models;
using NurSapat.Infrastructure.Updates.Options;

namespace NurSapat.App.Services.Help;

public interface IReleaseNotesService
{
    Task<ReleaseNotesResult> GetReleaseNotesAsync(CancellationToken cancellationToken = default);
}

public sealed class ReleaseNotesResult
{
    public IReadOnlyList<VersionInfo> Versions { get; init; } = [];

    public bool LoadedFromCache { get; init; }

    public bool IsEmpty { get; init; }

    public string? StatusMessage { get; init; }
}

public sealed class GitHubReleaseNotesService : IReleaseNotesService
{
    private readonly UpdateOptions _options;
    private readonly IGitHubReleaseCache _cache;
    private readonly IConnectivityMonitor _connectivity;
    private static readonly HttpClient Http = CreateClient();

    public GitHubReleaseNotesService(
        IOptions<UpdateOptions> options,
        IGitHubReleaseCache cache,
        IConnectivityMonitor connectivity)
    {
        _options = options.Value;
        _cache = cache;
        _connectivity = connectivity;
    }

    public async Task<ReleaseNotesResult> GetReleaseNotesAsync(CancellationToken cancellationToken = default)
    {
        await _cache.EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        if (await _connectivity.IsOnlineAsync(cancellationToken).ConfigureAwait(false)
            && !string.IsNullOrWhiteSpace(_options.Owner)
            && !string.IsNullOrWhiteSpace(_options.Repository))
        {
            try
            {
                var remote = await FetchFromGitHubAsync(cancellationToken).ConfigureAwait(false);
                if (remote.Count > 0)
                {
                    var now = DateTimeOffset.UtcNow;
                    var cached = remote.Select(r => new CachedGitHubRelease
                    {
                        Id = r.Id.ToString(),
                        TagName = r.TagName ?? string.Empty,
                        Name = r.Name ?? r.TagName ?? string.Empty,
                        Body = r.Body ?? string.Empty,
                        PublishedAt = r.PublishedAt ?? now,
                        FetchedAt = now
                    }).ToList();

                    await _cache.SaveAsync(cached, cancellationToken).ConfigureAwait(false);
                    return new ReleaseNotesResult
                    {
                        Versions = MapVersions(cached),
                        LoadedFromCache = false,
                        IsEmpty = false
                    };
                }
            }
            catch
            {
                // fall through to cache
            }
        }

        var local = await _cache.LoadAsync(cancellationToken).ConfigureAwait(false);
        if (local.Count > 0)
        {
            return new ReleaseNotesResult
            {
                Versions = MapVersions(local),
                LoadedFromCache = true,
                IsEmpty = false,
                StatusMessage = "Показана сохранённая история релизов (офлайн-кэш)."
            };
        }

        return new ReleaseNotesResult
        {
            Versions = [],
            LoadedFromCache = false,
            IsEmpty = true,
            StatusMessage = "Здесь появится история обновлений при подключении к сети."
        };
    }

    private async Task<IReadOnlyList<GitHubReleaseDto>> FetchFromGitHubAsync(CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{_options.Owner}/{_options.Repository}/releases?per_page=20";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(string.IsNullOrWhiteSpace(_options.UserAgent)
            ? "NurSapat"
            : _options.UserAgent);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await Http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var list = await JsonSerializer.DeserializeAsync<List<GitHubReleaseDto>>(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return list ?? [];
    }

    private static IReadOnlyList<VersionInfo> MapVersions(IEnumerable<CachedGitHubRelease> releases) =>
        releases
            .OrderByDescending(r => r.PublishedAt)
            .Select(r => new VersionInfo
            {
                Version = string.IsNullOrWhiteSpace(r.Name) ? r.TagName : r.Name,
                ReleaseDate = r.PublishedAt.ToLocalTime().ToString("dd.MM.yyyy"),
                Changes = SplitBody(r.Body)
            })
            .ToList();

    private static IReadOnlyList<string> SplitBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return ["Описание релиза отсутствует."];
        }

        var lines = body
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.TrimStart('-', '*', ' ', '\t'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(40)
            .ToList();

        return lines.Count > 0 ? lines : [body.Trim()];
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("NurSapat");
        return client;
    }

    private sealed class GitHubReleaseDto
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }
    }
}
