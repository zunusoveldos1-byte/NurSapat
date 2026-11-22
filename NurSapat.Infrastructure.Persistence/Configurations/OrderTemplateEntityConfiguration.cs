using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class OrderTemplateEntityConfiguration : IEntityTypeConfiguration<OrderTemplateEntity>
{
    public void Configure(EntityTypeBuilder<OrderTemplateEntity> builder)
    {
        builder.ToTable("order_templates");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Name).HasMaxLength(256).IsRequired();
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.DefaultOrderNote).HasMaxLength(2000);
        builder.Property(entity => entity.ProfitMargin).HasPrecision(9, 2);
        builder.Property(entity => entity.AdditionalExpensesFixed).HasPrecision(18, 2);
        builder.Property(entity => entity.DismantlingPrice).HasPrecision(18, 2);
        builder.Property(entity => entity.InstallationPrice).HasPrecision(18, 2);
        builder.Property(entity => entity.DefaultPrepaymentPercent).HasPrecision(9, 2);
        builder.Property(entity => entity.DealerDiscountPercent).HasPrecision(9, 2);

        builder.HasIndex(entity => entity.Name);
    }
}
