using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services;

namespace NurSapat.App.ViewModels;

public sealed partial class UnsavedChangesViewModel : ObservableObject
{
    public string Title { get; }

    public string Message { get; }

    public event EventHandler<UnsavedChangesChoice>? CloseRequested;

    public UnsavedChangesViewModel(string title, string message)
    {
        Title = title;
        Message = message;
    }

    [RelayCommand]
    private void Save() => CloseRequested?.Invoke(this, UnsavedChangesChoice.Save);

    [RelayCommand]
    private void SaveAsCopy() => CloseRequested?.Invoke(this, UnsavedChangesChoice.SaveAsCopy);

    [RelayCommand]
    private void Discard() => CloseRequested?.Invoke(this, UnsavedChangesChoice.Discard);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, UnsavedChangesChoice.Cancel);
}
