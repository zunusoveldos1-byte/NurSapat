using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Orders;
using NurSapat.App.Services;
using NurSapat.App.Services.Orders;
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
    private readonly IAppMessageService _messageService;

    public OrdersDictionaryViewModel(IOrdersStore store, IAppMessageService messageService)
    {
        _store = store;
        _messageService = messageService;
        StatusFilterOptions =
        [
            new OrderStatusFilterOption { Display = "Все", Status = null },
            new OrderStatusFilterOption { Display = "Расчет", Status = OrderStatus.Calculation },
            new OrderStatusFilterOption { Display = "Замер", Status = OrderStatus.Measurement },
            new OrderStatusFilterOption { Display = "В производстве", Status = OrderStatus.InProduction },
            new OrderStatusFilterOption { Display = "Готов", Status = OrderStatus.Ready },
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
        foreach (var item in _store.GetAll().OrderByDescending(x => x.DateCreated))
        {
            AllItems.Add(OrderDictionaryItemViewModel.FromModel(item));
        }

        return Task.CompletedTask;
    }

    protected override Task AddItemAsync() => Task.CompletedTask;

    protected override Task EditItemAsync(OrderDictionaryItemViewModel item) => Task.CompletedTask;

    protected override Task DeleteItemAsync(OrderDictionaryItemViewModel item) => Task.CompletedTask;

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

    private bool HasSelectedOrders() => SelectedOrders.Count > 0;

    private string SelectedOrdersSummary() =>
        string.Join(", ", SelectedOrders.Select(order => order.OrderNumber));

    private void NotifyBatchCommandsCanExecuteChanged()
    {
        OpenOrderCommand.NotifyCanExecuteChanged();
        SendEmailCommand.NotifyCanExecuteChanged();
        OptimizeCutCommand.NotifyCanExecuteChanged();
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
    private Task OpenOrder()
    {
        var order = SelectedOrders[0];
        _messageService.LogInfo($"Открытие заказа {order.OrderNumber} ({order.CustomerName}) в конструкторе.");
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private Task SendEmail()
    {
        _messageService.LogInfo($"Отправка по почте: {SelectedOrdersSummary()}.");
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private async Task OptimizeCut()
    {
        var confirmed = await ConfirmOptimizeCutAsync();
        if (!confirmed)
        {
            return;
        }

        _messageService.LogInfo(
            $"Раскрой материалов выполнен по заказам: {SelectedOrdersSummary()}. Обрезки списаны со склада.");
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private Task GlazingAndNetRequest()
    {
        _messageService.LogInfo($"Заявка на стеклопакеты и москитные сетки: {SelectedOrdersSummary()}.");
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private Task MaterialsRequest()
    {
        _messageService.LogInfo($"Заявка на материалы передана на склад: {SelectedOrdersSummary()}.");
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private Task HardwareRequest()
    {
        _messageService.LogInfo($"Заявка на фурнитуру сформирована: {SelectedOrdersSummary()}.");
        return Task.CompletedTask;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedOrders))]
    private Task EconomicReport()
    {
        var total = SelectedOrders.Sum(order => order.TotalSum);
        _messageService.LogInfo(
            $"Отчёт по заказам ({SelectedOrders.Count}): {SelectedOrdersSummary()}. Сумма: {total:N2} сом.");
        return Task.CompletedTask;
    }

    private async Task<bool> ConfirmOptimizeCutAsync()
    {
        if (OwnerWindow is null)
        {
            return false;
        }

        var dialog = new Window
        {
            Title = "Подтверждение раскроя",
            Width = 480,
            Height = 210,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        dialog.Content = BuildConfirmContent(
            $"Выполнить раскрой материалов по {SelectedOrders.Count} выбранным заказам и списать обрезки со склада?",
            confirmed => dialog.Close(confirmed));

        return await dialog.ShowDialog<bool>(OwnerWindow);
    }

    private static Control BuildConfirmContent(string message, Action<bool> close)
    {
        var panel = new StackPanel
        {
            Margin = new Avalonia.Thickness(20),
            Spacing = 16
        };

        panel.Children.Add(new TextBlock
        {
            Text = message,
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        });

        var buttons = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 8
        };

        var cancelButton = new Button
        {
            Content = "Отмена",
            MinWidth = 96,
            MinHeight = 36,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        cancelButton.Click += (_, _) => close(false);
        Grid.SetColumn(cancelButton, 1);

        var okButton = new Button
        {
            Content = "Списать обрезки",
            MinWidth = 140,
            MinHeight = 36,
            HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center
        };
        okButton.Click += (_, _) => close(true);
        Grid.SetColumn(okButton, 2);

        buttons.Children.Add(cancelButton);
        buttons.Children.Add(okButton);
        panel.Children.Add(buttons);

        return panel;
    }
}
