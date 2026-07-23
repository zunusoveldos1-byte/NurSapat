using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class ConstructionElementEntityConfiguration : IEntityTypeConfiguration<ConstructionElementEntity>
{
    public void Configure(EntityTypeBuilder<ConstructionElementEntity> builder)
    {
        builder.ToTable("construction_elements");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Name).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.Article).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.GlassFormula).HasMaxLength(128);
        builder.Property(entity => entity.GlassStructure).HasMaxLength(256);
        builder.Property(entity => entity.Notes).HasMaxLength(4000);

        builder.HasIndex(entity => entity.MaterialId);
        builder.HasIndex(entity => entity.Article);

        builder.HasOne(entity => entity.Material)
            .WithMany()
            .HasForeignKey(entity => entity.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(entity => entity.AssociatedSeries)
            .WithOne(series => series.ConstructionElement)
            .HasForeignKey(series => series.ConstructionElementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(entity => entity.HardwareRules)
            .WithOne(rule => rule.ConstructionElement)
            .HasForeignKey(rule => rule.ConstructionElementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ConstructionElementSeriesEntityConfiguration : IEntityTypeConfiguration<ConstructionElementSeriesEntity>
{
    public void Configure(EntityTypeBuilder<ConstructionElementSeriesEntity> builder)
    {
        builder.ToTable("construction_element_series");

        builder.HasKey(entity => new { entity.ConstructionElementId, entity.SeriesId });
    }
}

public sealed class HardwareAutoPickRuleEntityConfiguration : IEntityTypeConfiguration<HardwareAutoPickRuleEntity>
{
    public void Configure(EntityTypeBuilder<HardwareAutoPickRuleEntity> builder)
    {
        builder.ToTable("hardware_auto_pick_rules");

        builder.HasKey(entity => entity.Id);

        builder.HasIndex(entity => entity.ConstructionElementId);
    }
}
