using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;
using NurSapat.Core.Interfaces.Currency;

namespace NurSapat.App;

public partial class App : Application
{
    private readonly IServiceProvider? _serviceProvider;

    public static IServiceProvider? ServiceProvider { get; private set; }

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ServiceProvider = serviceProvider;
    }

    public App()
    {
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (Avalonia.Controls.Design.IsDesignMode)
            {
                desktop.MainWindow = new MainWindow();
            }
            else
            {
                desktop.MainWindow = _serviceProvider!.GetRequiredService<MainWindow>();
                _ = SyncCurrencyRatesOnStartupAsync();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task SyncCurrencyRatesOnStartupAsync()
    {
        try
        {
            var currencyService = _serviceProvider!.GetRequiredService<ICurrencyService>();
            await currencyService.CheckAndSyncRatesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Синхронизация курсов НБКР при старте: {ex.Message}");
        }
    }
}
