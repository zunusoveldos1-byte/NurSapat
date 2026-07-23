using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Options;

namespace NurSapat.Infrastructure.Sync.Services;

/// <summary>Lightweight connectivity probe against the configured Supabase URL.</summary>
public sealed class HttpConnectivityMonitor : IConnectivityMonitor
{
    private readonly SyncOptions _options;
    private readonly ILogger<HttpConnectivityMonitor>? _logger;
    private static readonly HttpClient SharedHttp = CreateClient();

    public HttpConnectivityMonitor(IOptions<SyncOptions> options, ILogger<HttpConnectivityMonitor>? logger = null)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return false;
        }

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(4));

            var url = _options.SupabaseUrl.TrimEnd('/') + "/auth/v1/health";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddWithoutValidation("apikey", _options.SupabaseAnonKey);

            using var response = await SharedHttp.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token)
                .ConfigureAwait(false);

            // Any HTTP response means the network path works.
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            _logger?.LogDebug(ex, "Connectivity check failed.");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "Unexpected connectivity check error.");
            return false;
        }
    }

    private static HttpClient CreateClient()
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }
}
