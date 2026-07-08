using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NurSapat.App.Services.Help;

public sealed class AppUpdateService : IAppUpdateService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(20)
    };

    private readonly string _checkUrl;

    public AppUpdateService()
    {
        _checkUrl = ReadCheckUrl();
        HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NurSapat-Updater/9.0");
        HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public string CurrentVersion => ResolveCurrentVersion();

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var current = CurrentVersion;

        try
        {
            using var response = await HttpClient.GetAsync(_checkUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    IsError = true,
                    Message = $"Сервер обновлений вернул код {(int)response.StatusCode}."
                };
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<GitHubReleaseResponse>(stream, cancellationToken: cancellationToken);

            if (payload is null || string.IsNullOrWhiteSpace(payload.TagName))
            {
                return new UpdateCheckResult
                {
                    CurrentVersion = current,
                    IsError = true,
                    Message = "Не удалось прочитать данные о версии."
                };
            }

            var latest = NormalizeVersion(payload.TagName);
            var isNewer = CompareVersions(latest, current) > 0;

            return new UpdateCheckResult
            {
                CurrentVersion = current,
                LatestVersion = latest,
                ReleaseNotes = payload.Body,
                DownloadUrl = payload.HtmlUrl,
                IsUpdateAvailable = isNewer,
                Message = isNewer
                    ? $"Доступна новая версия {latest}. Текущая версия: {current}."
                    : $"Установлена актуальная версия {current}."
            };
        }
        catch (TaskCanceledException)
        {
            return new UpdateCheckResult
            {
                CurrentVersion = current,
                IsError = true,
                Message = "Проверка обновлений отменена или превышено время ожидания."
            };
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult
            {
                CurrentVersion = current,
                IsError = true,
                Message = $"Ошибка проверки обновлений: {ex.Message}"
            };
        }
    }

    private static string ResolveCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational.Split('+')[0];
        }

        return assembly.GetName().Version?.ToString(3) ?? "9.0.0";
    }

    private static string ReadCheckUrl()
    {
        const string fallback = "https://api.github.com/repos/NurSapat/NurSapat/releases/latest";
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            if (document.RootElement.TryGetProperty("Update", out var updateSection)
                && updateSection.TryGetProperty("CheckUrl", out var urlElement))
            {
                var url = urlElement.GetString();
                if (!string.IsNullOrWhiteSpace(url))
                {
                    return url;
                }
            }
        }
        catch
        {
            // ignore malformed config
        }

        return fallback;
    }

    private static string NormalizeVersion(string raw)
    {
        var value = raw.Trim();
        if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            value = value[1..];
        }

        var dashIndex = value.IndexOf('-', StringComparison.Ordinal);
        if (dashIndex > 0)
        {
            value = value[..dashIndex];
        }

        return value;
    }

    private static int CompareVersions(string left, string right)
    {
        if (Version.TryParse(left, out var leftVersion) && Version.TryParse(right, out var rightVersion))
        {
            return leftVersion.CompareTo(rightVersion);
        }

        return string.Compare(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class GitHubReleaseResponse
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }
    }
}
