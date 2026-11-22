using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Auth;
using NurSapat.Infrastructure.Sync.Models;
using NurSapat.Infrastructure.Sync.Options;
using NurSapat.Infrastructure.Sync.Remote;
using Supabase.Postgrest;
using System.Net.Http.Json;
using System.Text.Json;

namespace NurSapat.Infrastructure.Sync.Services;

public sealed class SyncService : ISyncService
{
    private readonly SupabaseClientFactory _clientFactory;
    private readonly IOfflineMaterialRepository _materials;
    private readonly IOfflineOrderRepository _orders;
    private readonly IConnectivityMonitor _connectivity;
    private readonly ISupabaseAuthService _auth;
    private readonly ILocalDbService _localDb;
    private readonly SyncOptions _options;
    private readonly ILogger<SyncService>? _logger;
    private readonly SemaphoreSlim _cycleGate = new(1, 1);
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(30) };

    public SyncService(
        SupabaseClientFactory clientFactory,
        IOfflineMaterialRepository materials,
        IOfflineOrderRepository orders,
        IConnectivityMonitor connectivity,
        ISupabaseAuthService auth,
        ILocalDbService localDb,
        IOptions<SyncOptions> options,
        ILogger<SyncService>? logger = null)
    {
        _clientFactory = clientFactory;
        _materials = materials;
        _orders = orders;
        _connectivity = connectivity;
        _auth = auth;
        _localDb = localDb;
        _options = options.Value;
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
            await PullCompleteMirrorAsync(cancellationToken).ConfigureAwait(false);

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
            var pendingMirrorRows = _localDb.IsUnlocked
                ? await _localDb.GetPendingRowsAsync(cancellationToken).ConfigureAwait(false)
                : [];

            var pushedMaterials = 0;
            var pushedOrders = 0;
            var failed = 0;

            foreach (var pending in pendingMirrorRows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await PushMirrorRowAsync(pending, cancellationToken).ConfigureAwait(false);
                    await _localDb.MarkSyncedAsync(
                        pending.TableName,
                        pending.LocalKey,
                        cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    failed++;
                    _logger?.LogWarning(
                        ex,
                        "Generic mirror push for {Table}/{Key} deferred.",
                        pending.TableName,
                        pending.LocalKey);
                    break;
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger?.LogError(
                        ex,
                        "Generic mirror push for {Table}/{Key} failed.",
                        pending.TableName,
                        pending.LocalKey);
                }
            }

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

    private async Task PullCompleteMirrorAsync(CancellationToken cancellationToken)
    {
        if (!_localDb.IsUnlocked || _auth.CurrentSession is null)
        {
            return;
        }

        var schemas = await ReadRemoteSchemaAsync(cancellationToken).ConfigureAwait(false);
        if (schemas.Count == 0)
        {
            _logger?.LogWarning(
                "Supabase OpenAPI returned no visible tables; typed sync remains active.");
            return;
        }

        await _localDb.EnsureMirrorSchemaAsync(schemas, cancellationToken).ConfigureAwait(false);
        foreach (var table in schemas)
        {
            if (table.Name.Equals(_options.MaterialsTable, StringComparison.OrdinalIgnoreCase) ||
                table.Name.Equals(_options.OrdersTable, StringComparison.OrdinalIgnoreCase))
            {
                // These two tables use conflict-aware strongly typed repositories below.
                continue;
            }

            try
            {
                var rows = await ReadAllRowsAsync(table.Name, cancellationToken)
                    .ConfigureAwait(false);
                await _localDb.MergeRemoteRowsAsync(table.Name, rows, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger?.LogWarning(
                    ex,
                    "Table {Table} could not be refreshed; its existing local mirror was preserved.",
                    table.Name);
            }
        }
    }

    private async Task<IReadOnlyList<RemoteTableSchema>> ReadRemoteSchemaAsync(
        CancellationToken cancellationToken)
    {
        using var request = CreateRestRequest(HttpMethod.Get, string.Empty);
        request.Headers.Accept.ParseAdd("application/openapi+json");
        using var response = await _httpClient.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var root = document.RootElement;
        JsonElement definitions;
        if (root.TryGetProperty("definitions", out var v2Definitions))
        {
            definitions = v2Definitions;
        }
        else if (root.TryGetProperty("components", out var components) &&
                 components.TryGetProperty("schemas", out var v3Definitions))
        {
            definitions = v3Definitions;
        }
        else
        {
            return [];
        }

        var tables = new List<RemoteTableSchema>();
        foreach (var definition in definitions.EnumerateObject())
        {
            if (!definition.Value.TryGetProperty("properties", out var properties))
            {
                continue;
            }

            var required = definition.Value.TryGetProperty("required", out var requiredElement)
                ? requiredElement.EnumerateArray()
                    .Select(item => item.GetString() ?? string.Empty)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                : [];
            var columns = new List<RemoteColumnSchema>();
            foreach (var property in properties.EnumerateObject())
            {
                var type = property.Value.TryGetProperty("type", out var typeElement)
                    ? typeElement.GetString()
                    : "string";
                var format = property.Value.TryGetProperty("format", out var formatElement)
                    ? formatElement.GetString()
                    : null;
                var description =
                    property.Value.TryGetProperty("description", out var descriptionElement)
                        ? descriptionElement.GetString() ?? string.Empty
                        : string.Empty;
                var primaryKey =
                    description.Contains("primary key", StringComparison.OrdinalIgnoreCase) ||
                    description.Contains("<pk", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(property.Name, "id", StringComparison.OrdinalIgnoreCase);
                columns.Add(new RemoteColumnSchema(
                    property.Name,
                    ToSqliteType(type, format),
                    !required.Contains(property.Name),
                    primaryKey));
            }

            tables.Add(new RemoteTableSchema(definition.Name, columns));
        }

        return tables;
    }

    private async Task<IReadOnlyCollection<Dictionary<string, object?>>> ReadAllRowsAsync(
        string table,
        CancellationToken cancellationToken)
    {
        const int pageSize = 1000;
        var rows = new List<Dictionary<string, object?>>();
        for (var offset = 0; ; offset += pageSize)
        {
            using var request = CreateRestRequest(
                HttpMethod.Get,
                $"{Uri.EscapeDataString(table)}?select=*");
            request.Headers.TryAddWithoutValidation(
                "Range",
                $"{offset}-{offset + pageSize - 1}");
            using var response = await _httpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var page = await response.Content.ReadFromJsonAsync<List<Dictionary<string, object?>>>(
                           cancellationToken: cancellationToken)
                       ?? [];
            rows.AddRange(page);
            if (page.Count < pageSize)
            {
                break;
            }
        }

        return rows;
    }

    private async Task PushMirrorRowAsync(
        PendingMirrorRow pending,
        CancellationToken cancellationToken)
    {
        if (pending.Operation == "delete")
        {
            if (!pending.Values.TryGetValue("id", out var id) || id is null)
            {
                throw new InvalidOperationException(
                    $"Cannot delete {pending.TableName}: the mirrored row has no id.");
            }

            using var delete = CreateRestRequest(
                HttpMethod.Delete,
                $"{Uri.EscapeDataString(pending.TableName)}?id=eq.{Uri.EscapeDataString(id.ToString()!)}");
            using var deleteResponse = await _httpClient.SendAsync(delete, cancellationToken)
                .ConfigureAwait(false);
            deleteResponse.EnsureSuccessStatusCode();
            return;
        }

        using var request = CreateRestRequest(
            HttpMethod.Post,
            Uri.EscapeDataString(pending.TableName));
        request.Headers.TryAddWithoutValidation("Prefer", "resolution=merge-duplicates");
        request.Content = JsonContent.Create(pending.Values);
        using var response = await _httpClient.SendAsync(request, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    private HttpRequestMessage CreateRestRequest(HttpMethod method, string relative)
    {
        var request = new HttpRequestMessage(
            method,
            $"{_options.SupabaseUrl.TrimEnd('/')}/rest/v1/{relative}");
        request.Headers.TryAddWithoutValidation("apikey", _options.SupabaseAnonKey);
        var token = _auth.CurrentSession?.AccessToken;
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private static string ToSqliteType(string? type, string? format) =>
        (type, format) switch
        {
            ("integer", _) or ("boolean", _) => "INTEGER",
            ("number", _) => "REAL",
            ("string", "byte") or ("string", "binary") => "BLOB",
            _ => "TEXT"
        };
}
