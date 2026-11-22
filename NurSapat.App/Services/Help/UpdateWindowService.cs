using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.ViewModels.Help;
using NurSapat.App.Views.Help;

namespace NurSapat.App.Services.Help;

public interface IUpdateWindowService
{
    bool IsUpdateWindowOpen { get; }

    Task ShowAvailableUpdateAsync(Window? owner, UpdateCheckResult result);

    Task<bool> ShowSuccessPromptAsync(Window? owner, string version);
}

public sealed class UpdateWindowService(
    IServiceProvider serviceProvider,
    IAppUpdateService appUpdateService) : IUpdateWindowService
{
    private readonly object _sync = new();
    private Window? _updateWindow;

    public bool IsUpdateWindowOpen
    {
        get
        {
            lock (_sync)
            {
                return _updateWindow is { IsVisible: true };
            }
        }
    }

    public async Task ShowAvailableUpdateAsync(Window? owner, UpdateCheckResult result) =>
        await Dispatcher.UIThread.InvokeAsync(() => ShowAvailableUpdateCoreAsync(owner, result));

    public async Task<bool> ShowSuccessPromptAsync(Window? owner, string version) =>
        await Dispatcher.UIThread.InvokeAsync(() => ShowSuccessPromptCoreAsync(owner, version));

    private async Task ShowAvailableUpdateCoreAsync(Window? owner, UpdateCheckResult result)
    {
        Window window;
        UpdateViewModel viewModel;

        lock (_sync)
        {
            if (_updateWindow is { IsVisible: true } existingWindow)
            {
                if (existingWindow.DataContext is UpdateViewModel existingVm)
                {
                    existingVm.ApplyCheckResult(result);
                }

                existingWindow.Activate();
                return;
            }

            appUpdateService.StopPeriodicChecks();

            viewModel = serviceProvider.GetRequiredService<UpdateViewModel>();
            viewModel.SuppressAutoCheckOnLoad = true;
            viewModel.ApplyCheckResult(result);

            window = new UpdateWindow(viewModel);
            viewModel.OwnerWindow = window;
            _updateWindow = window;

            window.Closed += OnUpdateWindowClosed;
        }

        var resolvedOwner = GetVisibleOwner(owner) ?? await WaitForMainWindowAsync(TimeSpan.FromSeconds(8));
        resolvedOwner = GetVisibleOwner(resolvedOwner);
        if (resolvedOwner is not null)
        {
            await window.ShowDialog(resolvedOwner);
            return;
        }

        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnClosed(object? _, EventArgs __) => closed.TrySetResult();
        window.Closed += OnClosed;
        window.Show();
        await closed.Task;
    }

    private void OnUpdateWindowClosed(object? sender, EventArgs e)
    {
        if (sender is Window window)
        {
            window.Closed -= OnUpdateWindowClosed;
        }

        lock (_sync)
        {
            if (ReferenceEquals(_updateWindow, sender))
            {
                _updateWindow = null;
            }
        }

        appUpdateService.StartPeriodicChecks();
    }

    private async Task<bool> ShowSuccessPromptCoreAsync(Window? owner, string version)
    {
        var viewModel = new UpdateSuccessViewModel(version);
        var window = new UpdateSuccessWindow(viewModel);
        viewModel.OwnerWindow = window;
        viewModel.RequestClose += () =>
        {
            if (window.IsVisible)
            {
                window.Close();
            }
        };

        var resolvedOwner = GetVisibleOwner(owner) ?? await WaitForMainWindowAsync(TimeSpan.FromSeconds(5));
        resolvedOwner = GetVisibleOwner(resolvedOwner);
        if (resolvedOwner is not null)
        {
            await window.ShowDialog(resolvedOwner);
            return viewModel.RestartRequested;
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        window.Closed += (_, _) => tcs.TrySetResult(viewModel.RestartRequested);
        window.Show();
        return await tcs.Task;
    }

    private static async Task<Window?> WaitForMainWindowAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var main = GetVisibleOwner(null);
            if (main is not null)
            {
                return main;
            }

            await Task.Delay(100);
        }

        return GetVisibleOwner(null);
    }

    private static Window? GetVisibleOwner(Window? preferred)
    {
        if (preferred is { IsVisible: true })
        {
            return preferred;
        }

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow is { IsVisible: true } main ? main : null;
        }

        return null;
    }
}
