using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class OrderConstructionEntityConfiguration : IEntityTypeConfiguration<OrderConstructionEntity>
{
    public void Configure(EntityTypeBuilder<OrderConstructionEntity> builder)
    {
        builder.ToTable("order_constructions");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Name).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.DrawingJson).IsRequired();
        builder.Property(entity => entity.ProfileSystemName).HasMaxLength(256);
        builder.Property(entity => entity.ColorName).HasMaxLength(128);
        builder.Property(entity => entity.ColorHex).HasMaxLength(16);
        builder.Property(entity => entity.Manager).HasMaxLength(256);
        builder.Property(entity => entity.Supervisor).HasMaxLength(256);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);
        builder.Property(entity => entity.OuterWidthMm).HasPrecision(18, 4);
        builder.Property(entity => entity.OuterHeightMm).HasPrecision(18, 4);
        builder.Property(entity => entity.GlazingAreaSqM).HasPrecision(18, 4);
        builder.Property(entity => entity.ProductAreaSqM).HasPrecision(18, 4);
        builder.Property(entity => entity.WeightKg).HasPrecision(18, 4);
        builder.Property(entity => entity.ProfileCost).HasPrecision(18, 2);
        builder.Property(entity => entity.ComponentsCost).HasPrecision(18, 2);
        builder.Property(entity => entity.SealantCost).HasPrecision(18, 2);
        builder.Property(entity => entity.GlazingCost).HasPrecision(18, 2);
        builder.Property(entity => entity.HardwareCost).HasPrecision(18, 2);
        builder.Property(entity => entity.InstallationMaterialsCost).HasPrecision(18, 2);
        builder.Property(entity => entity.Expenses).HasPrecision(18, 2);
        builder.Property(entity => entity.LaborCost).HasPrecision(18, 2);
        builder.Property(entity => entity.MaterialsWithoutMarkup).HasPrecision(18, 2);
        builder.Property(entity => entity.ProfitabilityPercent).HasPrecision(9, 2);
        builder.Property(entity => entity.AdditionalExpensesPercent).HasPrecision(9, 2);
        builder.Property(entity => entity.AdditionalExpensesFixed).HasPrecision(18, 2);
        builder.Property(entity => entity.Discount1Percent).HasPrecision(9, 2);
        builder.Property(entity => entity.DemolitionCost).HasPrecision(18, 2);
        builder.Property(entity => entity.InstallationCost).HasPrecision(18, 2);

        builder.HasIndex(entity => new { entity.OrderId, entity.Number }).IsUnique();

        builder.HasMany(entity => entity.GeometryElements)
            .WithOne(element => element.OrderConstruction)
            .HasForeignKey(element => element.OrderConstructionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.SpecificationLines)
            .WithOne(line => line.OrderConstruction)
            .HasForeignKey(line => line.OrderConstructionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
