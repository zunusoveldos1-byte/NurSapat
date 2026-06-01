using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.Core.Commands;
using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Parametric;
using NurSapat.Cad.Core.Snapping;
using NurSapat.Cad.Core.Templates;
using NurSapat.Cad.Core.Viewport;
using NurSapat.Cad.Tools;
using NurSapat.Cad.UI.Training;
using System.Collections.ObjectModel;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel : ObservableObject
{
    private readonly CadToolFactory _toolFactory;
    private readonly CadTemplateSerializer _templateSerializer = new();

    public CadDocument Document { get; } = new();

    public CadViewport Viewport { get; } = new();

    public CadSnappingEngine Snapping { get; } = new();

    public CadCommandManager Commands { get; }

    public CadTrainingTracker TrainingTracker { get; } = new();

    public OrderCalculationState OrderState { get; }

    public ObservableCollection<CadToolButtonViewModel> Tools { get; }

    [ObservableProperty]
    private string _activeToolId = "select"; // Инициализация по умолчанию

    public IRelayCommand<string> ActivateToolCommand { get; }

    [ObservableProperty]
    private ICadTool _activeTool;

    [ObservableProperty]
    private bool _isTrainingVisible = true;

    [ObservableProperty]
    private string _statusText = "Готово. Выберите проём или инструмент.";

    [ObservableProperty]
    private WindowSection? _selectedSection;

    [ObservableProperty]
    private bool _isLeftPanelVisible = true;

    [ObservableProperty]
    private bool _isRightPanelVisible = true;

    [ObservableProperty]
    private bool _isBottomPanelVisible = true;

    [RelayCommand]
    private void ToggleLeftPanel() => IsLeftPanelVisible = !IsLeftPanelVisible;

    [RelayCommand]
    private void ToggleRightPanel() => IsRightPanelVisible = !IsRightPanelVisible;

    [RelayCommand]
    private void ToggleBottomPanel() => IsBottomPanelVisible = !IsBottomPanelVisible;

    [ObservableProperty]
    private bool _isTopPanelVisible = true;

    [RelayCommand]
    private void ToggleTopPanel() => IsTopPanelVisible = !IsTopPanelVisible;

    [RelayCommand]
    private void ClearAll()
    {
        Document.ClearRectangles();
        SelectedRectangle = null;
        RefreshSelection();
        StatusText = "Все элементы очищены";
    }

    [ObservableProperty]
    private CadRect? _selectedRectangle;

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedRectangle != null)
        {
            // ИСПРАВЛЕНИЕ: используем .Value, чтобы получить CadRect (не nullable)
            Document.RemoveRectangle(SelectedRectangle.Value);
            SelectedRectangle = null;
            RefreshSelection();
            StatusText = "Выбранный объект удален";
        }
    }

    public CadEditorViewModel(OrderCalculationState orderState)
    {
        OrderState = orderState;
        _toolFactory = new CadToolFactory();
        Commands = new CadCommandManager(Document);
        Tools = new ObservableCollection<CadToolButtonViewModel>(
            _toolFactory.Tools.Select(t => new CadToolButtonViewModel(t.Id, t.DisplayName)));

        ActivateToolCommand = new RelayCommand<string>(ActivateTool);

        ActiveTool = _toolFactory.Create("rectangle");

        SetActiveToolState("rectangle");
        PublishOrderState();
    }

    public CadToolContext CreateToolContext()
    {
        Snapping.GridStep = Viewport.ChooseGridStep();
        Snapping.ToleranceWorld = Math.Max(5, 12 / Viewport.ZoomFactor);
        return new CadToolContext(Document, Commands, Snapping)
        {
            Track = TrainingTracker.Track
        };
    }

    public void RefreshSelection()
    {
        SelectedSection = Document.SelectedSection;
        OnPropertyChanged(nameof(SelectedKind));
        OnPropertyChanged(nameof(IsFrameSelected));
        OnPropertyChanged(nameof(IsImpostSelected));
        OnPropertyChanged(nameof(IsSashSelected));
        OnPropertyChanged(nameof(IsGlazingSelected));
        OnPropertyChanged(nameof(IsSectionSelected));
        OnPropertyChanged(nameof(FrameWidth));
        OnPropertyChanged(nameof(FrameHeight));
        OnPropertyChanged(nameof(FrameProfileSystem));
        OnPropertyChanged(nameof(FrameLaminationColor));
        OnPropertyChanged(nameof(SelectedProfileSystem));
        OnPropertyChanged(nameof(SelectedLaminationColor));
        OnPropertyChanged(nameof(SelectedOpeningType));
        OnPropertyChanged(nameof(SelectedHardwareBrand));
        OnPropertyChanged(nameof(SelectedGlassFormula));
        OnPropertyChanged(nameof(SelectedImpostCoordinate));
        OnPropertyChanged(nameof(HasCloser));
        OnPropertyChanged(nameof(HasLock));
        OnPropertyChanged(nameof(HasLimiter));
    }

    public void Recalculate(string reason)
    {
        Document.Structure.Recalculate();
        PublishOrderState();
        RefreshSelection();
        TrainingTracker.Track("Параметры", reason, "Успешно");
        StatusText = reason;
    }

    public void PublishOrderState() => OrderState.UpdateFrom(Document.Structure);

    public CadSelectionKind SelectedKind => Document.Structure.SelectedKind;

    public bool IsFrameSelected => SelectedKind == CadSelectionKind.Frame || SelectedSection == null && SelectedKind != CadSelectionKind.Impost;

    public bool IsSectionSelected => SelectedKind == CadSelectionKind.Section;

    public bool IsImpostSelected => SelectedKind == CadSelectionKind.Impost;

    public bool IsSashSelected => SelectedKind == CadSelectionKind.Sash;

    public bool IsGlazingSelected => SelectedKind == CadSelectionKind.Glazing;

    public double FrameWidth
    {
        get => Document.Structure.Options.Width;
        set
        {
            if (Math.Abs(Document.Structure.Options.Width - value) > 0.1)
            {
                Document.Structure.Options.Width = Math.Max(300, value);
                Recalculate($"Изменена ширина конструкции: {value:0} мм");
                OnPropertyChanged();
            }
        }
    }

    public double FrameHeight
    {
        get => Document.Structure.Options.Height;
        set
        {
            if (Math.Abs(Document.Structure.Options.Height - value) > 0.1)
            {
                Document.Structure.Options.Height = Math.Max(300, value);
                Recalculate($"Изменена высота конструкции: {value:0} мм");
                OnPropertyChanged();
            }
        }
    }

    public string FrameProfileSystem
    {
        get => Document.Structure.Options.ProfileSystem;
        set
        {
            if (Document.Structure.Options.ProfileSystem != value)
            {
                Document.Structure.Options.ProfileSystem = value;
                Recalculate($"Изменена серия профиля: {value}");
                OnPropertyChanged();
            }
        }
    }

    public string FrameLaminationColor
    {
        get => Document.Structure.Options.LaminationColor;
        set
        {
            if (Document.Structure.Options.LaminationColor != value)
            {
                Document.Structure.Options.LaminationColor = value;
                Recalculate($"Изменен цвет ламинации: {value}");
                OnPropertyChanged();
            }
        }
    }

    public double SelectedImpostCoordinate
    {
        get => Document.Structure.SelectedImpostOwner?.SplitCoordinate ?? 0;
        set
        {
            Document.Structure.MoveSelectedImpost(value);
            Recalculate($"Изменена координата импоста: {value:0} мм");
            OnPropertyChanged();
        }
    }

    public IReadOnlyList<string> ProfileSystems { get; } =
    [
        "Default PVC 72",
        "Aldoks",
        "Aluminum Warm 72",
        "Hidden auxiliary"
    ];

    public IReadOnlyList<string> LaminationColors { get; } =
    [
        "Белый",
        "Золотой дуб",
        "Антрацит",
        "Орех",
        "Черный матовый"
    ];

    public IReadOnlyList<string> OpeningTypes { get; } =
    [
        "Поворотная",
        "Поворотно-откидная",
        "Фрамуга"
    ];

    public IReadOnlyList<string> HardwareBrands { get; } =
    [
        "Roto",
        "Maco",
        "Siegenia",
        "Vorne"
    ];

    public IReadOnlyList<string> GlassFormulas { get; } =
    [
        "4*16*4 Простой",
        "4LowE*16*4",
        "Сэндвич 24мм",
        "4*10*4*10*4"
    ];

    public string? SelectedProfileSystem
    {
        get => SelectedSection?.Fill.ProfileSystem;
        set => SetSelectedFillValue(value, (fill, v) => fill.ProfileSystem = v, "Изменен профиль проёма");
    }

    public string? SelectedLaminationColor
    {
        get => SelectedSection?.Fill.LaminationColor;
        set => SetSelectedFillValue(value, (fill, v) => fill.LaminationColor = v, "Изменена ламинация проёма");
    }

    public string? SelectedOpeningType
    {
        get => SelectedSection?.Fill.OpeningType;
        set => SetSelectedFillValue(value, (fill, v) => fill.OpeningType = v, "Изменен тип открывания");
    }

    public string? SelectedHardwareBrand
    {
        get => SelectedSection?.Fill.HardwareBrand;
        set => SetSelectedFillValue(value, (fill, v) => fill.HardwareBrand = v, "Изменен бренд фурнитуры");
    }

    public string? SelectedGlassFormula
    {
        get => SelectedSection?.Fill.GlassFormula;
        set => SetSelectedFillValue(value, (fill, v) => fill.GlassFormula = v, "Изменена формула заполнения");
    }

    public bool HasCloser
    {
        get => HasModifier(SashHardwareModifier.Closer);
        set => SetModifier(SashHardwareModifier.Closer, value, "Доводчик");
    }

    public bool HasLock
    {
        get => HasModifier(SashHardwareModifier.Lock);
        set => SetModifier(SashHardwareModifier.Lock, value, "Замок");
    }

    public bool HasLimiter
    {
        get => HasModifier(SashHardwareModifier.OpeningLimiter);
        set => SetModifier(SashHardwareModifier.OpeningLimiter, value, "Ограничитель");
    }

    [RelayCommand]
    private void FitAll()
    {
        Viewport.FitToWorld(Document.Structure.Options.Width, Document.Structure.Options.Height);
        TrainingTracker.Track("Навигация", "Показать всё", "Успешно");
    }

    [RelayCommand]
    private void ManualDimensions()
    {
        Document.Structure.InitializeFrame(Document.Structure.Options.Width, Document.Structure.Options.Height);
        Recalculate($"Ввод размеров {Document.Structure.Options.Width:0}x{Document.Structure.Options.Height:0} мм");
    }

    [RelayCommand]
    private void Undo()
    {
        Commands.Undo();
        Recalculate("Отменить");
    }

    [RelayCommand]
    private void Redo()
    {
        Commands.Redo();
        Recalculate("Повторить");
    }

    [RelayCommand]
    private async Task SaveTemplate()
    {
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "NurSapat",
            "Templates",
            $"{Document.Name}-{DateTime.Now:yyyyMMdd-HHmmss}.nursapat_tpl");
        await _templateSerializer.SaveAsync(Document, path);
        TrainingTracker.Track("Шаблоны", $"Сохранение шаблона {path}", "Успешно");
        StatusText = $"Шаблон сохранен: {path}";
    }

    [RelayCommand]
    private void ToggleTraining()
    {
        IsTrainingVisible = !IsTrainingVisible;
    }

    private void ActivateTool(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        ActiveTool = _toolFactory.Create(id);
        ActiveToolId = id; // Обновляем свойство
        SetActiveToolState(id);
        TrainingTracker.Track(ActiveTool.DisplayName, "Активация инструмента", "Успешно");
        StatusText = ActiveTool.DisplayName;
    }

    private void SetSelectedFillValue(string? value, Action<SectionFill, string> setter, string reason)
    {
        if (SelectedSection == null || value == null)
        {
            return;
        }

        setter(SelectedSection.Fill, value);
        Recalculate($"{reason}: {value}");
    }

    private bool HasModifier(SashHardwareModifier modifier) =>
        SelectedSection?.Fill.HardwareModifiers.Contains(modifier) == true;

    private void SetModifier(SashHardwareModifier modifier, bool enabled, string label)
    {
        Document.Structure.SetHardwareModifier(modifier, enabled);
        Recalculate($"{label}: {(enabled ? "включен" : "выключен")}");
    }

    private void SetActiveToolState(string activeId)
    {
        foreach (var tool in Tools)
        {
            tool.IsActive = tool.Id == activeId;
        }
    }
}