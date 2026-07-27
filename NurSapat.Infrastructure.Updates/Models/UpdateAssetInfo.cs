namespace NurSapat.Infrastructure.Updates.Models;

public sealed class UpdateAssetInfo
{
    public required string Name { get; init; }

    public required string DownloadUrl { get; init; }

    public long Size { get; init; }

    public string? ContentType { get; init; }
}
