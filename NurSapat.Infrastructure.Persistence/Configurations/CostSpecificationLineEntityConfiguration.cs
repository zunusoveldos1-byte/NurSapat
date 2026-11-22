using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class CostSpecificationLineEntityConfiguration : IEntityTypeConfiguration<CostSpecificationLineEntity>
{
    public void Configure(EntityTypeBuilder<CostSpecificationLineEntity> builder)
    {
        builder.ToTable("cost_specification_lines");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.MaterialName).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.Article).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Unit).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Quantity).HasPrecision(18, 4);
        builder.Property(entity => entity.UnitPrice).HasPrecision(18, 4);
        builder.Property(entity => entity.WastePercent).HasPrecision(9, 2);
        builder.Property(entity => entity.PriceSourceJson).HasColumnType("jsonb");

        builder.HasIndex(entity => entity.OrderConstructionId);
        builder.HasIndex(entity => entity.MaterialId);
    }
}
