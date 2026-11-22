using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class GeometryElementEntityConfiguration : IEntityTypeConfiguration<GeometryElementEntity>
{
    public void Configure(EntityTypeBuilder<GeometryElementEntity> builder)
    {
        builder.ToTable("geometry_elements");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Label).HasMaxLength(256);
        builder.Property(entity => entity.SpokeColor).HasMaxLength(64);
        builder.Property(entity => entity.SourceToolId).HasMaxLength(64);
        builder.Property(entity => entity.X).HasPrecision(18, 4);
        builder.Property(entity => entity.Y).HasPrecision(18, 4);
        builder.Property(entity => entity.WidthMm).HasPrecision(18, 4);
        builder.Property(entity => entity.HeightMm).HasPrecision(18, 4);
        builder.Property(entity => entity.EndX).HasPrecision(18, 4);
        builder.Property(entity => entity.EndY).HasPrecision(18, 4);
        builder.Property(entity => entity.SpokeArcCenterX).HasPrecision(18, 4);
        builder.Property(entity => entity.SpokeArcCenterY).HasPrecision(18, 4);

        builder.HasIndex(entity => entity.OrderConstructionId);
        builder.HasIndex(entity => entity.MaterialId);
    }
}
