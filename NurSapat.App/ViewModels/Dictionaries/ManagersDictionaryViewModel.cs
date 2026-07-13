using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class ManagersDictionaryViewModel : BaseDictionaryViewModel<ManagerDictionaryItemViewModel>
{
    private readonly IManagersStore _store;
    private readonly IManagerEditorService _editorService;
    private string _activeFilter = "Все";

    public ManagersDictionaryViewModel(IManagersStore store, IManagerEditorService editorService)
    {
        _store = store;
        _editorService = editorService;
        _store.Changed += (_, _) => _ = RefreshFromStoreAsync();
    }

    public override string Title => "Справочник менеджеров";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public IReadOnlyList<string> ActiveFilters { get; } = ["Все", "Активные", "Неактивные"];

    public string ActiveFilter
    {
        get => _activeFilter;
        set
        {
            if (_activeFilter == value)
            {
                return;
            }

            _activeFilter = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    protected override bool MatchesSearch(ManagerDictionaryItemViewModel item, string search) =>
        Contains(item.FullName, search)
        || Contains(item.Phone, search)
        || Contains(item.Email, search)
        || Contains(item.Position, search)
        || Contains(item.Department, search)
        || Contains(item.Login, search)
        || Contains(item.Note, search);

    protected override void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<ManagerDictionaryItemViewModel> query = AllItems;

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item => MatchesSearch(item, search));
        }

        query = ActiveFilter switch
        {
            "Активные" => query.Where(x => x.IsActive),
            "Неактивные" => query.Where(x => !x.IsActive),
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
            AllItems.Add(ManagerDictionaryItemViewModel.FromModel(item, row++));
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
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, ManagerItem.CreateDefault(), isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        var item = ManagerDictionaryItemViewModel.FromModel(saved, AllItems.Count + 1);
        AllItems.Add(item);
        SelectedItem = item;
    }

    protected override async Task EditItemAsync(ManagerDictionaryItemViewModel item)
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

    protected override Task DeleteItemAsync(ManagerDictionaryItemViewModel item)
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

    public async Task OpenEditorForItemAsync(ManagerDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }
}
