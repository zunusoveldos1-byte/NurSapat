using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Stock;
using NurSapat.App.Services.Stock;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class StockProfileCutsDictionaryViewModel : BaseDictionaryViewModel<StockProfileCutDictionaryItemViewModel>
{
    private readonly IStockProfileCutsStore _store;
    private readonly IProfileCutEditorService _editorService;

    public StockProfileCutsDictionaryViewModel(
        IStockProfileCutsStore store,
        IProfileCutEditorService editorService)
    {
        _store = store;
        _editorService = editorService;
    }

    public override string Title => "Склад: обрезки профиля";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public IAsyncRelayCommand AddCutCommand => AddCommand;

    public IAsyncRelayCommand EditCutCommand => EditCommand;

    public IAsyncRelayCommand DeleteCutCommand => DeleteCommand;

    protected override bool MatchesSearch(StockProfileCutDictionaryItemViewModel item, string search) =>
        Contains(item.Article, search)
        || Contains(item.Name, search)
        || Contains(item.Color, search);

    protected override void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<StockProfileCutDictionaryItemViewModel> query = AllItems;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item => MatchesSearch(item, search));
        }

        query = query.OrderBy(item => item.Name).ThenBy(item => item.LengthMm);

        FilteredItems.Clear();
        foreach (var item in query)
        {
            FilteredItems.Add(item);
        }

        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var item in _store.GetAll())
        {
            AllItems.Add(StockProfileCutDictionaryItemViewModel.FromModel(item));
        }

        return Task.CompletedTask;
    }

    protected override async Task AddItemAsync()
    {
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, ProfileCutItem.CreateDefault(), isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        var item = StockProfileCutDictionaryItemViewModel.FromModel(saved);
        AllItems.Add(item);
        SelectedItem = item;
    }

    protected override async Task EditItemAsync(StockProfileCutDictionaryItemViewModel item)
    {
        var source = _store.GetById(item.Id)?.Clone() ?? item.ToModel();
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        _store.Update(saved);
        item.RefreshFrom(saved);
    }

    protected override Task DeleteItemAsync(StockProfileCutDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    public async Task OpenEditorForItemAsync(StockProfileCutDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }
}
