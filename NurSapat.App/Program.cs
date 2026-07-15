using Avalonia;
using Avalonia.Fonts.Inter;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services;
using NurSapat.App.Services.Currency;
using NurSapat.Core.Interfaces.Currency;
using NurSapat.Core.Services.Currency;
using NurSapat.Core.Calculations;
using NurSapat.Core.Interfaces;
using NurSapat.App.Services.Documents;
using NurSapat.App.Services.Dictionaries;
using NurSapat.App.Services.EditDialogs;
using NurSapat.App.Services.Materials;
using NurSapat.App.Services.Orders;
using NurSapat.App.Services.Stock;
using NurSapat.App.Services.Structures;
using NurSapat.App.Services.Config;
using NurSapat.App.Services.Export;
using NurSapat.App.Services.Help;
using NurSapat.App.Services.Settings;
using NurSapat.App.Services.Users;
using NurSapat.App.Services.Utilities;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.ViewModels.Materials;
using NurSapat.App.ViewModels.Stock;
using NurSapat.App.ViewModels.Structures;
using NurSapat.App.ViewModels;
using NurSapat.App.ViewModels.Config;
using NurSapat.App.ViewModels.Account;
using NurSapat.App.ViewModels.Export;
using NurSapat.App.ViewModels.Help;
using NurSapat.App.ViewModels.Settings;
using NurSapat.App.ViewModels.Utilities;
using NurSapat.App.Views;
using NurSapat.Cad.UI.Services;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Settings;
using NurSapat.Core.Services.Orders;
using Microsoft.Extensions.Configuration;
using NurSapat.App.Services.Persistence;
using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Infrastructure.Persistence.DependencyInjection;
using NurSapat.Data;
using NurSapat.Data.Legacy;
using NurSapat.Data.Repositories;
using System;
using System.IO;
using System.Text.Json;

namespace NurSapat.App;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            var provider = services.BuildServiceProvider();

            // Для реального запуска передаем провайдер
            BuildAvaloniaApp(provider).StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"!!! КРИТИЧЕСКАЯ ОШИБКА: {ex}");
            Console.WriteLine($"!!! КРИТИЧЕСКАЯ ОШИБКА: {ex}");
            throw;
        }
    }

    // ЭТОТ МЕТОД ДЛЯ РЕАЛЬНОГО ЗАПУСКА ПРИЛОЖЕНИЯ
    public static AppBuilder BuildAvaloniaApp(IServiceProvider provider)
        => AppBuilder.Configure(() => new App(provider))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    // ЭТОТ МЕТОД ПРЕВЬЮСЕР ВЫЗЫВАЕТ БЕЗ ПАРАМЕТРОВ!
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure(() => new App())
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static void ConfigureServices(IServiceCollection services)
    {
        var configuration = BuildConfiguration();
        services.AddSingleton<IConfiguration>(configuration);

        var environmentSettings = new EnvironmentSettings();
        environmentSettings.EnsureDirectories();

        services.AddSingleton(environmentSettings);
        services.AddSingleton(new ProductionOptions());
        services.AddSingleton<IWorkspaceWindowService, WorkspaceWindowService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IProjectSerializer, ProjectSerializer>();
        services.AddSingleton<IRecentFilesService, RecentFilesService>();
        services.AddSingleton<IAutoSaveService, AutoSaveService>();
        services.AddSingleton<IPrintSettingsService, PrintSettingsService>();
        services.AddSingleton<IWorkspaceNavigationService, WorkspaceNavigationService>();
        services.AddSingleton<IOrderBatchOperationsService, OrderBatchOperationsService>();
        services.AddSingleton<IOrderHeaderEditorService, OrderHeaderEditorService>();
        services.AddSingleton<IGraphicSymbolEditorService, GraphicSymbolEditorService>();
        services.AddSingleton<IOrderTemplateParametersService, OrderTemplateParametersService>();
        services.AddSingleton<IPrintDocumentRenderer, PrintDocumentRenderer>();
        services.AddSingleton<IPrintService, PrintService>();
        services.AddSingleton<IDrawingExportService, DrawingExportService>();
        services.AddSingleton<IRecentColorsService, RecentColorsService>();
        services.AddSingleton<IConstructionColorService, ConstructionColorService>();
        services.AddSingleton<IOrderColorService, OrderColorService>();
        services.AddSingleton<ICadPreviewEditorFactory, CadPreviewEditorFactory>();
        services.AddSingleton<IEditDialogService, EditDialogService>();
        services.AddSingleton<IStockProfileCutsStore, StockProfileCutsStore>();
        services.AddSingleton<IProfileCutEditorService, ProfileCutEditorService>();
        services.AddSingleton<IStockGlassCutsStore, StockGlassCutsStore>();
        services.AddSingleton<IGlassCutEditorService, GlassCutEditorService>();
        services.AddSingleton<IFinishedGoodEditorService, FinishedGoodEditorService>();
        services.AddSingleton<IGraphicSymbolsStore, GraphicSymbolsStore>();
        services.AddSingleton<IStandardStructuresStore, StandardStructuresStore>();
        services.AddSingleton<IStandardStructureEditorService, StandardStructureEditorService>();
        services.AddSingleton<IUserSessionService, UserSessionService>();
        services.AddSingleton<IUserService, UserService>();
        services.AddSingleton<IUserWindowService, UserWindowService>();
        services.AddSingleton<IUserEditorService, UserEditorService>();
        services.AddSingleton<IProfileSeriesStore, ProfileSeriesStore>();
        services.AddSingleton<ISeriesEditorService, SeriesEditorService>();
        services.AddSingleton<IOrderNumberService, OrderNumberService>();
        services.AddSingleton<IDealersCatalog, DealersCatalog>();
        services.AddSingleton<IOrderErpDialogService, OrderErpDialogService>();
        services.AddSingleton<ICustomerEditorService, CustomerEditorService>();
        services.AddSingleton<IManagerEditorService, ManagerEditorService>();
        services.AddSingleton<IManagersStore, ManagersStore>();
        services.AddSingleton<IExchangeRateEditorService, ExchangeRateEditorService>();
        services.AddSingleton<ICurrencyRateImporter, CurrencyRateImporter>();
        services.AddSingleton<ICurrencySyncLogger, AppMessageCurrencySyncLogger>();
        services.AddSingleton<ICurrencyService, CurrencyService>();

        services.AddAppPersistence(configuration);

        if (!configuration.IsPostgreSqlPersistence())
        {
            services.AddSingleton<SettingsService>();
            services.AddSingleton<IEnvironmentSettingsService, EnvironmentSettingsService>();
            services.AddSingleton<IUsersStore, UsersStore>();
            services.AddSingleton<IOrdersStore, OrdersStore>();
            services.AddSingleton<ICustomersStore, CustomersStore>();
            services.AddSingleton<ISuppliersStore, SuppliersStore>();
            services.AddSingleton<IMaterialsDetailStore, MaterialsDetailStore>();
            services.AddSingleton<IStockMaterialsStore, StockMaterialsStore>();
            services.AddSingleton<IStockFinishedGoodsStore, StockFinishedGoodsStore>();
            services.AddSingleton<IOrderTemplatesStore, OrderTemplatesStore>();
            services.AddSingleton<IExchangeRatesStore, ExchangeRatesStore>();
            services.AddSingleton<IConstructionElementsStore, ConstructionElementsStore>();
        }
        else
        {
            services.AddSingleton<SettingsService>(sp =>
                new SettingsService(sp.GetRequiredService<IAppSettingRepository>()));
            services.AddSingleton<IEnvironmentSettingsService, EnvironmentSettingsService>();
        }

        services.AddSingleton<TutorialService>();

        services.AddSingleton<IMaterialCatalogResolver, MaterialCatalogResolver>();
        services.AddSingleton<IMaterialPriceCatalog, MaterialsDetailPriceCatalog>();
        services.AddSingleton<OrderCalculationService>();
        services.AddSingleton<IMaterialExportService, MaterialExportService>();
        services.AddSingleton<IMaterialEditorService, MaterialEditorService>();
        services.AddSingleton<IManufacturersStore, ManufacturersStore>();
        services.AddSingleton<IManufacturerEditorService, ManufacturerEditorService>();
        services.AddSingleton<ISupplierEditorService, SupplierEditorService>();
        services.AddSingleton<IOrderTemplateEditorService, OrderTemplateEditorService>();
        services.AddSingleton<IStructureNamesStore, StructureNamesStore>();
        services.AddSingleton<IStructureNameEditorService, StructureNameEditorService>();
        services.AddSingleton<IWindowColorsStore, WindowColorsStore>();
        services.AddSingleton<IStockDocumentEditorService, StockDocumentEditorService>();
        services.AddSingleton<IMaterialPickerService, MaterialPickerService>();
        services.AddSingleton<IStockReportDialogService, StockReportDialogService>();
        services.AddSingleton<IStockDocumentExportService, StockDocumentExportService>();
        services.AddSingleton<IDictionaryWindowService, DictionaryWindowService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IExportWindowService, ExportWindowService>();
        services.AddSingleton<IDatabaseUtilitiesService, DatabaseUtilitiesService>();
        services.AddSingleton<IDatabasePasswordService, DatabasePasswordService>();
        services.AddSingleton<IDealerBaseService, DealerBaseService>();
        services.AddSingleton<IProductionBaseService, ProductionBaseService>();
        services.AddSingleton<IUtilitiesWindowService, UtilitiesWindowService>();
        services.AddSingleton<IAppUpdateService, AppUpdateService>();
        services.AddSingleton<IUserGuideService, UserGuideService>();
        services.AddSingleton<IChangelogService, ChangelogService>();
        services.AddSingleton<IHelpWindowService, HelpWindowService>();
        services.AddSingleton<IAppMessageService, AppMessageService>();
        services.AddSingleton<IDocumentManager, DocumentManager>();
        services.AddSingleton(new DatabaseService(ReadConnectionString()));
        services.AddSingleton<DataMigrationService>();
        services.AddSingleton<ISystemLogRepository, SystemLogRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<ILegacyMaterialRepository, LegacyMaterialRepository>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegistrationViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SystemLogsViewModel>();
        services.AddTransient<MainWorkspaceViewModel>();
        services.AddTransient<WorkspaceSectionViewModel>();
        services.AddTransient<ProductionOptionsViewModel>();
        services.AddTransient<EnvironmentPathsViewModel>();
        services.AddTransient<EnvironmentSettingsViewModel>();
        services.AddTransient<ConstructionElementsViewModel>();
        services.AddTransient<ChangeConfigurationViewModel>();
        services.AddTransient<ExportDbfJsonViewModel>();
        services.AddTransient<ExportXlsViewModel>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<UtilitiesViewModel>();
        services.AddTransient<DealerBaseViewModel>();
        services.AddTransient<ProductionBaseViewModel>();
        services.AddTransient<SetDatabasePasswordViewModel>();
        services.AddTransient<UtilityProgressViewModel>();
        services.AddTransient<HelpGuideViewModel>();
        services.AddTransient<WhatsNewViewModel>();
        services.AddTransient<UpdateViewModel>();
        services.AddTransient<SupportViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<DataTransferViewModel>();
        services.AddSingleton<AdditionalDashboardViewModel>();
        services.AddSingleton<OrderCalculationState>();
        services.AddSingleton<CadTemplateStore>();
        services.AddSingleton<ICadDialogService, CadDialogService>();
        services.AddSingleton<CadEditorViewModel>(sp =>
            new CadEditorViewModel(
                sp.GetRequiredService<ICadDialogService>(),
                sp.GetRequiredService<CadTemplateStore>()));
        services.AddSingleton<OrderWorkspaceViewModel>(sp =>
            new OrderWorkspaceViewModel(
                sp.GetRequiredService<CadEditorViewModel>(),
                sp.GetRequiredService<IOrderErpDialogService>(),
                sp.GetRequiredService<OrderCalculationService>(),
                sp.GetRequiredService<ICustomersStore>(),
                sp.GetRequiredService<IManagersStore>(),
                sp.GetRequiredService<IOrderNumberService>(),
                sp.GetRequiredService<IDictionaryWindowService>()));
        services.AddSingleton<OrderSheetViewModel>(sp => sp.GetRequiredService<OrderWorkspaceViewModel>().Sheet);
        services.AddSingleton<OrderDrawingViewModel>(sp => sp.GetRequiredService<OrderWorkspaceViewModel>().Drawing);
        services.AddTransient<CostTableViewModel>();
        services.AddTransient<MaterialEditViewModel>();
        services.AddTransient<MaterialColorAddDialogViewModel>();
        services.AddTransient<MaterialsDictionaryViewModel>();
        services.AddTransient<StockProfileCutsDictionaryViewModel>();
        services.AddTransient<ProfileCutEditViewModel>();
        services.AddTransient<StockGlassCutsDictionaryViewModel>();
        services.AddTransient<GlassCutEditViewModel>();
        services.AddTransient<GraphicSymbolsDictionaryViewModel>();
        services.AddTransient<StandardStructuresDictionaryViewModel>();
        services.AddTransient<StandardStructureGroupEditViewModel>();
        services.AddTransient<StandardStructureEditViewModel>();
        services.AddTransient<UsersDictionaryViewModel>();
        services.AddTransient<UserEditViewModel>();
        services.AddTransient<SeriesDictionaryViewModel>();
        services.AddTransient<SeriesEditViewModel>();
        services.AddTransient<CustomersDictionaryViewModel>();
        services.AddTransient<ManagersDictionaryViewModel>();
        services.AddTransient<ManagerEditViewModel>();
        services.AddTransient<OrdersDictionaryViewModel>();
        services.AddTransient<OrderHeaderEditViewModel>();
        services.AddTransient<GraphicSymbolEditViewModel>();
        services.AddTransient<OrderTemplateParametersViewModel>();
        services.AddTransient<CustomerEditViewModel>();
        services.AddTransient<ExchangeRatesDictionaryViewModel>();
        services.AddTransient<ExchangeRateEditViewModel>();
        services.AddTransient<ManufacturersDictionaryViewModel>();
        services.AddTransient<ManufacturerEditViewModel>();
        services.AddTransient<SuppliersDictionaryViewModel>();
        services.AddTransient<SupplierEditViewModel>();
        services.AddTransient<StockFinishedGoodsViewModel>();
        services.AddTransient<FinishedGoodEditViewModel>();
        services.AddTransient<OrderTemplatesDictionaryViewModel>();
        services.AddTransient<OrderTemplateEditViewModel>();
        services.AddTransient<StructureNamesDictionaryViewModel>();
        services.AddTransient<StructureNameEditViewModel>();
        services.AddTransient<WindowColorsDictionaryViewModel>();
        services.AddTransient<StockMaterialsViewModel>();
        services.AddTransient<StockDocumentEditViewModel>();
        services.AddTransient<MaterialPickerViewModel>();
        services.AddTransient<StockDateRangeDialogViewModel>();
        services.AddTransient<OrderEditViewModel>();
        services.AddSingleton<MessagesViewModel>();

        // Главное окно
        services.AddTransient<MainWindow>(sp =>
        {
            var viewModel = sp.GetRequiredService<MainWindowViewModel>();
            return new MainWindow(viewModel);
        });
        services.AddTransient<MainWorkspaceWindow>();
    }

    private static IConfiguration BuildConfiguration()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "NurSapat.App", "appsettings.json");
        }

        return new ConfigurationBuilder()
            .AddJsonFile(path, optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    private static string ReadConnectionString()
    {
        const string fallback = "Host=localhost;Database=nursapat;Username=postgres;Password=7720";
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");

        if (!File.Exists(path))
        {
            path = Path.Combine(Directory.GetCurrentDirectory(), "NurSapat.App", "appsettings.json");
        }

        if (!File.Exists(path))
        {
            return fallback;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement
            .GetProperty("ConnectionStrings")
            .GetProperty("Default")
            .GetString() ?? fallback;
    }
}

public class AppSettings
{
    public ConnectionStringsSection? ConnectionStrings { get; set; }
}

public class ConnectionStringsSection
{
    public string? Default { get; set; }
}