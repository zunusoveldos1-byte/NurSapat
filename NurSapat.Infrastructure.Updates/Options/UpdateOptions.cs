namespace NurSapat.Infrastructure.Updates.Options;

public sealed class UpdateOptions
{
    public const string SectionName = "Update";

    public string Owner { get; set; } = "NurSapat";

    public string Repository { get; set; } = "NurSapat";

    public int CheckIntervalHours { get; set; } = 6;

    public string PackageAssetName { get; set; } = "NurSapat-win-x64.zip";

    public string UpdaterAssetName { get; set; } = "Updater.exe";

    public string VersionManifestAssetName { get; set; } = "version.json";

    public string ApplicationExeName { get; set; } = "NurSapat.App.exe";

    public string UserAgent { get; set; } = "NurSapat-Updater/9.0";
}
