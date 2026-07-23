using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Services;

public sealed class UpdateValidator : IUpdateValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IUpdateDownloader _downloader;
    private readonly IUpdateLogger _logger;

    public UpdateValidator(IUpdateDownloader downloader, IUpdateLogger logger)
    {
        _downloader = downloader;
        _logger = logger;
    }

    public async Task<VersionManifest?> FetchManifestAsync(
        UpdateAssetInfo manifestAsset,
        CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"nursapat-version-{Guid.NewGuid():N}.json");
        try
        {
            await _downloader.DownloadAsync(
                new Uri(manifestAsset.DownloadUrl),
                tempPath,
                manifestAsset.Size,
                null,
                cancellationToken);

            return await LoadManifestAsync(tempPath, cancellationToken);
        }
        finally
        {
            TryDelete(tempPath);
        }
    }

    public async Task<VersionManifest?> LoadManifestAsync(
        string manifestPath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(manifestPath);
        return await JsonSerializer.DeserializeAsync<VersionManifest>(stream, JsonOptions, cancellationToken);
    }

    public async Task<UpdateOperationResult> ValidatePackageAsync(
        string packagePath,
        VersionManifest? manifest,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(packagePath))
        {
            return UpdateOperationResult.Fail("Архив обновления не найден.");
        }

        var fileInfo = new FileInfo(packagePath);
        if (manifest?.Package?.Size is > 0 && fileInfo.Length != manifest.Package.Size)
        {
            return UpdateOperationResult.Fail("Размер архива не совпадает с манифестом.");
        }

        if (!string.IsNullOrWhiteSpace(manifest?.Package?.Sha256))
        {
            var actual = await ComputeSha256Async(packagePath, cancellationToken);
            if (!HashEquals(actual, manifest.Package.Sha256))
            {
                _logger.Error("Контрольная сумма SHA-256 архива не совпадает.");
                return UpdateOperationResult.Fail("Контрольная сумма архива не совпадает.");
            }
        }
        else if (!string.IsNullOrWhiteSpace(manifest?.Package?.Sha512))
        {
            var actual = await ComputeSha512Async(packagePath, cancellationToken);
            if (!HashEquals(actual, manifest.Package.Sha512))
            {
                _logger.Error("Контрольная сумма SHA-512 архива не совпадает.");
                return UpdateOperationResult.Fail("Контрольная сумма архива не совпадает.");
            }
        }

        try
        {
            using var archive = ZipFile.OpenRead(packagePath);
            if (archive.Entries.Count == 0)
            {
                return UpdateOperationResult.Fail("Архив обновления пуст.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Архив обновления повреждён.", ex);
            return UpdateOperationResult.Fail("Архив обновления повреждён.");
        }

        _logger.Info("Контрольная сумма архива успешна.");
        return UpdateOperationResult.Ok();
    }

    public async Task<UpdateOperationResult> ValidateUpdaterAsync(
        string updaterPath,
        VersionManifest? manifest,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(updaterPath))
        {
            return UpdateOperationResult.Fail("Файл Updater.exe не найден.");
        }

        if (!string.IsNullOrWhiteSpace(manifest?.Updater?.Sha256))
        {
            var actual = await ComputeSha256Async(updaterPath, cancellationToken);
            if (!HashEquals(actual, manifest.Updater.Sha256))
            {
                return UpdateOperationResult.Fail("Контрольная сумма Updater.exe не совпадает.");
            }
        }

        return UpdateOperationResult.Ok();
    }

    private static async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static async Task<string> ComputeSha512Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        var hash = await SHA512.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static bool HashEquals(string actual, string expected) =>
        string.Equals(actual, expected.Trim(), StringComparison.OrdinalIgnoreCase);

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
            // best effort cleanup
        }
    }
}
