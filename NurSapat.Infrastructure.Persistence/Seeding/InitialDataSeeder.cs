using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Materials;
using NurSapat.Infrastructure.Persistence.Abstractions;
using NurSapat.Infrastructure.Persistence.Models;

namespace NurSapat.Infrastructure.Persistence.Seeding;

public sealed class InitialDataSeeder
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;
    private readonly IErpMaterialRepository _materialRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISupplierRepository _supplierRepository;

    public InitialDataSeeder(
        IDbContextFactory<NurSapatDbContext> dbContextFactory,
        IErpMaterialRepository materialRepository,
        ICustomerRepository customerRepository,
        ISupplierRepository supplierRepository)
    {
        _dbContextFactory = dbContextFactory;
        _materialRepository = materialRepository;
        _customerRepository = customerRepository;
        _supplierRepository = supplierRepository;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (!await dbContext.Materials.AnyAsync(cancellationToken))
        {
            foreach (var material in CreateDefaultMaterials())
            {
                await _materialRepository.SaveAsync(material, cancellationToken);
            }
        }

        if (!await dbContext.Customers.AnyAsync(cancellationToken))
        {
            await _customerRepository.SaveAsync(new()
            {
                Id = Guid.Parse("a1000001-0000-4000-8000-000000000001"),
                FullName = "Иванов Алексей Петрович",
                Phone = "+996 555 123 456",
                Type = Core.Models.Orders.CustomerType.Individual,
                Turnover = 485_600m,
                Debt = 0m,
                OkpoCode = "21456398741236",
                BankName = "ОАО «РСК Банк»",
                CheckingAccount = "11800000012345678901",
                Mfo = "118001",
                LegalAddress = "г. Бишкек, ул. Чуй 145, кв. 32",
                Note = "Постоянный розничный клиент"
            }, cancellationToken);

            await _customerRepository.SaveAsync(new()
            {
                Id = Guid.Parse("a1000002-0000-4000-8000-000000000002"),
                FullName = "ОсОО «Окна Бишкек»",
                Phone = "+996 312 45 67 89",
                Type = Core.Models.Orders.CustomerType.LegalEntity,
                Turnover = 2_450_000m,
                Debt = 125_000m,
                OkpoCode = "21456398741237",
                BankName = "ОАО «Кыргызстан»",
                CheckingAccount = "11800000098765432109",
                Mfo = "118002",
                LegalAddress = "г. Бишкек, ул. Московская 42",
                Note = "Дилер, скидка 15%"
            }, cancellationToken);
        }

        if (!await dbContext.Suppliers.AnyAsync(cancellationToken))
        {
            await _supplierRepository.SaveAsync(new()
            {
                Id = Guid.Parse("b1000001-0000-4000-8000-000000000001"),
                Name = "Rehau Кыргызстан",
                Phone = "+996 312 90 12 34",
                LegalAddress = "г. Бишкек, промзона",
                ContractTerms = "Отсрочка 14 дней",
                Note = "Основной поставщик профиля"
            }, cancellationToken);
        }
    }

    private static IEnumerable<ErpMaterialDetail> CreateDefaultMaterials() =>
    [
        CreateMaterial(CatalogMaterialIds.ProfileRehau70, CatalogMaterialArticles.ProfileRehau70,
            "Профиль Rehau 70 белый", "Rehau", ErpMaterialSection.Profile, ErpMaterialMainUnit.LinearMeter, 1250m),
        CreateMaterial(CatalogMaterialIds.HardwareMacoEgress, CatalogMaterialArticles.HardwareMacoEgress,
            "Ножницы Maco Multi-Matic", "Maco", ErpMaterialSection.Hardware, ErpMaterialMainUnit.Piece, 4200m),
        CreateMaterial(CatalogMaterialIds.Glazing4_16_4, CatalogMaterialArticles.Glazing4_16_4,
            "Стеклопакет 4-16-4", "СтеклоТорг", ErpMaterialSection.Glazing, ErpMaterialMainUnit.SquareMeter, 2850m,
            squareGroup: ErpMaterialSquareGroup.Glass),
        CreateMaterial(CatalogMaterialIds.SealantEpdm, CatalogMaterialArticles.SealantEpdm,
            "Уплотнитель EPDM", "Rehau", ErpMaterialSection.Sealants, ErpMaterialMainUnit.LinearMeter, 85m),
        CreateMaterial(CatalogMaterialIds.Reinforcement12, CatalogMaterialArticles.Reinforcement12,
            "Армирование 1.2 мм", "Rehau", ErpMaterialSection.Profile, ErpMaterialMainUnit.LinearMeter, 320m,
            linearGroup: ErpMaterialLinearGroup.Reinforcement)
    ];

    private static ErpMaterialDetail CreateMaterial(
        Guid id,
        string article,
        string name,
        string manufacturer,
        ErpMaterialSection section,
        ErpMaterialMainUnit mainUnit,
        decimal basePrice,
        ErpMaterialLinearGroup? linearGroup = null,
        ErpMaterialSquareGroup? squareGroup = null) => new()
    {
        Id = id,
        Article = article,
        Name = name,
        Manufacturer = manufacturer,
        Section = section,
        MainUnit = mainUnit,
        LinearGroup = linearGroup,
        SquareGroup = squareGroup,
        BasePrice = basePrice,
        EnableLengthCutOptimization = section == ErpMaterialSection.Profile,
        ColorPrices =
        [
            new ErpMaterialColorPrice
            {
                Id = Guid.NewGuid(),
                ColorName = "Белый",
                ColorHex = "#FFFFFF",
                Price = basePrice,
                IsDefault = true
            }
        ]
    };
}
