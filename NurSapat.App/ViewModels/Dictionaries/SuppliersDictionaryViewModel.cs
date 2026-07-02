using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class SuppliersDictionaryViewModel : BaseDictionaryViewModel<SupplierDictionaryItemViewModel>
{
    private readonly ISuppliersStore _store;
    private readonly ISupplierEditorService _editorService;
    private readonly IAppMessageService _messageService;

    public SuppliersDictionaryViewModel(
        ISuppliersStore store,
        ISupplierEditorService editorService,
        IAppMessageService messageService)
    {
        _store = store;
        _editorService = editorService;
        _messageService = messageService;
    }

    public override string Title => "Справочник поставщиков";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public IAsyncRelayCommand AddSupplierCommand => AddCommand;

    public IAsyncRelayCommand EditSupplierCommand => EditCommand;

    public IAsyncRelayCommand DeleteSupplierCommand => DeleteCommand;

    protected override bool MatchesSearch(SupplierDictionaryItemViewModel item, string search) =>
        Contains(item.Name, search)
        || Contains(item.Phone, search)
        || Contains(item.LegalAddress, search)
        || Contains(item.ContractTerms, search)
        || Contains(item.Note, search);

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var item in _store.GetAll())
        {
            AllItems.Add(SupplierDictionaryItemViewModel.FromModel(item));
        }

        return Task.CompletedTask;
    }

    protected override async Task AddItemAsync()
    {
        var draft = new SupplierItem
        {
            Name = "Новый поставщик",
            Phone = "+996"
        };

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, draft, isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        var item = SupplierDictionaryItemViewModel.FromModel(saved);
        AllItems.Add(item);
        SelectedItem = item;
        _messageService.LogInfo($"Поставщик «{saved.Name}» добавлен.");
    }

    protected override async Task EditItemAsync(SupplierDictionaryItemViewModel item)
    {
        var source = _store.GetById(item.Id)?.Clone() ?? item.ToModel();
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        _store.Update(saved);
        item.RefreshFrom(saved);
        _messageService.LogInfo($"Поставщик «{saved.Name}» сохранён.");
    }

    protected override Task DeleteItemAsync(SupplierDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        _messageService.LogInfo($"Поставщик «{item.Name}» удалён.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    public async Task OpenEditorForItemAsync(SupplierDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }
}
