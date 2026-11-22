using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class OrderTemplatesDictionaryViewModel : BaseDictionaryViewModel<OrderTemplateDictionaryItemViewModel>
{
    private readonly IOrderTemplatesStore _store;
    private readonly IManufacturersStore _manufacturersStore;
    private readonly IOrderTemplateEditorService _editorService;
    private readonly IAppMessageService _messageService;

    public OrderTemplatesDictionaryViewModel(
        IOrderTemplatesStore store,
        IManufacturersStore manufacturersStore,
        IOrderTemplateEditorService editorService,
        IAppMessageService messageService)
    {
        _store = store;
        _manufacturersStore = manufacturersStore;
        _editorService = editorService;
        _messageService = messageService;
    }

    public override string Title => "Шаблоны заказов";

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    public IAsyncRelayCommand AddTemplateCommand => AddCommand;

    public IAsyncRelayCommand EditTemplateCommand => EditCommand;

    public IAsyncRelayCommand DeleteTemplateCommand => DeleteCommand;

    protected override bool MatchesSearch(OrderTemplateDictionaryItemViewModel item, string search) =>
        Contains(item.Name, search)
        || Contains(item.ManufacturerDisplay, search)
        || Contains(item.CurrencyCode, search)
        || item.InstallationPriceDisplay.Contains(search, StringComparison.OrdinalIgnoreCase)
        || item.ProfitMarginDisplay.Contains(search, StringComparison.OrdinalIgnoreCase);

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var template in _store.GetAll())
        {
            AllItems.Add(CreateItemViewModel(template));
        }

        return Task.CompletedTask;
    }

    protected override async Task AddItemAsync()
    {
        var draft = new OrderTemplateItem
        {
            Name = "Новый шаблон",
            CurrencyCode = "KGS",
            ProfitMargin = 20m,
            WindPressure = 400,
            InstallationPrice = 2500m
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
        _messageService.LogInfo($"Шаблон «{saved.Name}» добавлен.");
    }

    protected override async Task EditItemAsync(OrderTemplateDictionaryItemViewModel item)
    {
        var source = _store.GetById(item.Id)?.Clone() ?? item.ToModel();
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        _store.Update(saved);
        item.RefreshFrom(saved, ResolveManufacturerDisplay(saved.ManufacturerId));
        _messageService.LogInfo($"Шаблон «{saved.Name}» сохранён.");
    }

    protected override Task DeleteItemAsync(OrderTemplateDictionaryItemViewModel item)
    {
        _store.Delete(item.Id);
        _messageService.LogInfo($"Шаблон «{item.Name}» удалён.");
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    public async Task OpenEditorForItemAsync(OrderTemplateDictionaryItemViewModel item)
    {
        SelectedItem = item;
        await EditCommand.ExecuteAsync(null);
    }

    private OrderTemplateDictionaryItemViewModel CreateItemViewModel(OrderTemplateItem template) =>
        OrderTemplateDictionaryItemViewModel.FromModel(template, ResolveManufacturerDisplay(template.ManufacturerId));

    private string ResolveManufacturerDisplay(Guid? manufacturerId) =>
        manufacturerId is null
            ? "Все"
            : _manufacturersStore.GetById(manufacturerId.Value)?.Name ?? "—";
}
