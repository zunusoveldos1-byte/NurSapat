using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class CustomersDictionaryViewModel : BaseDictionaryViewModel<CustomerDictionaryItemViewModel>
{
    private readonly ICustomersStore _store;
    private readonly ICustomerEditorService _editorService;
    private string _typeFilter = "Все";
    private string _blockedFilter = "Все";

    public CustomersDictionaryViewModel(ICustomersStore store, ICustomerEditorService editorService)
    {
        _store = store;
        _editorService = editorService;
        _store.Changed += (_, _) => _ = RefreshFromStoreAsync();
    }

    public override string Title => "Справочник заказчиков";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public IReadOnlyList<string> TypeFilters { get; } = ["Все", "Физ. лицо", "Юр. лицо", "Дилер"];

    public IReadOnlyList<string> BlockedFilters { get; } = ["Все", "Активные", "Заблокированные"];

    public string TypeFilter
    {
        get => _typeFilter;
        set
        {
            if (_typeFilter == value)
            {
                return;
            }

            _typeFilter = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public string BlockedFilter
    {
        get => _blockedFilter;
        set
        {
            if (_blockedFilter == value)
            {
                return;
            }

            _blockedFilter = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    protected override bool MatchesSearch(CustomerDictionaryItemViewModel item, string search) =>
        Contains(item.FullName, search)
        || Contains(item.Phone, search)
        || Contains(item.City, search)
        || Contains(item.DealerCode, search)
        || Contains(item.ActualAddress, search)
        || Contains(item.Note, search)
        || Contains(item.TypeDisplay, search);

    protected override void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<CustomerDictionaryItemViewModel> query = AllItems;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item => MatchesSearch(item, search));
        }

        query = TypeFilter switch
        {
            "Физ. лицо" => query.Where(x => x.Type == Core.Models.Orders.CustomerType.Individual),
            "Юр. лицо" => query.Where(x => x.Type == Core.Models.Orders.CustomerType.LegalEntity),
            "Дилер" => query.Where(x => x.Type == Core.Models.Orders.CustomerType.Dealer),
            _ => query
        };

        query = BlockedFilter switch
        {
            "Активные" => query.Where(x => !x.IsBlocked),
            "Заблокированные" => query.Where(x => x.IsBlocked),
            _ => query
        };

        FilteredItems.Clear();
        var index = 1;
        foreach (var item in query.OrderBy(x => x.FullName))
        {
            item.RowNumber = index++;
            FilteredItems.Add(item);
        }

        EditCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        var row = 1;
        foreach (var item in _store.GetAll())
        {
            AllItems.Add(CustomerDictionaryItemViewModel.FromModel(item, row++));
        }

        return Task.CompletedTask;
    }

    private async Task RefreshFromStoreAsync()
    {
        await LoadItemsAsync();
        ApplyFilter();
    }

    protected override async Task AddItemAsync()
    {
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, CustomerItem.CreateDefault(), isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        var item = CustomerDictionaryItemViewModel.FromModel(saved, AllItems.Count + 1);
        AllItems.Add(item);
        SelectedItem = item;
    }

    protected override async Task EditItemAsync(CustomerDictionaryItemViewModel item)
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

    protected override Task DeleteItemAsync(CustomerDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    [RelayCommand(CanExecute = nameof(CanViewSelected))]
    private async Task ViewSelected()
    {
        if (SelectedItem is null)
        {
            return;
        }

        await EditItemAsync(SelectedItem);
        ApplyFilter();
    }

    private bool CanViewSelected() => SelectedItem is not null;

    public async Task OpenEditorForItemAsync(CustomerDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }
}
