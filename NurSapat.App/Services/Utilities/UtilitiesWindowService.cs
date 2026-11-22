using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.ViewModels.Utilities;
using NurSapat.App.Views.Utilities;

namespace NurSapat.App.Services.Utilities;

public interface IUtilitiesWindowService
{
    Task ShowDealerBaseAsync(Window? owner);

    Task ShowProductionBaseImportAsync(Window? owner);

    Task ShowSetDatabasePasswordAsync(Window? owner);

    Task ShowCompressProgressAsync(Window? owner, Func<CancellationToken, Task> operation);
}

public sealed class UtilitiesWindowService(IServiceProvider serviceProvider) : IUtilitiesWindowService
{
    public async Task ShowDealerBaseAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<DealerBaseViewModel>();
        await ShowDialogSafeAsync(new DealerBaseWindow(viewModel), owner);
    }

    public async Task ShowProductionBaseImportAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<ProductionBaseViewModel>();
        await ShowDialogSafeAsync(new ProductionBaseWindow(viewModel), owner);
    }

    public async Task ShowSetDatabasePasswordAsync(Window? owner)
    {
        var viewModel = serviceProvider.GetRequiredService<SetDatabasePasswordViewModel>();
        await ShowDialogSafeAsync(new SetDatabasePasswordWindow(viewModel), owner);
    }

    public async Task ShowCompressProgressAsync(Window? owner, Func<CancellationToken, Task> operation)
    {
        var viewModel = serviceProvider.GetRequiredService<UtilityProgressViewModel>();
        viewModel.Configure(operation);
        await ShowDialogSafeAsync(new UtilityProgressWindow(viewModel), owner);
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
