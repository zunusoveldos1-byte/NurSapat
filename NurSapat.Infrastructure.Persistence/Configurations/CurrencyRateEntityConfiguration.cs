using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class CurrencyRateEntityConfiguration : IEntityTypeConfiguration<CurrencyRateEntity>
{
    public void Configure(EntityTypeBuilder<CurrencyRateEntity> builder)
    {
        builder.ToTable("currency_rates");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.Rate).HasPrecision(18, 6);
        builder.Property(entity => entity.RateDate).HasColumnType("date");
        builder.Property(entity => entity.CreatedAt).IsRequired();

        builder.HasIndex(entity => new { entity.CurrencyCode, entity.RateDate }).IsUnique();
        builder.HasIndex(entity => entity.RateDate);
    }
}
