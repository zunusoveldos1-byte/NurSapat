using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Stock;
using NurSapat.App.Services;
using NurSapat.App.Services.Stock;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Stock;

public sealed partial class StockFinishedGoodsViewModel : ObservableObject
{
    private readonly IStockFinishedGoodsStore _store;
    private readonly IFinishedGoodEditorService _editorService;
    private readonly IAppMessageService _messageService;

    public StockFinishedGoodsViewModel(
        IStockFinishedGoodsStore store,
        IFinishedGoodEditorService editorService,
        IAppMessageService messageService)
    {
        _store = store;
        _editorService = editorService;
        _messageService = messageService;
    }

    public string Title => "Склад: готовая продукция";

    public ObservableCollection<FinishedGoodDictionaryItemViewModel> FilteredItems { get; } = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private FinishedGoodDictionaryItemViewModel? _selectedItem;

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public async Task InitializeAsync()
    {
        await LoadItemsAsync();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedItemChanged(FinishedGoodDictionaryItemViewModel? value)
    {
        EditRecordCommand.NotifyCanExecuteChanged();
        DeleteRecordCommand.NotifyCanExecuteChanged();
        ShipCommand.NotifyCanExecuteChanged();
    }

    private void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<FinishedGoodItem> query = _store.GetAll();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item =>
                item.OrderNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.ProductCode.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.CellAddress.Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.State.ToDisplayName().Contains(search, StringComparison.OrdinalIgnoreCase)
                || item.UserName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        FilteredItems.Clear();
        foreach (var item in query)
        {
            FilteredItems.Add(FinishedGoodDictionaryItemViewModel.FromModel(item));
        }

        EditRecordCommand.NotifyCanExecuteChanged();
        DeleteRecordCommand.NotifyCanExecuteChanged();
        ShipCommand.NotifyCanExecuteChanged();
    }

    private Task LoadItemsAsync() => Task.CompletedTask;

    private bool HasSelection() => SelectedItem is not null;

    [RelayCommand]
    private async Task ReceiveOnStockAsync()
    {
        var draft = new FinishedGoodItem
        {
            State = FinishedGoodState.OnWarehouse,
            DateIncoming = DateTime.Today
        };

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, draft, isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        ApplyFilter();
        SelectedItem = FilteredItems.FirstOrDefault(item => item.Id == saved.Id);
        _messageService.LogInfo($"Продукция {saved.ProductCode} принята на склад.");
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task EditRecordAsync()
    {
        if (SelectedItem is null)
        {
            return;
        }

        var source = _store.GetById(SelectedItem.Id)?.Clone() ?? SelectedItem.ToModel();
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        _store.Update(saved);
        SelectedItem.RefreshFrom(saved);
        ApplyFilter();
        _messageService.LogInfo($"Запись {saved.ProductCode} обновлена.");
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task DeleteRecordAsync()
    {
        if (SelectedItem is null)
        {
            return Task.CompletedTask;
        }

        var productCode = SelectedItem.ProductCode;
        _store.Delete(SelectedItem.Id);
        SelectedItem = null;
        ApplyFilter();
        _messageService.LogInfo($"Запись {productCode} удалена.");
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private Task ShipAsync()
    {
        if (SelectedItem is null)
        {
            return Task.CompletedTask;
        }

        var source = _store.GetById(SelectedItem.Id);
        if (source is null)
        {
            return Task.CompletedTask;
        }

        var updated = source.Clone();
        updated.State = FinishedGoodState.Shipped;
        _store.Update(updated);
        SelectedItem.RefreshFrom(updated);
        ApplyFilter();
        _messageService.LogInfo($"Продукция {updated.ProductCode} отгружена.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    public async Task OpenEditorForItemAsync(FinishedGoodDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditRecordCommand.ExecuteAsync(null);
    }
}
