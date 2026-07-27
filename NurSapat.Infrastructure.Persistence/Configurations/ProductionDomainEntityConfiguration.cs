using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class PriceListVersionEntityConfiguration : IEntityTypeConfiguration<PriceListVersionEntity>
{
    public void Configure(EntityTypeBuilder<PriceListVersionEntity> builder)
    {
        builder.ToTable("price_list_versions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.IsActive, x.EffectiveFrom });
        builder.HasMany(x => x.Items).WithOne(x => x.PriceListVersion)
            .HasForeignKey(x => x.PriceListVersionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PriceListItemEntityConfiguration : IEntityTypeConfiguration<PriceListItemEntity>
{
    public void Configure(EntityTypeBuilder<PriceListItemEntity> builder)
    {
        builder.ToTable("price_list_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialArticle).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PurchasePrice).HasPrecision(18, 4);
        builder.Property(x => x.BaseRetailPrice).HasPrecision(18, 4);
        builder.Property(x => x.DealerPrice).HasPrecision(18, 4);
        builder.HasIndex(x => x.MaterialArticle);
        builder.HasIndex(x => new { x.PriceListVersionId, x.MaterialArticle }).IsUnique();
    }
}

public sealed class OrderCalculationSnapshotEntityConfiguration
    : IEntityTypeConfiguration<OrderCalculationSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<OrderCalculationSnapshotEntity> builder)
    {
        builder.ToTable("order_calculation_snapshots");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(x => x.ExchangeRate).HasPrecision(18, 8);
        builder.Property(x => x.ItemsSnapshotJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TotalCostPrice).HasPrecision(18, 2);
        builder.Property(x => x.TotalMarginPercent).HasPrecision(9, 4);
        builder.Property(x => x.TotalDiscountPercent).HasPrecision(9, 4);
        builder.Property(x => x.TotalPrice).HasPrecision(18, 2);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.OrderId, x.SnapshotDate });
        builder.HasOne(x => x.Order).WithMany(x => x.CalculationSnapshots)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.PriceListVersion).WithMany(x => x.CalculationSnapshots)
            .HasForeignKey(x => x.PriceListVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TechnologyRuleEntityConfiguration : IEntityTypeConfiguration<TechnologyRuleEntity>
{
    public void Configure(EntityTypeBuilder<TechnologyRuleEntity> builder)
    {
        builder.ToTable("technology_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProfileSystemCode).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Article).HasMaxLength(128).IsRequired();
        ConfigurePrecision(builder);
        builder.HasIndex(x => x.Article);
        builder.HasIndex(x => new { x.ProfileSystemCode, x.Article }).IsUnique();
    }

    private static void ConfigurePrecision(EntityTypeBuilder<TechnologyRuleEntity> builder)
    {
        builder.Property(x => x.MinRadiusMm).HasPrecision(18, 3);
        builder.Property(x => x.MaxSashWidthMm).HasPrecision(18, 3);
        builder.Property(x => x.MaxSashHeightMm).HasPrecision(18, 3);
        builder.Property(x => x.MaxSashWeightKg).HasPrecision(18, 3);
        builder.Property(x => x.MaxInertiaIx).HasPrecision(24, 6);
    }
}

public sealed class ProductionHardwareKitEntityConfiguration
    : IEntityTypeConfiguration<ProductionHardwareKitEntity>
{
    public void Configure(EntityTypeBuilder<ProductionHardwareKitEntity> builder)
    {
        builder.ToTable("production_hardware_kits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SystemBrand).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ComponentsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.MinSashWidth).HasPrecision(18, 3);
        builder.Property(x => x.MaxSashWidth).HasPrecision(18, 3);
        builder.Property(x => x.MinSashHeight).HasPrecision(18, 3);
        builder.Property(x => x.MaxSashHeight).HasPrecision(18, 3);
        builder.Property(x => x.MaxWeightKg).HasPrecision(18, 3);
    }
}

public sealed class OperationRouteEntityConfiguration : IEntityTypeConfiguration<OperationRouteEntity>
{
    public void Configure(EntityTypeBuilder<OperationRouteEntity> builder)
    {
        builder.ToTable("operation_routes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();
        builder.Property(x => x.OperationName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.LaborCostPerHour).HasPrecision(18, 4);
        builder.Property(x => x.StandardMinutesPerUnit).HasPrecision(18, 4);
        builder.HasIndex(x => new { x.Name, x.SequenceOrder }).IsUnique();
    }
}

public sealed class ProductionBatchEntityConfiguration : IEntityTypeConfiguration<ProductionBatchEntity>
{
    public void Configure(EntityTypeBuilder<ProductionBatchEntity> builder)
    {
        builder.ToTable("production_batches");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BatchNumber).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.BatchNumber).IsUnique();
        builder.HasIndex(x => x.PlannedStartDate);
    }
}

public sealed class MaterialReservationEntityConfiguration
    : IEntityTypeConfiguration<MaterialReservationEntity>
{
    public void Configure(EntityTypeBuilder<MaterialReservationEntity> builder)
    {
        builder.ToTable("material_reservations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialArticle).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ReservedQuantity).HasPrecision(18, 4);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.MaterialArticle);
        builder.HasOne(x => x.Order).WithMany(x => x.MaterialReservations)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StockPostingEntityConfiguration : IEntityTypeConfiguration<StockPostingEntity>
{
    public void Configure(EntityTypeBuilder<StockPostingEntity> builder)
    {
        builder.ToTable("stock_postings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MaterialArticle).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.MaterialArticle);
        builder.HasOne(x => x.Order).WithMany(x => x.StockPostings)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Batch).WithMany(x => x.StockPostings)
            .HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CuttingJobEntityConfiguration : IEntityTypeConfiguration<CuttingJobEntity>
{
    public void Configure(EntityTypeBuilder<CuttingJobEntity> builder)
    {
        builder.ToTable("cutting_jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CuttingDataJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.BatchId);
        builder.HasOne(x => x.Batch).WithMany(x => x.CuttingJobs)
            .HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Cascade);
    }
}
