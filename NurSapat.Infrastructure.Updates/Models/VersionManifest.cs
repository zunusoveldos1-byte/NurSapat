using System.Text.Json.Serialization;

namespace NurSapat.Infrastructure.Updates.Models;

public sealed class VersionManifest
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("package")]
    public VersionManifestFile? Package { get; set; }

    [JsonPropertyName("updater")]
    public VersionManifestFile? Updater { get; set; }
}

public sealed class VersionManifestFile
{
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("sha256")]
    public string? Sha256 { get; set; }

    [JsonPropertyName("sha512")]
    public string? Sha512 { get; set; }

    [JsonPropertyName("size")]
    public long? Size { get; set; }
}
