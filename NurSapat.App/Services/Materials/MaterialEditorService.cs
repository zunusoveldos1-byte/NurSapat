using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NurSapat.App.Models.Materials;
using NurSapat.App.ViewModels.Materials;
using NurSapat.App.Views.Materials;

namespace NurSapat.App.Services.Materials;

public interface IMaterialEditorService
{
    Task<MaterialDetail?> ShowEditorAsync(Window? owner, MaterialDetail source, bool isClone = false);
}

public sealed class MaterialEditorService(IServiceProvider serviceProvider) : IMaterialEditorService
{
    public async Task<MaterialDetail?> ShowEditorAsync(Window? owner, MaterialDetail source, bool isClone = false)
    {
        var viewModel = serviceProvider.GetRequiredService<MaterialEditViewModel>();
        viewModel.Load(source, isClone);

        var window = new MaterialEditWindow(viewModel);
        var resolvedOwner = owner ?? ResolveMainWindow();

        bool? result = resolvedOwner is not null
            ? await window.ShowDialog<bool?>(resolvedOwner)
            : await ShowWithoutOwnerAsync(window, viewModel);

        return result == true ? viewModel.BuildResult() : null;
    }

    private static async Task<bool?> ShowWithoutOwnerAsync(MaterialEditWindow window, MaterialEditViewModel viewModel)
    {
        var completion = new TaskCompletionSource<bool?>();

        void OnRequestClose(bool value)
        {
            viewModel.RequestClose -= OnRequestClose;
            completion.TrySetResult(value);
        }

        window.Closed += (_, _) => completion.TrySetResult(false);
        viewModel.RequestClose += OnRequestClose;
        window.Show();

        return await completion.Task;
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
