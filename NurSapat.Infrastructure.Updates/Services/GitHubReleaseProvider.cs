using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Internal;
using NurSapat.Infrastructure.Updates.Models;
using NurSapat.Infrastructure.Updates.Options;

namespace NurSapat.Infrastructure.Updates.Services;

public sealed class GitHubReleaseProvider : IUpdateReleaseProvider
{
    private readonly UpdateOptions _options;
    private readonly UpdateHttpClientFactory _httpClientFactory;
    private readonly IVersionService _versionService;
    private readonly IUpdateLogger _logger;
    private bool _configDiagnosticsLogged;

    public GitHubReleaseProvider(
        IOptions<UpdateOptions> options,
        UpdateHttpClientFactory httpClientFactory,
        IVersionService versionService,
        IUpdateLogger logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _versionService = versionService;
        _logger = logger;
    }

    public async Task<UpdateReleaseInfo?> GetLatestReleaseAsync(
        UpdateChannel channel,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigDiagnostics();
        _logger.Info($"Запрос релиза GitHub для канала {channel}.");

        GitHubReleaseDto? dto = channel switch
        {
            UpdateChannel.Beta => await GetLatestChannelReleaseDtoAsync(
                release => release.Prerelease && !IsDevelopmentTag(release.TagName),
                cancellationToken),
            UpdateChannel.Development => await GetLatestChannelReleaseDtoAsync(
                release => release.Prerelease,
                cancellationToken),
            _ => await GetLatestStableReleaseAsync(cancellationToken)
        };

        if (dto is null)
        {
            _logger.Warning("Подходящий релиз не найден.");
            return null;
        }

        _logger.Info($"Получен релиз {dto.TagName}.");
        return MapRelease(dto);
    }

    private void EnsureConfigDiagnostics()
    {
        if (_configDiagnosticsLogged)
        {
            return;
        }

        _configDiagnosticsLogged = true;
        var baseDirectory = AppContext.BaseDirectory;
        var appsettingsPath = Path.GetFullPath(Path.Combine(baseDirectory, "appsettings.json"));
        GitHubApiDiagnostics.LogConfigSource(
            _logger,
            appsettingsPath,
            File.Exists(appsettingsPath),
            _options.Owner,
            _options.Repository,
            _options.UserAgent,
            baseDirectory);
    }

    private async Task<GitHubReleaseDto?> GetLatestStableReleaseAsync(CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{_options.Owner}/{_options.Repository}/releases/latest";
        using var response = await SendDiagnosedAsync(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            GitHubApiDiagnostics.Log(
                _logger,
                $"releases/latest вернул 404 для {_options.Owner}/{_options.Repository}. Пробуем список releases.");
            return await GetLatestChannelReleaseDtoAsync(release => !release.Prerelease, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw CreateHttpException(response);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<GitHubReleaseDto>(stream, cancellationToken: cancellationToken);
    }

    private async Task<GitHubReleaseDto?> GetLatestChannelReleaseDtoAsync(
        Func<GitHubReleaseDto, bool> predicate,
        CancellationToken cancellationToken)
    {
        var url = $"https://api.github.com/repos/{_options.Owner}/{_options.Repository}/releases?per_page=30";
        using var response = await SendDiagnosedAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateHttpException(response);
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var releases = await JsonSerializer.DeserializeAsync<List<GitHubReleaseDto>>(stream, cancellationToken: cancellationToken)
            ?? [];

        return releases.FirstOrDefault(predicate);
    }

    private async Task<HttpResponseMessage> SendDiagnosedAsync(string url, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.GetClient();

        GitHubApiDiagnostics.LogRequest(
            _logger,
            url,
            _options.Owner,
            _options.Repository,
            _options.UserAgent,
            client.DefaultRequestHeaders);

        var response = await _httpClientFactory.SendWithRetryAsync(
            (http, token) => http.GetAsync(url, token),
            cancellationToken);

        await GitHubApiDiagnostics.LogResponseAsync(_logger, response, cancellationToken);
        return response;
    }

    private UpdateReleaseInfo MapRelease(GitHubReleaseDto dto)
    {
        var version = _versionService.ParseVersion(dto.TagName ?? "0.0.0");
        var assets = dto.Assets ?? [];

        return new UpdateReleaseInfo
        {
            TagName = dto.TagName ?? version.ToString(3),
            Version = version,
            ReleaseNotes = dto.Body,
            PublishedAt = dto.PublishedAt,
            HtmlUrl = dto.HtmlUrl ?? string.Empty,
            IsPrerelease = dto.Prerelease,
            PackageAsset = FindAsset(assets, _options.PackageAssetName),
            UpdaterAsset = FindAsset(assets, _options.UpdaterAssetName),
            VersionManifestAsset = FindAsset(assets, _options.VersionManifestAssetName)
        };
    }

    private static UpdateAssetInfo? FindAsset(IEnumerable<GitHubAssetDto> assets, string fileName) =>
        assets
            .Where(asset => string.Equals(asset.Name, fileName, StringComparison.OrdinalIgnoreCase))
            .Select(asset => new UpdateAssetInfo
            {
                Name = asset.Name ?? fileName,
                DownloadUrl = asset.BrowserDownloadUrl ?? string.Empty,
                Size = asset.Size,
                ContentType = asset.ContentType
            })
            .FirstOrDefault(asset => !string.IsNullOrWhiteSpace(asset.DownloadUrl));

    private static bool IsDevelopmentTag(string? tagName) =>
        !string.IsNullOrWhiteSpace(tagName)
        && tagName.Contains("dev", StringComparison.OrdinalIgnoreCase);

    private static Exception CreateHttpException(HttpResponseMessage response) =>
        new($"GitHub API вернул код {(int)response.StatusCode} ({response.ReasonPhrase}).");

    private sealed class GitHubReleaseDto
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("published_at")]
        public DateTimeOffset? PublishedAt { get; set; }

        [JsonPropertyName("prerelease")]
        public bool Prerelease { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAssetDto>? Assets { get; set; }
    }

    private sealed class GitHubAssetDto
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }
    }
}
