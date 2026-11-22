using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class MaterialEntityConfiguration : IEntityTypeConfiguration<MaterialEntity>
{
    public void Configure(EntityTypeBuilder<MaterialEntity> builder)
    {
        builder.ToTable("materials");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Article).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.Note).HasMaxLength(4000);
        builder.Property(entity => entity.Manufacturer).HasMaxLength(256);
        builder.Property(entity => entity.SuppliersText).HasMaxLength(1000);
        builder.Property(entity => entity.BasePrice).HasPrecision(18, 4);
        builder.Property(entity => entity.WastePercent).HasPrecision(9, 2);
        builder.Property(entity => entity.InnerCoatingAreaPerMeter).HasPrecision(18, 4);
        builder.Property(entity => entity.OuterCoatingAreaPerMeter).HasPrecision(18, 4);
        builder.Property(entity => entity.BarLengthMm).HasPrecision(18, 4);
        builder.Property(entity => entity.IlliquidThresholdMm).HasPrecision(18, 4);
        builder.Property(entity => entity.ScrapMm).HasPrecision(18, 4);
        builder.Property(entity => entity.SheetWidthMm).HasPrecision(18, 4);
        builder.Property(entity => entity.SheetHeightMm).HasPrecision(18, 4);
        builder.Property(entity => entity.GlassTint).HasPrecision(9, 4);
        builder.Property(entity => entity.MinimumCalculatedAreaSqM).HasPrecision(18, 4);
        builder.Property(entity => entity.SecondaryUnitCoefficient).HasPrecision(18, 6);

        builder.HasIndex(entity => entity.Article).IsUnique();
        builder.HasIndex(entity => entity.Name);

        builder.HasMany(entity => entity.ColorPrices)
            .WithOne(price => price.Material)
            .HasForeignKey(price => price.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.DimensionPrices)
            .WithOne(price => price.Material)
            .HasForeignKey(price => price.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class MaterialColorPriceEntityConfiguration : IEntityTypeConfiguration<MaterialColorPriceEntity>
{
    public void Configure(EntityTypeBuilder<MaterialColorPriceEntity> builder)
    {
        builder.ToTable("material_color_prices");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.ColorArticle).HasMaxLength(64);
        builder.Property(entity => entity.ColorName).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.ColorHex).HasMaxLength(16);
        builder.Property(entity => entity.LinkArticle).HasMaxLength(64);
        builder.Property(entity => entity.Price).HasPrecision(18, 4);
        builder.Property(entity => entity.CustomBarLengthMm).HasPrecision(18, 4);

        builder.HasIndex(entity => entity.MaterialId);
    }
}

public sealed class MaterialDimensionPriceEntityConfiguration : IEntityTypeConfiguration<MaterialDimensionPriceEntity>
{
    public void Configure(EntityTypeBuilder<MaterialDimensionPriceEntity> builder)
    {
        builder.ToTable("material_dimension_prices");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.WidthMm).HasPrecision(18, 4);
        builder.Property(entity => entity.HeightMm).HasPrecision(18, 4);
        builder.Property(entity => entity.Price).HasPrecision(18, 4);

        builder.HasIndex(entity => entity.MaterialId);
    }
}
