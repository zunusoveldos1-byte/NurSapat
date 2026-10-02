using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Configurations;

public sealed class StockDocumentEntityConfiguration : IEntityTypeConfiguration<StockDocumentEntity>
{
    public void Configure(EntityTypeBuilder<StockDocumentEntity> builder)
    {
        builder.ToTable("stock_documents");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Number).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Note).HasMaxLength(4000);

        builder.HasIndex(entity => entity.Number);
        builder.HasIndex(entity => entity.Date);

        builder.HasMany(entity => entity.Lines)
            .WithOne(line => line.StockDocument)
            .HasForeignKey(line => line.StockDocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class StockDocumentLineEntityConfiguration : IEntityTypeConfiguration<StockDocumentLineEntity>
{
    public void Configure(EntityTypeBuilder<StockDocumentLineEntity> builder)
    {
        builder.ToTable("stock_document_lines");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Article).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(512).IsRequired();
        builder.Property(entity => entity.Color).HasMaxLength(128);
        builder.Property(entity => entity.Unit).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Currency).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.Quantity).HasPrecision(18, 4);
        builder.Property(entity => entity.Price).HasPrecision(18, 4);

        builder.HasIndex(entity => entity.StockDocumentId);
        builder.HasIndex(entity => entity.MaterialId);

        builder.HasOne(entity => entity.Material)
            .WithMany()
            .HasForeignKey(entity => entity.MaterialId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FinishedGoodEntityConfiguration : IEntityTypeConfiguration<FinishedGoodEntity>
{
    public void Configure(EntityTypeBuilder<FinishedGoodEntity> builder)
    {
        builder.ToTable("finished_goods");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.OrderNumber).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.ProductCode).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.CellAddress).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.UserName).HasMaxLength(256);

        builder.HasIndex(entity => entity.OrderId);
        builder.HasIndex(entity => entity.ProductCode);

        builder.HasOne(entity => entity.Order)
            .WithMany()
            .HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
