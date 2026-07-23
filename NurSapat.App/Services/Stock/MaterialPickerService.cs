using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Materials;
using NurSapat.App.ViewModels.Stock;
using NurSapat.App.Views.Stock;

namespace NurSapat.App.Services.Stock;

public interface IMaterialPickerService
{
    Task<MaterialDetail?> PickMaterialAsync(Window? owner, MaterialSection? sectionFilter = null);
}

public sealed class MaterialPickerService(IServiceProvider serviceProvider) : IMaterialPickerService
{
    public async Task<MaterialDetail?> PickMaterialAsync(Window? owner, MaterialSection? sectionFilter = null)
    {
        var viewModel = serviceProvider.GetRequiredService<MaterialPickerViewModel>();
        viewModel.Configure(sectionFilter);
        await viewModel.InitializeAsync();

        var window = new MaterialPickerWindow(viewModel);
        var resolvedOwner = owner ?? ResolveMainWindow();

        bool? result = resolvedOwner is not null
            ? await window.ShowDialog<bool?>(resolvedOwner)
            : null;

        return result == true ? viewModel.SelectedMaterial : null;
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
