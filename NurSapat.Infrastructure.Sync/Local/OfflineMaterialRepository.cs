using System.Globalization;
using Microsoft.Data.Sqlite;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Local;

public sealed class OfflineMaterialRepository : IOfflineMaterialRepository
{
    private readonly SqliteConnectionFactory _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public OfflineMaterialRepository(SqliteConnectionFactory factory)
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

    public async Task<IReadOnlyList<LocalMaterial>> GetAllAsync(
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = includeDeleted
            ? "SELECT * FROM materials ORDER BY name COLLATE NOCASE;"
            : "SELECT * FROM materials WHERE is_deleted = 0 ORDER BY name COLLATE NOCASE;";

        var list = new List<LocalMaterial>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(SqliteReaders.ReadMaterial(reader));
        }

        return list;
    }

    public async Task<LocalMaterial?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM materials WHERE id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", id.ToString("D"));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return SqliteReaders.ReadMaterial(reader);
    }

    public async Task<LocalMaterial> InsertAsync(LocalMaterial material, CancellationToken cancellationToken = default)
    {
        if (material.Id == Guid.Empty)
        {
            material.Id = Guid.NewGuid();
        }

        material.SyncStatus = SyncStatus.PendingInsert;
        material.IsDeleted = false;
        material.UpdatedAt = DateTimeOffset.UtcNow;
        material.LastSyncError = null;

        await UpsertLocalAsync(material, cancellationToken).ConfigureAwait(false);
        return material;
    }

    public async Task UpdateAsync(LocalMaterial material, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(material.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Material {material.Id} not found in local SQLite.");

        material.SyncStatus = existing.SyncStatus == SyncStatus.PendingInsert
            ? SyncStatus.PendingInsert
            : SyncStatus.PendingUpdate;
        material.IsDeleted = false;
        material.UpdatedAt = DateTimeOffset.UtcNow;
        material.LastSyncError = null;
        material.UserId ??= existing.UserId;

        await UpsertLocalAsync(material, cancellationToken).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        // Never synced to cloud — remove physically to keep local clean.
        if (existing.SyncStatus == SyncStatus.PendingInsert)
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var delete = connection.CreateCommand();
            delete.CommandText = "DELETE FROM materials WHERE id = $id;";
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

    public async Task<IReadOnlyList<LocalMaterial>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT * FROM materials
            WHERE sync_status IN ($i, $u, $d)
            ORDER BY updated_at;
            """;
        command.Parameters.AddWithValue("$i", (int)SyncStatus.PendingInsert);
        command.Parameters.AddWithValue("$u", (int)SyncStatus.PendingUpdate);
        command.Parameters.AddWithValue("$d", (int)SyncStatus.PendingDelete);

        var list = new List<LocalMaterial>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            list.Add(SqliteReaders.ReadMaterial(reader));
        }

        return list;
    }

    public async Task MarkSyncedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE materials
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
            UPDATE materials
            SET last_sync_error = $error
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$error", error.Length > 2000 ? error[..2000] : error);
        command.Parameters.AddWithValue("$id", id.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task MergeFromRemoteAsync(
        IEnumerable<LocalMaterial> remoteRows,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);

        foreach (var remote in remoteRows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var local = await GetByIdAsync(remote.Id, cancellationToken).ConfigureAwait(false);
            if (local is not null && local.SyncStatus != SyncStatus.Synced)
            {
                // Protect unsynced local edits from being overwritten by pull.
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

    private async Task UpsertLocalAsync(LocalMaterial material, CancellationToken cancellationToken)
    {
        await EnsureSchemaAsync(cancellationToken).ConfigureAwait(false);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO materials (
                    id, article, name, note, manufacturer, section, main_unit, linear_group, square_group,
                    currency, base_price, waste_percent, print_on_assembly_drawing, print_on_commercial_offer,
                    print_on_shipping_sheet, group_print_positions, is_warm_profile,
                    inner_coating_area_per_meter, outer_coating_area_per_meter,
                    enable_length_cut_optimization, bar_length_mm, illiquid_threshold_mm, scrap_mm,
                    enable_plane_cut_optimization, sheet_width_mm, sheet_height_mm, glass_tint,
                    minimum_calculated_area_sq_m, use_secondary_unit, secondary_unit_coefficient,
                    price_by_kilogram, prices_json, user_id, is_deleted, sync_status, updated_at, last_sync_error
                ) VALUES (
                    $id, $article, $name, $note, $manufacturer, $section, $main_unit, $linear_group, $square_group,
                    $currency, $base_price, $waste_percent, $print_on_assembly_drawing, $print_on_commercial_offer,
                    $print_on_shipping_sheet, $group_print_positions, $is_warm_profile,
                    $inner_coating_area_per_meter, $outer_coating_area_per_meter,
                    $enable_length_cut_optimization, $bar_length_mm, $illiquid_threshold_mm, $scrap_mm,
                    $enable_plane_cut_optimization, $sheet_width_mm, $sheet_height_mm, $glass_tint,
                    $minimum_calculated_area_sq_m, $use_secondary_unit, $secondary_unit_coefficient,
                    $price_by_kilogram, $prices_json, $user_id, $is_deleted, $sync_status, $updated_at, $last_sync_error
                )
                ON CONFLICT(id) DO UPDATE SET
                    article = excluded.article,
                    name = excluded.name,
                    note = excluded.note,
                    manufacturer = excluded.manufacturer,
                    section = excluded.section,
                    main_unit = excluded.main_unit,
                    linear_group = excluded.linear_group,
                    square_group = excluded.square_group,
                    currency = excluded.currency,
                    base_price = excluded.base_price,
                    waste_percent = excluded.waste_percent,
                    print_on_assembly_drawing = excluded.print_on_assembly_drawing,
                    print_on_commercial_offer = excluded.print_on_commercial_offer,
                    print_on_shipping_sheet = excluded.print_on_shipping_sheet,
                    group_print_positions = excluded.group_print_positions,
                    is_warm_profile = excluded.is_warm_profile,
                    inner_coating_area_per_meter = excluded.inner_coating_area_per_meter,
                    outer_coating_area_per_meter = excluded.outer_coating_area_per_meter,
                    enable_length_cut_optimization = excluded.enable_length_cut_optimization,
                    bar_length_mm = excluded.bar_length_mm,
                    illiquid_threshold_mm = excluded.illiquid_threshold_mm,
                    scrap_mm = excluded.scrap_mm,
                    enable_plane_cut_optimization = excluded.enable_plane_cut_optimization,
                    sheet_width_mm = excluded.sheet_width_mm,
                    sheet_height_mm = excluded.sheet_height_mm,
                    glass_tint = excluded.glass_tint,
                    minimum_calculated_area_sq_m = excluded.minimum_calculated_area_sq_m,
                    use_secondary_unit = excluded.use_secondary_unit,
                    secondary_unit_coefficient = excluded.secondary_unit_coefficient,
                    price_by_kilogram = excluded.price_by_kilogram,
                    prices_json = excluded.prices_json,
                    user_id = excluded.user_id,
                    is_deleted = excluded.is_deleted,
                    sync_status = excluded.sync_status,
                    updated_at = excluded.updated_at,
                    last_sync_error = excluded.last_sync_error;
                """;

            Bind(command, material);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void Bind(SqliteCommand command, LocalMaterial m)
    {
        command.Parameters.AddWithValue("$id", m.Id.ToString("D"));
        command.Parameters.AddWithValue("$article", m.Article);
        command.Parameters.AddWithValue("$name", m.Name);
        command.Parameters.AddWithValue("$note", m.Note);
        command.Parameters.AddWithValue("$manufacturer", m.Manufacturer);
        command.Parameters.AddWithValue("$section", m.Section);
        command.Parameters.AddWithValue("$main_unit", m.MainUnit);
        command.Parameters.AddWithValue("$linear_group", SqliteReaders.Db(m.LinearGroup));
        command.Parameters.AddWithValue("$square_group", SqliteReaders.Db(m.SquareGroup));
        command.Parameters.AddWithValue("$currency", m.Currency);
        command.Parameters.AddWithValue("$base_price", (double)m.BasePrice);
        command.Parameters.AddWithValue("$waste_percent", (double)m.WastePercent);
        command.Parameters.AddWithValue("$print_on_assembly_drawing", m.PrintOnAssemblyDrawing ? 1 : 0);
        command.Parameters.AddWithValue("$print_on_commercial_offer", m.PrintOnCommercialOffer ? 1 : 0);
        command.Parameters.AddWithValue("$print_on_shipping_sheet", m.PrintOnShippingSheet ? 1 : 0);
        command.Parameters.AddWithValue("$group_print_positions", m.GroupPrintPositions ? 1 : 0);
        command.Parameters.AddWithValue("$is_warm_profile", m.IsWarmProfile ? 1 : 0);
        command.Parameters.AddWithValue("$inner_coating_area_per_meter", (double)m.InnerCoatingAreaPerMeter);
        command.Parameters.AddWithValue("$outer_coating_area_per_meter", (double)m.OuterCoatingAreaPerMeter);
        command.Parameters.AddWithValue("$enable_length_cut_optimization", m.EnableLengthCutOptimization ? 1 : 0);
        command.Parameters.AddWithValue("$bar_length_mm", (double)m.BarLengthMm);
        command.Parameters.AddWithValue("$illiquid_threshold_mm", (double)m.IlliquidThresholdMm);
        command.Parameters.AddWithValue("$scrap_mm", (double)m.ScrapMm);
        command.Parameters.AddWithValue("$enable_plane_cut_optimization", m.EnablePlaneCutOptimization ? 1 : 0);
        command.Parameters.AddWithValue("$sheet_width_mm", (double)m.SheetWidthMm);
        command.Parameters.AddWithValue("$sheet_height_mm", (double)m.SheetHeightMm);
        command.Parameters.AddWithValue("$glass_tint", (double)m.GlassTint);
        command.Parameters.AddWithValue("$minimum_calculated_area_sq_m", (double)m.MinimumCalculatedAreaSqM);
        command.Parameters.AddWithValue("$use_secondary_unit", m.UseSecondaryUnit ? 1 : 0);
        command.Parameters.AddWithValue("$secondary_unit_coefficient", (double)m.SecondaryUnitCoefficient);
        command.Parameters.AddWithValue("$price_by_kilogram", m.PriceByKilogram ? 1 : 0);
        command.Parameters.AddWithValue("$prices_json", m.PricesJson);
        command.Parameters.AddWithValue("$user_id", SqliteReaders.Db(m.UserId));
        command.Parameters.AddWithValue("$is_deleted", m.IsDeleted ? 1 : 0);
        command.Parameters.AddWithValue("$sync_status", (int)m.SyncStatus);
        command.Parameters.AddWithValue("$updated_at", m.UpdatedAt.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$last_sync_error", SqliteReaders.Db(m.LastSyncError));
    }
}
