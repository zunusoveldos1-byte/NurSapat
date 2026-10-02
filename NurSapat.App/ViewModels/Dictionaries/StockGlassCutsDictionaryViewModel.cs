using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Stock;
using NurSapat.App.Services.Stock;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class StockGlassCutsDictionaryViewModel : BaseDictionaryViewModel<StockGlassCutDictionaryItemViewModel>
{
    private readonly IStockGlassCutsStore _store;
    private readonly IGlassCutEditorService _editorService;

    public StockGlassCutsDictionaryViewModel(
        IStockGlassCutsStore store,
        IGlassCutEditorService editorService)
    {
        _store = store;
        _editorService = editorService;
    }

    public override string Title => "Склад: обрезки стекла";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public IAsyncRelayCommand AddCutCommand => AddCommand;

    public IAsyncRelayCommand EditCutCommand => EditCommand;

    public IAsyncRelayCommand DeleteCutCommand => DeleteCommand;

    protected override bool MatchesSearch(StockGlassCutDictionaryItemViewModel item, string search) =>
        Contains(item.Article, search)
        || Contains(item.Name, search)
        || Contains(item.Color, search);

    protected override void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<StockGlassCutDictionaryItemViewModel> query = AllItems;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item => MatchesSearch(item, search));
        }

        query = query.OrderBy(item => item.Name).ThenBy(item => item.WidthMm);

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
            AllItems.Add(StockGlassCutDictionaryItemViewModel.FromModel(item));
        }

        return Task.CompletedTask;
    }

    protected override async Task AddItemAsync()
    {
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, GlassCutItem.CreateDefault(), isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        AllItems.Add(StockGlassCutDictionaryItemViewModel.FromModel(saved));
        SelectedItem = AllItems.LastOrDefault(item => item.Id == saved.Id);
    }

    protected override async Task EditItemAsync(StockGlassCutDictionaryItemViewModel item)
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

    protected override Task DeleteItemAsync(StockGlassCutDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    public async Task OpenEditorForItemAsync(StockGlassCutDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }
}
