using System.Diagnostics;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Diagnostics;
using NurSapat.Infrastructure.Sync.Models;
using NurSapat.Infrastructure.Sync.Options;

namespace NurSapat.Infrastructure.Sync.Services;

/// <summary>
/// Periodically probes the Supabase health endpoint. An HTTP response of any
/// status proves that the network path is alive; latency and recent failures
/// determine the displayed quality.
/// </summary>
public sealed class NetworkStatusService : INetworkStatusService
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan ProbeInterval = TimeSpan.FromSeconds(7);
    private static readonly TimeSpan PoorLatency = TimeSpan.FromMilliseconds(800);

    private readonly SyncOptions _options;
    private readonly ILogger<NetworkStatusService>? _logger;
    private readonly HttpClient _httpClient;
    private readonly object _stateGate = new();
    private readonly Queue<bool> _recentResults = new();
    private CancellationTokenSource? _loopCts;
    private Task? _loop;
    private NetworkStatusSnapshot _current = NetworkStatusSnapshot.Checking;

    public NetworkStatusService(
        IOptions<SyncOptions> options,
        ILogger<NetworkStatusService>? logger = null)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = new HttpClient { Timeout = ProbeTimeout };
    }

    public NetworkStatusSnapshot Current
    {
        get
        {
            lock (_stateGate)
            {
                return _current;
            }
        }
    }

    public event EventHandler<NetworkStatusSnapshot>? StatusChanged;

    public void Start()
    {
        lock (_stateGate)
        {
            if (_loop is not null)
            {
                return;
            }

            _loopCts = new CancellationTokenSource();
            _loop = Task.Run(() => RunAsync(_loopCts.Token));
        }
    }

    public async Task StopAsync()
    {
        Task? loop;
        CancellationTokenSource? cts;
        lock (_stateGate)
        {
            loop = _loop;
            cts = _loopCts;
            _loop = null;
            _loopCts = null;
        }

        if (loop is null)
        {
            return;
        }

        cts!.Cancel();
        try
        {
            await loop.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cts.Dispose();
        }
    }

    public async Task<bool> IsOnlineAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await CheckNowAsync(cancellationToken).ConfigureAwait(false);
        return snapshot.IsOnline;
    }

    public async Task<NetworkStatusSnapshot> CheckNowAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_options.IsConfigured)
        {
            return Publish(NetworkQuality.Offline, null, succeeded: false);
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"{_options.SupabaseUrl.TrimEnd('/')}/auth/v1/health");
            request.Headers.TryAddWithoutValidation("apikey", _options.SupabaseAnonKey);

            using var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                .ConfigureAwait(false);

            stopwatch.Stop();
            var hasRecentFailure = RecordResult(true);
            var quality = stopwatch.Elapsed >= PoorLatency || hasRecentFailure
                ? NetworkQuality.Poor
                : NetworkQuality.Excellent;
            return Publish(quality, stopwatch.Elapsed, succeeded: true, recordResult: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (IsNetworkFailure(ex))
        {
            _logger?.LogDebug(ex, "Supabase connectivity probe failed.");
            StartupTrace.Error(
                "[LOG] NetworkStatusService: Supabase health probe завершился сетевой ошибкой",
                ex);
            return Publish(NetworkQuality.Offline, null, succeeded: false);
        }
        catch (Exception ex)
        {
            // A health probe is advisory. Any failure means that Supabase cannot
            // currently be reached and must not escape into the login UI.
            _logger?.LogWarning(ex, "Unexpected Supabase connectivity probe failure.");
            StartupTrace.Error(
                "[LOG] NetworkStatusService: непредвиденная ошибка health probe",
                ex);
            return Publish(NetworkQuality.Offline, null, succeeded: false);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _httpClient.Dispose();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CheckNowAsync(cancellationToken).ConfigureAwait(false);
                await Task.Delay(ProbeInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Network status loop iteration failed.");
                StartupTrace.Error(
                    "[LOG] NetworkStatusService: ошибка фоновой проверки сети",
                    ex);
            }
        }
    }

    private NetworkStatusSnapshot Publish(
        NetworkQuality quality,
        TimeSpan? latency,
        bool succeeded,
        bool recordResult = true)
    {
        if (recordResult)
        {
            RecordResult(succeeded);
        }

        var text = quality switch
        {
            NetworkQuality.Excellent => "Отличная сеть",
            NetworkQuality.Poor => "Плохая сеть",
            NetworkQuality.Offline => "Оффлайн-режим",
            _ => "Проверка сети…"
        };
        var next = new NetworkStatusSnapshot(quality, text, latency, DateTimeOffset.UtcNow);
        EventHandler<NetworkStatusSnapshot>? handler;

        lock (_stateGate)
        {
            _current = next;
            handler = StatusChanged;
        }

        handler?.Invoke(this, next);
        return next;
    }

    private bool RecordResult(bool succeeded)
    {
        lock (_stateGate)
        {
            _recentResults.Enqueue(succeeded);
            while (_recentResults.Count > 5)
            {
                _recentResults.Dequeue();
            }

            return _recentResults.Any(static value => !value);
        }
    }

    private static bool IsNetworkFailure(Exception ex)
    {
        var pending = new Stack<Exception>();
        pending.Push(ex);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (current is HttpRequestException or SocketException or
                TaskCanceledException or TimeoutException)
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(current.Message) &&
                (current.Message.Contains("Этот хост неизвестен", StringComparison.OrdinalIgnoreCase) ||
                 current.Message.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) ||
                 current.Message.Contains("Name or service not known", StringComparison.OrdinalIgnoreCase) ||
                 current.Message.Contains("Host not found", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions)
                {
                    pending.Push(inner);
                }
            }
            else if (current.InnerException is not null)
            {
                pending.Push(current.InnerException);
            }
        }

        return false;
    }
}
