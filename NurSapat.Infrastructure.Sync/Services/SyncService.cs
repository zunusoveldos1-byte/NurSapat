using Microsoft.Extensions.Logging;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Auth;
using NurSapat.Infrastructure.Sync.Models;
using NurSapat.Infrastructure.Sync.Remote;
using Supabase.Postgrest;

namespace NurSapat.Infrastructure.Sync.Services;

public sealed class SyncService : ISyncService
{
    private readonly SupabaseClientFactory _clientFactory;
    private readonly IOfflineMaterialRepository _materials;
    private readonly IOfflineOrderRepository _orders;
    private readonly IConnectivityMonitor _connectivity;
    private readonly ISupabaseAuthService _auth;
    private readonly ILogger<SyncService>? _logger;
    private readonly SemaphoreSlim _cycleGate = new(1, 1);

    public SyncService(
        SupabaseClientFactory clientFactory,
        IOfflineMaterialRepository materials,
        IOfflineOrderRepository orders,
        IConnectivityMonitor connectivity,
        ISupabaseAuthService auth,
        ILogger<SyncService>? logger = null)
    {
        _clientFactory = clientFactory;
        _materials = materials;
        _orders = orders;
        _connectivity = connectivity;
        _auth = auth;
        _logger = logger;
    }

    public async Task BootstrapAfterLoginAsync(CancellationToken cancellationToken = default)
    {
        await PullRemoteAsync(cancellationToken).ConfigureAwait(false);
        await PushPendingAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task PullRemoteAsync(CancellationToken cancellationToken = default)
    {
        if (!_clientFactory.IsConfigured)
        {
            _logger?.LogWarning("Pull skipped: Supabase is not configured.");
            return;
        }

        if (!await _connectivity.IsOnlineAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger?.LogInformation("Pull skipped: offline.");
            return;
        }

        try
        {
            var client = await _clientFactory.GetClientAsync(cancellationToken).ConfigureAwait(false);

            var materialsResponse = await client
                .From<RemoteMaterialRow>()
                .Get(cancellationToken)
                .ConfigureAwait(false);

            var materials = materialsResponse.Models.Select(RemoteMappers.ToLocal).ToList();
            await _materials.MergeFromRemoteAsync(materials, cancellationToken).ConfigureAwait(false);

            var ordersResponse = await client
                .From<RemoteOrderRow>()
                .Get(cancellationToken)
                .ConfigureAwait(false);

            var orders = ordersResponse.Models.Select(RemoteMappers.ToLocal).ToList();
            await _orders.MergeFromRemoteAsync(orders, cancellationToken).ConfigureAwait(false);

            _logger?.LogInformation(
                "Pull complete: {Materials} materials, {Orders} orders.",
                materials.Count,
                orders.Count);
        }
        catch (Exception ex) when (IsTransient(ex))
        {
            _logger?.LogWarning(ex, "Pull failed due to network instability.");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Pull failed.");
            throw;
        }
    }

    public async Task<SyncCycleResult> PushPendingAsync(CancellationToken cancellationToken = default)
    {
        if (!_clientFactory.IsConfigured)
        {
            return new SyncCycleResult
            {
                NetworkAvailable = false,
                ErrorMessage = "Supabase is not configured."
            };
        }

        if (!await _connectivity.IsOnlineAsync(cancellationToken).ConfigureAwait(false))
        {
            return new SyncCycleResult
            {
                NetworkAvailable = false,
                ErrorMessage = "Offline — pending changes kept in SQLite."
            };
        }

        await _cycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var client = await _clientFactory.GetClientAsync(cancellationToken).ConfigureAwait(false);
            var userId = _auth.CurrentUserId;

            var pendingMaterials = await _materials.GetPendingAsync(cancellationToken).ConfigureAwait(false);
            var pendingOrders = await _orders.GetPendingAsync(cancellationToken).ConfigureAwait(false);

            var pushedMaterials = 0;
            var pushedOrders = 0;
            var failed = 0;

            foreach (var material in pendingMaterials)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (material.UserId is null && userId is not null)
                    {
                        material.UserId = userId;
                    }

                    material.UpdatedAt = DateTimeOffset.UtcNow;
                    var row = RemoteMappers.ToRemote(material);
                    await client
                        .From<RemoteMaterialRow>()
                        .Upsert(row, new QueryOptions { OnConflict = "id" })
                        .ConfigureAwait(false);

                    await _materials.MarkSyncedAsync(material.Id, cancellationToken).ConfigureAwait(false);
                    pushedMaterials++;
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    failed++;
                    await _materials.MarkSyncErrorAsync(material.Id, ex.Message, cancellationToken).ConfigureAwait(false);
                    _logger?.LogWarning(ex, "Material {Id} push deferred (network).", material.Id);
                    break; // stop push storm when network flaps
                }
                catch (Exception ex)
                {
                    failed++;
                    await _materials.MarkSyncErrorAsync(material.Id, ex.Message, cancellationToken).ConfigureAwait(false);
                    _logger?.LogError(ex, "Material {Id} push failed.", material.Id);
                }
            }

            foreach (var order in pendingOrders)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    if (order.UserId is null && userId is not null)
                    {
                        order.UserId = userId;
                    }

                    order.UpdatedAt = DateTimeOffset.UtcNow;
                    var row = RemoteMappers.ToRemote(order);
                    await client
                        .From<RemoteOrderRow>()
                        .Upsert(row, new QueryOptions { OnConflict = "id" })
                        .ConfigureAwait(false);

                    await _orders.MarkSyncedAsync(order.Id, cancellationToken).ConfigureAwait(false);
                    pushedOrders++;
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    failed++;
                    await _orders.MarkSyncErrorAsync(order.Id, ex.Message, cancellationToken).ConfigureAwait(false);
                    _logger?.LogWarning(ex, "Order {Id} push deferred (network).", order.Id);
                    break;
                }
                catch (Exception ex)
                {
                    failed++;
                    await _orders.MarkSyncErrorAsync(order.Id, ex.Message, cancellationToken).ConfigureAwait(false);
                    _logger?.LogError(ex, "Order {Id} push failed.", order.Id);
                }
            }

            return new SyncCycleResult
            {
                NetworkAvailable = true,
                PushedMaterials = pushedMaterials,
                PushedOrders = pushedOrders,
                FailedItems = failed
            };
        }
        finally
        {
            _cycleGate.Release();
        }
    }

    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or TimeoutException ||
        ex.InnerException is HttpRequestException or TaskCanceledException;
}
