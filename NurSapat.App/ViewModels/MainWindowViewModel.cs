using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace NurSapat.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private object? _currentView;

    public MainWindowViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        NavigateToLogin();
    }

    public void NavigateToLogin()
        => CurrentView = _serviceProvider.GetRequiredService<LoginViewModel>();

    public void NavigateToRegister()
        => CurrentView = _serviceProvider.GetRequiredService<RegistrationViewModel>();

    public void NavigateToMain()
    {
        try
        {
            CurrentView = _serviceProvider.GetRequiredService<DashboardViewModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка навигации: {ex}");
        }
    }
}