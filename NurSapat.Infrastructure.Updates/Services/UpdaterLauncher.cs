using System.Diagnostics;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Models;
using NurSapat.Infrastructure.Updates.Options;

namespace NurSapat.Infrastructure.Updates.Services;

public sealed class UpdaterLauncher : IUpdaterLauncher
{
    private readonly UpdateOptions _options;
    private readonly IUpdateDownloader _downloader;
    private readonly IUpdateValidator _validator;
    private readonly IUpdateInstaller _installer;
    private readonly IUpdateLogger _logger;

    public UpdaterLauncher(
        IOptions<UpdateOptions> options,
        IUpdateDownloader downloader,
        IUpdateValidator validator,
        IUpdateInstaller installer,
        IUpdateLogger logger)
    {
        _options = options.Value;
        _downloader = downloader;
        _validator = validator;
        _installer = installer;
        _logger = logger;
    }

    public string GetLocalUpdaterPath() =>
        Path.Combine(AppContext.BaseDirectory, _options.UpdaterAssetName);

    public async Task<UpdateOperationResult> EnsureUpdaterAsync(
        UpdateReleaseInfo release,
        VersionManifest? manifest,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var localPath = GetLocalUpdaterPath();
        var localValidation = await _validator.ValidateUpdaterAsync(localPath, manifest, cancellationToken);
        if (localValidation.Success)
        {
            return localValidation;
        }

        if (release.UpdaterAsset is null)
        {
            if (File.Exists(localPath))
            {
                return UpdateOperationResult.Ok();
            }

            return UpdateOperationResult.Fail("В релизе отсутствует Updater.exe.");
        }

        await _downloader.DownloadAsync(
            new Uri(release.UpdaterAsset.DownloadUrl),
            localPath,
            release.UpdaterAsset.Size,
            progress,
            cancellationToken);

        return await _validator.ValidateUpdaterAsync(localPath, manifest, cancellationToken);
    }

    public UpdateOperationResult Launch(UpdateInstallPlan plan)
    {
        var updaterPath = GetLocalUpdaterPath();
        if (!File.Exists(updaterPath))
        {
            return UpdateOperationResult.Fail("Updater.exe не найден.");
        }

        var arguments = string.Join(' ',
            $"--package \"{plan.PackagePath}\"",
            $"--target \"{plan.TargetDirectory}\"",
            $"--pid {plan.ProcessId}",
            $"--exe \"{plan.ApplicationExeName}\"",
            $"--backup \"{plan.BackupDirectory}\"",
            $"--version \"{plan.TargetVersion}\"",
            "--apply");

        _logger.Info($"Запущен Updater: {updaterPath} {arguments}");

        var startInfo = new ProcessStartInfo
        {
            FileName = updaterPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = plan.TargetDirectory
        };

        Process.Start(startInfo);
        return UpdateOperationResult.Ok();
    }
}
