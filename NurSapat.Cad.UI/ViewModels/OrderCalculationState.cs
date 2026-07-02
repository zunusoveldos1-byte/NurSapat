using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;
using System.Collections.ObjectModel;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class OrderCalculationState : ObservableObject
{
    private string _lastCalculationFingerprint = "";

    [ObservableProperty] private string _customerName = "";
    [ObservableProperty] private string _orderNumber = "";
    [ObservableProperty] private DateTime _orderDate = DateTime.Today;
    [ObservableProperty] private string _contractNumber = "";
    [ObservableProperty] private DateTime _contractDate = DateTime.Today;
    [ObservableProperty] private string _customerPhone = "";
    [ObservableProperty] private string _siteAddress = "";
    [ObservableProperty] private string _dealerName = "";
    [ObservableProperty] private string _customerCity = "";
    [ObservableProperty] private string _customerCode = "";
    [ObservableProperty] private DateTime _productionDate = DateTime.Today.AddDays(10);
    [ObservableProperty] private bool _hasDelivery = true;
    [ObservableProperty] private bool _hasInstallation;
    [ObservableProperty] private decimal _prepayment;
    [ObservableProperty] private decimal _debt;

    [ObservableProperty] private double _width;
    [ObservableProperty] private double _height;
    [ObservableProperty] private double _itemArea;
    [ObservableProperty] private double _glassArea;
    [ObservableProperty] private double _profileLength;
    [ObservableProperty] private double _perimeter;
    [ObservableProperty] private int _sectionCount;
    [ObservableProperty] private int _sashCount;
    [ObservableProperty] private int _impostCount;
    [ObservableProperty] private string _fillSummary = "Нет заполнений";
    [ObservableProperty] private string _hardwareSummary = "Нет активной створки";
    [ObservableProperty] private decimal _profileCutLength;
    [ObservableProperty] private decimal _impostCutLength;
    [ObservableProperty] private decimal _glassTotalArea;
    [ObservableProperty] private decimal _estimatedMaterialCost;
    [ObservableProperty] private decimal _discount;
    [ObservableProperty] private decimal _totalCost;
    [ObservableProperty] private decimal _unitAreaPrice;
    [ObservableProperty] private string _profileSystem = "";
    [ObservableProperty] private string _laminationColor = "";
    [ObservableProperty] private string _glassFormula = "";
    [ObservableProperty] private string _workType = "";
    [ObservableProperty] private int _quantity = 1;

    public ObservableCollection<OrderSpecificationLine> SpecificationLines { get; } = new();

    public ObservableCollection<OrderActivityEvent> ActivityLog { get; } = new();

    public ObservableCollection<ExchangeRateLine> ExchangeRates { get; } =
    [
        new("KGS", "Кыргызский сом", 1m, DateTime.Today, "Базовая валюта"),
        new("USD", "Доллар США", 89.45m, DateTime.Today, "Ручной ввод"),
        new("EUR", "Евро", 96.12m, DateTime.Today, "Ручной ввод"),
        new("RUB", "Российский рубль", 0.98m, DateTime.Today, "Ручной ввод")
    ];

    public string OrderDateText => OrderDate.ToString("dd.MM.yyyy");

    public string ContractDateText => ContractDate.ToString("dd.MM.yyyy");

    public string ProductionDateText => ProductionDate.ToString("dd.MM.yyyy");

    public string PaymentStatus => Debt <= 0 && TotalCost > 0
        ? "Оплачено"
        : Prepayment > 0
            ? "Частично оплачено"
            : "Оплата ожидается";

    public string ServicesSummary => string.Join(", ", new[]
        {
            HasDelivery ? "Доставка" : null,
            HasInstallation ? "Установка" : null
        }.Where(value => !string.IsNullOrWhiteSpace(value)))
        is { Length: > 0 } value
            ? value
            : "Услуги не выбраны";

    public string CurrencyStatus => ExchangeRates.Count > 1
        ? $"Обновлено {ExchangeRates.Max(rate => rate.UpdatedAt):dd.MM.yyyy HH:mm}"
        : "API курса не настроен";

    public OrderCalculationState()
    {
        ActivityLog.Add(new OrderActivityEvent(
            DateTime.Now,
            "Пользователь открыл заказ",
            "Состояние заказа готово к расчёту",
            "Иванов И.И."));
    }

    // ИЗМЕНЕНИЕ: метод обновления теперь принимает CadDocument
    public void UpdateFrom(CadDocument document)
    {
        var rects = document.Rectangles;

        // Вычисляем габариты всех прямоугольников
        if (rects.Count > 0)
        {
            var minX = rects.Min(r => r.X);
            var maxX = rects.Max(r => r.X + r.Width);
            var minY = rects.Min(r => r.Y - r.Height);
            var maxY = rects.Max(r => r.Y);

            Width = Math.Round(maxX - minX, 1);
            Height = Math.Round(maxY - minY, 1);
            ItemArea = Math.Round(Width * Height / 1_000_000d, 3);
            Perimeter = Math.Round((Width + Height) * 2 / 1000d, 2);
        }
        else
        {
            Width = 0;
            Height = 0;
            ItemArea = 0;
            Perimeter = 0;
        }

        // Сброс специфичных для окон свойств
        SectionCount = 0;
        ImpostCount = 0;
        SashCount = 0;
        FillSummary = rects.Count > 0 ? $"Прямоугольников: {rects.Count}" : "Нет элементов";
        HardwareSummary = "Не поддерживается";
        ProfileSystem = "Не указана";
        LaminationColor = "Не указан";
        GlassFormula = "Не указана";
        WorkType = HasInstallation ? "Производство и монтаж" : "Производство";
        GlassTotalArea = (decimal)ItemArea;

        // Пример расчета стоимости (можно заменить на свою логику)
        EstimatedMaterialCost = (decimal)ItemArea * 5000m; // Примерная цена за квадратный метр
        RecalculateTotals();

        RebuildSpecificationLines();
        AddCalculationActivityIfChanged();
        NotifyDerivedProperties();
    }

    public void AddActivity(string title, string details, string user = "Система")
    {
        ActivityLog.Insert(0, new OrderActivityEvent(DateTime.Now, title, details, user));
        while (ActivityLog.Count > 50)
        {
            ActivityLog.RemoveAt(ActivityLog.Count - 1);
        }
    }

    partial void OnPrepaymentChanged(decimal value)
    {
        Debt = Math.Max(0, TotalCost - value);
        NotifyDerivedProperties();
    }

    partial void OnDiscountChanged(decimal value)
    {
        RecalculateTotals();
        NotifyDerivedProperties();
    }

    partial void OnQuantityChanged(int value)
    {
        Quantity = Math.Max(1, value);
        RecalculateTotals();
        NotifyDerivedProperties();
    }

    partial void OnHasInstallationChanged(bool value)
    {
        WorkType = value ? "Производство и монтаж" : "Производство";
        NotifyDerivedProperties();
    }

    partial void OnHasDeliveryChanged(bool value) => NotifyDerivedProperties();

    partial void OnOrderDateChanged(DateTime value) => NotifyDerivedProperties();

    partial void OnContractDateChanged(DateTime value) => NotifyDerivedProperties();

    partial void OnProductionDateChanged(DateTime value) => NotifyDerivedProperties();

    private void RebuildSpecificationLines()
    {
        SpecificationLines.Clear();
        AddSpecificationLine("Общая площадь", (decimal)ItemArea, "м²", EstimatedMaterialCost);
    }

    private void RecalculateTotals()
    {
        TotalCost = Math.Max(0, EstimatedMaterialCost * Math.Max(1, Quantity) - Discount);
        Debt = Math.Max(0, TotalCost - Prepayment);
        UnitAreaPrice = ItemArea <= 0 ? 0 : decimal.Round(TotalCost / (decimal)ItemArea, 2, MidpointRounding.AwayFromZero);
    }

    private void AddSpecificationLine(string name, decimal quantity, string unit, decimal price)
    {
        SpecificationLines.Add(new OrderSpecificationLine(
            name,
            decimal.Round(quantity, 3, MidpointRounding.AwayFromZero),
            unit,
            decimal.Round(price, 2, MidpointRounding.AwayFromZero)));
    }

    private void AddCalculationActivityIfChanged()
    {
        var fingerprint = $"{Width:0.###}|{Height:0.###}|{ItemArea:0.###}|{TotalCost}";
        if (fingerprint == _lastCalculationFingerprint) return;

        _lastCalculationFingerprint = fingerprint;
        AddActivity("Расчет обновлен", $"{Width:0}x{Height:0} мм, площадь: {ItemArea:0.##} м², сумма: {TotalCost:0.##}");
    }

    private void NotifyDerivedProperties()
    {
        OnPropertyChanged(nameof(OrderDateText));
        OnPropertyChanged(nameof(ContractDateText));
        OnPropertyChanged(nameof(ProductionDateText));
        OnPropertyChanged(nameof(PaymentStatus));
        OnPropertyChanged(nameof(ServicesSummary));
        OnPropertyChanged(nameof(CurrencyStatus));
    }
}

public sealed record OrderSpecificationLine(string Name, decimal Quantity, string Unit, decimal Price);

public sealed record OrderActivityEvent(DateTime Timestamp, string Title, string Details, string User = "Система")
{
    public string Time => Timestamp.ToString("HH:mm");
}

public sealed record ExchangeRateLine(string Code, string Name, decimal Rate, DateTime UpdatedAt, string Source)
{
    public string RateText => $"{Rate:0.####}";

    public string PeriodStartText => UpdatedAt.ToString("dd.MM.yyyy");

    public string PeriodEndText => "—";

    public string ActionText => Source;
}