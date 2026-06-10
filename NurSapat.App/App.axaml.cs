using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;
using System;

namespace NurSapat.App;

public partial class App : Application
{
    private readonly IServiceProvider? _serviceProvider;

    // ДОБАВЛЕНО: Статическое свойство для доступа к DI из MainWorkspaceViewModel
    public static IServiceProvider? ServiceProvider { get; private set; }

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        ServiceProvider = serviceProvider; // Инициализируем статическое свойство
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
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}