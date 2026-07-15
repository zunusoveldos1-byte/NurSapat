using System.IO.Compression;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Internal;
using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Services;

public sealed class UpdateInstaller : IUpdateInstaller
{
    private readonly IUpdateDownloader _downloader;
    private readonly IUpdateValidator _validator;
    private readonly IUpdateLogger _logger;

    public UpdateInstaller(
        IUpdateDownloader downloader,
        IUpdateValidator validator,
        IUpdateLogger logger)
    {
        _downloader = downloader;
        _validator = validator;
        _logger = logger;
    }

    public string GetUpdatesDirectory()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Updates");
        Directory.CreateDirectory(directory);
        return directory;
    }

    public async Task<UpdateOperationResult> PrepareInstallAsync(
        UpdateReleaseInfo release,
        VersionManifest? manifest,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        if (release.PackageAsset is null)
        {
            return UpdateOperationResult.Fail("В релизе отсутствует архив обновления.");
        }

        var updatesDirectory = GetUpdatesDirectory();
        var packagePath = Path.Combine(updatesDirectory, release.PackageAsset.Name);

        await _downloader.DownloadAsync(
            new Uri(release.PackageAsset.DownloadUrl),
            packagePath,
            release.PackageAsset.Size,
            progress,
            cancellationToken);

        var validation = await _validator.ValidatePackageAsync(packagePath, manifest, cancellationToken);
        if (!validation.Success)
        {
            TryDelete(packagePath);
            return validation;
        }

        _logger.Info("Пакет обновления подготовлен.");
        return UpdateOperationResult.Ok("Пакет обновления подготовлен.", packagePath);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // best effort
        }
    }
}

public sealed class UpdateFileReplacer
{
    private readonly IUpdateLogger _logger;

    public UpdateFileReplacer(IUpdateLogger logger)
    {
        _logger = logger;
    }

    public async Task<UpdateOperationResult> ExtractAndReplaceAsync(
        string packagePath,
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        var extractDirectory = Path.Combine(Path.GetTempPath(), $"nursapat-update-{Guid.NewGuid():N}");
        Directory.CreateDirectory(extractDirectory);

        try
        {
            _logger.Info("Распаковка архива обновления.");
            ZipFile.ExtractToDirectory(packagePath, extractDirectory, overwriteFiles: true);

            var sourceRoot = ResolvePayloadRoot(extractDirectory);
            await CopyReplacingAsync(sourceRoot, targetDirectory, cancellationToken);
            _logger.Info("Замена файлов завершена.");
            return UpdateOperationResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.Error("Ошибка при замене файлов.", ex);
            return UpdateOperationResult.Fail($"Ошибка замены файлов: {ex.Message}");
        }
        finally
        {
            TryDeleteDirectory(extractDirectory);
        }
    }

    public async Task CreateBackupAsync(string sourceDirectory, string backupDirectory, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Создание резервной копии: {backupDirectory}");
        Directory.CreateDirectory(backupDirectory);

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceDirectory, file);
            if (UserDataProtection.ShouldSkipPath(relative))
            {
                continue;
            }

            var destination = Path.Combine(backupDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }

        await Task.CompletedTask;
    }

    public UpdateOperationResult VerifyRequiredFiles(string targetDirectory, string applicationExeName)
    {
        var exePath = Path.Combine(targetDirectory, applicationExeName);
        if (!File.Exists(exePath))
        {
            return UpdateOperationResult.Fail($"После обновления не найден {applicationExeName}.");
        }

        return UpdateOperationResult.Ok();
    }

    private async Task CopyReplacingAsync(string sourceRoot, string targetDirectory, CancellationToken cancellationToken)
    {
        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceRoot, file);
            if (UserDataProtection.ShouldSkipPath(relative))
            {
                continue;
            }

            var destination = Path.Combine(targetDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

            var tempDestination = destination + ".new";
            File.Copy(file, tempDestination, overwrite: true);

            if (File.Exists(destination))
            {
                File.Delete(destination);
            }

            File.Move(tempDestination, destination);
        }

        await Task.CompletedTask;
    }

    private static string ResolvePayloadRoot(string extractDirectory)
    {
        var directExe = Directory.GetFiles(extractDirectory, "*.exe", SearchOption.TopDirectoryOnly);
        if (directExe.Length > 0)
        {
            return extractDirectory;
        }

        var subdirectories = Directory.GetDirectories(extractDirectory);
        if (subdirectories.Length == 1)
        {
            return subdirectories[0];
        }

        return extractDirectory;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // best effort
        }
    }
}

public sealed class RollbackService : IRollbackService
{
    private readonly UpdateFileReplacer _fileReplacer;
    private readonly IUpdateLogger _logger;

    public RollbackService(UpdateFileReplacer fileReplacer, IUpdateLogger logger)
    {
        _fileReplacer = fileReplacer;
        _logger = logger;
    }

    public async Task RestoreFromBackupAsync(
        string backupDirectory,
        string targetDirectory,
        CancellationToken cancellationToken = default)
    {
        _logger.Warning($"Восстановление из резервной копии: {backupDirectory}");

        foreach (var file in Directory.EnumerateFiles(backupDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(backupDirectory, file);
            if (UserDataProtection.ShouldSkipPath(relative))
            {
                continue;
            }

            var destination = Path.Combine(targetDirectory, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination, overwrite: true);
        }

        await Task.CompletedTask;
        _logger.Info("Восстановление из резервной копии завершено.");
    }
}
