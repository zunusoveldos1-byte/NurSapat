using Microsoft.EntityFrameworkCore;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models.Currency;
using NurSapat.Core.Models.Materials;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Users;
using NurSapat.Infrastructure.Persistence.Abstractions;
using NurSapat.Infrastructure.Persistence.Models;

namespace NurSapat.Infrastructure.Persistence.Seeding;

public sealed class InitialDataSeeder
{
    private readonly IDbContextFactory<NurSapatDbContext> _dbContextFactory;
    private readonly IErpMaterialRepository _materialRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUserAccountRepository _userAccountRepository;
    private readonly IOrderTemplateRepository _orderTemplateRepository;
    private readonly ICurrencyRateRepository _currencyRateRepository;

    public InitialDataSeeder(
        IDbContextFactory<NurSapatDbContext> dbContextFactory,
        IErpMaterialRepository materialRepository,
        ICustomerRepository customerRepository,
        ISupplierRepository supplierRepository,
        IUserAccountRepository userAccountRepository,
        IOrderTemplateRepository orderTemplateRepository,
        ICurrencyRateRepository currencyRateRepository)
    {
        _dbContextFactory = dbContextFactory;
        _materialRepository = materialRepository;
        _customerRepository = customerRepository;
        _supplierRepository = supplierRepository;
        _userAccountRepository = userAccountRepository;
        _orderTemplateRepository = orderTemplateRepository;
        _currencyRateRepository = currencyRateRepository;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Old Dapper schema (users/materials/clients/projects) blocks EF CreateTable — move aside first.
        await LegacyDatabasePreparer.PrepareAsync(dbContext, cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
        await LegacyDatabasePreparer.ImportLegacyUsersAsync(dbContext, cancellationToken);

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
                Type = CustomerType.Individual,
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
                Type = CustomerType.LegalEntity,
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

        if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            foreach (var user in CreateDefaultUsers())
            {
                await _userAccountRepository.SaveAsync(user, cancellationToken);
            }
        }

        if (!await dbContext.OrderTemplates.AnyAsync(cancellationToken))
        {
            foreach (var template in CreateDefaultOrderTemplates())
            {
                await _orderTemplateRepository.SaveAsync(template, cancellationToken);
            }
        }

        if (!await dbContext.CurrencyRates.AnyAsync(cancellationToken))
        {
            await _currencyRateRepository.UpsertRatesAsync(CreateDefaultCurrencyRates(), cancellationToken);
        }

        if (!await dbContext.ConstructionElements.AnyAsync(cancellationToken))
        {
            foreach (var element in CreateDefaultConstructionElements())
            {
                await _materialRepository.SaveConstructionElementAsync(element, cancellationToken);
            }
        }
    }

    private static IEnumerable<UserAccount> CreateDefaultUsers()
    {
        yield return CreateUser(
            Guid.Parse("c1000001-0000-4000-8000-000000000001"),
            "admin",
            "admin",
            1,
            """{"canManageUsers":true,"canAccessMaterialsDictionary":true,"canConfigureConstructionElements":true,"canAccessEconomics":true,"canEditOrderSheetCoefficients":true,"canEditOrderSheetCostCoefficients":true,"canEditOrderSheetExtraExpenseCoefficients":true,"canPrintMaterialPrices":true,"canDeleteOrders":true,"canAccessWarehouse":true,"canEditWarehouse":true,"canEditWarehouseInvoice":true,"groupOnlyDataAccess":false}""");

        yield return CreateUser(
            Guid.Parse("c1000002-0000-4000-8000-000000000002"),
            "manager",
            "manager",
            1,
            """{"canAccessMaterialsDictionary":true,"canAccessEconomics":true,"canAccessWarehouse":true,"canEditWarehouse":true,"canPrintMaterialPrices":true}""");

        yield return CreateUser(
            Guid.Parse("c1000003-0000-4000-8000-000000000003"),
            "operator",
            "operator",
            2,
            """{"canAccessMaterialsDictionary":true,"canAccessWarehouse":true,"groupOnlyDataAccess":true}""");
    }

    private static UserAccount CreateUser(Guid id, string username, string password, int groupNumber, string permissionsJson) =>
        new()
        {
            Id = id,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            GroupNumber = groupNumber,
            PermissionsJson = permissionsJson
        };

    private static IEnumerable<OrderTemplate> CreateDefaultOrderTemplates() =>
    [
        new()
        {
            Id = Guid.Parse("d1000001-0000-4000-8000-000000000001"),
            Name = "Дилерский Ош",
            ManufacturerId = Guid.Parse("A1000001-0000-4000-8000-000000000002"),
            WindPressure = 420,
            ProfitMargin = 18m,
            AdditionalExpensesPercent = 5,
            AdditionalExpensesFixed = 1500m,
            DismantlingPrice = 800m,
            InstallationPrice = 2200m,
            CurrencyCode = "KGS"
        },
        new()
        {
            Id = Guid.Parse("d1000002-0000-4000-8000-000000000002"),
            Name = "Розничный стандарт",
            WindPressure = 380,
            ProfitMargin = 25m,
            AdditionalExpensesPercent = 7,
            AdditionalExpensesFixed = 2000m,
            DismantlingPrice = 1000m,
            InstallationPrice = 3500m,
            CurrencyCode = "KGS"
        },
        new()
        {
            Id = Guid.Parse("d1000003-0000-4000-8000-000000000003"),
            Name = "Опт",
            ManufacturerId = Guid.Parse("A1000001-0000-4000-8000-000000000003"),
            WindPressure = 450,
            ProfitMargin = 12m,
            AdditionalExpensesPercent = 3,
            AdditionalExpensesFixed = 900m,
            DismantlingPrice = 600m,
            InstallationPrice = 1800m,
            CurrencyCode = "USD"
        }
    ];

    private static IReadOnlyList<CurrencyRate> CreateDefaultCurrencyRates()
    {
        var createdAt = DateTime.UtcNow;
        return
        [
            CreateRate("USD", 86.20m, new DateTime(2026, 1, 1), createdAt),
            CreateRate("EUR", 98.10m, new DateTime(2026, 1, 1), createdAt),
            CreateRate("CNY", 12.55m, new DateTime(2026, 1, 1), createdAt),
            CreateRate("UE", 86.20m, new DateTime(2026, 1, 1), createdAt),
            CreateRate("USD", 87.45m, new DateTime(2026, 7, 1), createdAt),
            CreateRate("EUR", 99.62m, new DateTime(2026, 7, 1), createdAt),
            CreateRate("CNY", 12.88m, new DateTime(2026, 7, 1), createdAt),
            CreateRate("UE", 87.45m, new DateTime(2026, 7, 1), createdAt)
        ];
    }

    private static CurrencyRate CreateRate(string code, decimal rate, DateTime date, DateTime createdAt) =>
        new()
        {
            CurrencyCode = code,
            Rate = rate,
            RateDate = date,
            CreatedAt = createdAt
        };

    private static IEnumerable<ConstructionElementModel> CreateDefaultConstructionElements() =>
    [
        new()
        {
            Id = Guid.Parse("e1000001-0000-4000-8000-000000000001"),
            MaterialId = CatalogMaterialIds.ProfileRehau70,
            Name = "Рама Galwin 60",
            ManufacturerId = Guid.Parse("A1000001-0000-4000-8000-000000000002"),
            Section = ConstructionSectionTypeModel.Frame,
            WidthMm = 60,
            HeightMm = 76,
            WallThicknessMm = 2.8,
            Article = CatalogMaterialArticles.ProfileRehau70,
            WeightKg = 1.12
        },
        new()
        {
            Id = Guid.Parse("e1000002-0000-4000-8000-000000000002"),
            MaterialId = CatalogMaterialIds.ProfileRehau70,
            Name = "Створка Galwin 60",
            ManufacturerId = Guid.Parse("A1000001-0000-4000-8000-000000000002"),
            Section = ConstructionSectionTypeModel.Sash,
            WidthMm = 60,
            HeightMm = 68,
            WallThicknessMm = 2.5,
            Article = CatalogMaterialArticles.ProfileRehau70,
            WeightKg = 0.98
        },
        new()
        {
            Id = Guid.Parse("e1000003-0000-4000-8000-000000000003"),
            MaterialId = CatalogMaterialIds.ProfileRehau70,
            Name = "Импост Galwin 60",
            ManufacturerId = Guid.Parse("A1000001-0000-4000-8000-000000000002"),
            Section = ConstructionSectionTypeModel.Impost,
            WidthMm = 60,
            HeightMm = 60,
            WallThicknessMm = 2.5,
            Article = CatalogMaterialArticles.ProfileRehau70,
            WeightKg = 0.85
        },
        new()
        {
            Id = Guid.Parse("e1000004-0000-4000-8000-000000000004"),
            MaterialId = CatalogMaterialIds.Glazing4_16_4,
            Name = "4-16-4",
            ManufacturerId = Guid.Parse("A1000001-0000-4000-8000-000000000002"),
            Section = ConstructionSectionTypeModel.Glazing,
            Article = CatalogMaterialArticles.Glazing4_16_4,
            GlassFormula = "4-16-4",
            GlassThicknessMm = 24,
            GlassStructure = "Стекло-аргон-стекло"
        },
        new()
        {
            Id = Guid.Parse("e1000005-0000-4000-8000-000000000005"),
            MaterialId = CatalogMaterialIds.HardwareMacoEgress,
            Name = "Комплект Maco",
            ManufacturerId = Guid.Parse("A1000001-0000-4000-8000-000000000001"),
            Section = ConstructionSectionTypeModel.Hardware,
            Article = CatalogMaterialArticles.HardwareMacoEgress,
            HardwareRules =
            [
                new HardwareAutoPickRuleModel
                {
                    Id = Guid.NewGuid(),
                    MinWidthFalzMm = 400,
                    MaxWidthFalzMm = 800,
                    MinHeightFalzMm = 600,
                    MaxHeightFalzMm = 1400,
                    ScissorsCount = 1,
                    MiddleLockCount = 1
                },
                new HardwareAutoPickRuleModel
                {
                    Id = Guid.NewGuid(),
                    MinWidthFalzMm = 801,
                    MaxWidthFalzMm = 1400,
                    MinHeightFalzMm = 600,
                    MaxHeightFalzMm = 2200,
                    ScissorsCount = 2,
                    MiddleLockCount = 2
                }
            ]
        }
    ];

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
