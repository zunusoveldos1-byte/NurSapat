namespace NurSapat.Infrastructure.Updates.Models;

public sealed class UpdateReleaseInfo
{
    public required string TagName { get; init; }

    public required Version Version { get; init; }

    public string? ReleaseNotes { get; init; }

    public DateTimeOffset? PublishedAt { get; init; }

    public required string HtmlUrl { get; init; }

    public bool IsPrerelease { get; init; }

    public UpdateAssetInfo? PackageAsset { get; init; }

    public UpdateAssetInfo? UpdaterAsset { get; init; }

    public UpdateAssetInfo? VersionManifestAsset { get; init; }
}
