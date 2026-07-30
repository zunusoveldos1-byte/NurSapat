using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Services.Documents;
using NurSapat.App.Services.Users;
using NurSapat.App.ViewModels;
using NurSapat.App.Views;
using NurSapat.Infrastructure.Sync.Abstractions;
using NurSapat.Infrastructure.Sync.Diagnostics;

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

        try
        {
            StartupTrace.Info("[LOG] DI: создание MainWorkspaceWindow...");
            var workspaceWindow = _serviceProvider.GetRequiredService<MainWorkspaceWindow>();
            StartupTrace.Info("[LOG] DI: создание MainWorkspaceViewModel...");
            var workspaceVm = _serviceProvider.GetRequiredService<MainWorkspaceViewModel>();
            StartupTrace.Info("[LOG] DI: MainWorkspaceViewModel создан успешно.");
            var documentManager = _serviceProvider.GetRequiredService<IDocumentManager>();

            workspaceWindow.DataContext = workspaceVm;
            StartupTrace.Info("[LOG] MainWorkspaceViewModel.InitializeDocumentLifecycle...");
            workspaceVm.InitializeDocumentLifecycle();

            var oldMainWindow = desktop.MainWindow;
            desktop.MainWindow = workspaceWindow;
            workspaceWindow.Show();
            oldMainWindow?.Close();

            _ = documentManager.TryRecoverAutoSaveAsync();
            documentManager.StartAutoSave();
        }
        catch (Exception ex)
        {
            StartupTrace.Error(
                "[LOG] Ошибка создания/инициализации MainWorkspaceViewModel или MainWorkspaceWindow",
                ex);
            throw;
        }
    }

    public async Task ReturnToLoginAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return;
        }

        var documentManager = _serviceProvider.GetRequiredService<IDocumentManager>();
        documentManager.StopAutoSave();

        _serviceProvider.GetRequiredService<IUserSessionService>().Clear();

        // AuthService clears DPAPI tokens and the SQLCipher key before awaiting
        // the best-effort remote Supabase sign-out.
        await SignOutSupabaseSafeAsync();

        var loginWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var shell = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        shell.NavigateToLogin();
        if (shell.CurrentView is LoginViewModel loginVm)
        {
            loginVm.LoadSavedCredentials();
        }
        loginWindow.DataContext = shell;

        var oldMainWindow = desktop.MainWindow;
        desktop.MainWindow = loginWindow;
        loginWindow.Show();
        oldMainWindow?.Close();
    }

    private async Task SignOutSupabaseSafeAsync()
    {
        try
        {
            var auth = _serviceProvider.GetService<ISupabaseAuthService>();
            if (auth is not null)
            {
                await auth.SignOutAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            StartupTrace.Error(
                "[LOG] Logout: SignOutSupabaseSafeAsync завершился ошибкой",
                ex);
        }
    }
}
