using System.Globalization;
using Microsoft.Data.Sqlite;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Local;

public sealed class OfflineOrderRepository : IOfflineOrderRepository
{
    private readonly SqliteConnectionFactory _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public OfflineOrderRepository(SqliteConnectionFactory factory)
    {
        _factory = factory;
    }

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await SqliteSchema.EnsureCreatedAsync(connection, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<LocalOrder>> GetAllAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = includeDeleted
            ? "SELECT * FROM orders ORDER BY order_date DESC;"
            : "SELECT * FROM orders WHERE is_deleted = 0 ORDER BY order_date DESC;";

        var list = new List<LocalOrder>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(SqliteReaders.ReadOrder(reader));
        }

        return list;
    }

    public async Task<LocalOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM orders WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return SqliteReaders.ReadOrder(reader);
    }

    public async Task<LocalOrder> InsertAsync(LocalOrder order, CancellationToken cancellationToken = default)
    {
        if (order.Id == Guid.Empty)
        {
            order.Id = Guid.NewGuid();
        }

        if (order.OrderDate == default)
        {
            order.OrderDate = DateTime.UtcNow;
        }

        order.SyncStatus = SyncStatus.PendingInsert;
        order.IsDeleted = false;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.LastSyncError = null;

        await UpsertLocalAsync(order, cancellationToken).ConfigureAwait(false);
        return order;
    }

    public async Task UpdateAsync(LocalOrder order, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(order.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Order {order.Id} not found in local SQLite.");

        order.SyncStatus = existing.SyncStatus == SyncStatus.PendingInsert
            ? SyncStatus.PendingInsert
            : SyncStatus.PendingUpdate;
        order.IsDeleted = false;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        order.LastSyncError = null;
        order.UserId ??= existing.UserId;

        await UpsertLocalAsync(order, cancellationToken).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        if (existing.SyncStatus == SyncStatus.PendingInsert)
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var delete = connection.CreateCommand();
            delete.CommandText = "DELETE FROM orders WHERE id = $id;";
            delete.Parameters.AddWithValue("$id", id.ToString("D"));
            await delete.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        existing.IsDeleted = true;
        existing.SyncStatus = SyncStatus.PendingDelete;
        existing.UpdatedAt = DateTimeOffset.UtcNow;
        existing.LastSyncError = null;
        await UpsertLocalAsync(existing, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<LocalOrder>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM orders
            WHERE sync_status IN ($i, $u, $d)
            ORDER BY updated_at;
            """;
        command.Parameters.AddWithValue("$i", (int)SyncStatus.PendingInsert);
        command.Parameters.AddWithValue("$u", (int)SyncStatus.PendingUpdate);
        command.Parameters.AddWithValue("$d", (int)SyncStatus.PendingDelete);

        var list = new List<LocalOrder>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(SqliteReaders.ReadOrder(reader));
        }

        return list;
    }

    public async Task MarkSyncedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE orders
            SET sync_status = $synced,
                last_sync_error = NULL,
                updated_at = $updated
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$synced", (int)SyncStatus.Synced);
        command.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MarkSyncErrorAsync(Guid id, string error, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE orders
            SET last_sync_error = $error
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$error", error.Length > 2000 ? error[..2000] : error);
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MergeFromRemoteAsync(
        IEnumerable<LocalOrder> remoteRows,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        foreach (var remote in remoteRows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var local = await GetByIdAsync(remote.Id, cancellationToken).ConfigureAwait(false);
            if (local is not null && local.SyncStatus != SyncStatus.Synced)
            {
                continue;
            }

            remote.SyncStatus = SyncStatus.Synced;
            remote.LastSyncError = null;
            if (remote.UpdatedAt == default)
            {
                remote.UpdatedAt = DateTimeOffset.UtcNow;
            }

            await UpsertLocalAsync(remote, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UpsertLocalAsync(LocalOrder order, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO orders (
                    id, profitability_percent, order_number, order_date, status, customer_id, dealer_id,
                    current_price_mode, total_discount_percent, total_discount_sum,
                    customer_name, customer_phone, site_address, dealer_name, order_manager_name,
                    customer_city, customer_code, contract_number, contract_date, production_date,
                    delivery_date, installation_date, has_delivery, has_installation,
                    profile_note, production_note, note, currency_code,
                    constructions_subtotal, discount2_percent, delivery_cost,
                    additional_materials_cost, additional_materials_installation_cost,
                    vat_percent, vat_mode, agreed_amount, paid_amount, payment_currency_code,
                    selected_construction_id, export_file_path, constructions_json,
                    user_id, is_deleted, sync_status, updated_at, last_sync_error
                ) VALUES (
                    $id, $profitability_percent, $order_number, $order_date, $status, $customer_id, $dealer_id,
                    $current_price_mode, $total_discount_percent, $total_discount_sum,
                    $customer_name, $customer_phone, $site_address, $dealer_name, $order_manager_name,
                    $customer_city, $customer_code, $contract_number, $contract_date, $production_date,
                    $delivery_date, $installation_date, $has_delivery, $has_installation,
                    $profile_note, $production_note, $note, $currency_code,
                    $constructions_subtotal, $discount2_percent, $delivery_cost,
                    $additional_materials_cost, $additional_materials_installation_cost,
                    $vat_percent, $vat_mode, $agreed_amount, $paid_amount, $payment_currency_code,
                    $selected_construction_id, $export_file_path, $constructions_json,
                    $user_id, $is_deleted, $sync_status, $updated_at, $last_sync_error
                )
                ON CONFLICT(id) DO UPDATE SET
                    profitability_percent = excluded.profitability_percent,
                    order_number = excluded.order_number,
                    order_date = excluded.order_date,
                    status = excluded.status,
                    customer_id = excluded.customer_id,
                    dealer_id = excluded.dealer_id,
                    current_price_mode = excluded.current_price_mode,
                    total_discount_percent = excluded.total_discount_percent,
                    total_discount_sum = excluded.total_discount_sum,
                    customer_name = excluded.customer_name,
                    customer_phone = excluded.customer_phone,
                    site_address = excluded.site_address,
                    dealer_name = excluded.dealer_name,
                    order_manager_name = excluded.order_manager_name,
                    customer_city = excluded.customer_city,
                    customer_code = excluded.customer_code,
                    contract_number = excluded.contract_number,
                    contract_date = excluded.contract_date,
                    production_date = excluded.production_date,
                    delivery_date = excluded.delivery_date,
                    installation_date = excluded.installation_date,
                    has_delivery = excluded.has_delivery,
                    has_installation = excluded.has_installation,
                    profile_note = excluded.profile_note,
                    production_note = excluded.production_note,
                    note = excluded.note,
                    currency_code = excluded.currency_code,
                    constructions_subtotal = excluded.constructions_subtotal,
                    discount2_percent = excluded.discount2_percent,
                    delivery_cost = excluded.delivery_cost,
                    additional_materials_cost = excluded.additional_materials_cost,
                    additional_materials_installation_cost = excluded.additional_materials_installation_cost,
                    vat_percent = excluded.vat_percent,
                    vat_mode = excluded.vat_mode,
                    agreed_amount = excluded.agreed_amount,
                    paid_amount = excluded.paid_amount,
                    payment_currency_code = excluded.payment_currency_code,
                    selected_construction_id = excluded.selected_construction_id,
                    export_file_path = excluded.export_file_path,
                    constructions_json = excluded.constructions_json,
                    user_id = excluded.user_id,
                    is_deleted = excluded.is_deleted,
                    sync_status = excluded.sync_status,
                    updated_at = excluded.updated_at,
                    last_sync_error = excluded.last_sync_error;
                """;

            Bind(command, order);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void Bind(SqliteCommand command, LocalOrder o)
    {
        command.Parameters.AddWithValue("$id", o.Id.ToString("D"));
        command.Parameters.AddWithValue("$profitability_percent", (double)o.ProfitabilityPercent);
        command.Parameters.AddWithValue("$order_number", o.OrderNumber);
        command.Parameters.AddWithValue("$order_date", o.OrderDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$status", o.Status);
        command.Parameters.AddWithValue("$customer_id", SqliteReaders.Db(o.CustomerId));
        command.Parameters.AddWithValue("$dealer_id", SqliteReaders.Db(o.DealerId));
        command.Parameters.AddWithValue("$current_price_mode", o.CurrentPriceMode);
        command.Parameters.AddWithValue("$total_discount_percent", (double)o.TotalDiscountPercent);
        command.Parameters.AddWithValue("$total_discount_sum", (double)o.TotalDiscountSum);
        command.Parameters.AddWithValue("$customer_name", o.CustomerName);
        command.Parameters.AddWithValue("$customer_phone", o.CustomerPhone);
        command.Parameters.AddWithValue("$site_address", o.SiteAddress);
        command.Parameters.AddWithValue("$dealer_name", o.DealerName);
        command.Parameters.AddWithValue("$order_manager_name", o.OrderManagerName);
        command.Parameters.AddWithValue("$customer_city", o.CustomerCity);
        command.Parameters.AddWithValue("$customer_code", o.CustomerCode);
        command.Parameters.AddWithValue("$contract_number", o.ContractNumber);
        command.Parameters.AddWithValue("$contract_date", SqliteReaders.Db(o.ContractDate));
        command.Parameters.AddWithValue("$production_date", SqliteReaders.Db(o.ProductionDate));
        command.Parameters.AddWithValue("$delivery_date", SqliteReaders.Db(o.DeliveryDate));
        command.Parameters.AddWithValue("$installation_date", SqliteReaders.Db(o.InstallationDate));
        command.Parameters.AddWithValue("$has_delivery", o.HasDelivery ? 1 : 0);
        command.Parameters.AddWithValue("$has_installation", o.HasInstallation ? 1 : 0);
        command.Parameters.AddWithValue("$profile_note", o.ProfileNote);
        command.Parameters.AddWithValue("$production_note", o.ProductionNote);
        command.Parameters.AddWithValue("$note", o.Note);
        command.Parameters.AddWithValue("$currency_code", o.CurrencyCode);
        command.Parameters.AddWithValue("$constructions_subtotal", (double)o.ConstructionsSubtotal);
        command.Parameters.AddWithValue("$discount2_percent", (double)o.Discount2Percent);
        command.Parameters.AddWithValue("$delivery_cost", (double)o.DeliveryCost);
        command.Parameters.AddWithValue("$additional_materials_cost", (double)o.AdditionalMaterialsCost);
        command.Parameters.AddWithValue("$additional_materials_installation_cost", (double)o.AdditionalMaterialsInstallationCost);
        command.Parameters.AddWithValue("$vat_percent", (double)o.VatPercent);
        command.Parameters.AddWithValue("$vat_mode", o.VatMode);
        command.Parameters.AddWithValue("$agreed_amount", (double)o.AgreedAmount);
        command.Parameters.AddWithValue("$paid_amount", (double)o.PaidAmount);
        command.Parameters.AddWithValue("$payment_currency_code", o.PaymentCurrencyCode);
        command.Parameters.AddWithValue("$selected_construction_id", SqliteReaders.Db(o.SelectedConstructionId));
        command.Parameters.AddWithValue("$export_file_path", SqliteReaders.Db(o.ExportFilePath));
        command.Parameters.AddWithValue("$constructions_json", o.ConstructionsJson);
        command.Parameters.AddWithValue("$user_id", SqliteReaders.Db(o.UserId));
        command.Parameters.AddWithValue("$is_deleted", o.IsDeleted ? 1 : 0);
        command.Parameters.AddWithValue("$sync_status", (int)o.SyncStatus);
        command.Parameters.AddWithValue("$updated_at", o.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$last_sync_error", SqliteReaders.Db(o.LastSyncError));
    }
}
