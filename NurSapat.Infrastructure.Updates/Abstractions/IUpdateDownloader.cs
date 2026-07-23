using NurSapat.Infrastructure.Updates.Models;

namespace NurSapat.Infrastructure.Updates.Abstractions;

public interface IUpdateDownloader
{
    Task<string> DownloadAsync(
        Uri url,
        string destinationPath,
        long? expectedSize,
        IProgress<UpdateDownloadProgress>? progress,
        CancellationToken cancellationToken = default);
}
