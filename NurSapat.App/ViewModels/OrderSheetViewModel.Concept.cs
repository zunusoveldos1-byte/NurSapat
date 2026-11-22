using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.App.ViewModels;

/// <summary>
/// Дополнительные свойства и команды листа заказа для нового интерфейса
/// («Общие данные заказа», KPI рентабельности, итог, декоративные расходы).
/// Только надстройка над существующей бизнес-логикой — доменные данные берутся
/// из заголовка заказа, себестоимости конструкции и итогов заказа.
/// </summary>
public sealed partial class OrderSheetViewModel
{
    // ======================= Общие данные заказа =======================

    public string DealerName
    {
        get => Header.DealerName;
        set
        {
            if (Header.DealerName == value)
            {
                return;
            }

            Header.DealerName = value;
            ActiveConstruction.OrderDetails.DealerName = value;
            OnPropertyChanged();
            ProjectModified?.Invoke(this, EventArgs.Empty);
        }
    }

    public string CustomerCity
    {
        get => Header.CustomerCity;
        set
        {
            if (Header.CustomerCity == value)
            {
                return;
            }

            Header.CustomerCity = value;
            ActiveConstruction.OrderDetails.CustomerCity = value;
            OnPropertyChanged();
            ProjectModified?.Invoke(this, EventArgs.Empty);
        }
    }

    public string CustomerCode
    {
        get => Header.CustomerCode;
        set
        {
            if (Header.CustomerCode == value)
            {
                return;
            }

            Header.CustomerCode = value;
            ActiveConstruction.OrderDetails.CustomerCode = value;
            OnPropertyChanged();
            ProjectModified?.Invoke(this, EventArgs.Empty);
        }
    }

    public DateTimeOffset? OrderDate
    {
        get => new DateTimeOffset(Header.OrderDate);
        set
        {
            if (value is null || Header.OrderDate == value.Value.DateTime)
            {
                return;
            }

            Header.OrderDate = value.Value.DateTime;
            ActiveConstruction.OrderDetails.OrderDate = Header.OrderDate;
            OnPropertyChanged();
            NotifyOrderDateParts();
            ProjectModified?.Invoke(this, EventArgs.Empty);
        }
    }

    public string OrderDateDay
    {
        get => Header.OrderDate.Day.ToString("00");
        set => TryApplyOrderDatePart(dayText: value);
    }

    public string OrderDateMonth
    {
        get => Header.OrderDate.ToString("MMMM", RuCulture);
        set => TryApplyOrderDatePart(monthText: value);
    }

    public string OrderDateYear
    {
        get => Header.OrderDate.Year.ToString("0000");
        set => TryApplyOrderDatePart(yearText: value);
    }

    public DateTimeOffset? ContractDate
    {
        get => Header.ContractDate.HasValue ? new DateTimeOffset(Header.ContractDate.Value) : null;
        set
        {
            var newValue = value?.DateTime;
            if (Header.ContractDate == newValue)
            {
                return;
            }

            Header.ContractDate = newValue;
            ActiveConstruction.OrderDetails.ContractDate = newValue;
            OnPropertyChanged();
            NotifyContractDateParts();
            ProjectModified?.Invoke(this, EventArgs.Empty);
        }
    }

    public string ContractDateDay
    {
        get => Header.ContractDate?.Day.ToString("00") ?? string.Empty;
        set => TryApplyContractDatePart(dayText: value);
    }

    public string ContractDateMonth
    {
        get => Header.ContractDate?.ToString("MMMM", RuCulture) ?? string.Empty;
        set => TryApplyContractDatePart(monthText: value);
    }

    public string ContractDateYear
    {
        get => Header.ContractDate?.Year.ToString("0000") ?? string.Empty;
        set => TryApplyContractDatePart(yearText: value);
    }

    private static readonly System.Globalization.CultureInfo RuCulture =
        new("ru-RU");

    private void NotifyOrderDateParts()
    {
        OnPropertyChanged(nameof(OrderDateDay));
        OnPropertyChanged(nameof(OrderDateMonth));
        OnPropertyChanged(nameof(OrderDateYear));
    }

    private void NotifyContractDateParts()
    {
        OnPropertyChanged(nameof(ContractDateDay));
        OnPropertyChanged(nameof(ContractDateMonth));
        OnPropertyChanged(nameof(ContractDateYear));
    }

    private void TryApplyOrderDatePart(string? dayText = null, string? monthText = null, string? yearText = null)
    {
        if (!TryComposeDate(
                dayText ?? OrderDateDay,
                monthText ?? OrderDateMonth,
                yearText ?? OrderDateYear,
                Header.OrderDate,
                out var composed))
        {
            NotifyOrderDateParts();
            return;
        }

        if (Header.OrderDate == composed)
        {
            NotifyOrderDateParts();
            return;
        }

        Header.OrderDate = composed;
        ActiveConstruction.OrderDetails.OrderDate = composed;
        OnPropertyChanged(nameof(OrderDate));
        NotifyOrderDateParts();
        ProjectModified?.Invoke(this, EventArgs.Empty);
    }

    private void TryApplyContractDatePart(string? dayText = null, string? monthText = null, string? yearText = null)
    {
        var fallback = Header.ContractDate ?? Header.OrderDate;
        if (!TryComposeDate(
                dayText ?? ContractDateDay,
                monthText ?? ContractDateMonth,
                yearText ?? ContractDateYear,
                fallback,
                out var composed))
        {
            NotifyContractDateParts();
            return;
        }

        if (Header.ContractDate == composed)
        {
            NotifyContractDateParts();
            return;
        }

        Header.ContractDate = composed;
        ActiveConstruction.OrderDetails.ContractDate = composed;
        OnPropertyChanged(nameof(ContractDate));
        NotifyContractDateParts();
        ProjectModified?.Invoke(this, EventArgs.Empty);
    }

    private static bool TryComposeDate(
        string dayText,
        string monthText,
        string yearText,
        DateTime fallback,
        out DateTime composed)
    {
        composed = fallback;

        if (!int.TryParse(dayText?.Trim(), out var day) || day is < 1 or > 31)
        {
            day = fallback.Day;
        }

        if (!TryParseMonth(monthText, out var month))
        {
            month = fallback.Month;
        }

        if (!int.TryParse(yearText?.Trim(), out var year) || year is < 1900 or > 2100)
        {
            year = fallback.Year;
        }

        var daysInMonth = DateTime.DaysInMonth(year, month);
        day = Math.Clamp(day, 1, daysInMonth);
        composed = new DateTime(year, month, day);
        return true;
    }

    private static bool TryParseMonth(string? text, out int month)
    {
        month = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        text = text.Trim();
        if (int.TryParse(text, out var numeric) && numeric is >= 1 and <= 12)
        {
            month = numeric;
            return true;
        }

        for (var m = 1; m <= 12; m++)
        {
            var name = RuCulture.DateTimeFormat.GetMonthName(m);
            var abbr = RuCulture.DateTimeFormat.GetAbbreviatedMonthName(m);
            if (text.Equals(name, StringComparison.OrdinalIgnoreCase)
                || text.Equals(abbr, StringComparison.OrdinalIgnoreCase)
                || (name.Length >= 3
                    && text.StartsWith(name.Substring(0, 3), StringComparison.OrdinalIgnoreCase)))
            {
                month = m;
                return true;
            }
        }

        return false;
    }

    // ======================= Итог (правая колонка) =======================

    public string SummaryProductCostDisplay => FormatMoney(Cost.ConstructionSum);

    public decimal ProductionMontageAmount =>
        Cost.LaborCost + Cost.InstallationCost + Cost.DemolitionCost + Totals.DeliveryCost;

    public string SummaryProductionCostDisplay => FormatMoney(ProductionMontageAmount);

    public string SummaryAdditionalDisplay => FormatMoney(Totals.AdditionalMaterialsCost);

    public string SummaryTotalDisplay => FormatMoney(Totals.GrandTotal);

    public string SummaryVatDisplay => FormatMoney(Totals.VatAmount);

    public string SummaryPayableDisplay => FormatMoney(TotalPrice);

    // ======================= Производство и монтаж (таблица) =======================

    public string ProductLineItemPrice => FormatMoney(Cost.ConstructionSum);

    public string ProductLineItemSum => FormatMoney(Cost.ConstructionSumTotal);

    public string ProductLineDeliveryPrice => FormatMoney(Totals.DeliveryCost);

    public string ProductLineInstallPrice => FormatMoney(Cost.InstallationCost);

    public string ProductLineDemolitionPrice => FormatMoney(Cost.DemolitionCost);

    public string ManufacturingTimeDisplay => Metrics.ManufacturingTime.ToString(@"hh\:mm\:ss");

    public string LaborCostDisplay => FormatMoney(Cost.LaborCost);

    // ======================= KPI (Стоимость и рентабельность) =======================

    public string KpiGrossCostDisplay => FormatMoney(Totals.GrandTotal);

    public string KpiNetCostDisplay => FormatMoney(Cost.ConstructionSum);

    public string KpiProfitabilityDisplay => FormatPercent(Cost.ProfitabilityPercent);

    public decimal RentabilityPercent => Cost.ConstructionSum > 0
        ? Math.Round((Cost.ConstructionSum - Cost.CostPrice) / Cost.ConstructionSum * 100m, 1)
        : 0m;

    public string KpiRentabilityDisplay => FormatPercent(RentabilityPercent);

    public string KpiDiscountDisplay => FormatPercent(Cost.Discount1Percent + Totals.Discount2Percent);

    public decimal GlazingSharePercent => Metrics.ProductAreaSqM > 0
        ? Math.Round(Metrics.GlazingAreaSqM / Metrics.ProductAreaSqM * 100m, 1)
        : 0m;

    public string KpiGlazingDisplay => FormatPercent(GlazingSharePercent);

    // ======================= Профиль / Стеклование / Герметики =======================

    public string SealantDisplay => ActiveConstruction.Appearance.ColorName is { Length: > 0 } color
        ? color
        : "Белый";

    // ======================= Дополнительные расходы (декоративные) =======================

    public ObservableCollection<AdditionalExpenseLine> AdditionalExpenses { get; } = new();

    public decimal AdditionalExpensesTotal =>
        AdditionalExpenses.Sum(line => line.Sum);

    public string AdditionalExpensesTotalDisplay => FormatMoney(AdditionalExpensesTotal);

    public bool ShowAdditionalExpensesEmpty => AdditionalExpenses.Count == 0;

    [RelayCommand]
    private void AddAdditionalExpense()
    {
        var line = new AdditionalExpenseLine();
        line.PropertyChanged += OnAdditionalExpenseLineChanged;
        AdditionalExpenses.Add(line);
        NotifyAdditionalExpensesProperties();
    }

    [RelayCommand]
    private void RemoveAdditionalExpense(AdditionalExpenseLine? line)
    {
        if (line is null)
        {
            return;
        }

        line.PropertyChanged -= OnAdditionalExpenseLineChanged;
        AdditionalExpenses.Remove(line);
        NotifyAdditionalExpensesProperties();
    }

    private void OnAdditionalExpenseLineChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AdditionalExpenseLine.Sum))
        {
            NotifyAdditionalExpensesProperties();
        }
    }

    private void NotifyAdditionalExpensesProperties()
    {
        OnPropertyChanged(nameof(AdditionalExpensesTotal));
        OnPropertyChanged(nameof(AdditionalExpensesTotalDisplay));
        OnPropertyChanged(nameof(ShowAdditionalExpensesEmpty));
    }

    // ======================= Левые действия =======================

    [RelayCommand]
    private void Recalculate()
    {
        RecalculateAll();
        NotifyConceptProperties();
    }

    [RelayCommand]
    private void OpenParameters()
    {
        if (EditConstructionCommand.CanExecute(null))
        {
            EditConstructionCommand.Execute(null);
        }
    }

    [RelayCommand]
    private void OpenReference()
    {
        if (EditConstructionCommand.CanExecute(null))
        {
            EditConstructionCommand.Execute(null);
        }
    }

    [RelayCommand]
    private async Task PrintAsync()
    {
        // Печать листа заказа пока сводится к развёрнутому предпросмотру конструкции.
        await ExpandPreviewCommand.ExecuteAsync(null);
    }

    // ======================= Инфраструктура =======================

    private void InitializeConceptExtensions()
    {
        AdditionalExpenses.CollectionChanged += OnAdditionalExpensesCollectionChanged;
    }

    private void OnAdditionalExpensesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        NotifyAdditionalExpensesProperties();
    }

    private void NotifyConceptProperties()
    {
        OnPropertyChanged(nameof(SummaryProductCostDisplay));
        OnPropertyChanged(nameof(SummaryProductionCostDisplay));
        OnPropertyChanged(nameof(SummaryAdditionalDisplay));
        OnPropertyChanged(nameof(SummaryTotalDisplay));
        OnPropertyChanged(nameof(SummaryVatDisplay));
        OnPropertyChanged(nameof(SummaryPayableDisplay));

        OnPropertyChanged(nameof(ProductLineItemPrice));
        OnPropertyChanged(nameof(ProductLineItemSum));
        OnPropertyChanged(nameof(ProductLineDeliveryPrice));
        OnPropertyChanged(nameof(ProductLineInstallPrice));
        OnPropertyChanged(nameof(ProductLineDemolitionPrice));
        OnPropertyChanged(nameof(ManufacturingTimeDisplay));
        OnPropertyChanged(nameof(LaborCostDisplay));

        OnPropertyChanged(nameof(KpiGrossCostDisplay));
        OnPropertyChanged(nameof(KpiNetCostDisplay));
        OnPropertyChanged(nameof(KpiProfitabilityDisplay));
        OnPropertyChanged(nameof(RentabilityPercent));
        OnPropertyChanged(nameof(KpiRentabilityDisplay));
        OnPropertyChanged(nameof(KpiDiscountDisplay));
        OnPropertyChanged(nameof(GlazingSharePercent));
        OnPropertyChanged(nameof(KpiGlazingDisplay));

        OnPropertyChanged(nameof(SealantDisplay));
    }

    private string FormatMoney(decimal value) => $"{value:N2}";

    private static string FormatPercent(decimal value)
    {
        var rounded = Math.Round(value, 1);
        return rounded == Math.Truncate(rounded)
            ? $"{rounded:N0} %"
            : $"{rounded:N1} %";
    }
}

public sealed partial class AdditionalExpenseLine : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _unit = "шт";

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private decimal _price;

    public decimal Sum => Math.Round(Quantity * Price, 2, MidpointRounding.AwayFromZero);

    public string SumDisplay => $"{Sum:N2}";

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(Sum));
        OnPropertyChanged(nameof(SumDisplay));
    }

    partial void OnPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(Sum));
        OnPropertyChanged(nameof(SumDisplay));
    }
}
