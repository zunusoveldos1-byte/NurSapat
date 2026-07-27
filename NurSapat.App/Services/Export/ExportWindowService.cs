using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.ViewModels.Export;
using NurSapat.App.Views.Export;

namespace NurSapat.App.Services.Export;

public interface IExportWindowService
{
    Task ShowDbfJsonExportAsync(Window? owner);

    Task ShowXlsExportAsync(Window? owner);
}

public sealed class ExportWindowService(IServiceProvider serviceProvider) : IExportWindowService
{
    public async Task ShowDbfJsonExportAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<ExportDbfJsonViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new ExportDbfJsonWindow(viewModel), owner);
    }

    public async Task ShowXlsExportAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<ExportXlsViewModel>();
        await viewModel.InitializeAsync();
        await ShowDialogSafeAsync(new ExportXlsWindow(viewModel), owner);
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
