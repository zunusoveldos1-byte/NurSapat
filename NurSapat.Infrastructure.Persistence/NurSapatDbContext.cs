using Microsoft.EntityFrameworkCore;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence;

public sealed class NurSapatDbContext : DbContext
{
    public NurSapatDbContext(DbContextOptions<NurSapatDbContext> options)
        : base(options)
    {
    }

    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();

    public DbSet<SupplierEntity> Suppliers => Set<SupplierEntity>();

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<OrderConstructionEntity> OrderConstructions => Set<OrderConstructionEntity>();

    public DbSet<GeometryElementEntity> GeometryElements => Set<GeometryElementEntity>();

    public DbSet<CostSpecificationLineEntity> CostSpecificationLines => Set<CostSpecificationLineEntity>();

    public DbSet<MaterialEntity> Materials => Set<MaterialEntity>();

    public DbSet<MaterialColorPriceEntity> MaterialColorPrices => Set<MaterialColorPriceEntity>();

    public DbSet<MaterialDimensionPriceEntity> MaterialDimensionPrices => Set<MaterialDimensionPriceEntity>();

    public DbSet<ConstructionElementEntity> ConstructionElements => Set<ConstructionElementEntity>();

    public DbSet<ConstructionElementSeriesEntity> ConstructionElementSeries => Set<ConstructionElementSeriesEntity>();

    public DbSet<HardwareAutoPickRuleEntity> HardwareAutoPickRules => Set<HardwareAutoPickRuleEntity>();

    public DbSet<StockDocumentEntity> StockDocuments => Set<StockDocumentEntity>();

    public DbSet<StockDocumentLineEntity> StockDocumentLines => Set<StockDocumentLineEntity>();

    public DbSet<FinishedGoodEntity> FinishedGoods => Set<FinishedGoodEntity>();

    public DbSet<CurrencyRateEntity> CurrencyRates => Set<CurrencyRateEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NurSapatDbContext).Assembly);
    }
}
