using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class SupplierEntityConfiguration : IEntityTypeConfiguration<SupplierEntity>
{
    public void Configure(EntityTypeBuilder<SupplierEntity> builder)
    {
        builder.ToTable("suppliers");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Name).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Phone).HasMaxLength(32);
        builder.Property(entity => entity.LegalAddress).HasMaxLength(512);
        builder.Property(entity => entity.ContractTerms).HasMaxLength(2000);
        builder.Property(entity => entity.Note).HasMaxLength(2000);

        builder.HasIndex(entity => entity.Name);
    }
}
