using System.Text.Json;
using NurSapat.App.Models.Help;

namespace NurSapat.App.Services.Help;

public sealed class ChangelogService : IChangelogService
{
    private static readonly string[] CandidatePaths =
    [
        Path.Combine(AppContext.BaseDirectory, "Assets", "Help", "Changelog.json"),
        Path.Combine(AppContext.BaseDirectory, "Help", "Changelog.json")
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IReadOnlyList<VersionInfo> LoadVersions()
    {
        foreach (var path in CandidatePaths)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(path);
                var document = JsonSerializer.Deserialize<ChangelogDocument>(json, JsonOptions);
                if (document?.Versions is { Count: > 0 } versions)
                {
                    return versions
                        .OrderByDescending(v => ParseVersion(v.Version))
                        .ToList();
                }
            }
            catch
            {
                // try next path
            }
        }

        return [];
    }

    private static Version ParseVersion(string raw)
    {
        var value = raw.Trim();
        if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            value = value[1..];
        }

        return Version.TryParse(value, out var version) ? version : new Version(0, 0, 0);
    }
}
