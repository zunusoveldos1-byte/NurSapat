using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class OrderEntityConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.OrderNumber).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.CustomerName).HasMaxLength(256);
        builder.Property(entity => entity.CustomerPhone).HasMaxLength(32);
        builder.Property(entity => entity.SiteAddress).HasMaxLength(512);
        builder.Property(entity => entity.DealerName).HasMaxLength(256);
        builder.Property(entity => entity.OrderManagerName).HasMaxLength(256);
        builder.Property(entity => entity.CustomerCity).HasMaxLength(128);
        builder.Property(entity => entity.CustomerCode).HasMaxLength(64);
        builder.Property(entity => entity.ContractNumber).HasMaxLength(64);
        builder.Property(entity => entity.ProfileNote).HasMaxLength(2000);
        builder.Property(entity => entity.ProductionNote).HasMaxLength(2000);
        builder.Property(entity => entity.Note).HasMaxLength(4000);
        builder.Property(entity => entity.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.PaymentCurrencyCode).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.ProfitabilityPercent).HasPrecision(9, 2);
        builder.Property(entity => entity.TotalDiscountPercent).HasPrecision(9, 2);
        builder.Property(entity => entity.TotalDiscountSum).HasPrecision(18, 2);
        builder.Property(entity => entity.ConstructionsSubtotal).HasPrecision(18, 2);
        builder.Property(entity => entity.Discount2Percent).HasPrecision(9, 2);
        builder.Property(entity => entity.DeliveryCost).HasPrecision(18, 2);
        builder.Property(entity => entity.AdditionalMaterialsCost).HasPrecision(18, 2);
        builder.Property(entity => entity.AdditionalMaterialsInstallationCost).HasPrecision(18, 2);
        builder.Property(entity => entity.VatPercent).HasPrecision(9, 2);
        builder.Property(entity => entity.AgreedAmount).HasPrecision(18, 2);
        builder.Property(entity => entity.PaidAmount).HasPrecision(18, 2);

        builder.HasIndex(entity => entity.OrderNumber).IsUnique();
        builder.HasIndex(entity => entity.OrderDate);
        builder.HasIndex(entity => entity.CustomerId);

        builder.HasOne(entity => entity.Customer)
            .WithMany()
            .HasForeignKey(entity => entity.CustomerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(entity => entity.Constructions)
            .WithOne(construction => construction.Order)
            .HasForeignKey(construction => construction.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
