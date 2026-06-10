using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Calculations;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace NurSapat.App.ViewModels;

public sealed partial class OrderSheetViewModel : ViewModelBase
{
    private readonly OrderCalculationService _calculationService = new();

    public Order Order { get; }

    public OrderHeader Header => Order.Header;

    public OrderCostTotals Totals => Order.Totals;

    public OrderPayment Payment => Order.Payment;

    public ConstructionCostBreakdown Cost => ActiveConstruction.Cost;

    public ConstructionMetrics Metrics => ActiveConstruction.Metrics;

    public ObservableCollection<CostSpecificationLine> SpecificationLines { get; } = new();

    public OrderConstruction ActiveConstruction => Order.EnsureDefaultConstruction();

    [ObservableProperty]
    private decimal _outerWidthMm;

    [ObservableProperty]
    private decimal _outerHeightMm;

    public decimal ProfitabilityPercent
    {
        get => Order.ProfitabilityPercent;
        set
        {
            if (Order.ProfitabilityPercent == value) return;
            Order.ProfitabilityPercent = value;
            OnPropertyChanged();
            Recalculate();
        }
    }

    public decimal Discount1Percent
    {
        get => Cost.Discount1Percent;
        set
        {
            if (Cost.Discount1Percent == value) return;
            Cost.Discount1Percent = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Cost));
            RefreshFinancialSummary();
        }
    }

    public decimal Discount2Percent
    {
        get => Totals.Discount2Percent;
        set
        {
            if (Totals.Discount2Percent == value) return;
            Totals.Discount2Percent = value;
            OnPropertyChanged();
            Totals.NotifyDerived();
            SyncPayment();
            NotifyFinancialProperties();
        }
    }

    public decimal Prepayment
    {
        get => Payment.PaidAmount;
        set
        {
            if (Payment.PaidAmount == value) return;
            Payment.PaidAmount = value;
            OnPropertyChanged();
            Payment.NotifyDebt();
            OnPropertyChanged(nameof(PaymentDebt));
            OnPropertyChanged(nameof(PaymentStatus));
        }
    }

    public decimal GrandTotal => Totals.GrandTotal;

    public decimal PaymentDebt => Payment.Debt;

    public string PaymentStatus => Payment.PaymentStatus;

    public string CurrencyCode => Header.CurrencyCode;

    public string GrandTotalText => $"{GrandTotal:N2} {CurrencyCode}";

    public DateTimeOffset? OrderDateOffset
    {
        get => new DateTimeOffset(DateTime.SpecifyKind(Header.OrderDate, DateTimeKind.Local));
        set
        {
            if (value is null) return;
            Header.OrderDate = value.Value.DateTime;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? ContractDateOffset
    {
        get => Header.ContractDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(Header.ContractDate.Value, DateTimeKind.Local))
            : null;
        set
        {
            Header.ContractDate = value?.DateTime;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? ProductionDateOffset
    {
        get => Header.ProductionDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(Header.ProductionDate.Value, DateTimeKind.Local))
            : null;
        set
        {
            Header.ProductionDate = value?.DateTime;
            OnPropertyChanged();
        }
    }

    public DateTimeOffset? DeliveryDateOffset
    {
        get => Header.DeliveryDate.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(Header.DeliveryDate.Value, DateTimeKind.Local))
            : null;
        set
        {
            Header.DeliveryDate = value?.DateTime;
            OnPropertyChanged();
        }
    }

    public OrderSheetViewModel(Order order)
    {
        Order = order;
        Order.Payment.PropertyChanged += OnPaymentPropertyChanged;
        Order.Totals.PropertyChanged += OnTotalsPropertyChanged;
        Cost.PropertyChanged += OnCostPropertyChanged;

        OuterWidthMm = ActiveConstruction.Geometry.OuterWidthMm;
        OuterHeightMm = ActiveConstruction.Geometry.OuterHeightMm;

        Recalculate();
    }

    partial void OnOuterWidthMmChanged(decimal value)
    {
        ActiveConstruction.Geometry.OuterWidthMm = value;
        Recalculate();
    }

    partial void OnOuterHeightMmChanged(decimal value)
    {
        ActiveConstruction.Geometry.OuterHeightMm = value;
        Recalculate();
    }

    [RelayCommand]
    private void Recalculate() => RecalculateAll();

    public void RecalculateAll()
    {
        _calculationService.Recalculate(Order);
        RebuildSpecificationLines();
        NotifyFinancialProperties();
    }

    private void RefreshFinancialSummary()
    {
        Order.Totals.ConstructionsSubtotal = Order.Constructions
            .Where(item => item.IsSelectedForCalculation)
            .Sum(item => item.Cost.ConstructionSumTotal);
        Order.Totals.NotifyDerived();
        SyncPayment();
        RebuildSpecificationLines();
        NotifyFinancialProperties();
    }

    private void SyncPayment()
    {
        if (Payment.AgreedAmount < GrandTotal)
        {
            Payment.AgreedAmount = GrandTotal;
        }

        Payment.NotifyDebt();
    }

    private void RebuildSpecificationLines()
    {
        SpecificationLines.Clear();
        AddLine("Профиль", Cost.ProfileCost);
        AddLine("Комплектующие", Cost.ComponentsCost);
        AddLine("Уплотнители", Cost.SealantCost);
        AddLine("Остекление", Cost.GlazingCost);
        AddLine("Фурнитура", Cost.HardwareCost);
        AddLine("Материалы для монтажа", Cost.InstallationMaterialsCost);
        AddLine("Затраты", Cost.Expenses);
        AddLine("Трудозатраты", Cost.LaborCost);
    }

    private void AddLine(string name, decimal amount)
    {
        SpecificationLines.Add(new CostSpecificationLine(
            name,
            "сумма",
            1m,
            $"{Metrics.ProductAreaSqM:N3} м²",
            amount));
    }

    private void OnPaymentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OrderPayment.Debt) or nameof(OrderPayment.PaymentStatus))
        {
            OnPropertyChanged(nameof(PaymentDebt));
            OnPropertyChanged(nameof(PaymentStatus));
        }
    }

    private void OnTotalsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OrderCostTotals.GrandTotal))
        {
            NotifyFinancialProperties();
        }
    }

    private void OnCostPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ConstructionCostBreakdown.ConstructionSumTotal))
        {
            RefreshFinancialSummary();
        }
    }

    private void NotifyFinancialProperties()
    {
        OnPropertyChanged(nameof(GrandTotal));
        OnPropertyChanged(nameof(GrandTotalText));
        OnPropertyChanged(nameof(PaymentDebt));
        OnPropertyChanged(nameof(PaymentStatus));
        OnPropertyChanged(nameof(Cost));
        OnPropertyChanged(nameof(Totals));
        OnPropertyChanged(nameof(Metrics));
    }
}

public sealed record CostSpecificationLine(
    string Name,
    string Unit,
    decimal Quantity,
    string QuantityText,
    decimal Amount)
{
    public string AmountText => $"{Amount:N2}";
}
