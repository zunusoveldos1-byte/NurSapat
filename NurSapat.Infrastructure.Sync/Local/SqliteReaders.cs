using System.Globalization;
using Microsoft.Data.Sqlite;
using NurSapat.Infrastructure.Sync.Models;

namespace NurSapat.Infrastructure.Sync.Local;

internal static class SqliteReaders
{
    public static LocalMaterial ReadMaterial(SqliteDataReader reader)
    {
        return new LocalMaterial
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            Article = reader.GetString(reader.GetOrdinal("article")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Note = reader.GetString(reader.GetOrdinal("note")),
            Manufacturer = reader.GetString(reader.GetOrdinal("manufacturer")),
            Section = reader.GetInt32(reader.GetOrdinal("section")),
            MainUnit = reader.GetInt32(reader.GetOrdinal("main_unit")),
            LinearGroup = GetNullableInt(reader, "linear_group"),
            SquareGroup = GetNullableInt(reader, "square_group"),
            Currency = reader.GetInt32(reader.GetOrdinal("currency")),
            BasePrice = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("base_price")), CultureInfo.InvariantCulture),
            WastePercent = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("waste_percent")), CultureInfo.InvariantCulture),
            PrintOnAssemblyDrawing = reader.GetInt32(reader.GetOrdinal("print_on_assembly_drawing")) != 0,
            PrintOnCommercialOffer = reader.GetInt32(reader.GetOrdinal("print_on_commercial_offer")) != 0,
            PrintOnShippingSheet = reader.GetInt32(reader.GetOrdinal("print_on_shipping_sheet")) != 0,
            GroupPrintPositions = reader.GetInt32(reader.GetOrdinal("group_print_positions")) != 0,
            IsWarmProfile = reader.GetInt32(reader.GetOrdinal("is_warm_profile")) != 0,
            InnerCoatingAreaPerMeter = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("inner_coating_area_per_meter")), CultureInfo.InvariantCulture),
            OuterCoatingAreaPerMeter = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("outer_coating_area_per_meter")), CultureInfo.InvariantCulture),
            EnableLengthCutOptimization = reader.GetInt32(reader.GetOrdinal("enable_length_cut_optimization")) != 0,
            BarLengthMm = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("bar_length_mm")), CultureInfo.InvariantCulture),
            IlliquidThresholdMm = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("illiquid_threshold_mm")), CultureInfo.InvariantCulture),
            ScrapMm = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("scrap_mm")), CultureInfo.InvariantCulture),
            EnablePlaneCutOptimization = reader.GetInt32(reader.GetOrdinal("enable_plane_cut_optimization")) != 0,
            SheetWidthMm = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("sheet_width_mm")), CultureInfo.InvariantCulture),
            SheetHeightMm = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("sheet_height_mm")), CultureInfo.InvariantCulture),
            GlassTint = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("glass_tint")), CultureInfo.InvariantCulture),
            MinimumCalculatedAreaSqM = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("minimum_calculated_area_sq_m")), CultureInfo.InvariantCulture),
            UseSecondaryUnit = reader.GetInt32(reader.GetOrdinal("use_secondary_unit")) != 0,
            SecondaryUnitCoefficient = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("secondary_unit_coefficient")), CultureInfo.InvariantCulture),
            PriceByKilogram = reader.GetInt32(reader.GetOrdinal("price_by_kilogram")) != 0,
            PricesJson = reader.GetString(reader.GetOrdinal("prices_json")),
            UserId = GetNullableGuid(reader, "user_id"),
            IsDeleted = reader.GetInt32(reader.GetOrdinal("is_deleted")) != 0,
            SyncStatus = (SyncStatus)reader.GetInt32(reader.GetOrdinal("sync_status")),
            UpdatedAt = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("updated_at")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            LastSyncError = GetNullableString(reader, "last_sync_error")
        };
    }

    public static LocalOrder ReadOrder(SqliteDataReader reader)
    {
        return new LocalOrder
        {
            Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
            ProfitabilityPercent = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("profitability_percent")), CultureInfo.InvariantCulture),
            OrderNumber = reader.GetString(reader.GetOrdinal("order_number")),
            OrderDate = DateTime.Parse(reader.GetString(reader.GetOrdinal("order_date")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            Status = reader.GetInt32(reader.GetOrdinal("status")),
            CustomerId = GetNullableGuid(reader, "customer_id"),
            DealerId = GetNullableGuid(reader, "dealer_id"),
            CurrentPriceMode = reader.GetInt32(reader.GetOrdinal("current_price_mode")),
            TotalDiscountPercent = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("total_discount_percent")), CultureInfo.InvariantCulture),
            TotalDiscountSum = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("total_discount_sum")), CultureInfo.InvariantCulture),
            CustomerName = reader.GetString(reader.GetOrdinal("customer_name")),
            CustomerPhone = reader.GetString(reader.GetOrdinal("customer_phone")),
            SiteAddress = reader.GetString(reader.GetOrdinal("site_address")),
            DealerName = reader.GetString(reader.GetOrdinal("dealer_name")),
            OrderManagerName = reader.GetString(reader.GetOrdinal("order_manager_name")),
            CustomerCity = reader.GetString(reader.GetOrdinal("customer_city")),
            CustomerCode = reader.GetString(reader.GetOrdinal("customer_code")),
            ContractNumber = reader.GetString(reader.GetOrdinal("contract_number")),
            ContractDate = GetNullableDateTime(reader, "contract_date"),
            ProductionDate = GetNullableDateTime(reader, "production_date"),
            DeliveryDate = GetNullableDateTime(reader, "delivery_date"),
            InstallationDate = GetNullableDateTime(reader, "installation_date"),
            HasDelivery = reader.GetInt32(reader.GetOrdinal("has_delivery")) != 0,
            HasInstallation = reader.GetInt32(reader.GetOrdinal("has_installation")) != 0,
            ProfileNote = reader.GetString(reader.GetOrdinal("profile_note")),
            ProductionNote = reader.GetString(reader.GetOrdinal("production_note")),
            Note = reader.GetString(reader.GetOrdinal("note")),
            CurrencyCode = reader.GetString(reader.GetOrdinal("currency_code")),
            ConstructionsSubtotal = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("constructions_subtotal")), CultureInfo.InvariantCulture),
            Discount2Percent = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("discount2_percent")), CultureInfo.InvariantCulture),
            DeliveryCost = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("delivery_cost")), CultureInfo.InvariantCulture),
            AdditionalMaterialsCost = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("additional_materials_cost")), CultureInfo.InvariantCulture),
            AdditionalMaterialsInstallationCost = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("additional_materials_installation_cost")), CultureInfo.InvariantCulture),
            VatPercent = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("vat_percent")), CultureInfo.InvariantCulture),
            VatMode = reader.GetInt32(reader.GetOrdinal("vat_mode")),
            AgreedAmount = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("agreed_amount")), CultureInfo.InvariantCulture),
            PaidAmount = Convert.ToDecimal(reader.GetDouble(reader.GetOrdinal("paid_amount")), CultureInfo.InvariantCulture),
            PaymentCurrencyCode = reader.GetString(reader.GetOrdinal("payment_currency_code")),
            SelectedConstructionId = GetNullableGuid(reader, "selected_construction_id"),
            ExportFilePath = GetNullableString(reader, "export_file_path"),
            ConstructionsJson = reader.GetString(reader.GetOrdinal("constructions_json")),
            UserId = GetNullableGuid(reader, "user_id"),
            IsDeleted = reader.GetInt32(reader.GetOrdinal("is_deleted")) != 0,
            SyncStatus = (SyncStatus)reader.GetInt32(reader.GetOrdinal("sync_status")),
            UpdatedAt = DateTimeOffset.Parse(reader.GetString(reader.GetOrdinal("updated_at")), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            LastSyncError = GetNullableString(reader, "last_sync_error")
        };
    }

    public static int? GetNullableInt(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    public static Guid? GetNullableGuid(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetString(ordinal);
        return string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value);
    }

    public static string? GetNullableString(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static DateTime? GetNullableDateTime(SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return DateTime.Parse(reader.GetString(ordinal), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }

    public static object Db(Guid? value) => value.HasValue ? value.Value.ToString("D") : DBNull.Value;

    public static object Db(DateTime? value) =>
        value.HasValue ? value.Value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) : DBNull.Value;

    public static object Db(string? value) => value is null ? DBNull.Value : value;

    public static object Db(int? value) => value.HasValue ? value.Value : DBNull.Value;
}
