using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class CustomersDictionaryViewModel : BaseDictionaryViewModel<CustomerDictionaryItemViewModel>
{
    private readonly ICustomersStore _store;
    private readonly ICustomerEditorService _editorService;

    public CustomersDictionaryViewModel(ICustomersStore store, ICustomerEditorService editorService)
    {
        _store = store;
        _editorService = editorService;
    }

    public override string Title => "Справочник заказчиков";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    protected override bool MatchesSearch(CustomerDictionaryItemViewModel item, string search) =>
        Contains(item.FullName, search)
        || Contains(item.Phone, search);

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var item in _store.GetAll())
        {
            AllItems.Add(CustomerDictionaryItemViewModel.FromModel(item));
        }

        return Task.CompletedTask;
    }

    protected override async Task AddItemAsync()
    {
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, CustomerItem.CreateDefault(), isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        var item = CustomerDictionaryItemViewModel.FromModel(saved);
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

    public async Task OpenEditorForItemAsync(CustomerDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }
}
