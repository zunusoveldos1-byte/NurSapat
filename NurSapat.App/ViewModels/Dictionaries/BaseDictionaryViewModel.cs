using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Dictionaries;

public abstract partial class BaseDictionaryViewModel<TItem> : ObservableObject
    where TItem : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private TItem? _selectedItem;

    public ObservableCollection<TItem> FilteredItems { get; } = [];

    protected ObservableCollection<TItem> AllItems { get; } = [];

    public abstract string Title { get; }

    public async Task InitializeAsync()
    {
        await LoadItemsAsync();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedItemChanged(TItem? value)
    {
        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    protected virtual void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<TItem> query = AllItems;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = AllItems.Where(item => MatchesSearch(item, search));
        }

        FilteredItems.Clear();
        foreach (var item in query)
        {
            FilteredItems.Add(item);
        }

        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    protected abstract bool MatchesSearch(TItem item, string search);

    protected abstract Task LoadItemsAsync();

    protected abstract Task AddItemAsync();

    protected abstract Task EditItemAsync(TItem item);

    protected abstract Task DeleteItemAsync(TItem item);

    protected static bool Contains(string? value, string search) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Contains(search, StringComparison.OrdinalIgnoreCase);

    protected static string FormatSom(decimal value) => $"{value:N2} сом";

    private bool HasSelection() => SelectedItem is not null;

    [RelayCommand]
    private async Task Add()
    {
        await AddItemAsync();
        ApplyFilter();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task Edit()
    {
        if (SelectedItem is null)
        {
            return;
        }

        await EditItemAsync(SelectedItem);
        ApplyFilter();
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task Delete()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var item = SelectedItem;
        await DeleteItemAsync(item);
        AllItems.Remove(item);
        SelectedItem = FilteredItems.FirstOrDefault();
        ApplyFilter();
    }
}
