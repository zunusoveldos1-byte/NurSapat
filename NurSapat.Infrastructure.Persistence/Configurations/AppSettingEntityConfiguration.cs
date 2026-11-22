using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class AppSettingEntityConfiguration : IEntityTypeConfiguration<AppSettingEntity>
{
    public void Configure(EntityTypeBuilder<AppSettingEntity> builder)
    {
        builder.ToTable("app_settings");

        builder.HasKey(entity => entity.Key);

        builder.Property(entity => entity.Key).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Value).IsRequired();
        builder.Property(entity => entity.UpdatedAt).IsRequired();
    }
}
