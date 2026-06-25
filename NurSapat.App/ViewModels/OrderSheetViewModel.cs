using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Documents;
using NurSapat.Cad.Core.Viewport;
using NurSapat.Cad.UI.ViewModels;
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
    private IDocumentManager? _documentManager;

    public CadEditorViewModel PreviewEditor { get; }

    public Order Order { get; }

    public ObservableCollection<OrderConstruction> Constructions { get; } = new();

    public ObservableCollection<MaterialQuantityLine> MaterialLines { get; } = new();

    public OrderHeader Header => Order.Header;

    public OrderCostTotals Totals => Order.Totals;

    public OrderPayment Payment => Order.Payment;

    public ConstructionCostBreakdown Cost => ActiveConstruction.Cost;

    public ConstructionMetrics Metrics => ActiveConstruction.Metrics;

    public OrderConstruction ActiveConstruction => SelectedConstruction ?? Order.EnsureDefaultConstruction();

    [ObservableProperty]
    private OrderConstruction? _selectedConstruction;

    [ObservableProperty]
    private bool _isAutoSaving;

    public bool HasConstructions => Constructions.Any(HasConstructionContent);

    public bool ShowConstructionEmptyState => !HasConstructions;

    public bool IsConstructionSelected => SelectedConstruction != null;

    public bool ShowConstructionNotSelected => SelectedConstruction is null;

    public bool HasConstructionCalculation =>
        Metrics.ProductAreaSqM > 0 && Cost.ConstructionSum > 0;

    public bool ShowMaterialsEmptyState => !HasConstructionCalculation;

    public bool HasPreviewGeometry =>
        ActiveConstruction.Geometry.Elements.Count > 0 ||
        (ActiveConstruction.Geometry.OuterWidthMm > 0 && ActiveConstruction.Geometry.OuterHeightMm > 0);

    public bool ShowPreviewPlaceholder => !HasPreviewGeometry;

    public string PreviewZoomPercent => PreviewEditor.ZoomPercent;

    public string ConstructionAreaDisplay => Metrics.ProductAreaSqM > 0
        ? $"{Metrics.ProductAreaSqM:N2} м²"
        : "Нет данных";

    public string ConstructionWeightDisplay => Metrics.WeightKg > 0
        ? $"{Metrics.WeightKg:N0} кг"
        : "Нет данных";

    public string ConstructionMaterialsCostDisplay => Cost.MaterialsTotal > 0
        ? $"{Cost.MaterialsTotal:N0} {CurrencyCode}"
        : "Нет данных";

    public string ConstructionLaborCostDisplay => Cost.LaborCost > 0
        ? $"{Cost.LaborCost:N0} {CurrencyCode}"
        : "Нет данных";

    public string ConstructionTotalCostDisplay => Cost.ConstructionSum > 0
        ? $"{Cost.ConstructionSum:N0} {CurrencyCode}"
        : "Нет данных";

    public string CostPerSqMDisplay
    {
        get
        {
            if (Cost.ConstructionSum <= 0 || Metrics.ProductAreaSqM <= 0)
            {
                return "Нет данных";
            }

            var perSqM = Cost.ConstructionSum / Metrics.ProductAreaSqM;
            return $"{perSqM:N0} {CurrencyCode}";
        }
    }

    public string CostPriceDisplay => Cost.CostPrice > 0
        ? $"{Cost.CostPrice:N0} {CurrencyCode}"
        : "Пока пусто";

    public string ProfitabilityDisplay => Cost.ProfitabilityPercent > 0
        ? $"{Cost.ProfitabilityPercent:N1} %"
        : "Пока пусто";

    public string MarkupDisplay => Cost.ProfitabilityAmount > 0
        ? $"{Cost.ProfitabilityAmount:N0} {CurrencyCode}"
        : "Пока пусто";

    public string PaymentTotalDisplay => HasConstructionCalculation
        ? $"{Cost.ConstructionSum:N0} {CurrencyCode}"
        : "0";

    public string PaidAmountDisplay => Payment.PaidAmount > 0
        ? $"{Payment.PaidAmount:N0} {CurrencyCode}"
        : "0";

    public string RemainingAmountDisplay => Payment.PaidAmount > 0 || Payment.AgreedAmount > 0
        ? $"{PaymentDebt:N0} {CurrencyCode}"
        : "0";

    public string LastPaymentDisplay => Payment.PaidAmount > 0
        ? DateTime.Now.ToString("dd.MM.yyyy")
        : "Пока пусто";

    public bool HasPaymentData => Payment.PaidAmount > 0;

    public string PaymentEmptyHint => HasPaymentData
        ? string.Empty
        : "Оплата ещё не зарегистрирована.";

    public decimal PaymentDebt => Payment.Debt;

    public double PaymentPercent => Payment.AgreedAmount <= 0
        ? 0
        : Math.Min(100, (double)(Payment.PaidAmount / Payment.AgreedAmount * 100m));

    public string CurrencyCode => Header.CurrencyCode;

    public string SaveStatusText => IsAutoSaving
        ? "⟳ Выполняется автосохранение..."
        : _documentManager?.IsModified == true
            ? "● Есть несохранённые изменения"
            : "✔ Все изменения сохранены";

    public string AutoSaveStatusText => "Автосохранение: включено";

    public string ProjectVersionText => "Версия проекта: 1.0";

    public string CurrentUserText => "Текущий пользователь: —";

    public string LastModifiedText => $"Последнее изменение: {DateTime.Now:dd.MM.yyyy}";

    public string LastModifiedTimeText => $"Время изменения: {DateTime.Now:HH:mm}";

    public event EventHandler? ProjectModified;

    public OrderSheetViewModel(Order order, CadEditorViewModel previewEditor)
    {
        Order = order;
        PreviewEditor = previewEditor;
        Order.Payment.PropertyChanged += OnPaymentPropertyChanged;
        Order.Totals.PropertyChanged += OnTotalsPropertyChanged;

        RebuildConstructions();
        SelectedConstruction = Order.EnsureDefaultConstruction();
        ActiveConstruction.Cost.PropertyChanged += OnCostPropertyChanged;

        RecalculateAll();
    }

    public void AttachDocumentManager(IDocumentManager documentManager)
    {
        if (_documentManager is not null)
        {
            _documentManager.DocumentStateChanged -= OnDocumentStateChanged;
        }

        _documentManager = documentManager;
        _documentManager.DocumentStateChanged += OnDocumentStateChanged;
        OnPropertyChanged(nameof(SaveStatusText));
    }

    public void EnterViewMode()
    {
        PreviewEditor.IsViewOnlyMode = true;
        SyncPreviewFromActiveConstruction();
    }

    public void ExitViewMode()
    {
        PreviewEditor.IsViewOnlyMode = false;
    }

    public void ReloadFromOrder(Guid? activeConstructionId)
    {
        RebuildConstructions();
        SelectedConstruction = activeConstructionId is Guid id
            ? Constructions.FirstOrDefault(c => c.Id == id) ?? Order.EnsureDefaultConstruction()
            : Order.EnsureDefaultConstruction();

        OnPropertyChanged(nameof(Header));
        RecalculateAll();
    }

    partial void OnSelectedConstructionChanged(OrderConstruction? value)
    {
        if (value is null)
        {
            NotifySelectionProperties();
            return;
        }

        Order.SelectedConstruction = value;
        value.Cost.PropertyChanged += OnCostPropertyChanged;
        SyncPreviewFromActiveConstruction();
        RecalculateAll();
        NotifySelectionProperties();
    }

    [RelayCommand]
    private void SelectConstruction(OrderConstruction? construction)
    {
        if (construction is null)
        {
            return;
        }

        SelectedConstruction = construction;
    }

    [RelayCommand]
    private void PreviewZoomIn()
    {
        PreviewEditor.Viewport.ZoomFactor = Math.Min(
            CadViewport.MaxZoom,
            PreviewEditor.Viewport.ZoomFactor * 1.15);
        PreviewEditor.RequestSceneRefresh();
        OnPropertyChanged(nameof(PreviewZoomPercent));
    }

    [RelayCommand]
    private void PreviewZoomOut()
    {
        PreviewEditor.Viewport.ZoomFactor = Math.Max(
            CadViewport.MinZoom,
            PreviewEditor.Viewport.ZoomFactor / 1.15);
        PreviewEditor.RequestSceneRefresh();
        OnPropertyChanged(nameof(PreviewZoomPercent));
    }

    [RelayCommand]
    private void PreviewFitToScreen()
    {
        PreviewEditor.FitDocumentToViewport();
        OnPropertyChanged(nameof(PreviewZoomPercent));
    }

    [RelayCommand]
    private void PreviewResetZoom()
    {
        PreviewEditor.Viewport.ResetToDefaultScale();
        PreviewEditor.RequestSceneRefresh();
        OnPropertyChanged(nameof(PreviewZoomPercent));
    }

    private void RebuildConstructions()
    {
        Constructions.Clear();
        foreach (var item in Order.Constructions)
        {
            Constructions.Add(item);
        }

        if (Constructions.Count == 0)
        {
            Order.EnsureDefaultConstruction();
            RebuildConstructions();
        }

        OnPropertyChanged(nameof(HasConstructions));
        OnPropertyChanged(nameof(ShowConstructionEmptyState));
    }

    public void RecalculateAll()
    {
        _calculationService.Recalculate(Order);
        RebuildMaterialLines();
        NotifyFinancialProperties();
        RefreshConstructionList();
    }

    private void RefreshConstructionList()
    {
        var selectedId = SelectedConstruction?.Id;
        RebuildConstructions();
        if (selectedId is Guid id)
        {
            SelectedConstruction = Constructions.FirstOrDefault(c => c.Id == id);
        }
    }

    private void SyncPreviewFromActiveConstruction()
    {
        var elements = ActiveConstruction.Geometry.Elements;
        PreviewEditor.SetDesignElements(elements);

        if (ActiveConstruction.Geometry.OuterWidthMm > 0 && ActiveConstruction.Geometry.OuterHeightMm > 0)
        {
            PreviewEditor.FitDocumentToViewport();
        }

        PreviewEditor.RequestSceneRefresh();
        OnPropertyChanged(nameof(PreviewZoomPercent));
        OnPropertyChanged(nameof(HasPreviewGeometry));
        OnPropertyChanged(nameof(ShowPreviewPlaceholder));
    }

    private void RebuildMaterialLines()
    {
        MaterialLines.Clear();

        if (!HasConstructionCalculation)
        {
            return;
        }

        var construction = ActiveConstruction;
        var geometry = construction.Geometry;
        var w = geometry.OuterWidthMm;
        var h = geometry.OuterHeightMm;
        var perimeterM = w > 0 && h > 0 ? (w + h) * 2m / 1000m : 0m;
        var area = Metrics.ProductAreaSqM;
        var mullionLength = geometry.Elements
            .Where(e => e.Kind == GeometryElementKind.Mullion)
            .Sum(_ => 1.4m);
        if (mullionLength <= 0)
        {
            mullionLength = 1.4m;
        }

        var connectors = Math.Max(2, geometry.Elements.Count(e =>
            e.Kind is GeometryElementKind.EdgeMullion or GeometryElementKind.Mullion));

        MaterialLines.Add(new MaterialQuantityLine(
            "Профиль",
            construction.ProfileDisplay,
            $"{perimeterM * 0.73m:N2} м"));
        MaterialLines.Add(new MaterialQuantityLine(
            "Армирование",
            string.Empty,
            $"{perimeterM * 0.73m:N2} м"));
        MaterialLines.Add(new MaterialQuantityLine(
            "Импост",
            string.Empty,
            $"{mullionLength:N2} м"));
        MaterialLines.Add(new MaterialQuantityLine(
            "Стеклопакет",
            construction.GlazingDisplay,
            $"{area:N2} м²"));
        MaterialLines.Add(new MaterialQuantityLine(
            "Уплотнитель",
            string.Empty,
            $"{perimeterM * 1.05m:N2} м"));
        MaterialLines.Add(new MaterialQuantityLine(
            "Фурнитура",
            construction.HardwareDisplay,
            "1 комплект"));
        MaterialLines.Add(new MaterialQuantityLine(
            "Штапик",
            string.Empty,
            $"{perimeterM * 1.07m:N2} м"));
        MaterialLines.Add(new MaterialQuantityLine(
            "Соединитель",
            string.Empty,
            $"{connectors} шт."));
    }

    private static bool HasConstructionContent(OrderConstruction construction) =>
        construction.Geometry.Elements.Count > 0 ||
        (construction.Geometry.OuterWidthMm > 0 && construction.Geometry.OuterHeightMm > 0);

    private void NotifySelectionProperties()
    {
        OnPropertyChanged(nameof(ActiveConstruction));
        OnPropertyChanged(nameof(Cost));
        OnPropertyChanged(nameof(Metrics));
        OnPropertyChanged(nameof(IsConstructionSelected));
        OnPropertyChanged(nameof(ShowConstructionNotSelected));
        OnPropertyChanged(nameof(HasConstructionCalculation));
        OnPropertyChanged(nameof(ShowMaterialsEmptyState));
        OnPropertyChanged(nameof(HasPreviewGeometry));
        OnPropertyChanged(nameof(ShowPreviewPlaceholder));
        OnPropertyChanged(nameof(ConstructionAreaDisplay));
        OnPropertyChanged(nameof(ConstructionWeightDisplay));
        OnPropertyChanged(nameof(ConstructionMaterialsCostDisplay));
        OnPropertyChanged(nameof(ConstructionLaborCostDisplay));
        OnPropertyChanged(nameof(ConstructionTotalCostDisplay));
        OnPropertyChanged(nameof(CostPerSqMDisplay));
        OnPropertyChanged(nameof(CostPriceDisplay));
        OnPropertyChanged(nameof(ProfitabilityDisplay));
        OnPropertyChanged(nameof(MarkupDisplay));
        OnPropertyChanged(nameof(PaymentTotalDisplay));
    }

    private void OnDocumentStateChanged(object? sender, EventArgs e) =>
        OnPropertyChanged(nameof(SaveStatusText));

    private void OnPaymentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(PaymentDebt));
        OnPropertyChanged(nameof(PaymentPercent));
        OnPropertyChanged(nameof(PaidAmountDisplay));
        OnPropertyChanged(nameof(RemainingAmountDisplay));
        OnPropertyChanged(nameof(LastPaymentDisplay));
        OnPropertyChanged(nameof(HasPaymentData));
        OnPropertyChanged(nameof(PaymentEmptyHint));
    }

    private void OnTotalsPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        NotifyFinancialProperties();

    private void OnCostPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        NotifyFinancialProperties();

    private void NotifyFinancialProperties()
    {
        OnPropertyChanged(nameof(Cost));
        OnPropertyChanged(nameof(Metrics));
        OnPropertyChanged(nameof(HasConstructionCalculation));
        OnPropertyChanged(nameof(ShowMaterialsEmptyState));
        OnPropertyChanged(nameof(ConstructionAreaDisplay));
        OnPropertyChanged(nameof(ConstructionWeightDisplay));
        OnPropertyChanged(nameof(ConstructionMaterialsCostDisplay));
        OnPropertyChanged(nameof(ConstructionLaborCostDisplay));
        OnPropertyChanged(nameof(ConstructionTotalCostDisplay));
        OnPropertyChanged(nameof(CostPerSqMDisplay));
        OnPropertyChanged(nameof(CostPriceDisplay));
        OnPropertyChanged(nameof(ProfitabilityDisplay));
        OnPropertyChanged(nameof(MarkupDisplay));
        OnPropertyChanged(nameof(PaymentTotalDisplay));
        OnPropertyChanged(nameof(PaymentPercent));
        RebuildMaterialLines();
    }
}

public sealed record MaterialQuantityLine(string Name, string Detail, string Quantity);
