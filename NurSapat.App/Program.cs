using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;
using NurSapat.Core.Interfaces;
using NurSapat.Data;
using NurSapat.Data.Repositories;
using System;
using System.IO;

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
        services.AddSingleton<SettingsService>();
        services.AddSingleton<TutorialService>();

        // Временная заглушка БД
        services.AddSingleton<IUserRepository, FakeUserRepository>();

        // ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegistrationViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<DashboardViewModel>();   

        // Главное окно
        services.AddTransient<MainWindow>();
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