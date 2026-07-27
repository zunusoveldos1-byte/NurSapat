using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using NurSapat.Infrastructure.Updates.Abstractions;

namespace NurSapat.Infrastructure.Updates.Internal;

/// <summary>
/// Diagnostic dumps for GitHub update API calls (Update.log + Debug window).
/// Does not alter request/response behaviour.
/// </summary>
internal static class GitHubApiDiagnostics
{
    public static void Log(IUpdateLogger logger, string message)
    {
        logger.Info(message);
        Debug.WriteLine(message);
        Trace.WriteLine(message);
        Console.WriteLine(message);
    }

    public static void LogConfigSource(
        IUpdateLogger logger,
        string appsettingsAbsolutePath,
        bool fileExists,
        string owner,
        string repository,
        string userAgent,
        string baseDirectory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Update configuration diagnostics ===");
        sb.AppendLine($"AppContext.BaseDirectory: {baseDirectory}");
        sb.AppendLine($"appsettings.json absolute path: {appsettingsAbsolutePath}");
        sb.AppendLine($"appsettings.json exists: {fileExists}");
        sb.AppendLine($"Runtime Owner: {owner}");
        sb.AppendLine($"Runtime Repository: {repository}");
        sb.AppendLine($"Runtime User-Agent: {userAgent}");
        sb.AppendLine($"Resolved API URL template: https://api.github.com/repos/{owner}/{repository}/releases/latest");
        Log(logger, sb.ToString().TrimEnd());
    }

    public static void LogRequest(
        IUpdateLogger logger,
        string url,
        string owner,
        string repository,
        string userAgent,
        HttpRequestHeaders headers)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== GitHub API request ===");
        sb.AppendLine($"Full URL: {url}");
        sb.AppendLine($"Owner: {owner}");
        sb.AppendLine($"Repository: {repository}");
        sb.AppendLine($"User-Agent: {userAgent}");
        sb.AppendLine("Request headers:");
        AppendHeaders(sb, headers);
        Log(logger, sb.ToString().TrimEnd());
    }

    public static async Task LogResponseAsync(
        IUpdateLogger logger,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("=== GitHub API response ===");
        sb.AppendLine($"HTTP StatusCode: {(int)response.StatusCode} ({response.StatusCode})");
        sb.AppendLine($"ReasonPhrase: {response.ReasonPhrase}");
        sb.AppendLine("Response headers:");
        AppendHeaders(sb, response.Headers);
        AppendHeaders(sb, response.Content.Headers);
        sb.AppendLine("Response Body:");
        sb.AppendLine(string.IsNullOrEmpty(body) ? "<empty>" : body);
        Log(logger, sb.ToString().TrimEnd());

        // Restore body for subsequent deserialize (Content stream already consumed).
        response.Content = new StringContent(body, Encoding.UTF8, GetMediaType(response) ?? "application/json");
    }

    private static string? GetMediaType(HttpResponseMessage response) =>
        response.Content.Headers.ContentType?.MediaType;

    private static void AppendHeaders(StringBuilder sb, HttpHeaders headers)
    {
        foreach (var header in headers)
        {
            sb.AppendLine($"  {header.Key}: {string.Join(", ", header.Value)}");
        }
    }
}
