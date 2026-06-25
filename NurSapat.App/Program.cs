using Avalonia;
using Avalonia.Fonts.Inter;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services;
using NurSapat.App.Services.Documents;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;
using NurSapat.Cad.UI.Services;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Settings;
using NurSapat.Core.Interfaces;
using NurSapat.Data;
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
        var environmentSettings = new EnvironmentSettings();
        environmentSettings.EnsureDirectories();

        services.AddSingleton(environmentSettings);
        services.AddSingleton(new ProductionOptions());
        services.AddSingleton<SettingsService>();
        services.AddSingleton<TutorialService>();
        services.AddSingleton<IWorkspaceWindowService, WorkspaceWindowService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IProjectSerializer, ProjectSerializer>();
        services.AddSingleton<IRecentFilesService, RecentFilesService>();
        services.AddSingleton<IAutoSaveService, AutoSaveService>();
        services.AddSingleton<IPrintSettingsService, PrintSettingsService>();
        services.AddSingleton<IPrintService, PrintService>();
        services.AddSingleton<IDocumentManager, DocumentManager>();
        services.AddSingleton(new DatabaseService(ReadConnectionString()));
        services.AddSingleton<DataMigrationService>();
        services.AddSingleton<ISystemLogRepository, SystemLogRepository>();
        services.AddSingleton<IUserRepository, UserRepository>();
        services.AddSingleton<IMaterialRepository, MaterialRepository>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegistrationViewModel>();
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<SystemLogsViewModel>();
        services.AddTransient<MainWorkspaceViewModel>();
        services.AddTransient<WorkspaceSectionViewModel>();
        services.AddTransient<ProductionOptionsViewModel>();
        services.AddTransient<EnvironmentSettingsViewModel>();
        services.AddTransient<DataTransferViewModel>();
        services.AddSingleton<AdditionalDashboardViewModel>();
        services.AddSingleton<OrderCalculationState>();
        services.AddSingleton<CadTemplateStore>();
        services.AddSingleton<ICadDialogService, CadDialogService>();
        services.AddSingleton<CadEditorViewModel>();
        services.AddSingleton<OrderWorkspaceViewModel>();
        services.AddSingleton<OrderSheetViewModel>(sp => sp.GetRequiredService<OrderWorkspaceViewModel>().Sheet);
        services.AddSingleton<OrderDrawingViewModel>(sp => sp.GetRequiredService<OrderWorkspaceViewModel>().Drawing);
        services.AddTransient<MaterialsViewModel>();

        // Главное окно
        services.AddTransient<MainWindow>();
        services.AddTransient<MainWorkspaceWindow>();
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