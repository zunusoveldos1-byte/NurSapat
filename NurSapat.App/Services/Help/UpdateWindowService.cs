using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.ViewModels.Help;
using NurSapat.App.Views.Help;

namespace NurSapat.App.Services.Help;

public interface IUpdateWindowService
{
    Task ShowAvailableUpdateAsync(Window? owner, UpdateCheckResult result);

    Task<bool> ShowSuccessPromptAsync(Window? owner, string version);
}

public sealed class UpdateWindowService(IServiceProvider serviceProvider) : IUpdateWindowService
{
    public async Task ShowAvailableUpdateAsync(Window? owner, UpdateCheckResult result)
    {
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var viewModel = serviceProvider.GetRequiredService<UpdateViewModel>();
            viewModel.ApplyCheckResult(result);
            var window = new UpdateWindow(viewModel);
            await ShowDialogSafeAsync(window, owner);
        });
    }

    public async Task<bool> ShowSuccessPromptAsync(Window? owner, string version)
    {
        return await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var viewModel = new UpdateSuccessViewModel(version);
            var window = new UpdateSuccessWindow(viewModel);
            viewModel.OwnerWindow = window;
            viewModel.RequestClose += () => window.Close();

            if (owner is not null)
            {
                await window.ShowDialog(owner);
            }
            else
            {
                window.Show();
                var tcs = new TaskCompletionSource<bool>();
                window.Closed += (_, _) => tcs.TrySetResult(viewModel.RestartRequested);
                return await tcs.Task;
            }

            return viewModel.RestartRequested;
        });
    }

    private static async Task ShowDialogSafeAsync(Window window, Window? owner)
    {
        if (owner is not null)
        {
            await window.ShowDialog(owner);
            return;
        }

        window.Show();
    }
}
