using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class StructureNamesDictionaryViewModel : BaseDictionaryViewModel<StructureNameDictionaryItemViewModel>
{
    private readonly IStructureNamesStore _store;
    private readonly IStructureNameEditorService _editorService;
    private readonly IAppMessageService _messageService;

    public StructureNamesDictionaryViewModel(
        IStructureNamesStore store,
        IStructureNameEditorService editorService,
        IAppMessageService messageService)
    {
        _store = store;
        _editorService = editorService;
        _messageService = messageService;
    }

    public override string Title => "Наименования конструкций";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    protected override bool MatchesSearch(StructureNameDictionaryItemViewModel item, string search) =>
        Contains(item.Code, search)
        || Contains(item.Name, search);

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var item in _store.GetAll())
        {
            AllItems.Add(StructureNameDictionaryItemViewModel.FromModel(item));
        }

        return Task.CompletedTask;
    }

    protected override async Task AddItemAsync()
    {
        var draft = new StructureNameItem
        {
            Code = "NEW-00",
            Name = "Новое наименование"
        };

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, draft, isNew: true);
        if (saved is null)
        {
            return;
        }

        if (HasDuplicateCode(saved))
        {
            _messageService.LogError("Наименование с таким кодом уже существует.");
            return;
        }

        _store.Add(saved);
        var item = StructureNameDictionaryItemViewModel.FromModel(saved);
        AllItems.Add(item);
        SelectedItem = item;
        _messageService.LogInfo($"Наименование «{saved.Code}» добавлено.");
    }

    protected override async Task EditItemAsync(StructureNameDictionaryItemViewModel item)
    {
        var source = _store.GetById(item.Id)?.Clone() ?? item.ToModel();
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        if (HasDuplicateCode(saved, saved.Id))
        {
            _messageService.LogError("Наименование с таким кодом уже существует.");
            return;
        }

        _store.Update(saved);
        item.RefreshFrom(saved);
        _messageService.LogInfo($"Наименование «{saved.Code}» сохранено.");
    }

    protected override Task DeleteItemAsync(StructureNameDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        _messageService.LogInfo($"Наименование «{item.Code}» удалено.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    public async Task OpenEditorForItemAsync(StructureNameDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }

    private bool HasDuplicateCode(StructureNameItem item, Guid? excludeId = null)
    {
        var duplicate = _store.GetByCode(item.Code);
        return duplicate is not null && duplicate.Id != (excludeId ?? item.Id);
    }
}
