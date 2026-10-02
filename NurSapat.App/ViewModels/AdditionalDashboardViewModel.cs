using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Interfaces.Currency;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using NurSapat.Cad.Core.Viewport;

namespace NurSapat.App.ViewModels;

public sealed partial class AdditionalDashboardViewModel : ObservableObject
{
    private static readonly (string Title, string ShortTitle)[] StageDefinitions =
    [
        ("Коммерческое предложение", "КП"),
        ("Замер", "Замер"),
        ("Договор", "Договор"),
        ("Оплата", "Оплата"),
        ("Производство", "Производство"),
        ("Покраска", "Покраска"),
        ("Сборка", "Сборка"),
        ("Монтаж", "Монтаж"),
        ("Завершено", "Завершено")
    ];

    private static readonly string[] DefaultCurrencyCodes = ["KGS", "USD", "EUR", "RUB"];

    private static readonly Dictionary<string, string> CurrencyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["KGS"] = "Кыргызский сом",
        ["USD"] = "Доллар США",
        ["EUR"] = "Евро",
        ["RUB"] = "Российский рубль",
        ["CNY"] = "Китайский юань"
    };

    private readonly CadEditorViewModel _cadEditor;
    private readonly ICurrencyService _currencyService;
    private readonly HashSet<string> _addedCurrencyCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _manuallyEditedCurrencyCodes = new(StringComparer.OrdinalIgnoreCase);
    private CurrencySyncResult? _lastSyncResult;
    private DateTime _lastModifiedAt = DateTime.Now;

    public OrderCalculationState OrderState { get; }

    public ObservableCollection<OrderStageCard> Stages { get; } = [];

    public ObservableCollection<OrderTemplateOption> Templates { get; } = [];

    public ObservableCollection<CurrencyRateDisplayModel> CurrencyRates { get; } = [];

    public ObservableCollection<OrderTemplateImpactItem> TemplateImpacts { get; } = [];

    public ObservableCollection<OrderProfileField> ProfileFields { get; } = [];

    public ObservableCollection<OrderAutomationToggle> AutomationRules { get; } = [];

    public ObservableCollection<OrderHistoryEvent> HistoryEvents { get; } = [];

    public ObservableCollection<OrderFileCard> Files { get; } = [];

    public ObservableCollection<OrderCommentMessage> Comments { get; } = [];

    public ObservableCollection<OrderReminderItem> Reminders { get; } = [];

    public ObservableCollection<OrderChecklistItem> ChecklistItems { get; } = [];

    [ObservableProperty] private OrderTemplateOption? _selectedTemplate;

    [ObservableProperty] private OrderStageCard? _selectedStage;

    [ObservableProperty] private bool _isOrderLocked;

    [ObservableProperty] private bool _autoSyncTemplate;

    [ObservableProperty] private bool _isCurrencyStatusVisible;

    [ObservableProperty] private bool _isLoadingRates;

    [ObservableProperty] private bool _showRecalculatePrompt;

    [ObservableProperty] private int _selectedDetailTabIndex;

    [ObservableProperty] private string _newCommentText = "";

    [ObservableProperty] private string _orderGuid = Guid.NewGuid().ToString();

    public string OrderNumberDisplay => string.IsNullOrWhiteSpace(OrderState.OrderNumber)
        ? "Новый заказ"
        : $"Заказ №{OrderState.OrderNumber}";

    public string CustomerNameDisplay => string.IsNullOrWhiteSpace(OrderState.CustomerName)
        ? "Не выбран"
        : OrderState.CustomerName;

    public string ManagerName => "Иванов И.И.";

    public string MeasurerName => "Петров П.П.";

    public string ProductionTeam => "Цех №1";

    public string OrderSource => "Офис";

    public string OrderVersion => "1.0";

    public string ApiStatus => OrderState.CurrencyStatus;

    public string RepeatCustomerText => "Нет";

    public string LastSyncText => DateTime.Now.ToString("dd.MM.yyyy HH:mm");

    public string CreatedDateText => OrderState.OrderDate.ToString("dd.MM.yyyy");

    public string ModifiedDateText => _lastModifiedAt.ToString("dd.MM.yyyy HH:mm");

    public string LastModifiedAgo => FormatRelativeTime(_lastModifiedAt);

    public string OrderStatusBadge => OrderStatus;

    public string OrderStatusEmoji => OrderStatus switch
    {
        "Ожидает оплаты" => "🟡",
        "Производство" => "🔵",
        "Завершён" => "⚪",
        _ => "🟢"
    };

    public string OrderStatus => ResolveOrderStatus();

    public string OrderStatusBackground => OrderStatus switch
    {
        "Ожидает оплаты" => "#FEF3E8",
        "Производство" => "#EAF2FD",
        "Завершён" => "#F1F5F9",
        _ => "#E8F7EF"
    };

    public string OrderStatusForeground => OrderStatus switch
    {
        "Ожидает оплаты" => "#B45309",
        "Производство" => "#2F80ED",
        "Завершён" => "#64748B",
        _ => "#27AE60"
    };

    public string TotalCostText => $"{OrderState.TotalCost:N0} сом";

    public string PaidAmountText => $"{OrderState.Prepayment:N0} сом";

    public string RemainingAmountText => $"{OrderState.Debt:N0} сом";

    public string DiscountText => $"{OrderState.Discount:N0} сом";

    public string CostPriceText => $"{OrderState.EstimatedMaterialCost:N0} сом";

    public string ProfitText => $"{Math.Max(0, OrderState.TotalCost - OrderState.EstimatedMaterialCost):N0} сом";

    public string MarginText => OrderState.TotalCost <= 0
        ? "0%"
        : $"{Math.Round((double)(OrderState.TotalCost - OrderState.EstimatedMaterialCost) / (double)OrderState.TotalCost * 100):0}%";

    public string RemainingAmountForeground => OrderState.Debt > 0 ? "#EB5757" : "#27AE60";

    public string PaidAmountForeground => OrderState.Prepayment > 0 ? "#27AE60" : "#64748B";

    public int ReadinessPercent => ChecklistItems.Count == 0
        ? 0
        : (int)Math.Round(ChecklistItems.Count(item => item.Status == OrderCheckStatus.Ok) * 100d / ChecklistItems.Count);

    public string ReadinessChecksText =>
        $"{ChecklistItems.Count(item => item.Status == OrderCheckStatus.Ok)} из {ChecklistItems.Count} требований";

    public bool HasBlockingChecklistErrors => ChecklistItems.Any(item => item.Status == OrderCheckStatus.Error);

    public bool CanAdvanceSelectedStage => SelectedStage is not null
        && !IsOrderLocked
        && !HasBlockingChecklistErrors
        && SelectedStage.Status != "Выполнено";

    public string CurrencyStatusText => _lastSyncResult?.Status switch
    {
        CurrencySyncStatus.Imported or CurrencySyncStatus.UpToDate => "Обновлено автоматически",
        CurrencySyncStatus.Offline => "Офлайн-режим",
        _ => "Загрузка курсов..."
    };

    public string CurrencyStatusBackground => _lastSyncResult?.Status switch
    {
        CurrencySyncStatus.Imported or CurrencySyncStatus.UpToDate => "#E8F7EF",
        CurrencySyncStatus.Offline => "#FEF3E8",
        _ => "#F1F5F9"
    };

    public string CurrencyStatusForeground => _lastSyncResult?.Status switch
    {
        CurrencySyncStatus.Imported or CurrencySyncStatus.UpToDate => "#27AE60",
        CurrencySyncStatus.Offline => "#B45309",
        _ => "#64748B"
    };

    public string[] DetailTabs => ["История", "Файлы", "Комментарии", "Напоминания", "Контроль", "Финансы"];

    public AdditionalDashboardViewModel(
        OrderCalculationState orderState,
        CadEditorViewModel cadEditor,
        ICurrencyService currencyService)
    {
        OrderState = orderState;
        _cadEditor = cadEditor;
        _currencyService = currencyService;

        EnsureTemplateImpacts();
        EnsureAutomationRules();
        EnsureSampleContent();
        SeedDemoOrderDataIfNeeded();
        RebuildTemplates();
        SelectedTemplate = Templates.FirstOrDefault();
        EnsureStages();
        UpdateStageStatuses();
        RebuildProfileFields();
        RebuildChecklist();
        RebuildHistory();

        OrderState.PropertyChanged += OnOrderStateChanged;
        OrderState.ActivityLog.CollectionChanged += OnActivityLogChanged;
        _ = LoadRatesAsync();
    }

    [RelayCommand]
    private async Task RefreshRatesAsync()
    {
        await LoadRatesAsync();
        ShowRecalculatePrompt = true;
        TouchModified();
        OrderState.AddActivity("Курсы валют обновлены", "Доступен пересчёт заказа", ManagerName);
        RebuildHistory();
    }

    [RelayCommand]
    private void RecalculateOrder()
    {
        OrderState.UpdateFrom(_cadEditor.Document);
        ShowRecalculatePrompt = false;
        TouchModified();
        OrderState.AddActivity("Заказ пересчитан", $"Сумма: {OrderState.TotalCost:N0} сом", ManagerName);
        UpdateStageStatuses();
        RebuildChecklist();
        RebuildHistory();
        NotifyFinance();
    }

    [RelayCommand]
    private void SyncOrderWithTemplate()
    {
        if (SelectedTemplate == null)
        {
            return;
        }

        var vp = _cadEditor.Viewport;
        var center = vp.ScreenToWorld(new ScreenPoint(vp.Width / 2, vp.Height / 2));
        var width = SelectedTemplate.Width;
        var height = SelectedTemplate.Height;
        var rect = new CadRect(center.X - width / 2, center.Y + height / 2, width, height);
        _cadEditor.Document.AddRectangle(rect);
        _cadEditor.RefreshSelection();
        OrderState.UpdateFrom(_cadEditor.Document);
        _cadEditor.FitDocumentToViewport();
        OrderState.AddActivity("Шаблон применён", SelectedTemplate.Title, ManagerName);
        TouchModified();
        UpdateStageStatuses();
        RebuildProfileFields();
        RebuildChecklist();
        RebuildHistory();
        NotifyFinance();
    }

    [RelayCommand]
    private void SelectStage(OrderStageCard? stage)
    {
        if (stage is null)
        {
            return;
        }

        foreach (var item in Stages)
        {
            item.IsSelected = item == stage;
        }

        SelectedStage = stage;
        OnPropertyChanged(nameof(CanAdvanceSelectedStage));
    }

    [RelayCommand]
    private void AdvanceStage()
    {
        if (SelectedStage is null || !CanAdvanceSelectedStage)
        {
            return;
        }

        ExecuteStageBusinessLogic(SelectedStage);
        SelectedStage.IsCompleted = true;
        SelectedStage.Status = "Выполнено";
        SelectedStage.EndDate = DateTime.Today;

        var nextStage = Stages.FirstOrDefault(stage => stage.Number == SelectedStage.Number + 1);
        if (nextStage is not null)
        {
            nextStage.Status = "В работе";
            nextStage.StartDate ??= DateTime.Today;
            SelectStage(nextStage);
        }

        OrderState.AddActivity("Этап завершён", SelectedStage.Title, ManagerName);
        TouchModified();
        UpdateStageStatuses();
        RebuildChecklist();
        RebuildHistory();
        NotifyFinance();
        OnPropertyChanged(nameof(CanAdvanceSelectedStage));
    }

    [RelayCommand]
    private void CancelStage()
    {
        if (SelectedStage is null || IsOrderLocked)
        {
            return;
        }

        SelectedStage.IsCompleted = false;
        SelectedStage.Status = "Ожидание";
        SelectedStage.EndDate = null;
        OrderState.AddActivity("Этап отменён", SelectedStage.Title, ManagerName);
        TouchModified();
        UpdateStageStatuses();
        RebuildHistory();
        OnPropertyChanged(nameof(CanAdvanceSelectedStage));
    }

    [RelayCommand]
    private void ConfigureAutomation()
    {
        OrderState.AddActivity("Автоматизация", "Открыта настройка правил автоматизации", ManagerName);
        RebuildHistory();
    }

    [RelayCommand]
    private void EditProfile()
    {
        OrderState.AddActivity("Профиль изделия", "Открыто редактирование профиля", ManagerName);
        RebuildHistory();
    }

    [RelayCommand]
    private void SendComment()
    {
        if (string.IsNullOrWhiteSpace(NewCommentText))
        {
            return;
        }

        Comments.Insert(0, new OrderCommentMessage(ManagerName, NewCommentText.Trim(), DateTime.Now));
        OrderState.AddActivity("Комментарий", NewCommentText.Trim(), ManagerName);
        NewCommentText = string.Empty;
        TouchModified();
        RebuildHistory();
    }

    [RelayCommand]
    private void ToggleReminder(OrderReminderItem? reminder)
    {
        if (reminder is null)
        {
            return;
        }

        reminder.IsCompleted = !reminder.IsCompleted;
    }

    [RelayCommand]
    private void AddSampleFile()
    {
        Files.Insert(0, new OrderFileCard(
            $"Документ_{DateTime.Now:HHmmss}.pdf",
            "PDF",
            "248 КБ",
            "Документы"));
        OrderState.AddActivity("Файл добавлен", "PDF документ", ManagerName);
        RebuildHistory();
    }

    [RelayCommand]
    private async Task AddCurrencyAsync()
    {
        const string code = "CNY";
        if (_addedCurrencyCodes.Contains(code))
        {
            return;
        }

        _addedCurrencyCodes.Add(code);
        await LoadRatesAsync();
        OrderState.AddActivity("Добавлена валюта", code, ManagerName);
        RebuildHistory();
    }

    partial void OnSelectedTemplateChanged(OrderTemplateOption? value)
    {
        if (AutoSyncTemplate && value is not null)
        {
            SyncOrderWithTemplate();
        }
    }

    partial void OnSelectedStageChanged(OrderStageCard? value) =>
        OnPropertyChanged(nameof(CanAdvanceSelectedStage));

    public bool IsHistoryTabSelected => SelectedDetailTabIndex == 0;

    public bool IsFilesTabSelected => SelectedDetailTabIndex == 1;

    public bool IsCommentsTabSelected => SelectedDetailTabIndex == 2;

    public bool IsRemindersTabSelected => SelectedDetailTabIndex == 3;

    public bool IsControlTabSelected => SelectedDetailTabIndex == 4;

    public bool IsFinanceTabSelected => SelectedDetailTabIndex == 5;

    [RelayCommand]
    private void SelectDetailTab(string? tabIndex)
    {
        if (int.TryParse(tabIndex, out var index) && index is >= 0 and <= 5)
        {
            SelectedDetailTabIndex = index;
        }
    }

    partial void OnSelectedDetailTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsHistoryTabSelected));
        OnPropertyChanged(nameof(IsFilesTabSelected));
        OnPropertyChanged(nameof(IsCommentsTabSelected));
        OnPropertyChanged(nameof(IsRemindersTabSelected));
        OnPropertyChanged(nameof(IsControlTabSelected));
        OnPropertyChanged(nameof(IsFinanceTabSelected));
    }

    public async Task LoadRatesAsync()
    {
        if (IsLoadingRates)
        {
            return;
        }

        IsLoadingRates = true;
        CurrencyRates.Clear();

        try
        {
            _lastSyncResult = await _currencyService.CheckAndSyncRatesAsync();
            var orderDate = OrderState.OrderDate.Date;
            var codes = BuildCurrencyCodeSet();
            var displayRows = new List<CurrencyRateDisplayModel>(codes.Count);

            foreach (var code in codes)
            {
                var row = await BuildRateRowAsync(code, orderDate);
                if (row is not null)
                {
                    displayRows.Add(row);
                }
            }

            foreach (var row in SortCurrencyRows(displayRows))
            {
                CurrencyRates.Add(row);
            }

            SyncOrderStateExchangeRates(CurrencyRates);
        }
        catch (Exception ex)
        {
            _lastSyncResult = new CurrencySyncResult(CurrencySyncStatus.Offline, null, null);
            System.Diagnostics.Debug.WriteLine($"LoadRatesAsync: {ex.Message}");
        }
        finally
        {
            IsLoadingRates = false;
            NotifyCurrencyStatus();
            NotifyFinance();
        }
    }

    private void ExecuteStageBusinessLogic(OrderStageCard stage)
    {
        stage.AutomationActions.Clear();

        switch (stage.ShortTitle)
        {
            case "Производство":
                stage.AutomationActions.Add("Создать производственное задание");
                stage.AutomationActions.Add("Зарезервировать материалы");
                stage.AutomationActions.Add("Отправить уведомление");
                stage.AutomationActions.Add("Изменить статус заказа");
                OrderState.ProductionDate = DateTime.Today;
                RunEnabledAutomation("Производство");
                break;
            case "Оплата":
                if (OrderState.Prepayment <= 0 && OrderState.TotalCost > 0)
                {
                    OrderState.Prepayment = Math.Min(OrderState.TotalCost, OrderState.TotalCost * 0.3m);
                }

                RunEnabledAutomation("Оплата");
                break;
            case "Договор":
                if (string.IsNullOrWhiteSpace(OrderState.ContractNumber))
                {
                    OrderState.ContractNumber = $"Д-{DateTime.Today:yyyyMMdd}";
                    OrderState.ContractDate = DateTime.Today;
                }

                RunEnabledAutomation("Договор");
                break;
            default:
                RunEnabledAutomation(stage.ShortTitle);
                break;
        }
    }

    private void RunEnabledAutomation(string context)
    {
        foreach (var rule in AutomationRules.Where(rule => rule.IsEnabled))
        {
            OrderState.AddActivity("Автоматизация", $"{rule.Title} ({context})", "Система");
        }
    }

    private void EnsureTemplateImpacts()
    {
        TemplateImpacts.Clear();
        TemplateImpacts.Add(new OrderTemplateImpactItem("этапы"));
        TemplateImpacts.Add(new OrderTemplateImpactItem("документы"));
        TemplateImpacts.Add(new OrderTemplateImpactItem("автоматизация"));
        TemplateImpacts.Add(new OrderTemplateImpactItem("валюта"));
        TemplateImpacts.Add(new OrderTemplateImpactItem("настройки"));
    }

    private void EnsureAutomationRules()
    {
        AutomationRules.Clear();
        AutomationRules.Add(new OrderAutomationToggle("После оплаты отправить в производство"));
        AutomationRules.Add(new OrderAutomationToggle("Создать договор", true));
        AutomationRules.Add(new OrderAutomationToggle("Создать счёт", true));
        AutomationRules.Add(new OrderAutomationToggle("Зарезервировать материалы", true));
        AutomationRules.Add(new OrderAutomationToggle("Уведомить менеджера", true));
        AutomationRules.Add(new OrderAutomationToggle("Уведомить производство"));
        AutomationRules.Add(new OrderAutomationToggle("Напечатать документы"));
        AutomationRules.Add(new OrderAutomationToggle("Отправить WhatsApp"));
        AutomationRules.Add(new OrderAutomationToggle("Отправить SMS"));
    }

    private void SeedDemoOrderDataIfNeeded()
    {
        if (string.IsNullOrWhiteSpace(OrderState.OrderNumber))
        {
            OrderState.OrderNumber = "2026-07-0137";
        }

        if (string.IsNullOrWhiteSpace(OrderState.CustomerName))
        {
            OrderState.CustomerName = "Иванов";
        }

        if (OrderState.TotalCost <= 0)
        {
            OrderState.EstimatedMaterialCost = 154_000m;
            OrderState.Discount = 4_000m;
            OrderState.Prepayment = 50_000m;
        }

        if (OrderState.ActivityLog.Count <= 1)
        {
            OrderState.AddActivity("Изменена цена", "154000 → 150000", ManagerName);
            OrderState.AddActivity("Добавлено окно", "Петров расширил спецификацию", "Петров");
            OrderState.AddActivity("Создан договор", "Договор №Д-20260713", "Система");
            OrderState.AddActivity("Клиент оплатил", "50000", ManagerName);
            OrderState.AddActivity("Заказ переведён в производство", "Этап производства активирован", "Система");
        }
    }

    private void EnsureSampleContent()
    {
        Files.Clear();
        Files.Add(new OrderFileCard("Договор_2026-07-0137.pdf", "PDF", "412 КБ", "Документы"));
        Files.Add(new OrderFileCard("КП_окна.pdf", "PDF", "286 КБ", "Документы"));
        Files.Add(new OrderFileCard("Замер_фото.jpg", "JPG", "1.2 МБ", "Фото"));
        Files.Add(new OrderFileCard("Чертеж_фасад.dwg", "DWG", "3.4 МБ", "Чертежи"));
        Files.Add(new OrderFileCard("Спецификация.dxf", "DXF", "980 КБ", "Чертежи"));

        Comments.Clear();
        Comments.Add(new OrderCommentMessage(
            "Система",
            "Заказ создан и готов к расчёту.",
            DateTime.Now.AddMinutes(-45),
            true));
        Comments.Add(new OrderCommentMessage(
            ManagerName,
            "Клиент просит ускорить производство.",
            DateTime.Now.AddMinutes(-20)));
        Comments.Add(new OrderCommentMessage(
            "Петров П.П.",
            "Замер подтверждён, размеры совпадают.",
            DateTime.Now.AddMinutes(-8)));

        Reminders.Clear();
        Reminders.Add(new OrderReminderItem("Позвонить клиенту", "Через 2 дня"));
        Reminders.Add(new OrderReminderItem("Проверить оплату", "Через сутки"));
        Reminders.Add(new OrderReminderItem("Монтаж", OrderState.ProductionDate.ToString("d MMMM")));
    }

    private void RebuildProfileFields()
    {
        ProfileFields.Clear();
        ProfileFields.Add(new OrderProfileField("Профиль", string.IsNullOrWhiteSpace(OrderState.ProfileSystem) ? "VEKA Softline 70" : OrderState.ProfileSystem));
        ProfileFields.Add(new OrderProfileField("Толщина", "70 мм"));
        ProfileFields.Add(new OrderProfileField("Армирование", "1.5 мм"));
        ProfileFields.Add(new OrderProfileField("Стеклопакет", string.IsNullOrWhiteSpace(OrderState.GlassFormula) ? "4-16-4" : OrderState.GlassFormula));
        ProfileFields.Add(new OrderProfileField("Цвет", "Белый"));
        ProfileFields.Add(new OrderProfileField("Ламинация", string.IsNullOrWhiteSpace(OrderState.LaminationColor) ? "Нет" : OrderState.LaminationColor));
        ProfileFields.Add(new OrderProfileField("Фурнитура", string.IsNullOrWhiteSpace(OrderState.HardwareSummary) ? "Roto NT" : OrderState.HardwareSummary));
    }

    private void RebuildChecklist()
    {
        ChecklistItems.Clear();
        ChecklistItems.Add(new OrderChecklistItem(
            "Клиент выбран",
            string.IsNullOrWhiteSpace(OrderState.CustomerName) ? OrderCheckStatus.Error : OrderCheckStatus.Ok));
        ChecklistItems.Add(new OrderChecklistItem(
            "Договор создан",
            string.IsNullOrWhiteSpace(OrderState.ContractNumber) ? OrderCheckStatus.Warning : OrderCheckStatus.Ok));
        ChecklistItems.Add(new OrderChecklistItem(
            "Замер подтверждён",
            Stages.FirstOrDefault(stage => stage.ShortTitle == "Замер")?.IsCompleted == true
                ? OrderCheckStatus.Ok
                : OrderCheckStatus.Warning));
        ChecklistItems.Add(new OrderChecklistItem(
            "Предоплата отсутствует",
            OrderState.Prepayment > 0 ? OrderCheckStatus.Ok : OrderCheckStatus.Warning,
            OrderState.Prepayment > 0 ? "Оплачено" : "Требуется предоплата"));
        ChecklistItems.Add(new OrderChecklistItem(
            "Материалы не зарезервированы",
            Stages.FirstOrDefault(stage => stage.ShortTitle == "Производство")?.IsCompleted == true
                ? OrderCheckStatus.Ok
                : OrderCheckStatus.Warning));
        ChecklistItems.Add(new OrderChecklistItem(
            "Производство ещё не начато",
            Stages.FirstOrDefault(stage => stage.ShortTitle == "Производство") is { IsCompleted: true }
                ? OrderCheckStatus.Ok
                : OrderCheckStatus.Error));

        OnPropertyChanged(nameof(ReadinessPercent));
        OnPropertyChanged(nameof(ReadinessChecksText));
        OnPropertyChanged(nameof(HasBlockingChecklistErrors));
        OnPropertyChanged(nameof(CanAdvanceSelectedStage));
    }

    private void RebuildHistory()
    {
        HistoryEvents.Clear();
        foreach (var entry in OrderState.ActivityLog)
        {
            HistoryEvents.Add(new OrderHistoryEvent(
                entry.Timestamp,
                entry.Title,
                entry.Details,
                entry.User,
                ResolveHistoryCategory(entry.Title)));
        }
    }

    private static OrderHistoryCategory ResolveHistoryCategory(string title)
    {
        if (title.Contains("оплат", StringComparison.OrdinalIgnoreCase)
            || title.Contains("сумм", StringComparison.OrdinalIgnoreCase)
            || title.Contains("цен", StringComparison.OrdinalIgnoreCase))
        {
            return title.Contains("оплат", StringComparison.OrdinalIgnoreCase)
                ? OrderHistoryCategory.Payment
                : OrderHistoryCategory.Change;
        }

        if (title.Contains("договор", StringComparison.OrdinalIgnoreCase)
            || title.Contains("счёт", StringComparison.OrdinalIgnoreCase)
            || title.Contains("счет", StringComparison.OrdinalIgnoreCase))
        {
            return OrderHistoryCategory.Document;
        }

        if (title.Contains("этап", StringComparison.OrdinalIgnoreCase)
            || title.Contains("производ", StringComparison.OrdinalIgnoreCase))
        {
            return OrderHistoryCategory.Stage;
        }

        if (title.Contains("коммент", StringComparison.OrdinalIgnoreCase))
        {
            return OrderHistoryCategory.Comment;
        }

        return OrderHistoryCategory.System;
    }

    private void OnActivityLogChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildHistory();

    private void OnOrderStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OrderCalculationState.TotalCost)
            or nameof(OrderCalculationState.Prepayment)
            or nameof(OrderCalculationState.Debt)
            or nameof(OrderCalculationState.EstimatedMaterialCost)
            or nameof(OrderCalculationState.ProductionDate)
            or nameof(OrderCalculationState.OrderDate)
            or nameof(OrderCalculationState.CustomerName)
            or nameof(OrderCalculationState.ContractNumber)
            or nameof(OrderCalculationState.OrderNumber))
        {
            RebuildTemplates();
            UpdateStageStatuses();
            RebuildProfileFields();
            RebuildChecklist();
            NotifyOrderHeader();
            NotifyFinance();

            if (e.PropertyName == nameof(OrderCalculationState.OrderDate))
            {
                _ = LoadRatesAsync();
            }
        }
    }

    private async Task<CurrencyRateDisplayModel?> BuildRateRowAsync(string code, DateTime orderDate)
    {
        try
        {
            var record = await _currencyService.FindRateRecordAsync(code, orderDate);
            if (record is null)
            {
                return null;
            }

            var amount = code.Equals("KGS", StringComparison.OrdinalIgnoreCase)
                ? OrderState.TotalCost
                : OrderState.TotalCost > 0 && record.Rate > 0
                    ? decimal.Round(OrderState.TotalCost / record.Rate, 2)
                    : 0m;

            return new CurrencyRateDisplayModel
            {
                Code = code,
                Name = ResolveCurrencyName(code),
                Rate = record.Rate,
                RateDate = record.RateDate,
                ActionType = ResolveActionType(code, record),
                OrderAmount = amount
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BuildRateRowAsync({code}): {ex.Message}");
            return null;
        }
    }

    private string ResolveActionType(string code, CurrencyRateRecord record)
    {
        if (code.Equals("KGS", StringComparison.OrdinalIgnoreCase))
        {
            return "Базовая валюта";
        }

        if (_manuallyEditedCurrencyCodes.Contains(code))
        {
            return "Ручной ввод";
        }

        return _lastSyncResult?.Status switch
        {
            CurrencySyncStatus.Imported or CurrencySyncStatus.UpToDate => "НБКР (Авто)",
            CurrencySyncStatus.Offline => "НБКР (База)",
            _ => "Ручной ввод"
        };
    }

    private HashSet<string> BuildCurrencyCodeSet()
    {
        var codes = new HashSet<string>(DefaultCurrencyCodes, StringComparer.OrdinalIgnoreCase);
        foreach (var code in _addedCurrencyCodes)
        {
            codes.Add(code);
        }

        return codes;
    }

    private static IEnumerable<CurrencyRateDisplayModel> SortCurrencyRows(
        IEnumerable<CurrencyRateDisplayModel> rows) =>
        rows
            .OrderBy(row => row.Code.Equals("KGS", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(row => row.Code, StringComparer.OrdinalIgnoreCase);

    private static string ResolveCurrencyName(string code) =>
        CurrencyNames.TryGetValue(code, out var name) ? name : code;

    private void SyncOrderStateExchangeRates(IEnumerable<CurrencyRateDisplayModel> rows)
    {
        OrderState.ExchangeRates.Clear();
        foreach (var row in rows)
        {
            OrderState.ExchangeRates.Add(new ExchangeRateLine(
                row.Code,
                row.Name,
                row.Rate,
                row.RateDate,
                row.ActionType));
        }
    }

    private void NotifyCurrencyStatus()
    {
        IsCurrencyStatusVisible = _lastSyncResult?.Status is CurrencySyncStatus.Imported
            or CurrencySyncStatus.UpToDate
            or CurrencySyncStatus.Offline;
        OnPropertyChanged(nameof(CurrencyStatusText));
        OnPropertyChanged(nameof(CurrencyStatusBackground));
        OnPropertyChanged(nameof(CurrencyStatusForeground));
    }

    private void RebuildTemplates()
    {
        var selectedTitle = SelectedTemplate?.Title;
        Templates.Clear();

        var rects = _cadEditor.Document.Rectangles;
        if (rects.Count > 0)
        {
            var rect = rects[0];
            Templates.Add(new OrderTemplateOption(
                $"Прямоугольник {rect.Width:0}x{rect.Height:0}",
                rect.Width,
                rect.Height));
        }

        Templates.Add(new OrderTemplateOption("Стандарт 1000×1000", 1000, 1000));
        Templates.Add(new OrderTemplateOption("Стандарт 1500×1400", 1500, 1400));

        SelectedTemplate = Templates.FirstOrDefault(template => template.Title == selectedTitle)
            ?? Templates.FirstOrDefault();
    }

    private void EnsureStages()
    {
        if (Stages.Count == StageDefinitions.Length)
        {
            return;
        }

        Stages.Clear();
        for (var i = 0; i < StageDefinitions.Length; i++)
        {
            var (title, shortTitle) = StageDefinitions[i];
            var stage = new OrderStageCard(i + 1, title, shortTitle)
            {
                Responsible = shortTitle switch
                {
                    "КП" or "Договор" or "Оплата" => ManagerName,
                    "Замер" => MeasurerName,
                    "Производство" or "Покраска" or "Сборка" => ProductionTeam,
                    "Монтаж" => "Бригада монтажа",
                    _ => ManagerName
                }
            };

            stage.PropertyChanged += OnStageChanged;
            Stages.Add(stage);
        }

        SelectStage(Stages.First());
    }

    private void UpdateStageStatuses()
    {
        SetStage(1, !string.IsNullOrWhiteSpace(OrderState.OrderNumber) || OrderState.TotalCost > 0, "Выполнено", OrderState.OrderDate, OrderState.OrderDate);
        SetStage(2, Stages.ElementAtOrDefault(1)?.IsCompleted == true || OrderState.ItemArea > 0, Stages.ElementAtOrDefault(1)?.IsCompleted == true ? "Выполнено" : OrderState.ItemArea > 0 ? "В работе" : "Не начато", OrderState.OrderDate, null);
        SetStage(3, !string.IsNullOrWhiteSpace(OrderState.ContractNumber), !string.IsNullOrWhiteSpace(OrderState.ContractNumber) ? "Выполнено" : "В работе", OrderState.ContractDate, OrderState.ContractDate);
        SetStage(
            4,
            OrderState.Debt <= 0 && OrderState.TotalCost > 0,
            OrderState.Debt <= 0 && OrderState.TotalCost > 0
                ? "Выполнено"
                : OrderState.Prepayment > 0
                    ? "В работе"
                    : OrderState.TotalCost > 0
                        ? "Ожидание"
                        : "Не начато",
            OrderState.ContractDate,
            OrderState.Debt <= 0 && OrderState.TotalCost > 0 ? DateTime.Today : null);
        SetStage(5, OrderState.ProductionDate <= DateTime.Today && Stages.ElementAtOrDefault(4)?.IsCompleted == true, Stages.ElementAtOrDefault(4)?.IsCompleted == true ? "Выполнено" : OrderState.TotalCost > 0 ? "Ожидание" : "Не начато", OrderState.ProductionDate, null);
        SetStage(6, false, "Не начато", null, null);
        SetStage(7, false, "Не начато", null, null);
        SetStage(8, OrderState.HasInstallation, OrderState.HasInstallation ? "Ожидание" : "Не начато", OrderState.ProductionDate, null);
        SetStage(9, false, "Не начато", null, null);

        var activeAssigned = false;
        foreach (var stage in Stages)
        {
            stage.IsOverdue = stage.Status is "В работе" or "Ожидание"
                && stage.StartDate is { } start
                && start.Date < DateTime.Today.AddDays(-3);
            if (stage.IsOverdue && stage.Status != "Выполнено")
            {
                stage.Status = "Просрочено";
            }

            if (!activeAssigned && stage.Status is "В работе" or "Ожидание" or "Просрочено")
            {
                stage.TimelineState = OrderTimelineState.Active;
                activeAssigned = true;
            }
            else if (stage.IsCompleted || stage.Status == "Выполнено")
            {
                stage.TimelineState = OrderTimelineState.Completed;
            }
            else
            {
                stage.TimelineState = OrderTimelineState.Pending;
            }
        }

        SelectedStage ??= Stages.FirstOrDefault(stage => stage.TimelineState == OrderTimelineState.Active)
            ?? Stages.FirstOrDefault();

        if (SelectedStage is not null)
        {
            foreach (var stage in Stages)
            {
                stage.IsSelected = stage == SelectedStage;
            }
        }

        NotifyOrderHeader();
        RebuildChecklist();
    }

    private void SetStage(
        int number,
        bool isCompleted,
        string status,
        DateTime? startDate,
        DateTime? endDate)
    {
        var stage = Stages.FirstOrDefault(item => item.Number == number);
        if (stage is null)
        {
            return;
        }

        stage.IsCompleted = isCompleted;
        stage.Status = isCompleted ? "Выполнено" : status;
        stage.StartDate = startDate;
        stage.EndDate = isCompleted ? endDate ?? DateTime.Today : endDate;
    }

    private string ResolveOrderStatus()
    {
        if (Stages.LastOrDefault()?.IsCompleted == true)
        {
            return "Завершён";
        }

        if (Stages.FirstOrDefault(stage => stage.ShortTitle == "Производство")?.Status is "В работе" or "Ожидание")
        {
            return "Производство";
        }

        if (OrderState.Debt > 0 && OrderState.TotalCost > 0)
        {
            return "Ожидает оплаты";
        }

        return "В работе";
    }

    private void NotifyOrderHeader()
    {
        OnPropertyChanged(nameof(OrderNumberDisplay));
        OnPropertyChanged(nameof(CustomerNameDisplay));
        OnPropertyChanged(nameof(OrderStatus));
        OnPropertyChanged(nameof(OrderStatusBadge));
        OnPropertyChanged(nameof(OrderStatusEmoji));
        OnPropertyChanged(nameof(OrderStatusBackground));
        OnPropertyChanged(nameof(OrderStatusForeground));
        OnPropertyChanged(nameof(CreatedDateText));
        OnPropertyChanged(nameof(ModifiedDateText));
        OnPropertyChanged(nameof(LastModifiedAgo));
    }

    private void NotifyFinance()
    {
        OnPropertyChanged(nameof(TotalCostText));
        OnPropertyChanged(nameof(PaidAmountText));
        OnPropertyChanged(nameof(RemainingAmountText));
        OnPropertyChanged(nameof(DiscountText));
        OnPropertyChanged(nameof(CostPriceText));
        OnPropertyChanged(nameof(ProfitText));
        OnPropertyChanged(nameof(MarginText));
        OnPropertyChanged(nameof(RemainingAmountForeground));
        OnPropertyChanged(nameof(PaidAmountForeground));
    }

    private void TouchModified()
    {
        _lastModifiedAt = DateTime.Now;
        NotifyOrderHeader();
    }

    private void OnStageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OrderStageCard.IsCompleted) or nameof(OrderStageCard.Status))
        {
            RebuildChecklist();
            NotifyOrderHeader();
        }
    }

    private static string FormatRelativeTime(DateTime timestamp)
    {
        var delta = DateTime.Now - timestamp;
        if (delta.TotalMinutes < 1)
        {
            return "только что";
        }

        if (delta.TotalMinutes < 60)
        {
            return $"{(int)delta.TotalMinutes} минуты назад";
        }

        if (delta.TotalHours < 24)
        {
            return $"{(int)delta.TotalHours} часа назад";
        }

        return timestamp.ToString("dd.MM.yyyy HH:mm");
    }
}
