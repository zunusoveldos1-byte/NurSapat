using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Services.Orders;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Dialogs;

public sealed partial class OrderSettingsDialogViewModel : ViewModelBase
{
    private readonly Order _order;
    private readonly IDealersCatalog _dealersCatalog;
    private DateTime _originalOrderDate;
    private readonly OrderLifecycleService _lifecycleService;
    private readonly bool _hasCalculationSnapshot;

    public OrderSettingsDialogViewModel(
        Order order,
        bool isAdministrator,
        IDealersCatalog dealersCatalog,
        OrderLifecycleService lifecycleService,
        bool hasCalculationSnapshot)
    {
        _order = order;
        _dealersCatalog = dealersCatalog;
        _lifecycleService = lifecycleService;
        _hasCalculationSnapshot = hasCalculationSnapshot;
        Header = order.Header;
        IsAdministrator = isAdministrator;
        _originalOrderDate = Header.OrderDate;
        OrderNumber = Header.OrderNumber;
        Status = Header.Status;
        ProductionNote = Header.ProductionNote;
        StatusOptions = new ObservableCollection<OrderStatus>(OrderStatusExtensions.All);
    }

    public string Title => "Параметры заказа";

    public OrderHeader Header { get; }

    public bool IsAdministrator { get; }

    /// <summary>
    /// Дата редактируется администратором или в статусах «Черновик» / «Просчёт».
    /// </summary>
    public bool IsDateEditable =>
        IsAdministrator
        || Status is OrderStatus.Draft
        || Status is OrderStatus.Calculated;

    public ObservableCollection<OrderStatus> StatusOptions { get; }

    [ObservableProperty]
    private string _orderNumber = string.Empty;

    private DateTimeOffset? _selectedDateOffset;

    public DateTimeOffset? SelectedDateOffset
    {
        get => _selectedDateOffset ?? (Header.OrderDate != DateTime.MinValue
            ? new DateTimeOffset(Header.OrderDate.Date)
            : DateTimeOffset.Now);
        set
        {
            if (!value.HasValue)
            {
                return;
            }

            var date = value.Value.DateTime.Date;
            if (_selectedDateOffset == value)
            {
                return;
            }

            _selectedDateOffset = value;
            OnPropertyChanged(nameof(SelectedDateOffset));

            var result = PriceListValidationService.ValidateOrderDateChange(_originalOrderDate, date);
            PriceListNotice = result.RequiresRecalculation ? result.Message : null;
        }
    }

    [ObservableProperty]
    private OrderStatus _status;

    [ObservableProperty]
    private string _productionNote = string.Empty;

    [ObservableProperty]
    private string? _priceListNotice;

    [ObservableProperty]
    private string? _productionBlockReason;

    public event Action<bool>? CloseRequested;

    partial void OnStatusChanged(OrderStatus value)
    {
        ProductionBlockReason = null;
        OnPropertyChanged(nameof(IsDateEditable));
    }

    [RelayCommand]
    private void Confirm()
    {
        try
        {
            if (Status == Header.Status)
            {
                _lifecycleService.AssertCanEdit(_order);
            }
            else
            {
                _lifecycleService.AssertCanTransition(_order, Status, _hasCalculationSnapshot);
            }
        }
        catch (InvalidOperationException ex)
        {
            ProductionBlockReason = ex.Message;
            return;
        }

        if (string.IsNullOrWhiteSpace(OrderNumber))
        {
            OrderNumber = OrderNumberGenerator.GenerateNext();
        }

        if (Status == OrderStatus.InProduction)
        {
            var issue = OrderProductionValidator.Review(_order, OrderNumber).FirstOrDefault(item => item.BlocksProduction);
            if (issue is not null)
            {
                ProductionBlockReason = issue.Message;
                return;
            }

            var dealer = Header.DealerId is Guid dealerId
                ? _dealersCatalog.GetById(dealerId)
                : null;

            if (!OrderBusinessRules.CanSendToProduction(_order, dealer, out var blockReason))
            {
                ProductionBlockReason = blockReason;
                return;
            }
        }

        if (Header.Status < OrderStatus.InProduction)
        {
            Header.OrderNumber = OrderNumber.Trim();
            Header.OrderDate = SelectedDateOffset?.DateTime.Date ?? DateTime.Today;
            Header.ProductionNote = ProductionNote;
        }

        Header.Status = Status;
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    public void ResetDateValidationBaseline()
    {
        _originalOrderDate = SelectedDateOffset?.DateTime.Date ?? DateTime.Today;
        PriceListNotice = null;
    }
}
