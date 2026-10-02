using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class CustomerEntityConfiguration : IEntityTypeConfiguration<CustomerEntity>
{
    public void Configure(EntityTypeBuilder<CustomerEntity> builder)
    {
        builder.ToTable("customers");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.FullName).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.Phone).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.OkpoCode).HasMaxLength(32);
        builder.Property(entity => entity.BankName).HasMaxLength(256);
        builder.Property(entity => entity.CheckingAccount).HasMaxLength(64);
        builder.Property(entity => entity.Mfo).HasMaxLength(16);
        builder.Property(entity => entity.LegalAddress).HasMaxLength(512);
        builder.Property(entity => entity.City).HasMaxLength(128);
        builder.Property(entity => entity.ActualAddress).HasMaxLength(512);
        builder.Property(entity => entity.DealerCode).HasMaxLength(64);
        builder.Property(entity => entity.Note).HasMaxLength(2000);
        builder.Property(entity => entity.Turnover).HasPrecision(18, 2);
        builder.Property(entity => entity.Debt).HasPrecision(18, 2);

        builder.HasIndex(entity => entity.FullName);
        builder.HasIndex(entity => entity.Phone);
        builder.HasIndex(entity => entity.IsDeleted);
    }
}
