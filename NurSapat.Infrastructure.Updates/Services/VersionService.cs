using System.Reflection;
using System.Text.RegularExpressions;
using NurSapat.Infrastructure.Updates.Abstractions;

namespace NurSapat.Infrastructure.Updates.Services;

public sealed class VersionService : IVersionService
{
    private static readonly Regex VersionPrefixRegex = new(@"^v", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly Lazy<Version> _currentVersion;
    private readonly Lazy<string> _buildVersion;

    public VersionService()
    {
        _currentVersion = new Lazy<Version>(ResolveCurrentVersion);
        _buildVersion = new Lazy<string>(ResolveBuildVersion);
    }

    public Version GetCurrentVersion() => _currentVersion.Value;

    public string GetCurrentVersionDisplay() => GetCurrentVersion().ToString(3);

    public string GetBuildVersion() => _buildVersion.Value;

    public bool IsNewer(Version candidate, Version? baseline = null)
    {
        var current = baseline ?? GetCurrentVersion();
        return candidate > current;
    }

    public Version ParseVersion(string raw)
    {
        var normalized = VersionPrefixRegex.Replace(raw.Trim(), string.Empty);
        var dashIndex = normalized.IndexOf('-', StringComparison.Ordinal);
        if (dashIndex > 0)
        {
            normalized = normalized[..dashIndex];
        }

        if (Version.TryParse(normalized, out var version))
        {
            return version;
        }

        throw new FormatException($"Некорректный формат версии: {raw}");
    }

    private static Version ResolveCurrentVersion()
    {
        var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            var core = informational.Split('+')[0];
            if (Version.TryParse(core, out var parsed))
            {
                return parsed;
            }
        }

        var assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version
            ?? Assembly.GetExecutingAssembly().GetName().Version;

        return assemblyVersion ?? new Version(9, 0, 0);
    }

    private static string ResolveBuildVersion()
    {
        var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informational))
        {
            var parts = informational.Split('+', 2);
            if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]))
            {
                return parts[1];
            }
        }

        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "—";
    }
}
