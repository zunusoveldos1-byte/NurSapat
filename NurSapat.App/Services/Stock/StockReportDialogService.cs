using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.ViewModels.Stock;
using NurSapat.App.Views.Stock;

namespace NurSapat.App.Services.Stock;

public enum StockDateDialogMode
{
    SinglePeriod,
    DualPeriod
}

public interface IStockReportDialogService
{
    Task<StockDateRangeResult?> ShowDateRangeAsync(
        Window? owner,
        string title,
        StockDateDialogMode mode);
}

public sealed record StockDateRangeResult(
    DateTime Period1Start,
    DateTime Period1End,
    DateTime? Period2Start,
    DateTime? Period2End);

public sealed class StockReportDialogService(IServiceProvider serviceProvider) : IStockReportDialogService
{
    public async Task<StockDateRangeResult?> ShowDateRangeAsync(
        Window? owner,
        string title,
        StockDateDialogMode mode)
    {
        var viewModel = serviceProvider.GetRequiredService<StockDateRangeDialogViewModel>();
        viewModel.Configure(title, mode);

        var window = new StockDateRangeDialogWindow(viewModel);
        var resolvedOwner = owner ?? ResolveMainWindow();
        if (resolvedOwner is null)
        {
            return null;
        }

        var result = await window.ShowDialog<StockDateRangeResult?>(resolvedOwner);
        return result;
    }

    private static Window? ResolveMainWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }
}
