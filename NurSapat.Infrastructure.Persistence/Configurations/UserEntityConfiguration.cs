using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Username).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.PermissionsJson).HasColumnType("jsonb").IsRequired();

        builder.HasIndex(entity => entity.Username).IsUnique();
    }
}
