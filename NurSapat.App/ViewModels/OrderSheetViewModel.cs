using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Documents;
using NurSapat.App.Views.Dialogs;
using NurSapat.Cad.Core.Viewport;
using NurSapat.Cad.UI.Dialogs;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Calculations;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;

namespace NurSapat.App.ViewModels;

public sealed partial class OrderSheetViewModel : ViewModelBase
{
    public const int PageSize = 3;

    private const double WeightKgPerSqM = 25.0;
    private const decimal FallbackAreaPricePerSqM = 5_000m;

    private readonly OrderWorkspaceViewModel _workspace;
    private readonly OrderCalculationService _calculationService = new();
    private IDocumentManager? _documentManager;
    private readonly List<OrderConstructionLookupItemViewModel> _allConstructionCards = [];
    private bool _suppressPageChangedRefresh;

    public CadEditorViewModel PreviewEditor { get; }

    public Order Order { get; }

    public ObservableCollection<OrderConstruction> Constructions { get; } = new();

    public ObservableCollection<ConstructionModel> ConstructionModels { get; } = new();

    public ObservableCollection<OrderConstructionLookupItemViewModel> PagedConstructionCards { get; } = new();

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
    private ConstructionModel? _selectedConstructionModel;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private double _totalArea;

    [ObservableProperty]
    private double _totalWeight;

    [ObservableProperty]
    private decimal _totalPrice;

    public ObservableCollection<string> Managers { get; } =
    [
        "Иванов И.И.",
        "Петров П.П.",
        "Сидоров А.А."
    ];

    public ObservableCollection<string> Supervisors { get; } =
    [
        "Петров П.П.",
        "Козлов В.В.",
        "Нурбеков Э.Э."
    ];

    public ObservableCollection<string> Currencies { get; } =
    [
        "KGS",
        "USD",
        "RUB",
        "EUR"
    ];

    private DateTimeOffset? _lastAutosaveAt;

    public decimal AmountPaid
    {
        get => Payment.PaidAmount;
        set
        {
            if (Payment.PaidAmount == value)
            {
                return;
            }

            Payment.PaidAmount = value;
            OnPropertyChanged();
            NotifyPaymentDerivedProperties();
            RecalculateOrderTotals();
            ProjectModified?.Invoke(this, EventArgs.Empty);
        }
    }

    public string SelectedCurrency
    {
        get => Payment.CurrencyCode;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || Payment.CurrencyCode == value)
            {
                return;
            }

            Payment.CurrencyCode = value;
            Header.CurrencyCode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrencyCode));
            OnPropertyChanged(nameof(TotalPriceDisplay));
            OnPropertyChanged(nameof(AmountLeftToPayDisplay));
            NotifyPaymentDerivedProperties();
            ProjectModified?.Invoke(this, EventArgs.Empty);
        }
    }

    public decimal AmountLeftToPay => Math.Max(0, TotalPrice - AmountPaid);

    public string AmountLeftToPayDisplay => $"{AmountLeftToPay:N2} {SelectedCurrency}";

    public decimal AmountToPay => AmountLeftToPay;

    public string AmountToPayDisplay => AmountLeftToPayDisplay;

    public double PaymentPercentage => TotalPrice > 0
        ? Math.Min(100, (double)(AmountPaid / TotalPrice) * 100)
        : 0;

    public string PaymentPercentageDisplay => $"{Math.Round(PaymentPercentage)}%";

    public string ProfileType => ActiveConstruction.ProfileDisplay;

    public string ProfileColor => ActiveProfileColorDisplay;

    public string GlassPacket => ActiveConstruction.GlazingDisplay;

    public string Hardware => ActiveConstruction.HardwareDisplay;

    public string SaveStatusBulletText => $"• {SaveStatusShortText}";

    public bool IsUnsavedChanges => _documentManager?.IsModified == true;

    public string SaveStatusShortText => IsAutoSaving
        ? "Сохранение..."
        : IsUnsavedChanges
            ? "Есть несохранённые изменения"
            : "Изменения сохранены";

    public string LastAutosaveTimeDisplay => _lastAutosaveAt is null
        ? "Автосохранение: ещё не выполнялось"
        : $"Автосохранение: {FormatRelativeAutosave(_lastAutosaveAt.Value)}";

    public string CustomerName
    {
        get => Header.CustomerName;
        set
        {
            if (Header.CustomerName == value)
            {
                return;
            }

            Header.CustomerName = value;
            OnPropertyChanged();
        }
    }

    public string CustomerPhone
    {
        get => Header.CustomerPhone;
        set
        {
            if (Header.CustomerPhone == value)
            {
                return;
            }

            Header.CustomerPhone = value;
            OnPropertyChanged();
        }
    }

    public string ObjectAddress
    {
        get => Header.SiteAddress;
        set
        {
            if (Header.SiteAddress == value)
            {
                return;
            }

            Header.SiteAddress = value;
            OnPropertyChanged();
        }
    }

    public string ContractNumber
    {
        get => Header.ContractNumber;
        set
        {
            if (Header.ContractNumber == value)
            {
                return;
            }

            Header.ContractNumber = value;
            OnPropertyChanged();
        }
    }

    public string OrderManagerName
    {
        get => Header.OrderManagerName;
        set
        {
            if (Header.OrderManagerName == value)
            {
                return;
            }

            Header.OrderManagerName = value;
            OnPropertyChanged();
        }
    }

    public string TotalCountDisplay => $"{TotalCount} шт.";

    public string TotalAreaDisplay => $"{TotalArea:F2} м²";

    public string TotalWeightDisplay => $"{TotalWeight:F1} кг";

    public string TotalPriceDisplay => $"{TotalPrice:N2} {CurrencyCode}";

    public string ActiveProfileColorDisplay => "Белый";

    public bool ActiveWhiteOrientation => true;

    public string ActiveDimensionsDisplay => ActiveConstruction.DimensionsDisplay;

    [ObservableProperty]
    private bool _isAutoSaving;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private string _constructionSearchText = string.Empty;

    [ObservableProperty]
    private OrderConstructionLookupItemViewModel? _selectedConstructionCard;

    public const string DefaultActiveHintText = "Готово. Наведите на элемент для подсказки...";

    [ObservableProperty]
    private string _activeHintText = DefaultActiveHintText;

    private bool _isToolbarHintActive;

    public void ShowToolbarHint(string hint)
    {
        _isToolbarHintActive = true;
        ActiveHintText = hint;
    }

    public void ResetToolbarHint()
    {
        _isToolbarHintActive = false;
        ActiveHintText = DefaultActiveHintText;
    }

    public void UpdateCanvasHoverHint(string hint)
    {
        if (_isToolbarHintActive)
        {
            return;
        }

        ActiveHintText = string.IsNullOrWhiteSpace(hint) ? DefaultActiveHintText : hint;
    }

    public bool HasConstructions => Constructions.Any(HasConstructionContent);

    public bool ShowConstructionEmptyState => !HasConstructions;

    public bool ShowConstructionCardsEmptyState => GetFilteredConstructionCards().Count == 0;

    public bool CanGoToPreviousPage => CurrentPage > 1;

    public bool CanGoToNextPage => CurrentPage < TotalPages;

    public int TotalPages
    {
        get
        {
            var count = GetFilteredConstructionCards().Count;
            return Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
        }
    }

    public string PageIndicatorText => $"Стр. {CurrentPage} / {TotalPages}";

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

    public OrderSheetViewModel(OrderWorkspaceViewModel workspace, CadEditorViewModel previewEditor)
    {
        _workspace = workspace;
        Order = workspace.Order;
        PreviewEditor = previewEditor;
        Order.Payment.PropertyChanged += OnPaymentPropertyChanged;
        Order.Totals.PropertyChanged += OnTotalsPropertyChanged;

        RebuildConstructions();
        RebuildConstructionCards();
        SelectedConstruction = Order.EnsureDefaultConstruction();
        ActiveConstruction.Cost.PropertyChanged += OnCostPropertyChanged;

        RecalculateAll();
        RecalculateOrderTotals();

        if (string.IsNullOrWhiteSpace(Payment.CurrencyCode))
        {
            Payment.CurrencyCode = "KGS";
            Header.CurrencyCode = "KGS";
        }
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
        RebuildConstructionCards();
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
        OnPropertyChanged(nameof(CustomerName));
        OnPropertyChanged(nameof(CustomerPhone));
        OnPropertyChanged(nameof(ObjectAddress));
        OnPropertyChanged(nameof(ContractNumber));
        OnPropertyChanged(nameof(OrderManagerName));
        RecalculateAll();
    }

    partial void OnSelectedConstructionChanged(OrderConstruction? value)
    {
        if (value is null)
        {
            SelectedConstructionModel = null;
            NotifySelectionProperties();
            RebuildConstructionCards();
            return;
        }

        Order.SelectedConstruction = value;
        value.Cost.PropertyChanged += OnCostPropertyChanged;
        SyncSelectedConstructionModel();
        SyncPreviewFromActiveConstruction();
        RecalculateAll();
        NotifySelectionProperties();
    }

    partial void OnSelectedConstructionModelChanged(ConstructionModel? value)
    {
        OnPropertyChanged(nameof(ActiveConstructionModel));
    }

    partial void OnConstructionSearchTextChanged(string value)
    {
        CurrentPage = 1;
        RefreshPagedConstructions();
    }

    partial void OnCurrentPageChanged(int value)
    {
        if (_suppressPageChangedRefresh)
        {
            return;
        }

        UpdatePagedItems();
        NotifyPaginationProperties();
    }

    partial void OnSelectedConstructionCardChanged(OrderConstructionLookupItemViewModel? value)
    {
        if (value is not null && SelectedConstruction?.Id != value.Id)
        {
            SelectedConstruction = value.Construction;
        }
    }

    [RelayCommand]
    private void SelectConstructionCard(OrderConstructionLookupItemViewModel? card)
    {
        if (card is null)
        {
            return;
        }

        SelectedConstructionCard = card;
        SelectedConstruction = card.Construction;
    }

    [RelayCommand]
    private async Task AddConstructionAsync()
    {
        await _workspace.AddNewConstructionAsync();
        GoToLastPage();
        RefreshPagedConstructions();
    }

    [RelayCommand(CanExecute = nameof(CanDuplicateConstruction))]
    private async Task DuplicateConstructionAsync()
    {
        if (SelectedConstruction is null)
        {
            return;
        }

        await _workspace.DuplicateConstructionAsync(SelectedConstruction.Id);
        GoToLastPage();
        RefreshPagedConstructions();
    }

    private bool CanDuplicateConstruction => SelectedConstruction is not null;

    [RelayCommand(CanExecute = nameof(CanEditConstruction))]
    private async Task EditConstructionAsync()
    {
        if (SelectedConstruction is null)
        {
            return;
        }

        await _workspace.EditConstructionInDrawingAsync(SelectedConstruction.Id);
    }

    private bool CanEditConstruction => SelectedConstruction is not null;

    [RelayCommand(CanExecute = nameof(CanDeleteConstruction))]
    private async Task DeleteConstructionAsync()
    {
        if (SelectedConstruction is null)
        {
            return;
        }

        var confirmed = await ShowDeleteConfirmationAsync();
        if (!confirmed)
        {
            return;
        }

        var deletedId = SelectedConstruction.Id;
        await _workspace.RemoveConstructionAsync(deletedId);

        if (GetFilteredConstructionCards().Count == 0)
        {
            CurrentPage = 1;
        }
        else if (PagedConstructionCards.Count == 0 && CurrentPage > 1)
        {
            CurrentPage--;
        }
        else
        {
            RefreshPagedConstructions();
        }

        DeleteConstructionCommand.NotifyCanExecuteChanged();
        DuplicateConstructionCommand.NotifyCanExecuteChanged();
        EditConstructionCommand.NotifyCanExecuteChanged();
    }

    private bool CanDeleteConstruction =>
        SelectedConstruction is not null && Order.Constructions.Count > 1;

    [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
    private void GoToPreviousPage()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
    private void GoToNextPage()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }

    private async Task<bool> ShowDeleteConfirmationAsync()
    {
        var owner = GetOwnerWindow();
        if (owner is null)
        {
            return false;
        }

        var viewModel = new ConfirmDialogViewModel(
            "Вы уверены, что хотите удалить выбранную конструкцию?");
        var window = new ConfirmDialogWindow
        {
            Title = "Удаление конструкции",
            DataContext = viewModel
        };

        viewModel.CloseRequested += (_, confirmed) => window.Close(confirmed);
        return await window.ShowDialog<bool?>(owner) == true;
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
    private async Task OpenPreviewAsync(OrderConstructionLookupItemViewModel? construction)
    {
        if (construction is null)
        {
            return;
        }

        var owner = GetOwnerWindow();
        if (owner is null)
        {
            return;
        }

        var viewModel = new PreviewWindowViewModel(construction);
        var window = new PreviewWindow
        {
            DataContext = viewModel
        };

        await window.ShowDialog(owner);
    }

    private static Window? GetOwnerWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
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

    public ConstructionModel? ActiveConstructionModel => SelectedConstructionModel;

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
            return;
        }

        RebuildConstructionModels();
        OnPropertyChanged(nameof(HasConstructions));
        OnPropertyChanged(nameof(ShowConstructionEmptyState));
        RebuildConstructionCards();
    }

    private void RebuildConstructionModels()
    {
        DetachConstructionModels();
        ConstructionModels.Clear();

        foreach (var construction in Order.Constructions)
        {
            var previewEditor = construction.Id == SelectedConstruction?.Id ? PreviewEditor : null;
            var model = new ConstructionModel(construction, previewEditor);
            model.PropertyChanged += OnConstructionModelPropertyChanged;
            ConstructionModels.Add(model);
        }

        SyncSelectedConstructionModel();
        RecalculateOrderTotals();
    }

    private void DetachConstructionModels()
    {
        foreach (var model in ConstructionModels)
        {
            model.PropertyChanged -= OnConstructionModelPropertyChanged;
            model.Detach();
        }
    }

    private void SyncSelectedConstructionModel()
    {
        foreach (var model in ConstructionModels)
        {
            model.SetPreviewEditor(model.Id == SelectedConstruction?.Id ? PreviewEditor : null);
        }

        SelectedConstructionModel = SelectedConstruction is null
            ? null
            : ConstructionModels.FirstOrDefault(m => m.Id == SelectedConstruction.Id);
    }

    private void OnConstructionModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RecalculateOrderTotals();

        if (sender is not ConstructionModel model || SelectedConstruction?.Id != model.Id)
        {
            return;
        }

        if (e.PropertyName is nameof(ConstructionModel.Width) or nameof(ConstructionModel.Height))
        {
            _calculationService.Recalculate(Order);
            SyncPreviewFromActiveConstruction();
            NotifyFinancialProperties();
            RebuildConstructionCards();
            OnPropertyChanged(nameof(ActiveDimensionsDisplay));
            OnPropertyChanged(nameof(ProfileType));
            OnPropertyChanged(nameof(ProfileColor));
            OnPropertyChanged(nameof(GlassPacket));
            OnPropertyChanged(nameof(Hardware));
        }
        else if (e.PropertyName is nameof(ConstructionModel.Quantity))
        {
            _calculationService.Recalculate(Order);
            NotifyFinancialProperties();
            RebuildConstructionCards();
        }
        else if (e.PropertyName is nameof(ConstructionModel.Name))
        {
            RebuildConstructionCards();
        }
    }

    public void RecalculateOrderTotals()
    {
        TotalCount = ConstructionModels.Sum(model => model.Quantity);
        TotalArea = ConstructionModels.Sum(model => model.Area);
        TotalWeight = TotalArea * WeightKgPerSqM;

        TotalPrice = Totals.GrandTotal > 0
            ? Totals.GrandTotal
            : decimal.Round((decimal)TotalArea * FallbackAreaPricePerSqM, 2, MidpointRounding.AwayFromZero);

        if (Payment.AgreedAmount != TotalPrice)
        {
            Payment.AgreedAmount = TotalPrice;
        }

        NotifyOrderTotalsProperties();
        NotifyPaymentDerivedProperties();
    }

    private void NotifyPaymentDerivedProperties()
    {
        OnPropertyChanged(nameof(AmountLeftToPay));
        OnPropertyChanged(nameof(AmountLeftToPayDisplay));
        OnPropertyChanged(nameof(AmountToPay));
        OnPropertyChanged(nameof(AmountToPayDisplay));
        OnPropertyChanged(nameof(PaymentPercentage));
        OnPropertyChanged(nameof(PaymentPercentageDisplay));
        OnPropertyChanged(nameof(PaidAmountDisplay));
        OnPropertyChanged(nameof(RemainingAmountDisplay));
        OnPropertyChanged(nameof(PaymentDebt));
    }

    private void NotifyStatusProperties()
    {
        OnPropertyChanged(nameof(IsUnsavedChanges));
        OnPropertyChanged(nameof(SaveStatusShortText));
        OnPropertyChanged(nameof(SaveStatusBulletText));
        OnPropertyChanged(nameof(SaveStatusText));
        OnPropertyChanged(nameof(LastAutosaveTimeDisplay));
    }

    private static string FormatRelativeAutosave(DateTimeOffset savedAt)
    {
        var elapsed = DateTimeOffset.Now - savedAt;
        if (elapsed.TotalMinutes < 1)
        {
            return "только что";
        }

        if (elapsed.TotalMinutes < 60)
        {
            return $"{(int)elapsed.TotalMinutes} мин. назад";
        }

        if (elapsed.TotalHours < 24)
        {
            return $"{(int)elapsed.TotalHours} ч. назад";
        }

        return savedAt.LocalDateTime.ToString("dd.MM.yyyy HH:mm");
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (_documentManager is null || !IsUnsavedChanges)
        {
            return;
        }

        var path = _documentManager.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        await _documentManager.OpenOrderAsync(path);
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_documentManager is null)
        {
            return;
        }

        if (await _documentManager.SaveOrderAsync())
        {
            _lastAutosaveAt = DateTimeOffset.Now;
            NotifyStatusProperties();
        }
    }

    [RelayCommand]
    private async Task SaveAndCloseAsync()
    {
        if (_documentManager is null)
        {
            return;
        }

        if (await _documentManager.SaveOrderAsync())
        {
            _lastAutosaveAt = DateTimeOffset.Now;
            NotifyStatusProperties();
        }
    }

    [RelayCommand]
    private async Task ExpandPreviewAsync()
    {
        var card = SelectedConstructionCard
            ?? _allConstructionCards.FirstOrDefault(c => c.Id == SelectedConstruction?.Id);
        if (card is not null)
        {
            await OpenPreviewAsync(card);
        }
    }

    private void NotifyOrderTotalsProperties()
    {
        OnPropertyChanged(nameof(TotalCountDisplay));
        OnPropertyChanged(nameof(TotalAreaDisplay));
        OnPropertyChanged(nameof(TotalWeightDisplay));
        OnPropertyChanged(nameof(TotalPriceDisplay));
    }

    private void RebuildConstructionCards()
    {
        _allConstructionCards.Clear();

        foreach (var construction in Order.Constructions.OrderBy(c => c.Number))
        {
            _allConstructionCards.Add(new OrderConstructionLookupItemViewModel(
                construction,
                isActive: construction.Id == SelectedConstruction?.Id));
        }

        RefreshPagedConstructions();
    }

    private void RefreshPagedConstructions()
    {
        var filtered = GetFilteredConstructionCards();
        var totalPages = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)PageSize));
        var targetPage = Math.Clamp(CurrentPage, 1, totalPages);

        if (targetPage != CurrentPage)
        {
            _suppressPageChangedRefresh = true;
            CurrentPage = targetPage;
            _suppressPageChangedRefresh = false;
        }

        UpdatePagedItems();
        OnPropertyChanged(nameof(ShowConstructionCardsEmptyState));
        NotifyPaginationProperties();
    }

    private void UpdatePagedItems()
    {
        var filtered = GetFilteredConstructionCards();
        PagedConstructionCards.Clear();
        foreach (var card in filtered.Skip((CurrentPage - 1) * PageSize).Take(PageSize))
        {
            PagedConstructionCards.Add(card);
        }

        SyncSelectedConstructionCard();
    }

    private void SyncSelectedConstructionCard()
    {
        if (SelectedConstruction is null)
        {
            SelectedConstructionCard = null;
            return;
        }

        var match = PagedConstructionCards.FirstOrDefault(c => c.Id == SelectedConstruction.Id)
            ?? _allConstructionCards.FirstOrDefault(c => c.Id == SelectedConstruction.Id);
        if (match is not null && !ReferenceEquals(SelectedConstructionCard, match))
        {
            SelectedConstructionCard = match;
        }
    }

    private List<OrderConstructionLookupItemViewModel> GetFilteredConstructionCards()
    {
        if (string.IsNullOrWhiteSpace(ConstructionSearchText))
        {
            return _allConstructionCards;
        }

        var query = ConstructionSearchText.Trim();
        return _allConstructionCards
            .Where(c =>
                c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || c.DimensionsDisplay.Contains(query, StringComparison.OrdinalIgnoreCase)
                || c.QuantityDisplay.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void GoToLastPage()
    {
        var count = GetFilteredConstructionCards().Count;
        CurrentPage = Math.Max(1, (int)Math.Ceiling(count / (double)PageSize));
    }

    private void NotifyPaginationProperties()
    {
        OnPropertyChanged(nameof(CanGoToPreviousPage));
        OnPropertyChanged(nameof(CanGoToNextPage));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PageIndicatorText));
        GoToPreviousPageCommand.NotifyCanExecuteChanged();
        GoToNextPageCommand.NotifyCanExecuteChanged();
    }

    public void RecalculateAll()
    {
        _calculationService.Recalculate(Order);
        RebuildMaterialLines();
        NotifyFinancialProperties();
        RefreshConstructionList();
        RecalculateOrderTotals();
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
        OnPropertyChanged(nameof(ActiveConstructionModel));
        OnPropertyChanged(nameof(ActiveProfileColorDisplay));
        OnPropertyChanged(nameof(ActiveWhiteOrientation));
        OnPropertyChanged(nameof(ActiveDimensionsDisplay));
        OnPropertyChanged(nameof(ProfileType));
        OnPropertyChanged(nameof(ProfileColor));
        OnPropertyChanged(nameof(GlassPacket));
        OnPropertyChanged(nameof(Hardware));
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
        DuplicateConstructionCommand.NotifyCanExecuteChanged();
        EditConstructionCommand.NotifyCanExecuteChanged();
        DeleteConstructionCommand.NotifyCanExecuteChanged();
    }

    private void OnDocumentStateChanged(object? sender, EventArgs e)
    {
        if (!IsUnsavedChanges)
        {
            _lastAutosaveAt = DateTimeOffset.Now;
        }

        NotifyStatusProperties();
    }

    private void OnPaymentPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AmountPaid));
        OnPropertyChanged(nameof(SelectedCurrency));
        NotifyPaymentDerivedProperties();
        OnPropertyChanged(nameof(PaymentPercent));
        OnPropertyChanged(nameof(LastPaymentDisplay));
        OnPropertyChanged(nameof(HasPaymentData));
        OnPropertyChanged(nameof(PaymentEmptyHint));
    }

    private void OnTotalsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        NotifyFinancialProperties();
        RecalculateOrderTotals();
    }

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
