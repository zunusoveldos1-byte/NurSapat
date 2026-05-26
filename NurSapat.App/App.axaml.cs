using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Views;
using System;

namespace NurSapat.App;

public partial class App : Application
{
    private readonly IServiceProvider _provider;

    public App(IServiceProvider provider)
    {
        _provider = provider;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        try
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = _provider.GetRequiredService<MainWindow>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FATAL: {ex}");
            throw;
        }
        base.OnFrameworkInitializationCompleted();
    }
}