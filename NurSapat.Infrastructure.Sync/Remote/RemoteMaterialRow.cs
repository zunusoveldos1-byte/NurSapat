using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace NurSapat.Infrastructure.Sync.Remote;

/// <summary>PostgREST model for Supabase public.materials (without local SyncStatus).</summary>
[Table("materials")]
public sealed class RemoteMaterialRow : BaseModel
{
    [PrimaryKey("id", false)]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("article")]
    public string Article { get; set; } = string.Empty;

    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Column("note")]
    public string Note { get; set; } = string.Empty;

    [Column("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [Column("section")]
    public int Section { get; set; }

    [Column("main_unit")]
    public int MainUnit { get; set; }

    [Column("linear_group")]
    public int? LinearGroup { get; set; }

    [Column("square_group")]
    public int? SquareGroup { get; set; }

    [Column("currency")]
    public int Currency { get; set; }

    [Column("base_price")]
    public decimal BasePrice { get; set; }

    [Column("waste_percent")]
    public decimal WastePercent { get; set; }

    [Column("print_on_assembly_drawing")]
    public bool PrintOnAssemblyDrawing { get; set; }

    [Column("print_on_commercial_offer")]
    public bool PrintOnCommercialOffer { get; set; }

    [Column("print_on_shipping_sheet")]
    public bool PrintOnShippingSheet { get; set; }

    [Column("group_print_positions")]
    public bool GroupPrintPositions { get; set; }

    [Column("is_warm_profile")]
    public bool IsWarmProfile { get; set; }

    [Column("inner_coating_area_per_meter")]
    public decimal InnerCoatingAreaPerMeter { get; set; }

    [Column("outer_coating_area_per_meter")]
    public decimal OuterCoatingAreaPerMeter { get; set; }

    [Column("enable_length_cut_optimization")]
    public bool EnableLengthCutOptimization { get; set; }

    [Column("bar_length_mm")]
    public decimal BarLengthMm { get; set; }

    [Column("illiquid_threshold_mm")]
    public decimal IlliquidThresholdMm { get; set; }

    [Column("scrap_mm")]
    public decimal ScrapMm { get; set; }

    [Column("enable_plane_cut_optimization")]
    public bool EnablePlaneCutOptimization { get; set; }

    [Column("sheet_width_mm")]
    public decimal SheetWidthMm { get; set; }

    [Column("sheet_height_mm")]
    public decimal SheetHeightMm { get; set; }

    [Column("glass_tint")]
    public decimal GlassTint { get; set; }

    [Column("minimum_calculated_area_sq_m")]
    public decimal MinimumCalculatedAreaSqM { get; set; }

    [Column("use_secondary_unit")]
    public bool UseSecondaryUnit { get; set; }

    [Column("secondary_unit_coefficient")]
    public decimal SecondaryUnitCoefficient { get; set; }

    [Column("price_by_kilogram")]
    public bool PriceByKilogram { get; set; }

    [Column("prices_json")]
    public string PricesJson { get; set; } = "[]";

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }
}
