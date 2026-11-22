using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Tools;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels;

public sealed partial class OrderDrawingViewModel : ViewModelBase
{
    private readonly OrderWorkspaceViewModel _workspace;
    private readonly Order _order;
    private readonly OrderSheetViewModel _sheet;
    private readonly CadEditorViewModel _cadEditor;

    public CadEditorViewModel CadEditor => _cadEditor;

    public ObservableCollection<CadDesignToolGroup> ToolGroups { get; } = new();

    public IReadOnlyList<CadDesignTool> AllTools { get; }

    [ObservableProperty]
    private string _activeToolId = "select";

    [ObservableProperty]
    private string _statusMessage = "Выберите инструмент проектирования";

    [ObservableProperty]
    private bool _isToolPanelVisible = true;

    public double ToolPanelWidth => IsToolPanelVisible ? 300 : 0;

    public event EventHandler? ProjectModified;

    public OrderDrawingViewModel(OrderWorkspaceViewModel workspace, CadEditorViewModel cadEditor)
    {
        _workspace = workspace;
        _order = workspace.Order;
        _sheet = workspace.Sheet;
        _cadEditor = cadEditor;

        AllTools = BuildTools();
        BuildGroups();

        _cadEditor.SceneInvalidated += OnSceneInvalidated;
        _cadEditor.DesignPlacement = HandleDesignPlacement;
        _cadEditor.OnDesignPlacementCommit = HandleDesignCommit;
        _cadEditor.PropertyChanged += OnCadEditorPropertyChanged;
        _cadEditor.NewTemplateRequested += OnNewTemplateRequested;
        _cadEditor.FrameCreated += OnFrameCreated;
        SelectTool(AllTools.First(t => t.Id == "select"));
    }

    public void ReloadFromOrder()
    {
        if (_cadEditor.IsResettingCanvas)
        {
            return;
        }

        var construction = _order.SelectedConstruction ?? _order.EnsureDefaultConstruction();
        _workspace.LoadConstructionToCanvas(construction);
        StatusMessage = "Чертеж загружен";
    }

    private void NotifyModified() => ProjectModified?.Invoke(this, EventArgs.Empty);

    private ConstructionGeometry ActiveGeometry =>
        (_order.SelectedConstruction ?? _order.EnsureDefaultConstruction()).Geometry;

    private void OnFrameCreated(object? sender, NurSapat.Cad.UI.Models.FrameCreatedEventArgs e)
    {
        var geometry = ActiveGeometry;
        if (e.ReplaceExisting)
        {
            geometry.Elements.Clear();
        }

        geometry.Elements.Add(e.Element);
        geometry.OuterWidthMm = e.Element.WidthMm;
        geometry.OuterHeightMm = e.Element.HeightMm;
        _sheet.RecalculateAll();
        StatusMessage = $"Создано: {e.Element.Label}";
        NotifyModified();
    }

    private void OnNewTemplateRequested(object? sender, EventArgs e)
    {
        var geometry = ActiveGeometry;
        geometry.Elements.Clear();
        geometry.OuterWidthMm = 0;
        geometry.OuterHeightMm = 0;
        _sheet.RecalculateAll();
        StatusMessage = "Новый чертеж";
        NotifyModified();
    }

    private void OnCadEditorPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (!_cadEditor.IsViewOnlyMode
            && e.PropertyName is nameof(CadEditorViewModel.SelectedProfileSeriesItem)
                or nameof(CadEditorViewModel.SelectedConstructionMaterialOption)
                or nameof(CadEditorViewModel.SelectedMaterialColor)
                or nameof(CadEditorViewModel.SelectedContourColorOption))
        {
            _workspace.CommitActiveConstructionGeometry();
        }

        if (e.PropertyName != nameof(CadEditorViewModel.CurrentToolMode))
        {
            return;
        }

        var mapped = MapWindowToolMode(_cadEditor.CurrentToolMode);
        if (mapped != null)
        {
            SelectTool(mapped);
        }
    }

    private CadDesignTool? MapWindowToolMode(NurSapat.Cad.Core.Models.WindowToolMode mode) => mode switch
    {
        // RealFrame управляется панелью CadEditorView (FrameNode), не legacy RectangleTool.
        NurSapat.Cad.Core.Models.WindowToolMode.FakeFrame => AllTools.FirstOrDefault(t => t.Id == "dummy-frame"),
        NurSapat.Cad.Core.Models.WindowToolMode.FakeFrameCircle => AllTools.FirstOrDefault(t => t.Id == "round-dummy"),
        NurSapat.Cad.Core.Models.WindowToolMode.ExtremeImpost => AllTools.FirstOrDefault(t => t.Id == "edge-mullion"),
        NurSapat.Cad.Core.Models.WindowToolMode.RackRigelNone => AllTools.FirstOrDefault(t => t.Id == "edge-mullion"),
        _ => null
    };

    partial void OnIsToolPanelVisibleChanged(bool value) => OnPropertyChanged(nameof(ToolPanelWidth));

    [RelayCommand]
    private void ToggleToolPanel() => IsToolPanelVisible = !IsToolPanelVisible;

    [RelayCommand]
    private void SelectTool(CadDesignTool? tool)
    {
        if (tool is null) return;

        ActiveToolId = tool.Id;
        foreach (var item in AllTools)
        {
            item.IsActive = item.Id == tool.Id;
        }

        _cadEditor.ActiveDesignToolId = tool.Id;
        _cadEditor.ActiveDesignToolLabel = tool.Title;

        if (!string.IsNullOrWhiteSpace(tool.CadToolId))
        {
            _cadEditor.ActivateToolCommand.Execute(tool.CadToolId);
        }
        else
        {
            _cadEditor.ActivateToolCommand.Execute("select");
        }

        StatusMessage = tool.CadToolId switch
        {
            "mullion-place-v" => "Импост V: укажите положение внутри рамы (drag или клик)",
            "mullion-place-h" => "Импост H: укажите положение внутри рамы (drag или клик)",
            "mullion-place-d" => "Импост D: проведите линию от точки к точке",
            "design-rect" => $"{tool.Title}: выделите область внутри рамы",
            _ when tool.PlacesOnCanvas => $"{tool.Title}: кликните на чертёж",
            _ => $"Активен: {tool.Title}"
        };
        OnPropertyChanged(nameof(CadEditor));
    }

    private void OnSceneInvalidated(object? sender, EventArgs e)
    {
        if (_cadEditor.IsResettingCanvas)
        {
            return;
        }

        SyncGeometryFromCad();
        _workspaceRecalculate();
    }

    private void _workspaceRecalculate() => _sheet.RecalculateAll();

    private void SyncGeometryFromCad()
    {
        _workspace.CommitActiveConstructionGeometry();
    }

    private bool HandleDesignPlacement(CadPoint worldPoint)
    {
        var tool = AllTools.FirstOrDefault(t => t.Id == ActiveToolId);
        if (tool?.ElementKind is not { } kind || !tool.PlacesOnCanvas)
        {
            return false;
        }

        PlacePointElement(kind, tool, worldPoint);
        return true;
    }

    private void HandleDesignCommit(DesignPlacementCommit commit)
    {
        var tool = AllTools.FirstOrDefault(t => t.Id == commit.ToolId);
        if (tool?.ElementKind is not { } kind)
        {
            return;
        }

        switch (commit.Kind)
        {
            case DesignPlacementKind.MullionLine:
                PlaceMullionElement(kind, tool, commit);
                break;
            case DesignPlacementKind.AreaRect when commit.Rect is CadRect rect:
                PlaceRectElement(kind, tool, rect);
                break;
        }
    }

    private void PlaceMullionElement(GeometryElementKind kind, CadDesignTool tool, DesignPlacementCommit commit)
    {
        var end = commit.End ?? commit.Anchor;
        var isDiagonal = tool.Id == "mullion-d";

        var element = new GeometryElement
        {
            Kind = kind,
            Label = tool.Title,
            SourceToolId = tool.Id,
            X = (decimal)commit.Anchor.X,
            Y = (decimal)commit.Anchor.Y,
            WidthMm = 0,
            HeightMm = 0
        };

        if (isDiagonal)
        {
            element.EndX = (decimal)end.X;
            element.EndY = (decimal)end.Y;
        }
        else if (tool.Id == "mullion-h")
        {
            element.X = (decimal)commit.Anchor.X;
            element.Y = (decimal)commit.Anchor.Y;
            element.EndX = (decimal)end.X;
            element.EndY = (decimal)end.Y;
        }
        else
        {
            element.X = (decimal)commit.Anchor.X;
            element.Y = (decimal)commit.Anchor.Y;
            element.EndX = (decimal)end.X;
            element.EndY = (decimal)end.Y;
        }

        ActiveGeometry.Elements.Add(element);
        FinishPlacement(tool.Title);
    }

    private void PlaceRectElement(GeometryElementKind kind, CadDesignTool tool, CadRect rect)
    {
        ActiveGeometry.Elements.Add(new GeometryElement
        {
            Kind = kind,
            Label = tool.Title,
            SourceToolId = tool.Id,
            X = (decimal)(rect.X + rect.Width / 2),
            Y = (decimal)(rect.Y - rect.Height / 2),
            WidthMm = (decimal)rect.Width,
            HeightMm = (decimal)rect.Height,
            OpeningType = ResolveOpeningType(kind, tool.Id)
        });
        FinishPlacement(tool.Title);
    }

    private void PlacePointElement(GeometryElementKind kind, CadDesignTool tool, CadPoint worldPoint)
    {
        var geometry = ActiveGeometry;
        geometry.Elements.Add(new GeometryElement
        {
            Kind = kind,
            Label = tool.Title,
            SourceToolId = tool.Id,
            X = (decimal)worldPoint.X,
            Y = (decimal)worldPoint.Y,
            WidthMm = geometry.OuterWidthMm > 0 ? geometry.OuterWidthMm / 4 : 400m,
            HeightMm = geometry.OuterHeightMm > 0 ? geometry.OuterHeightMm / 4 : 500m,
            OpeningType = ResolveOpeningType(kind, tool.Id)
        });
        FinishPlacement(tool.Title);
    }

    private void FinishPlacement(string title)
    {
        StatusMessage = $"Добавлено: {title}";
        _sheet.RecalculateAll();
    }

    private static SashOpeningType? ResolveOpeningType(GeometryElementKind kind, string toolId) => kind switch
    {
        GeometryElementKind.Sash => SashOpeningType.LeftTurn,
        GeometryElementKind.SlidingSash when toolId == "slide-left" => SashOpeningType.SlideLeft,
        GeometryElementKind.SlidingSash when toolId == "slide-right" => SashOpeningType.SlideRight,
        _ => null
    };

    private void BuildGroups()
    {
        ToolGroups.Clear();
        foreach (var group in AllTools.GroupBy(t => t.Category))
        {
            ToolGroups.Add(new CadDesignToolGroup
            {
                Title = group.Key,
                Tools = group.ToList()
            });
        }
    }

    private static IReadOnlyList<CadDesignTool> BuildTools() =>
    [
        Tool("select", "Выделение", "Редактирование", "\uE8C1", cadToolId: "select"),
        Tool("dummy-frame", "Коробка фиктивная", "Базовая геометрия и рамы", "\uE739", GeometryElementKind.DummyFrame, cadToolId: "design-rect"),
        Tool("dummy-mullion", "Фиктивные импосты", "Базовая геометрия и рамы", "\uE8B0", GeometryElementKind.DummyMullion, places: true),
        Tool("frame", "Коробка (рама)", "Базовая геометрия и рамы", "\uE7F4", GeometryElementKind.Frame, cadToolId: "rectangle"),
        Tool("edge-mullion", "Стойка/ригель в крайнем положении", "Базовая геометрия и рамы", "CadIcon.RackRigelEdge", GeometryElementKind.EdgeMullion, places: true),
        Tool("round-dummy", "Коробка фиктивная круглая", "Базовая геометрия и рамы", "\uECAF", GeometryElementKind.RoundDummyFrame, cadToolId: "design-rect"),
        Tool("transition-frame", "Коробка и переходной профиль", "Базовая геометрия и рамы", "\uE8AB", GeometryElementKind.TransitionFrame, cadToolId: "design-rect"),
        Tool("sash", "Створка", "Створки и фурнитура", "\uE7C3", GeometryElementKind.Sash, cadToolId: "design-rect"),
        Tool("hinges", "Петли / механизмы", "Створки и фурнитура", "\uE946", GeometryElementKind.Hardware, places: true),
        Tool("handle", "Ручка", "Створки и фурнитура", "\uE7F3", GeometryElementKind.Handle, places: true),
        Tool("mullion-v", "Импост вертикальный", "Створки и фурнитура", "\uE8A4", GeometryElementKind.Mullion, cadToolId: "mullion-place-v"),
        Tool("mullion-h", "Импост горизонтальный", "Створки и фурнитура", "\uE8A3", GeometryElementKind.Mullion, cadToolId: "mullion-place-h"),
        Tool("mullion-d", "Импост наклонный", "Створки и фурнитура", "\uE8B6", GeometryElementKind.Mullion, cadToolId: "mullion-place-d"),
        Tool("lock", "Замок", "Створки и фурнитура", "\uE72E", GeometryElementKind.Hardware, places: true),
        Tool("limiter", "Ограничитель открывания", "Створки и фурнитура", "\uE7BA", GeometryElementKind.Hardware, places: true),
        Tool("closer", "Доводчик", "Створки и фурнитура", "\uE7C5", GeometryElementKind.Hardware, places: true),
        Tool("bolt", "Шпингалет", "Створки и фурнитура", "\uE8D7", GeometryElementKind.Hardware, places: true),
        Tool("corrugation-v", "Гофра вертикальная", "Заполнение и материалы", "\uE8A6", GeometryElementKind.Corrugation, cadToolId: "design-rect"),
        Tool("corrugation-h", "Гофра горизонтальная", "Заполнение и материалы", "\uE8A7", GeometryElementKind.Corrugation, cadToolId: "design-rect"),
        Tool("glazing", "Остекление (панели)", "Заполнение и материалы", "\uE771", GeometryElementKind.Glazing, cadToolId: "design-rect"),
        Tool("bead", "Штапик", "Заполнение и материалы", "\uE8B1", GeometryElementKind.Bead, places: true),
        Tool("mosquito-net", "Москитная сетка", "Заполнение и материалы", "\uE7B3", GeometryElementKind.MosquitoNet, cadToolId: "design-rect"),
        Tool("slide-left", "Створка раздвижная левая", "Заполнение и материалы", "\uE8AB", GeometryElementKind.SlidingSash, cadToolId: "design-rect"),
        Tool("slide-right", "Створка раздвижная правая", "Заполнение и материалы", "\uE8AC", GeometryElementKind.SlidingSash, cadToolId: "design-rect"),
        Tool("installation", "Монтажные элементы", "Дополнительные комплектующие", "\uE90F", GeometryElementKind.Installation, places: true),
        Tool("cornice", "Карниз", "Дополнительные комплектующие", "\uE8A9", GeometryElementKind.Accessory, places: true),
        Tool("control-unit", "Блок управления", "Дополнительные комплектующие", "\uE7F4", GeometryElementKind.Accessory, places: true),
        Tool("blinds-v", "Жалюзи вертикальные", "Дополнительные комплектующие", "\uE8A0", GeometryElementKind.Accessory, places: true),
        Tool("blinds-h", "Жалюзи горизонтальные", "Дополнительные комплектующие", "\uE8A1", GeometryElementKind.Accessory, places: true),
    ];

    private static CadDesignTool Tool(
        string id,
        string title,
        string category,
        string icon,
        GeometryElementKind? kind = null,
        string? cadToolId = null,
        bool places = false) => new()
    {
        Id = id,
        Title = title,
        Category = category,
        IconGlyph = icon,
        ElementKind = kind,
        CadToolId = cadToolId,
        PlacesOnCanvas = places
    };
}
