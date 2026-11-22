using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.App.ViewModels;

public sealed partial class PreviewWindowViewModel : ObservableObject
{
    public PreviewWindowViewModel(OrderConstructionLookupItemViewModel construction)
    {
        Title = construction.Name;
        PreviewEditor = construction.PreviewEditor;
    }

    public string Title { get; }

    public CadEditorViewModel PreviewEditor { get; }

    [RelayCommand]
    private void Close(Window? window) => window?.Close();
}
