using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Orders;
using NurSapat.App.Services;
using NurSapat.App.Services.Documents;
using NurSapat.App.Services.Orders;
using NurSapat.Core.Models.Orders;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed class OrderStatusFilterOption
{
    public string Display { get; init; } = string.Empty;

    public OrderStatus? Status { get; init; }

    public override string ToString() => Display;
}

public sealed partial class OrdersDictionaryViewModel : BaseDictionaryViewModel<OrderDictionaryItemViewModel>
{
    private readonly IOrdersStore _store;
    private readonly IOrderHeaderEditorService _editorService;
    private readonly IOrderBatchOperationsService _batchOperationsService;
    private readonly IDocumentManager _documentManager;
    private readonly IWorkspaceNavigationService _workspaceNavigation;
    private readonly IDialogService _dialogService;
    private readonly IAppMessageService _messageService;
    private readonly OrderWorkspaceViewModel _orderWorkspace;

    public OrdersDictionaryViewModel(
        IOrdersStore store,
        IOrderHeaderEditorService editorService,
        IOrderBatchOperationsService batchOperationsService,
        IDocumentManager documentManager,
        IWorkspaceNavigationService workspaceNavigation,
        IDialogService dialogService,
        IAppMessageService messageService,
        OrderWorkspaceViewModel orderWorkspace)
    {
        _store = store;
        _editorService = editorService;
        _batchOperationsService = batchOperationsService;
        _documentManager = documentManager;
        _workspaceNavigation = workspaceNavigation;
        _dialogService = dialogService;
        _messageService = messageService;
        _orderWorkspace = orderWorkspace;
        StatusFilterOptions =
        [
            new OrderStatusFilterOption { Display = "Все", Status = null },
            new OrderStatusFilterOption { Display = "Черновик", Status = OrderStatus.Draft },
            new OrderStatusFilterOption { Display = "Рассчитан", Status = OrderStatus.Calculated },
            new OrderStatusFilterOption { Display = "Утверждён", Status = OrderStatus.Approved },
            new OrderStatusFilterOption { Display = "В производстве", Status = OrderStatus.InProduction },
            new OrderStatusFilterOption { Display = "Раскрой завершён", Status = OrderStatus.CuttingCompleted },
            new OrderStatusFilterOption { Display = "Собран", Status = OrderStatus.Assembled },
            new OrderStatusFilterOption { Display = "Готов к отгрузке", Status = OrderStatus.ReadyForShipment },
            new OrderStatusFilterOption { Display = "Отгружен", Status = OrderStatus.Shipped }
        ];
        SelectedStatusFilter = StatusFilterOptions[0];
    }

    public override string Title => "Справочник заказов";

    public IReadOnlyList<OrderStatusFilterOption> StatusFilterOptions { get; }

    [ObservableProperty]
    private OrderStatusFilterOption? _selectedStatusFilter;

    public ObservableCollection<OrderDictionaryItemViewModel> SelectedOrders { get; } = [];

    public Window? OwnerWindow { get; set; }

    public event Action? RequestClose;

    partial void OnSelectedStatusFilterChanged(OrderStatusFilterOption? value) => ApplyFilter();

    protected override bool MatchesSearch(OrderDictionaryItemViewModel item, string search) =>
        Contains(item.OrderNumber, search)
        || Contains(item.CustomerName, search);

    protected override void ApplyFilter()
    {
        var search = SearchText.Trim();
        IEnumerable<OrderDictionaryItemViewModel> query = AllItems;

        if (SelectedStatusFilter?.Status is OrderStatus status)
        {
            query = query.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(item => MatchesSearch(item, search));
        }

        query = query.OrderByDescending(item => item.DateCreated);

        FilteredItems.Clear();
        foreach (var item in query)
        {
            FilteredItems.Add(item);
        }

        NotifyBatchCommandsCanExecuteChanged();
    }

    protected override Task LoadItemsAsync()
    {
        AllItems.Clear();
        foreach (var item in _store.GetAll())
        {
            AllItems.Add(OrderDictionaryItemViewModel.FromModel(item));
        }

        return Task.CompletedTask;
    }

    protected override async Task AddItemAsync()
    {
        var draft = new OrderHeaderItem
        {
            OrderNumber = $"#{DateTime.Today:yyyyMMdd}-{AllItems.Count + 1}",
            DateCreated = DateTime.Today,
            CustomerName = "Новый заказчик",
            Status = OrderStatus.Draft
        };

        var saved = await _editorService.ShowEditorAsync(OwnerWindow, draft, isNew: true);
        if (saved is null)
        {
            return;
        }

        var path = await _dialogService.ShowSaveFileDialogAsync($"{DateTime.Now:yyyyMMdd}.zp");
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await _documentManager.NewOrderAsync();
        var header = _orderWorkspace.Order.Header;
        header.OrderNumber = saved.OrderNumber;
        header.OrderDate = saved.DateCreated;
        header.CustomerName = saved.CustomerName;
        header.Status = saved.Status;
        _orderWorkspace.Sheet.CustomerName = saved.CustomerName;
        _orderWorkspace.RecalculateAll();

        if (!await _documentManager.SaveOrderToPathAsync(path))
        {
            _messageService.LogWarning("Не удалось сохранить файл нового заказа.");
            return;
        }

        saved.FilePath = path;
        saved.Id = _orderWorkspace.Order.Id;
        saved.StructuresCount = _orderWorkspace.Order.Constructions.Count;
        saved.TotalSum = _orderWorkspace.Order.Totals.GrandTotal;
        _store.Add(saved);
        var item = OrderDictionaryItemViewModel.FromModel(saved);
        AllItems.Add(item);
        SelectedItem = item;
        _messageService.LogInfo($"Заказ {saved.OrderNumber} создан.");
    }

    protected override async Task EditItemAsync(OrderDictionaryItemViewModel item)
    {
        var source = _store.GetById(item.Id)?.Clone() ?? item.ToModel();
        var saved = await _editorService.ShowEditorAsync(OwnerWindow, source, isNew: false);
        if (saved is null)
        {
            return;
        }

        _store.Update(saved);
        item.RefreshFrom(saved);
        _messageService.LogInfo($"Заказ {saved.OrderNumber} обновлён.");
    }

    protected override async Task DeleteItemAsync(OrderDictionaryItemViewModel item)
    {
        if (!string.IsNullOrWhiteSpace(item.FilePath) && File.Exists(item.FilePath))
        {
            File.Delete(item.FilePath);
        }

        _store.Delete(item.Id);
        _messageService.LogInfo($"Заказ {item.OrderNumber} удалён.");
        await Task.CompletedTask;
    }

    public void SyncSelectedOrders(IEnumerable<OrderDictionaryItemViewModel> items)
    {
        SelectedOrders.Clear();
        foreach (var item in items)
        {
            SelectedOrders.Add(item);
        }

        SelectedItem = SelectedOrders.LastOrDefault();
        NotifyBatchCommandsCanExecuteChanged();
    }

    private bool HasSelectedOrders() => SelectedOrders.Count > 0 || SelectedItem is not null;

    private OrderDictionaryItemViewModel? ResolvePrimaryOrder() =>
        SelectedOrders.FirstOrDefault() ?? SelectedItem;

    private IReadOnlyList<OrderHeaderItem> SelectedOrderModels()
    {
        IEnumerable<OrderDictionaryItemViewModel> items = SelectedOrders.Count > 0
            ? SelectedOrders
            : SelectedItem is null
                ? []
                : [SelectedItem];

        return items
            .Select(item => _store.GetById(item.Id)?.Clone() ?? item.ToModel())
            .ToList();
    }

    private string SelectedOrdersSummary() =>
        string.Join(", ", SelectedOrders.Select(order => order.OrderNumber));

    private void NotifyBatchCommandsCanExecuteChanged()
    {
        OpenOrderCommand.NotifyCanExecuteChanged();
        SendEmailCommand.NotifyCanExecuteChanged();
        ExportCutRequirementsCommand.NotifyCanExecuteChanged();
        GlazingAndNetRequestCommand.NotifyCanExecuteChanged();
        MaterialsRequestCommand.NotifyCanExecuteChanged();
        HardwareRequestCommand.NotifyCanExecuteChanged();
        EconomicReportCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void CloseWindow() => RequestClose?.Invoke();

    [RelayCommand]
    private async Task RefreshStore()
    {
        await _store.RefreshAsync();
        await LoadItemsAsync();
        ApplyFilter();
        _messageService.LogInfo("Справочник заказов обновлён.");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private async Task OpenOrder()
    {
        var order = ResolvePrimaryOrder();
        if (order is null)
        {
            _messageService.LogWarning("Выберите заказ в списке.");
            return;
        }

        var model = _store.GetById(order.Id);
        var opened = false;

        if (model?.FilePath is { Length: > 0 } existingPath && File.Exists(existingPath))
        {
            opened = await _documentManager.OpenOrderAsync(existingPath);
        }
        else
        {
            opened = await _documentManager.OpenOrderFromDatabaseAsync(order.Id);
            if (!opened)
            {
                var picked = await _dialogService.ShowOpenFileDialogAsync();
                if (string.IsNullOrWhiteSpace(picked))
                {
                    _messageService.LogWarning("Файл заказа не выбран.");
                    return;
                }

                model ??= order.ToModel();
                model.FilePath = picked;
                _store.Update(model);
                order.RefreshFrom(model);
                opened = await _documentManager.OpenOrderAsync(picked);
            }
        }

        if (!opened)
        {
            return;
        }

        _workspaceNavigation.NavigateTo("cad");
        RequestClose?.Invoke();
        _messageService.LogInfo($"Открыт заказ {order.OrderNumber}.");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private async Task SendEmail()
    {
        var result = await _batchOperationsService.SendEmailAsync(SelectedOrderModels());
        LogBatchResult(result);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private async Task ExportCutRequirements()
    {
        var result = await _batchOperationsService.ExportCutRequirementsAsync(SelectedOrderModels());
        LogBatchResult(result);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private async Task GlazingAndNetRequest()
    {
        var result = await _batchOperationsService.CreateGlazingRequestAsync(SelectedOrderModels());
        LogBatchResult(result);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private async Task MaterialsRequest()
    {
        var result = await _batchOperationsService.CreateMaterialsRequestAsync(SelectedOrderModels());
        LogBatchResult(result);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private async Task HardwareRequest()
    {
        var result = await _batchOperationsService.CreateHardwareRequestAsync(SelectedOrderModels());
        LogBatchResult(result);
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private async Task EconomicReport()
    {
        var result = await _batchOperationsService.CreateEconomicReportAsync(SelectedOrderModels());
        LogBatchResult(result);
    }

    private void LogBatchResult(OrderBatchResult result)
    {
        if (result.Success)
        {
            _messageService.LogInfo(result.Message);
        }
        else
        {
            _messageService.LogWarning(result.Message);
        }
    }

}
