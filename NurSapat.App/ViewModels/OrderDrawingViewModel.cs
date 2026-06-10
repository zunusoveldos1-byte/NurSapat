using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels;

public sealed partial class OrderDrawingViewModel : ViewModelBase
{
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

    public OrderDrawingViewModel(Order order, OrderSheetViewModel sheet, CadEditorViewModel cadEditor)
    {
        _order = order;
        _sheet = sheet;
        _cadEditor = cadEditor;

        AllTools = BuildTools();
        BuildGroups();

        _cadEditor.SceneInvalidated += OnSceneInvalidated;
        _cadEditor.DesignPlacement = HandleDesignPlacement;
        SelectTool(AllTools.First(t => t.Id == "select"));
    }

    [RelayCommand]
    private void SelectTool(CadDesignTool? tool)
    {
        if (tool is null) return;

        ActiveToolId = tool.Id;
        foreach (var item in AllTools)
        {
            item.IsActive = item.Id == tool.Id;
        }

        if (!string.IsNullOrWhiteSpace(tool.CadToolId))
        {
            _cadEditor.ActivateToolCommand.Execute(tool.CadToolId);
        }
        else
        {
            _cadEditor.ActivateToolCommand.Execute("select");
        }

        StatusMessage = tool.PlacesOnCanvas
            ? $"{tool.Title}: кликните на чертёж для размещения"
            : $"Активен: {tool.Title}";
        OnPropertyChanged(nameof(CadEditor));
    }

    private void OnSceneInvalidated(object? sender, EventArgs e)
    {
        SyncGeometryFromCad();
        _workspaceRecalculate();
    }

    private void _workspaceRecalculate() => _sheet.RecalculateAll();

    private void SyncGeometryFromCad()
    {
        var construction = _order.EnsureDefaultConstruction();
        var geometry = construction.Geometry;
        var rects = _cadEditor.Document.Rectangles;

        if (rects.Count == 0) return;

        var minX = rects.Min(r => r.X);
        var maxX = rects.Max(r => r.X + r.Width);
        var minY = rects.Min(r => r.Y - r.Height);
        var maxY = rects.Max(r => r.Y);

        geometry.OuterWidthMm = (decimal)Math.Round(maxX - minX, 1);
        geometry.OuterHeightMm = (decimal)Math.Round(maxY - minY, 1);

        _sheet.OuterWidthMm = geometry.OuterWidthMm;
        _sheet.OuterHeightMm = geometry.OuterHeightMm;
    }

    private bool HandleDesignPlacement(CadPoint worldPoint)
    {
        var tool = AllTools.FirstOrDefault(t => t.Id == ActiveToolId);
        if (tool?.ElementKind is not { } kind || !tool.PlacesOnCanvas)
        {
            return false;
        }

        PlaceElementAt(kind, tool.Title, worldPoint);
        return true;
    }

    private void PlaceElementAt(GeometryElementKind kind, string label, CadPoint worldPoint)
    {
        var construction = _order.EnsureDefaultConstruction();
        var geometry = construction.Geometry;

        geometry.Elements.Add(new GeometryElement
        {
            Kind = kind,
            Label = label,
            X = (decimal)worldPoint.X,
            Y = (decimal)worldPoint.Y,
            WidthMm = geometry.OuterWidthMm > 0 ? geometry.OuterWidthMm / 2 : 500m,
            HeightMm = geometry.OuterHeightMm > 0 ? geometry.OuterHeightMm / 2 : 700m,
            OpeningType = kind == GeometryElementKind.Sash ? SashOpeningType.TiltTurn : null
        });

        StatusMessage = $"Добавлено: {label}";
        _sheet.RecalculateAll();
    }

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
        Tool("dummy-frame", "Коробка фиктивная", "Базовая геометрия и рамы", "\uE739", GeometryElementKind.DummyFrame, places: true),
        Tool("dummy-mullion", "Фиктивные импосты", "Базовая геометрия и рамы", "\uE8B0", GeometryElementKind.DummyMullion, places: true),
        Tool("frame", "Коробка (рама)", "Базовая геометрия и рамы", "\uE7F4", GeometryElementKind.Frame, cadToolId: "rectangle"),
        Tool("edge-mullion", "Стойка/ригель в крайнем положении", "Базовая геометрия и рамы", "\uE8A5", GeometryElementKind.EdgeMullion, places: true),
        Tool("round-dummy", "Коробка фиктивная круглая", "Базовая геометрия и рамы", "\uECAF", GeometryElementKind.RoundDummyFrame, places: true),
        Tool("transition-frame", "Коробка и переходной профиль", "Базовая геометрия и рамы", "\uE8AB", GeometryElementKind.TransitionFrame, places: true),
        Tool("sash", "Створка", "Створки и фурнитура", "\uE7C3", GeometryElementKind.Sash, places: true),
        Tool("hinges", "Петли / механизмы", "Створки и фурнитура", "\uE946", GeometryElementKind.Hardware, places: true),
        Tool("handle", "Ручка", "Створки и фурнитура", "\uE7F3", GeometryElementKind.Handle, places: true),
        Tool("mullion-v", "Импост вертикальный", "Створки и фурнитура", "\uE8A4", GeometryElementKind.Mullion, places: true),
        Tool("mullion-h", "Импост горизонтальный", "Створки и фурнитура", "\uE8A3", GeometryElementKind.Mullion, places: true),
        Tool("mullion-d", "Импост наклонный", "Створки и фурнитура", "\uE8B6", GeometryElementKind.Mullion, places: true),
        Tool("lock", "Замок", "Створки и фурнитура", "\uE72E", GeometryElementKind.Hardware, places: true),
        Tool("limiter", "Ограничитель открывания", "Створки и фурнитура", "\uE7BA", GeometryElementKind.Hardware, places: true),
        Tool("closer", "Доводчик", "Створки и фурнитура", "\uE7C5", GeometryElementKind.Hardware, places: true),
        Tool("bolt", "Шпингалет", "Створки и фурнитура", "\uE8D7", GeometryElementKind.Hardware, places: true),
        Tool("corrugation-v", "Гофра вертикальная", "Заполнение и материалы", "\uE8A6", GeometryElementKind.Corrugation, places: true),
        Tool("corrugation-h", "Гофра горизонтальная", "Заполнение и материалы", "\uE8A7", GeometryElementKind.Corrugation, places: true),
        Tool("glazing", "Остекление (панели)", "Заполнение и материалы", "\uE771", GeometryElementKind.Glazing, places: true),
        Tool("bead", "Штапик", "Заполнение и материалы", "\uE8B1", GeometryElementKind.Bead, places: true),
        Tool("mosquito-net", "Москитная сетка", "Заполнение и материалы", "\uE7B3", GeometryElementKind.MosquitoNet, places: true),
        Tool("slide-left", "Створка раздвижная левая", "Заполнение и материалы", "\uE8AB", GeometryElementKind.SlidingSash, places: true),
        Tool("slide-right", "Створка раздвижная правая", "Заполнение и материалы", "\uE8AC", GeometryElementKind.SlidingSash, places: true),
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
