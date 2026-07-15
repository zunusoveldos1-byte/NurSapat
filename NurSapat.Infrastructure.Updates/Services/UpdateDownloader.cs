using System.Diagnostics;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Internal;
using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Services;

public sealed class UpdateDownloader : IUpdateDownloader
{
    private readonly UpdateHttpClientFactory _httpClientFactory;
    private readonly IUpdateLogger _logger;

    public UpdateDownloader(UpdateHttpClientFactory httpClientFactory, IUpdateLogger logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> DownloadAsync(
        Uri url,
        string destinationPath,
        long? expectedSize,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        var partialPath = destinationPath + ".partial";
        long existingBytes = 0;

        if (File.Exists(partialPath))
        {
            existingBytes = new FileInfo(partialPath).Length;
        }
        else if (File.Exists(destinationPath))
        {
            var info = new FileInfo(destinationPath);
            if (expectedSize is null or > 0 && expectedSize == info.Length)
            {
                _logger.Info($"Файл уже загружен: {destinationPath}");
                ReportComplete(progress, info.Length, expectedSize);
                return destinationPath;
            }

            File.Delete(destinationPath);
        }

        _logger.Info($"Начато скачивание: {url}");

        using var response = await _httpClientFactory.SendWithRetryAsync(
            async (client, token) =>
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                if (existingBytes > 0)
                {
                    request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingBytes, null);
                }

                return await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
            },
            cancellationToken);

        if (existingBytes > 0 && response.StatusCode == System.Net.HttpStatusCode.RequestedRangeNotSatisfiable)
        {
            response.Dispose();
            existingBytes = 0;
            File.Delete(partialPath);
            return await DownloadAsync(url, destinationPath, expectedSize, progress, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Не удалось скачать файл. Код {(int)response.StatusCode}.");
        }

        var totalBytes = expectedSize ?? response.Content.Headers.ContentLength;
        if (response.Content.Headers.ContentRange?.Length is long rangedTotal)
        {
            totalBytes = rangedTotal;
        }
        else if (totalBytes is not null && existingBytes > 0)
        {
            totalBytes += existingBytes;
        }

        await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var fileStream = new FileStream(
            partialPath,
            existingBytes > 0 && response.StatusCode == System.Net.HttpStatusCode.PartialContent
                ? FileMode.Append
                : FileMode.Create,
            FileAccess.Write,
            FileShare.None);

        var buffer = new byte[128 * 1024];
        long received = existingBytes;
        var stopwatch = Stopwatch.StartNew();
        var lastReport = Stopwatch.StartNew();
        long bytesSinceLastReport = 0;

        while (true)
        {
            var read = await contentStream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            received += read;
            bytesSinceLastReport += read;

            if (lastReport.ElapsedMilliseconds >= 250)
            {
                var speed = bytesSinceLastReport / Math.Max(lastReport.Elapsed.TotalSeconds, 0.001);
                Report(progress, received, totalBytes, speed, "Загрузка обновления");
                bytesSinceLastReport = 0;
                lastReport.Restart();
            }
        }

        fileStream.Close();
        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        File.Move(partialPath, destinationPath);
        ReportComplete(progress, received, totalBytes);
        _logger.Info($"Скачивание завершено: {destinationPath} ({received} байт).");
        return destinationPath;
    }

    private static void ReportComplete(IProgress<UpdateDownloadProgress>? progress, long received, long? totalBytes) =>
        Report(progress, received, totalBytes, 0, "Загрузка завершена");

    private static void Report(
        IProgress<UpdateDownloadProgress>? progress,
        long received,
        long? totalBytes,
        double bytesPerSecond,
        string status)
    {
        if (progress is null)
        {
            return;
        }

        var percent = totalBytes is > 0
            ? Math.Clamp(received * 100d / totalBytes.Value, 0, 100)
            : 0;

        TimeSpan? eta = null;
        if (totalBytes is > 0 && bytesPerSecond > 0)
        {
            var remaining = Math.Max(totalBytes.Value - received, 0);
            eta = TimeSpan.FromSeconds(remaining / bytesPerSecond);
        }

        progress.Report(new UpdateDownloadProgress
        {
            BytesReceived = received,
            TotalBytes = totalBytes,
            PercentComplete = percent,
            BytesPerSecond = bytesPerSecond,
            EstimatedTimeRemaining = eta,
            Status = status
        });
    }
}
