using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.App.ViewModels;

public sealed partial class ErpReferenceHubViewModel : ObservableObject
{
    public ErpReferenceHubViewModel(
        string title,
        string description,
        int itemCount,
        Func<Task> openDictionaryAsync,
        Func<Task>? addItemAsync = null)
    {
        Title = title;
        Description = description;
        ItemCount = itemCount;
        _openDictionaryAsync = openDictionaryAsync;
        _addItemAsync = addItemAsync;
    }

    public string Title { get; }

    public string Description { get; }

    public int ItemCount { get; }

    public string ItemCountText => $"Записей в справочнике: {ItemCount}";

    private readonly Func<Task> _openDictionaryAsync;
    private readonly Func<Task>? _addItemAsync;

    [RelayCommand]
    private Task OpenDictionary() => _openDictionaryAsync();

    [RelayCommand]
    private Task AddItem() => _addItemAsync?.Invoke() ?? Task.CompletedTask;

    public bool CanAdd => _addItemAsync is not null;
}
