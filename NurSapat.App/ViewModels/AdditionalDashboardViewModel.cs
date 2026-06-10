using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.UI.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using NurSapat.Cad.Core.Viewport;

namespace NurSapat.App.ViewModels;

public sealed partial class AdditionalDashboardViewModel : ObservableObject
{
    private readonly CadEditorViewModel _cadEditor;

    public OrderCalculationState OrderState { get; }

    public ObservableCollection<OrderStageCard> Stages { get; } = new();

    public ObservableCollection<OrderTemplateOption> Templates { get; } = new();

    [ObservableProperty] private OrderTemplateOption? _selectedTemplate;

    [ObservableProperty] private bool _isOrderLocked;

    [ObservableProperty] private string _note = "";

    public int NoteLength => Note.Length;

    public int TotalStages => Stages.Count;

    public int CompletedStages => Stages.Count(stage => stage.IsCompleted);

    public int InProgressStages => Stages.Count(stage => stage.Status == "В работе");

    public int WaitingStages => Stages.Count(stage => stage.Status == "Ожидание");

    public int OverdueStages => Stages.Count(stage => stage.IsOverdue);

    public AdditionalDashboardViewModel(OrderCalculationState orderState, CadEditorViewModel cadEditor)
    {
        OrderState = orderState;
        _cadEditor = cadEditor;
        RebuildTemplates();
        SelectedTemplate = Templates.FirstOrDefault();
        RebuildStages();
        OrderState.PropertyChanged += OnOrderStateChanged;
    }

    [RelayCommand]
    private void ApplyTemplate()
    {
        if (SelectedTemplate == null) return;

        // Создаём новый прямоугольник в центре вида
        var vp = _cadEditor.Viewport;
        var center = vp.ScreenToWorld(new ScreenPoint(vp.Width / 2, vp.Height / 2));
        var width = SelectedTemplate.Width;
        var height = SelectedTemplate.Height;

        var rect = new CadRect(center.X - width / 2, center.Y + height / 2, width, height);
        _cadEditor.Document.AddRectangle(rect);

        // Обновляем расчёт
        _cadEditor.RefreshSelection();
        OrderState.UpdateFrom(_cadEditor.Document);
        _cadEditor.FitDocumentToViewport();

        OrderState.AddActivity("Применен шаблон", SelectedTemplate.Title);
        RebuildStages();
    }

    [RelayCommand]
    private void RefreshOrder()
    {
        OrderState.UpdateFrom(_cadEditor.Document);
        OrderState.AddActivity("Заказ обновлен", $"Сумма: {OrderState.TotalCost:0.##}, площадь: {OrderState.ItemArea:0.###} м²");
        RebuildStages();
    }

    [RelayCommand]
    private void CompleteStage(OrderStageCard? stage)
    {
        if (stage == null) return;

        stage.IsCompleted = true;
        stage.Status = "Выполнено";
        stage.CompletedAt = DateTime.Today;
        OrderState.AddActivity("Этап выполнен", stage.Title);
        NotifyStageSummary();
    }

    partial void OnNoteChanged(string value)
    {
        OnPropertyChanged(nameof(NoteLength));
        if (!string.IsNullOrWhiteSpace(value))
        {
            OrderState.AddActivity("Обновлено примечание", value.Length > 80 ? value[..80] : value);
        }
    }

    private void OnOrderStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(OrderCalculationState.TotalCost)
            or nameof(OrderCalculationState.Prepayment)
            or nameof(OrderCalculationState.Debt)
            or nameof(OrderCalculationState.EstimatedMaterialCost)
            or nameof(OrderCalculationState.ProductionDate))
        {
            RebuildTemplates();
            RebuildStages();
        }
    }

    private void RebuildTemplates()
    {
        var selectedTitle = SelectedTemplate?.Title;
        Templates.Clear();

        // Добавляем шаблон на основе текущего прямоугольника (если есть)
        var rects = _cadEditor.Document.Rectangles;
        if (rects.Count > 0)
        {
            // Берём первый прямоугольник
            var rect = rects[0];
            Templates.Add(new OrderTemplateOption(
                $"Прямоугольник {rect.Width:0}x{rect.Height:0}",
                rect.Width,
                rect.Height));
        }

        // Оставляем возможность создать новый прямоугольник
        Templates.Add(new OrderTemplateOption("Новый (1000x1000)", 1000, 1000));
        Templates.Add(new OrderTemplateOption("Новый (1500x1400)", 1500, 1400));

        SelectedTemplate = Templates.FirstOrDefault(template => template.Title == selectedTitle) ?? Templates.FirstOrDefault();
    }

    private void RebuildStages()
    {
        var rects = _cadEditor.Document.Rectangles;

        UpsertStage(1, "Данные заказа", !string.IsNullOrWhiteSpace(OrderState.CustomerName) || !string.IsNullOrWhiteSpace(OrderState.OrderNumber), "В работе", OrderState.OrderDate);
        UpsertStage(2, "Чертеж", rects.Count > 0, "В работе", DateTime.Today);
        UpsertStage(3, "Расчет стоимости", OrderState.TotalCost > 0, "В работе", DateTime.Today);
        UpsertStage(4, "Оплата", OrderState.Debt <= 0 && OrderState.TotalCost > 0, OrderState.Prepayment > 0 ? "В работе" : "Ожидание", OrderState.ContractDate);
        UpsertStage(5, "Производство", false, OrderState.TotalCost > 0 ? "В работе" : "Ожидание", OrderState.ProductionDate);
        UpsertStage(6, "Доставка и монтаж", OrderState.HasDelivery || OrderState.HasInstallation, OrderState.ServicesSummary, OrderState.ProductionDate);
        NotifyStageSummary();
    }

    private void UpsertStage(int number, string title, bool isCompleted, string activeStatus, DateTime date)
    {
        var stage = Stages.FirstOrDefault(item => item.Number == number);
        if (stage == null)
        {
            stage = new OrderStageCard(number, title);
            stage.PropertyChanged += OnStageChanged;
            Stages.Add(stage);
        }

        stage.Title = title;
        stage.IsCompleted = isCompleted;
        stage.Status = isCompleted ? "Выполнено" : activeStatus;
        stage.PlannedAt = date;
        stage.IsOverdue = !isCompleted && date.Date < DateTime.Today;
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
        if (e.PropertyName is nameof(OrderStageCard.IsCompleted) or nameof(OrderStageCard.Status) or nameof(OrderStageCard.IsOverdue))
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

    [ObservableProperty] private string _status = "Ожидание";

    [ObservableProperty] private DateTime _plannedAt = DateTime.Today;

    [ObservableProperty] private DateTime? _completedAt;

    [ObservableProperty] private bool _isCompleted;

    [ObservableProperty] private bool _isOverdue;

    public string PlannedAtText => PlannedAt.ToString("dd.MM.yyyy");

    public string CompletedAtText => CompletedAt?.ToString("dd.MM.yyyy") ?? "";

    partial void OnIsCompletedChanged(bool value)
    {
        Status = value ? "Выполнено" : "Ожидание";
        CompletedAt = value ? DateTime.Today : null;
    }

    partial void OnPlannedAtChanged(DateTime value) => OnPropertyChanged(nameof(PlannedAtText));

    partial void OnCompletedAtChanged(DateTime? value) => OnPropertyChanged(nameof(CompletedAtText));
}

public sealed record OrderTemplateOption(string Title, double Width, double Height);