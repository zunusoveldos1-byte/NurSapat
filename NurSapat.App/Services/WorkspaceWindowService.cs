using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;
using System;

namespace NurSapat.App.Services;

public sealed class WorkspaceWindowService(IServiceProvider serviceProvider) : IWorkspaceWindowService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public void OpenWorkspace()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 1. Создаем рабочее окно и ViewModel
            var workspaceWindow = _serviceProvider.GetRequiredService<MainWorkspaceWindow>();
            var workspaceVm = _serviceProvider.GetRequiredService<MainWorkspaceViewModel>();

            // 2. Привязываем DataContext
            workspaceWindow.DataContext = workspaceVm;

            // 3. Сохраняем ссылку на старое окно (логин)
            var oldMainWindow = desktop.MainWindow;

            // 4. Устанавливаем новое главное окно и показываем его
            desktop.MainWindow = workspaceWindow;
            workspaceWindow.Show();

            // 5. Закрываем окно логина
            oldMainWindow?.Close();
        }
    }
}