using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.UI.ViewModels;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using NurSapat.Cad.Core.Viewport;

namespace NurSapat.App.ViewModels;

public sealed partial class AdditionalDashboardViewModel : ObservableObject
{
    private static readonly string[] StageTitles =
    [
        "Коммерческое предложение",
        "Договор",
        "Оплата",
        "Заказ материалов",
        "Списание материалов",
        "Покраска",
        "Ламинация",
        "Гибка профиля",
        "Стеклопакеты",
        "Раскрой",
        "Сборка",
        "Доставка",
        "Монтаж",
        "Акт",
        "Закрыть заказ"
    ];

    private readonly CadEditorViewModel _cadEditor;

    public OrderCalculationState OrderState { get; }

    public ObservableCollection<OrderStageCard> Stages { get; } = new();

    public ObservableCollection<OrderTemplateOption> Templates { get; } = new();

    [ObservableProperty] private OrderTemplateOption? _selectedTemplate;

    [ObservableProperty] private bool _isOrderLocked;

    [ObservableProperty] private bool _autoSyncTemplate;

    [ObservableProperty] private bool _showFullActivityLog;

    [ObservableProperty] private string _note = "";

    public int NoteLength => Note.Length;

    public string ProgramName => "NurSapat CAD";

    public string DepartmentName => "Основное производство";

    public string DepartmentShort => "Основной цех";

    public string CurrentUserDisplay => "Иванов И.И.";

    public string OrderStatus => "В работе";

    public string TotalCostText => $"{OrderState.TotalCost:N0} сом";

    public string CreatedDateText => OrderState.OrderDate.ToString("dd.MM.yyyy");

    public string ModifiedDateText => DateTime.Today.ToString("dd.MM.yyyy");

    public bool IsCurrencyApiConnected => OrderState.ExchangeRates.Count > 1;

    public string CurrencyStatusText => IsCurrencyApiConnected
        ? "Обновлено автоматически"
        : "API не подключен";

    public string CurrencyStatusBackground => IsCurrencyApiConnected ? "#E8F7EF" : "#FEF3E8";

    public string CurrencyStatusForeground => IsCurrencyApiConnected ? "#27AE60" : "#F2994A";

    public string ActivityLogToggleText => ShowFullActivityLog ? "Свернуть журнал" : "Показать весь журнал";

    public IEnumerable<OrderActivityEvent> VisibleActivityLog => ShowFullActivityLog
        ? OrderState.ActivityLog
        : OrderState.ActivityLog.Take(6);

    public int TotalStages => Stages.Count;

    public int CompletedStages => Stages.Count(stage => stage.Status == "Выполнено");

    public int InProgressStages => Stages.Count(stage => stage.Status == "В работе");

    public int WaitingStages => Stages.Count(stage => stage.Status is "Ожидание" or "Не начато");

    public int OverdueStages => Stages.Count(stage => stage.IsOverdue);

    public AdditionalDashboardViewModel(OrderCalculationState orderState, CadEditorViewModel cadEditor)
    {
        OrderState = orderState;
        _cadEditor = cadEditor;
        RebuildTemplates();
        SelectedTemplate = Templates.FirstOrDefault();
        EnsureStages();
        UpdateStageStatuses();
        OrderState.PropertyChanged += OnOrderStateChanged;
        OrderState.ActivityLog.CollectionChanged += OnActivityLogChanged;
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
        OrderState.AddActivity("Заказ синхронизирован с шаблоном", SelectedTemplate.Title, CurrentUserDisplay);
        UpdateStageStatuses();
        NotifyOrderInfo();
    }

    [RelayCommand]
    private void RefreshOrder()
    {
        OrderState.UpdateFrom(_cadEditor.Document);
        OrderState.AddActivity(
            "Заказ обновлён",
            $"Сумма: {OrderState.TotalCost:N0} сом, площадь: {OrderState.ItemArea:0.###} м²",
            CurrentUserDisplay);
        UpdateStageStatuses();
        NotifyOrderInfo();
    }

    [RelayCommand]
    private void CompleteStage(OrderStageCard? stage)
    {
        if (stage == null || IsOrderLocked)
        {
            return;
        }

        stage.IsCompleted = true;
        stage.Status = "Выполнено";
        stage.EndDate = DateTime.Today;
        OrderState.AddActivity("Этап выполнен", stage.Title, CurrentUserDisplay);
        NotifyStageSummary();
    }

    [RelayCommand]
    private void EditStage(OrderStageCard? stage)
    {
        if (stage == null || IsOrderLocked)
        {
            return;
        }

        stage.Status = stage.Status switch
        {
            "Не начато" => "Ожидание",
            "Ожидание" => "В работе",
            "В работе" => "Выполнено",
            "Просрочено" => "В работе",
            "Выполнено" => "Не начато",
            _ => "В работе"
        };

        if (stage.Status == "В работе" && stage.StartDate is null)
        {
            stage.StartDate = DateTime.Today;
        }

        if (stage.Status == "Выполнено")
        {
            stage.IsCompleted = true;
            stage.EndDate = DateTime.Today;
        }
        else
        {
            stage.IsCompleted = false;
            stage.EndDate = null;
        }

        OrderState.AddActivity("Изменён этап", stage.Title, CurrentUserDisplay);
        NotifyStageSummary();
    }

    [RelayCommand]
    private void ToggleActivityLog() => ShowFullActivityLog = !ShowFullActivityLog;

    [RelayCommand]
    private void AddCurrency()
    {
        OrderState.ExchangeRates.Add(new ExchangeRateLine(
            "CNY",
            "Китайский юань",
            12.4m,
            DateTime.Today,
            "Ручной ввод"));
        OnPropertyChanged(nameof(IsCurrencyApiConnected));
        OnPropertyChanged(nameof(CurrencyStatusText));
        OnPropertyChanged(nameof(CurrencyStatusBackground));
        OnPropertyChanged(nameof(CurrencyStatusForeground));
        OrderState.AddActivity("Добавлена валюта", "CNY", CurrentUserDisplay);
    }

    partial void OnNoteChanged(string value)
    {
        OnPropertyChanged(nameof(NoteLength));
        if (!string.IsNullOrWhiteSpace(value))
        {
            OrderState.AddActivity(
                "Изменено примечание",
                value.Length > 80 ? value[..80] : value,
                CurrentUserDisplay);
        }
    }

    partial void OnSelectedTemplateChanged(OrderTemplateOption? value)
    {
        if (AutoSyncTemplate && value is not null)
        {
            SyncOrderWithTemplate();
        }
    }

    partial void OnShowFullActivityLogChanged(bool value)
    {
        OnPropertyChanged(nameof(VisibleActivityLog));
        OnPropertyChanged(nameof(ActivityLogToggleText));
    }

    private void OnActivityLogChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        OnPropertyChanged(nameof(VisibleActivityLog));

    private void OnOrderStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OrderCalculationState.TotalCost)
            or nameof(OrderCalculationState.Prepayment)
            or nameof(OrderCalculationState.Debt)
            or nameof(OrderCalculationState.EstimatedMaterialCost)
            or nameof(OrderCalculationState.ProductionDate)
            or nameof(OrderCalculationState.OrderDate))
        {
            RebuildTemplates();
            UpdateStageStatuses();
            NotifyOrderInfo();
        }
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
        if (Stages.Count == StageTitles.Length)
        {
            return;
        }

        Stages.Clear();
        for (var i = 0; i < StageTitles.Length; i++)
        {
            var stage = new OrderStageCard(i + 1, StageTitles[i]);
            stage.PropertyChanged += OnStageChanged;
            Stages.Add(stage);
        }
    }

    private void UpdateStageStatuses()
    {
        SetStage(1, !string.IsNullOrWhiteSpace(OrderState.OrderNumber), "Выполнено", OrderState.OrderDate, OrderState.OrderDate);
        SetStage(2, !string.IsNullOrWhiteSpace(OrderState.ContractNumber), !string.IsNullOrWhiteSpace(OrderState.ContractNumber) ? "Выполнено" : "В работе", OrderState.ContractDate, OrderState.ContractDate);
        SetStage(
            3,
            OrderState.Debt <= 0 && OrderState.TotalCost > 0,
            OrderState.Prepayment > 0 ? "В работе" : OrderState.TotalCost > 0 ? "Ожидание" : "Не начато",
            OrderState.ContractDate,
            OrderState.Debt <= 0 && OrderState.TotalCost > 0 ? DateTime.Today : null);
        SetStage(4, OrderState.TotalCost > 0, OrderState.TotalCost > 0 ? "В работе" : "Не начато", DateTime.Today, null);
        SetStage(5, false, "Ожидание", null, null);
        SetStage(6, false, "Не начато", null, null);
        SetStage(7, false, "Не начато", null, null);
        SetStage(8, false, "Не начато", null, null);
        SetStage(9, OrderState.GlassTotalArea > 0, OrderState.GlassTotalArea > 0 ? "Ожидание" : "Не начато", null, null);
        SetStage(10, _cadEditor.Document.Rectangles.Count > 0, _cadEditor.Document.Rectangles.Count > 0 ? "Ожидание" : "Не начато", DateTime.Today, null);
        SetStage(11, false, "Не начато", null, null);
        SetStage(12, OrderState.HasDelivery, OrderState.HasDelivery ? "Ожидание" : "Не начато", OrderState.ProductionDate, null);
        SetStage(13, OrderState.HasInstallation, OrderState.HasInstallation ? "Ожидание" : "Не начато", OrderState.ProductionDate, null);
        SetStage(14, false, "Не начато", null, null);
        SetStage(15, false, "Не начато", null, null);

        foreach (var stage in Stages)
        {
            stage.IsOverdue = stage.Status is "В работе" or "Ожидание"
                && stage.StartDate is { } start
                && start.Date < DateTime.Today.AddDays(-3);
            if (stage.IsOverdue && stage.Status != "Выполнено")
            {
                stage.Status = "Просрочено";
            }
        }

        NotifyStageSummary();
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

    private void NotifyOrderInfo()
    {
        OnPropertyChanged(nameof(TotalCostText));
        OnPropertyChanged(nameof(CreatedDateText));
        OnPropertyChanged(nameof(ModifiedDateText));
    }

    private void NotifyStageSummary()
    {
        OnPropertyChanged(nameof(TotalStages));
        OnPropertyChanged(nameof(CompletedStages));
        OnPropertyChanged(nameof(InProgressStages));
        OnPropertyChanged(nameof(WaitingStages));
        OnPropertyChanged(nameof(OverdueStages));
    }

    private void OnStageChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OrderStageCard.IsCompleted)
            or nameof(OrderStageCard.Status)
            or nameof(OrderStageCard.IsOverdue))
        {
            NotifyStageSummary();
        }
    }
}

public sealed partial class OrderStageCard : ObservableObject
{
    public OrderStageCard(int number, string title)
    {
        Number = number;
        Title = title;
    }

    public int Number { get; }

    [ObservableProperty] private string _title;

    [ObservableProperty] private string _status = "Не начато";

    [ObservableProperty] private DateTime? _startDate;

    [ObservableProperty] private DateTime? _endDate;

    [ObservableProperty] private string _note = "";

    [ObservableProperty] private bool _isCompleted;

    [ObservableProperty] private bool _isOverdue;

    public string StartDateText => StartDate?.ToString("dd.MM.yyyy") ?? "—";

    public string EndDateText => EndDate?.ToString("dd.MM.yyyy") ?? "—";

    public string StatusForeground => Status switch
    {
        "В работе" => "#2F80ED",
        "Выполнено" => "#27AE60",
        "Ожидание" => "#F2994A",
        "Просрочено" => "#EB5757",
        _ => "#757575"
    };

    public string StatusBackground => Status switch
    {
        "В работе" => "#EAF2FD",
        "Выполнено" => "#E8F7EF",
        "Ожидание" => "#FEF3E8",
        "Просрочено" => "#FDECEA",
        _ => "#F2F2F2"
    };

    partial void OnStatusChanged(string value)
    {
        OnPropertyChanged(nameof(StatusForeground));
        OnPropertyChanged(nameof(StatusBackground));
    }

    partial void OnIsCompletedChanged(bool value)
    {
        if (value)
        {
            Status = "Выполнено";
            EndDate ??= DateTime.Today;
        }

        OnPropertyChanged(nameof(EndDateText));
    }

    partial void OnStartDateChanged(DateTime? value) => OnPropertyChanged(nameof(StartDateText));

    partial void OnEndDateChanged(DateTime? value) => OnPropertyChanged(nameof(EndDateText));
}

public sealed record OrderTemplateOption(string Title, double Width, double Height);
