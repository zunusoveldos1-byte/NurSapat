using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services.Documents;
using NurSapat.App.Services.Users;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;

namespace NurSapat.App.Services;

public sealed class WorkspaceWindowService(IServiceProvider serviceProvider) : IWorkspaceWindowService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public void OpenWorkspace()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var workspaceWindow = _serviceProvider.GetRequiredService<MainWorkspaceWindow>();
        var workspaceVm = _serviceProvider.GetRequiredService<MainWorkspaceViewModel>();
        var documentManager = _serviceProvider.GetRequiredService<IDocumentManager>();

        workspaceWindow.DataContext = workspaceVm;
        workspaceVm.InitializeDocumentLifecycle();

        var oldMainWindow = desktop.MainWindow;
        desktop.MainWindow = workspaceWindow;
        workspaceWindow.Show();
        oldMainWindow?.Close();

        _ = documentManager.TryRecoverAutoSaveAsync();
        documentManager.StartAutoSave();
    }

    public void ReturnToLogin()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var documentManager = _serviceProvider.GetRequiredService<IDocumentManager>();
        documentManager.StopAutoSave();

        _serviceProvider.GetRequiredService<IUserSessionService>().Clear();

        var loginWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var loginVm = _serviceProvider.GetRequiredService<LoginViewModel>();
        loginVm.LoadSavedCredentials();
        loginWindow.DataContext = loginVm;

        var oldMainWindow = desktop.MainWindow;
        desktop.MainWindow = loginWindow;
        loginWindow.Show();
        oldMainWindow?.Close();
    }
}
