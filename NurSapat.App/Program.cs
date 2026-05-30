using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;
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
    public static void Main(string[] args)
    {
        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            var provider = services.BuildServiceProvider();
            BuildAvaloniaApp(provider).StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"!!! КРИТИЧЕСКАЯ ОШИБКА: {ex}");
            Console.WriteLine($"!!! КРИТИЧЕСКАЯ ОШИБКА: {ex}");
            throw;
        }
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider provider)
        => AppBuilder.Configure(() => new App(provider))
            .UsePlatformDetect()
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
        services.AddSingleton(new DatabaseService(ReadConnectionString()));
        services.AddSingleton<DataMigrationService>();
        services.AddSingleton<ISystemLogRepository, SystemLogRepository>();

        // Временная заглушка БД
        services.AddSingleton<IUserRepository, UserRepository>();

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
        services.AddSingleton<OrderCalculationState>();
        services.AddSingleton<CadEditorViewModel>();
        services.AddSingleton<OrderEditViewModel>();

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