using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.ViewModels.Dictionaries;
using NurSapat.App.Views.Dictionaries;

namespace NurSapat.App.Services.Dictionaries;

public interface IExchangeRateEditorService
{
    Task<ExchangeRatePackage?> ShowEditorAsync(Window? owner, ExchangeRatePackage source, bool isNew);
}

public sealed class ExchangeRateEditorService(IServiceProvider serviceProvider) : IExchangeRateEditorService
{
    public async Task<ExchangeRatePackage?> ShowEditorAsync(Window? owner, ExchangeRatePackage source, bool isNew)
    {
        var viewModel = serviceProvider.GetRequiredService<ExchangeRateEditViewModel>();
        viewModel.Load(source, isNew);

        var window = new ExchangeRateEditWindow(viewModel);
        var resolvedOwner = owner ?? ResolveMainWindow();

        bool? result = resolvedOwner is not null
            ? await window.ShowDialog<bool?>(resolvedOwner)
            : null;

        return result == true ? viewModel.BuildResult() : null;
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
