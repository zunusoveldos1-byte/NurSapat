using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class SeriesDictionaryViewModel : BaseDictionaryViewModel<SeriesDictionaryItemViewModel>
{
    private readonly IProfileSeriesStore _store;
    private readonly IManufacturersStore _manufacturersStore;
    private readonly ISeriesEditorService _editorService;
    private readonly IAppMessageService _messageService;

    public SeriesDictionaryViewModel(
        IProfileSeriesStore store,
        IManufacturersStore manufacturersStore,
        ISeriesEditorService editorService,
        IAppMessageService messageService)
    {
        _store = store;
        _manufacturersStore = manufacturersStore;
        _editorService = editorService;
        _messageService = messageService;
    }

    public override string Title => "Серии";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public IAsyncRelayCommand AddSeriesCommand => AddCommand;

    public IAsyncRelayCommand EditSeriesCommand => EditCommand;

    public IAsyncRelayCommand DeleteSeriesCommand => DeleteCommand;

    protected override bool MatchesSearch(SeriesDictionaryItemViewModel item, string search) =>
        Contains(item.Name, search)
        || Contains(item.ManufacturerName, search)
        || item.HardwareAxisDisplay.Contains(search, StringComparison.OrdinalIgnoreCase);

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var series in _store.GetAll())
        {
            AllItems.Add(CreateItemViewModel(series));
        }

        return Task.CompletedTask;
    }

    protected override async Task AddItemAsync()
    {
        var firstManufacturer = _manufacturersStore.GetAll().FirstOrDefault();
        var draft = new ProfileSeriesItem
        {
            Name = "Новая серия",
            ManufacturerId = firstManufacturer?.Id ?? Guid.Empty,
            HardwareAxisMm = 13.0
        };

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, draft, isNew: true);
        if (saved is null)
        {
            return;
        }

        _store.Add(saved);
        var item = CreateItemViewModel(saved);
        AllItems.Add(item);
        SelectedItem = item;
        _messageService.LogInfo($"Серия «{saved.Name}» добавлена.");
    }

    protected override async Task EditItemAsync(SeriesDictionaryItemViewModel item)
    {
        var source = _store.GetById(item.Id)?.Clone() ?? item.ToModel();
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        _store.Update(saved);
        item.RefreshFrom(saved, ResolveManufacturerName(saved.ManufacturerId));
        _messageService.LogInfo($"Серия «{saved.Name}» сохранена.");
    }

    protected override Task DeleteItemAsync(SeriesDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        _messageService.LogInfo($"Серия «{item.Name}» удалена.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    public async Task OpenEditorForItemAsync(SeriesDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }

    private SeriesDictionaryItemViewModel CreateItemViewModel(ProfileSeriesItem series) =>
        SeriesDictionaryItemViewModel.FromModel(series, ResolveManufacturerName(series.ManufacturerId));

    private string ResolveManufacturerName(Guid manufacturerId) =>
        _manufacturersStore.GetById(manufacturerId)?.Name ?? "—";
}
