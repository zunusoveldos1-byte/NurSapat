using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Models;
using NurSapat.Infrastructure.Sync.Options;

namespace NurSapat.Infrastructure.Sync.Services;

/// <summary>
/// Periodically pushes Pending* rows when the network is available.
/// Designed for Avalonia DI (no Generic Host required).
/// </summary>
public sealed class BackgroundSynchronizer : IBackgroundSynchronizer
{
    private readonly ISyncService _syncService;
    private readonly INetworkStatusService _network;
    private readonly ISupabaseAuthService _auth;
    private readonly SyncOptions _options;
    private readonly ILogger<BackgroundSynchronizer>? _logger;

    private CancellationTokenSource? _cts;
    private Task? _loop;
    private int _running;
    private readonly SemaphoreSlim _triggerGate = new(1, 1);

    public BackgroundSynchronizer(
        ISyncService syncService,
        INetworkStatusService network,
        ISupabaseAuthService auth,
        IOptions<SyncOptions> options,
        ILogger<BackgroundSynchronizer>? logger = null)
    {
        _syncService = syncService;
        _network = network;
        _auth = auth;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsRunning => Volatile.Read(ref _running) == 1;

    public void Start()
    {
        if (!_options.Enabled)
        {
            _logger?.LogInformation("BackgroundSynchronizer disabled via Sync:Enabled=false.");
            return;
        }

        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            return;
        }

        _cts = new CancellationTokenSource();
        _network.StatusChanged += OnNetworkStatusChanged;
        _loop = Task.Run(() => RunLoopAsync(_cts.Token));
        _logger?.LogInformation(
            "BackgroundSynchronizer started (interval {Seconds}s).",
            Math.Max(15, _options.IntervalSeconds));
    }

    public async Task StopAsync()
    {
        if (Interlocked.Exchange(ref _running, 0) == 0)
        {
            return;
        }

        try
        {
            _cts?.Cancel();
            if (_loop is not null)
            {
                await _loop.ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // expected
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            _loop = null;
            _network.StatusChanged -= OnNetworkStatusChanged;
        }
    }

    public async Task TriggerNowAsync(CancellationToken cancellationToken = default)
    {
        if (!await _triggerGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            if (!_auth.IsAuthenticated)
            {
                return;
            }

            var status = await _network.CheckNowAsync(cancellationToken).ConfigureAwait(false);
            if (status.Quality != NetworkQuality.Excellent)
            {
                return;
            }

            var result = await _syncService.PushPendingAsync(cancellationToken).ConfigureAwait(false);
            if (result.PushedMaterials + result.PushedOrders > 0)
            {
                _logger?.LogInformation(
                    "Manual sync pushed materials={Materials}, orders={Orders}.",
                    result.PushedMaterials,
                    result.PushedOrders);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Manual sync cycle failed.");
        }
        finally
        {
            _triggerGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _triggerGate.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(15, _options.IntervalSeconds));
        var wasOnline = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var status = await _network.CheckNowAsync(cancellationToken).ConfigureAwait(false);
                var online = status.Quality == NetworkQuality.Excellent;
                if (online)
                {
                    // On network recovery — push immediately.
                    if (!wasOnline)
                    {
                        _logger?.LogInformation("Network recovered — triggering sync.");
                    }

                    await TriggerNowAsync(cancellationToken).ConfigureAwait(false);
                }

                wasOnline = online;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Background sync loop iteration failed.");
            }

            try
            {
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void OnNetworkStatusChanged(object? sender, NetworkStatusSnapshot status)
    {
        if (status.Quality == NetworkQuality.Excellent && IsRunning)
        {
            _ = TriggerNowAsync();
        }
    }
}
