using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Threading;
using NurSapat.Cad.Core.Commands;
using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Snapping;
using NurSapat.Cad.Core.Viewport;
using NurSapat.Cad.Tools;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.Services;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using MaterialModel = NurSapat.Core.Models.Material;
using System.Collections.ObjectModel;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel : ObservableObject
{
    private readonly CadToolFactory _toolFactory = new();
    private readonly ICadDialogService _dialogService;
    private readonly CadTemplateStore _templateStore;
    private readonly IMaterialRepository? _materialRepository;
    private readonly List<MaterialModel> _allMaterials = [];
    private bool _isApplyingDrawingTemplate;
    private bool _isSyncingSelection;
    private bool _isRunningFramePipeline;
    private CadPoint? _interactionStartWorld;
    private FrameNode? _interactionLeaf;
    private bool _isCanvasDragging;
    private SashHingeSide _sashPreviewHingeSide = SashHingeSide.Right;

    private const double SashDragMinDeltaMm = 5;
    private const double DefaultRealFrameWidth = 1000;
    private const double DefaultRealFrameHeight = 1000;
    private const double SingleMullionPlacementWidthMm = 32.0;

    private static double RoundMm(double value) =>
        Math.Round(value, 1, MidpointRounding.AwayFromZero);

    private static CadPoint RoundMm(CadPoint point) =>
        new(RoundMm(point.X), RoundMm(point.Y));

    private CadPoint? _realFrameStartWorld;
    private bool _isCommittingRealFrame;
    private bool _isShapeSizeDialogOpen;
    private readonly List<FrameNode> _impostSplitHistory = [];
    private readonly ConstructionPlacementHistory _placementHistory = new();

    private VerticalImpostTarget? _selectedMullion;
    private bool _isVerticalImpostDragging;
    private bool _isHorizontalImpostDragging;
    private double? _draggingMullionWorldX;
    private double? _draggingMullionWorldY;
    private bool _isSyncingImpostPanel;

    private const double VerticalImpostHitToleranceMm = 18;
    private const double VerticalImpostHitScreenPx = 14;
    private const double ImpostDragInnerInsetMm = 50;
    private const double MinEditableOpeningMm = 150;

    private readonly List<DimensionHitInfo> _dimensionHits = [];
    private DimensionHitInfo? _activeDimensionHit;

    private static bool IsDragCreateShapeMode(WindowToolMode mode) =>
        mode is WindowToolMode.RealFrame
            or WindowToolMode.FakeFrame
            or WindowToolMode.FakeFrameTriangle
            or WindowToolMode.FakeFrameArch
            or WindowToolMode.FakeFrameCircle;

    private static (GeometryElementKind Kind, string Label) GetDragShapeKindLabel(WindowToolMode mode) => mode switch
    {
        WindowToolMode.RealFrame => (GeometryElementKind.Frame, "Коробка (рама)"),
        WindowToolMode.FakeFrame => (GeometryElementKind.DummyFrame, "Коробка (фиктивная)"),
        WindowToolMode.FakeFrameTriangle => (GeometryElementKind.TriangleDummyFrame, "Коробка фиктивная треугольная"),
        WindowToolMode.FakeFrameArch => (GeometryElementKind.ArchDummyFrame, "Полукруг (фиктивная)"),
        WindowToolMode.FakeFrameCircle => (GeometryElementKind.RoundDummyFrame, "Коробка фиктивная круглая"),
        _ => (GeometryElementKind.DummyFrame, "Коробка")
    };

    private static (double Width, double Height) NormalizeCommittedShapeSize(WindowToolMode mode, double width, double height)
    {
        width = RoundMm(width);
        height = RoundMm(height);
        if (mode == WindowToolMode.FakeFrameArch)
        {
            height = RoundMm(width / 2.0);
        }
        else if (mode == WindowToolMode.FakeFrameCircle)
        {
            height = width;
        }

        return (width, height);
    }

    private static double DefaultHeightForMode(WindowToolMode mode) => mode switch
    {
        WindowToolMode.FakeFrameArch => DefaultRealFrameWidth / 2.0,
        _ => DefaultRealFrameHeight
    };

    private static readonly PlacementAnchorOption[] PlacementAnchorOptionList =
    [
        new() { Anchor = PlacementAnchor.BottomLeft, Label = "Снизу слева" },
        new() { Anchor = PlacementAnchor.BottomRight, Label = "Снизу справа" },
        new() { Anchor = PlacementAnchor.TopLeft, Label = "Сверху слева" },
        new() { Anchor = PlacementAnchor.TopRight, Label = "Сверху справа" }
    ];

    public IReadOnlyList<PlacementAnchorOption> PlacementAnchorOptions => PlacementAnchorOptionList;

    private (double X, double Y) ResolvePlacementOrigin(double width, double height)
    {
        width = RoundMm(width);
        height = RoundMm(height);

        return PlacementAnchor switch
        {
            PlacementAnchor.BottomRight => (RoundMm(-width), 0.0),
            PlacementAnchor.TopLeft => (0.0, RoundMm(-height)),
            PlacementAnchor.TopRight => (RoundMm(-width), RoundMm(-height)),
            _ => (0.0, 0.0)
        };
    }

    private void UpdatePlacementPanelCoordinatesFromAnchor()
    {
        if (!IsDragCreateShapeMode(CurrentToolMode) || FrameRoot != null || IsShapePlacementActive)
        {
            return;
        }

        var width = Length > 0 ? Length : DefaultRealFrameWidth;
        var height = Height > 0 ? Height : DefaultHeightForMode(CurrentToolMode);
        var (originX, originY) = ResolvePlacementOrigin(width, height);

        _isSyncingSelection = true;
        CoordinateX = originX;
        CoordinateY = originY;
        _isSyncingSelection = false;

        OnPropertyChanged(nameof(SelectedElementX));
        OnPropertyChanged(nameof(SelectedElementY));
    }

    public CadDocument Document { get; } = new();

    public CadViewport Viewport { get; } = new();

    public CadSnappingEngine Snapping { get; } = new();

    public CadCommandManager Commands { get; }

    public ObservableCollection<GeometryElement> CanvasElements { get; } = [];

    public ObservableCollection<CadDesignToolButtonViewModel> DesignTools { get; } = [];

    public event EventHandler? SceneInvalidated;

    public event EventHandler? NewTemplateRequested;

    public event EventHandler<FrameCreatedEventArgs>? FrameCreated;

    public Func<CadPoint, bool>? DesignPlacement { get; set; }

    public Action<DesignPlacementCommit>? OnDesignPlacementCommit { get; set; }

    public bool CanUndoPlacement => _placementHistory.CanUndo;

    public bool CanRedoPlacement => _placementHistory.CanRedo;

    public bool CanUndo => CanUndoPlacement || Commands.CanUndo;

    public IReadOnlyList<GeometryElement> DesignElements => CanvasElements;

    public string? ActiveDesignToolId { get; set; }

    public string? ActiveDesignToolLabel { get; set; }

    public ObservableCollection<string> ProfileSeries { get; } =
    [
        "ПВХ 60мм",
        "ПВХ 70мм",
        "Алюминий 50мм",
        "Алюминий 70мм"
    ];

    public ObservableCollection<ConstructionItemViewModel> Constructions { get; } = [];

    public ObservableCollection<MaterialModel> FilteredMaterials { get; } = [];

    public ObservableCollection<string> MaterialColors { get; } =
    [
        "Белый",
        "Коричневый",
        "Антрацит",
        "Золотой дуб",
        "Махагон",
        "Серый"
    ];

    public IReadOnlyList<CadDrawingTemplate> DrawingTemplates => _templateStore.Templates;

    [ObservableProperty]
    private string _activeToolId = "select";

    [ObservableProperty]
    private ICadTool _activeTool;

    [ObservableProperty]
    private WindowToolMode _currentToolMode = WindowToolMode.SelectElement;

    public WindowToolMode ActiveToolMode
    {
        get => CurrentToolMode;
        set
        {
            if (CurrentToolMode != value)
            {
                _ = SelectToolModeAsync(value);
            }
        }
    }

    [ObservableProperty]
    private string _statusText = "Выберите инструмент";

    [ObservableProperty]
    private double _cursorX;

    [ObservableProperty]
    private double _cursorY;

    [ObservableProperty]
    private double _coordinateX;

    [ObservableProperty]
    private double _coordinateY;

    [ObservableProperty]
    private double _length;

    [ObservableProperty]
    private double _height;

    [ObservableProperty]
    private bool _isGridEnabled = true;

    [ObservableProperty]
    private bool _isSnapEnabled = true;

    [ObservableProperty]
    private bool _isViewOnlyMode;

    [ObservableProperty]
    private bool _showDimensions = true;

    [ObservableProperty]
    private CadRect? _selectedRectangle;

    [ObservableProperty]
    private string? _selectedProfileSeries = "ПВХ 70мм";

    [ObservableProperty]
    private ConstructionItemViewModel? _selectedConstruction;

    [ObservableProperty]
    private bool _showAllMaterials;

    [ObservableProperty]
    private bool _isTiltBlockerEnabled;

    [ObservableProperty]
    private bool _isMicroVentilationEnabled;

    [ObservableProperty]
    private bool _isBalconyKitEnabled;

    [ObservableProperty]
    private bool _isMicroliftEnabled;

    [ObservableProperty]
    private int _constructionQuantity = 1;

    [ObservableProperty]
    private MaterialModel? _selectedMaterial;

    [ObservableProperty]
    private string? _selectedMaterialColor = "Белый";

    [ObservableProperty]
    private CadDrawingTemplate? _selectedDrawingTemplate;

    [ObservableProperty]
    private double _frameThickness = 80;

    [ObservableProperty]
    private double _impostThickness = 60;

    [ObservableProperty]
    private PlacementAnchor _placementAnchor = PlacementAnchor.BottomLeft;

    [ObservableProperty]
    private PlacementAnchorOption _selectedPlacementAnchorOption = PlacementAnchorOptionList[0];

    [ObservableProperty]
    private FrameNode? _frameRoot;

    [ObservableProperty]
    private FrameNode? _selectedElement;

    [ObservableProperty]
    private SashDragPreview? _sashDragPreview;

    [ObservableProperty]
    private CadRect? _realFramePreview;

    [ObservableProperty]
    private Guid? _handlePreviewSashId;

    [ObservableProperty]
    private bool _isLeftPanelVisible = true;

    [ObservableProperty]
    private bool _isRightPanelVisible;

    [ObservableProperty]
    private bool _isBottomPanelVisible = true;

    [ObservableProperty]
    private bool _isTopPanelVisible = true;

    public IRelayCommand<string> ActivateToolCommand { get; }

    public IRelayCommand<WindowToolMode> SelectToolModeCommand { get; }

    public Func<Task>? AddConstructionHandler { get; set; }

    public Func<Guid, Task>? SelectConstructionHandler { get; set; }

    public Func<Guid, Task>? RemoveConstructionHandler { get; set; }

    public Func<Task>? OpenConstructionsLookupHandler { get; set; }

    private bool _suppressConstructionSelectionCallback;
    private bool _isResettingCanvas;
    private int _canvasResetDepth;
    private Guid? _activeOrderConstructionId;

    public bool IsResettingCanvas => _isResettingCanvas || _canvasResetDepth > 0;

    public IRelayCommand ToggleLeftPanelCommand => new RelayCommand(() => IsLeftPanelVisible = !IsLeftPanelVisible);
    public IRelayCommand ToggleRightPanelCommand => new RelayCommand(() => IsRightPanelVisible = !IsRightPanelVisible);
    public IRelayCommand ToggleBottomPanelCommand => new RelayCommand(() => IsBottomPanelVisible = !IsBottomPanelVisible);
    public IRelayCommand ToggleTopPanelCommand => new RelayCommand(() => IsTopPanelVisible = !IsTopPanelVisible);

    public CadEditorViewModel()
        : this(new CadDialogService(), new CadTemplateStore(), null)
    {
    }

    public CadEditorViewModel(ICadDialogService dialogService, CadTemplateStore templateStore)
        : this(dialogService, templateStore, null)
    {
    }

    public CadEditorViewModel(ICadDialogService dialogService, CadTemplateStore templateStore, IMaterialRepository? materialRepository)
    {
        _dialogService = dialogService;
        _templateStore = templateStore;
        _materialRepository = materialRepository;
        Commands = new CadCommandManager(Document);
        ActivateToolCommand = new RelayCommand<string>(ActivateCadTool);
        SelectToolModeCommand = new RelayCommand<WindowToolMode>(mode => _ = SelectToolModeAsync(mode));

        InitializeDesignTools();
        EnsureDefaultConstructions();
        _isApplyingDrawingTemplate = true;
        SelectedDrawingTemplate = DrawingTemplates.FirstOrDefault();
        _isApplyingDrawingTemplate = false;
        _ = LoadMaterialsAsync();
        ActiveTool = _toolFactory.Create(CadToolMetadata.GetId(CadToolKind.SelectElements));
        ActiveToolId = CadToolMetadata.GetId(CadToolKind.SelectElements);
        UpdateDesignToolSelection(WindowToolMode.SelectElement);
        Viewport.ResetToDefaultScale(15.0);
        _placementHistory.Changed += (_, _) => NotifyPlacementHistoryChanged();
        Commands.HistoryChanged += (_, _) => NotifyPlacementHistoryChanged();
    }

    public double MillimetersPerPixel =>
        Viewport.ZoomFactor > CadMath.Epsilon ? 1.0 / Viewport.ZoomFactor : 15.0;

    public string ZoomPercent => $"{Viewport.ZoomFactor * 100:0}%";

    public string ActiveToolDisplayName => ActiveDesignToolLabel ?? ActiveTool.DisplayName;

    public bool IsPlacementToolActive =>
        CurrentToolMode is WindowToolMode.RealFrame or WindowToolMode.FakeFrame
            or WindowToolMode.FakeFrameCircle or WindowToolMode.FakeFrameTriangle
            or WindowToolMode.FakeFrameArch;

    public bool IsDragCreateShapeToolActive => IsDragCreateShapeMode(CurrentToolMode);

    public string CanvasHintText => string.Empty;

    public Action<string>? CanvasHoverHintChanged { get; set; }

    private string _lastCanvasHoverHint = string.Empty;

    public bool HasFakeFrameOnCanvas
    {
        get
        {
            var kind = CanvasElements.FirstOrDefault()?.Kind;
            return kind is GeometryElementKind.DummyFrame
                or GeometryElementKind.RoundDummyFrame
                or GeometryElementKind.TriangleDummyFrame
                or GeometryElementKind.ArchDummyFrame;
        }
    }

    public bool HasFrameOnCanvas => FrameRoot != null && CanvasElements.Count > 0;

    public bool IsShapePlacementActive => _realFrameStartWorld != null;

    public bool IsRealFramePlacementActive => IsShapePlacementActive;

    public double SelectedElementLength
    {
        get
        {
            if (RealFramePreview is { Width: > 0 } preview)
            {
                return RoundMm(preview.Width);
            }

            if (SelectedElement is { Width: > 0 } frame)
            {
                return RoundMm(frame.Width);
            }

            var element = CanvasElements.FirstOrDefault();
            return element != null ? RoundMm((double)element.WidthMm) : RoundMm(Length);
        }
    }

    public double SelectedElementHeight
    {
        get
        {
            if (RealFramePreview is { Height: > 0 } preview)
            {
                return RoundMm(preview.Height);
            }

            if (SelectedElement is { Height: > 0 } frame)
            {
                return RoundMm(frame.Height);
            }

            var element = CanvasElements.FirstOrDefault();
            return element != null ? RoundMm((double)element.HeightMm) : RoundMm(Height);
        }
    }

    public double SelectedElementX =>
        SelectedElement is FrameNode frame ? RoundMm(frame.X) : RoundMm(CoordinateX);

    public double SelectedElementY =>
        SelectedElement is FrameNode frame ? RoundMm(frame.Y) : RoundMm(CoordinateY);

    public bool IsImpostPanelActive =>
        _selectedMullion != null &&
        CurrentToolMode is WindowToolMode.OffsetImpostVertical or WindowToolMode.OffsetImpostHorizontal;

    public VerticalImpostTarget? SelectedMullion => _selectedMullion;

    public DimensionHitInfo? ActiveDimensionHit => _activeDimensionHit;

    public bool IsVerticalImpostDragging => _isVerticalImpostDragging;

    public bool IsHorizontalImpostDragging => _isHorizontalImpostDragging;

    public double? DraggingMullionWorldX => _draggingMullionWorldX;

    public double? DraggingMullionWorldY => _draggingMullionWorldY;

    public bool IsSelectedForOffset(FrameNode node, int uniformGapIndex = 0) =>
        _selectedMullion?.Matches(node, uniformGapIndex) == true;

    [ObservableProperty]
    private double _impostPanelX;

    [ObservableProperty]
    private double _impostPanelY;

    [ObservableProperty]
    private double _impostPanelL;

    [ObservableProperty]
    private double _impostPanelH;

    public bool IsVerticalImpostHighlighted(FrameNode node, int uniformGapIndex = 0) =>
        CurrentToolMode == WindowToolMode.OffsetImpostVertical &&
        !_draggingMullionWorldX.HasValue &&
        IsSelectedForOffset(node, uniformGapIndex);

    public bool IsHorizontalImpostHighlighted(FrameNode node, int uniformGapIndex = 0) =>
        CurrentToolMode == WindowToolMode.OffsetImpostHorizontal &&
        !_draggingMullionWorldY.HasValue &&
        IsSelectedForOffset(node, uniformGapIndex);

    public double GridSnapStep => Viewport.ChooseGridStep();

    public void SetDesignElements(IReadOnlyList<GeometryElement> elements)
    {
        if (_isResettingCanvas && _canvasResetDepth == 0)
        {
            return;
        }

        CanvasElements.Clear();
        foreach (var element in elements)
        {
            CanvasElements.Add(element);
        }

        if (FrameRoot != null && CanvasElements.Count > 0)
        {
            ApplyFrameSelection(FrameRoot);
        }

        RequestSceneRefresh();
    }

    public void SetViewportSize(double width, double height)
    {
        if (Math.Abs(Viewport.Width - Math.Max(1, width)) < CadMath.Epsilon &&
            Math.Abs(Viewport.Height - Math.Max(1, height)) < CadMath.Epsilon)
        {
            return;
        }

        Viewport.Resize(width, height);
        OnPropertyChanged(nameof(MillimetersPerPixel));
        RequestSceneRefresh();
    }

    public void RequestSceneRefresh()
    {
        OnPropertyChanged(nameof(MillimetersPerPixel));
        SceneInvalidated?.Invoke(this, EventArgs.Empty);
    }

    public void ClearDimensionHits() => _dimensionHits.Clear();

    public void RegisterDimensionHit(DimensionHitInfo hit) => _dimensionHits.Add(hit);

    public bool IsDimensionLabelBeingEdited(DimensionHitKind kind, int segmentIndex = 0) =>
        _activeDimensionHit is { } active &&
        active.Kind == kind &&
        active.SegmentIndex == segmentIndex;

    public bool TryBeginDimensionEdit(Point screenPoint)
    {
        if (!ShowDimensions || IsViewOnlyMode || FrameRoot is null || _activeDimensionHit is not null)
        {
            return false;
        }

        var hit = HitTestDimension(screenPoint);
        if (hit is null)
        {
            return false;
        }

        _activeDimensionHit = hit;
        OnPropertyChanged(nameof(ActiveDimensionHit));
        RequestSceneRefresh();
        return true;
    }

    public void CancelDimensionEdit()
    {
        if (_activeDimensionHit is null)
        {
            return;
        }

        _activeDimensionHit = null;
        OnPropertyChanged(nameof(ActiveDimensionHit));
        RequestSceneRefresh();
    }

    public bool DismissDimensionEditIfClickOutside(Point screenPoint)
    {
        if (_activeDimensionHit is null)
        {
            return false;
        }

        if (HitTestDimension(screenPoint) is not null)
        {
            return false;
        }

        CancelDimensionEdit();
        return true;
    }

    public bool TryCommitDimensionEdit(string? text)
    {
        if (_activeDimensionHit is not { } hit)
        {
            return false;
        }

        if (!TryParseDimensionInput(text, out var newValueMm))
        {
            StatusText = "Некорректное значение размера";
            CancelDimensionEdit();
            return false;
        }

        if (Math.Abs(newValueMm - hit.ValueMm) < CadMath.Epsilon)
        {
            CancelDimensionEdit();
            return true;
        }

        var applied = RecordPlacement(
            $"Размер {hit.Kind}: {hit.ValueMm:0} → {newValueMm:0} мм",
            () => TryChangeDimension(hit, newValueMm));

        CancelDimensionEdit();

        if (applied)
        {
            SyncFrameGeometryToDocument();
            RequestSceneRefresh();
            StatusText = $"Размер изменён: {newValueMm:0} мм";
        }
        else
        {
            StatusText = $"Минимальный проём — {MinEditableOpeningMm:0} мм";
        }

        return applied;
    }

    /// <summary>Применяет новое значение размерной надписи (в мм).</summary>
    public bool TryChangeDimension(DimensionHitInfo hit, double newValueMm)
    {
        if (newValueMm <= 0 || double.IsNaN(newValueMm) || double.IsInfinity(newValueMm))
        {
            return false;
        }

        newValueMm = RoundMm(newValueMm);

        return hit.Kind switch
        {
            DimensionHitKind.OverallWidth => TryChangeOverallWidth(newValueMm),
            DimensionHitKind.OverallHeight => TryChangeOverallHeight(newValueMm),
            DimensionHitKind.SegmentWidth => TryChangeSegmentWidth(hit, newValueMm),
            _ => false
        };
    }

    [RelayCommand]
    private void ChangeDimension(string? valueText)
    {
        if (_activeDimensionHit is not null)
        {
            TryCommitDimensionEdit(valueText);
        }
    }

    private DimensionHitInfo? HitTestDimension(Point screenPoint)
    {
        DimensionHitInfo? best = null;
        var bestArea = double.MaxValue;

        foreach (var hit in _dimensionHits)
        {
            if (!hit.ScreenRect.Contains(screenPoint))
            {
                continue;
            }

            var area = hit.ScreenRect.Width * hit.ScreenRect.Height;
            if (area < bestArea)
            {
                bestArea = area;
                best = hit;
            }
        }

        return best;
    }

    private const string CanvasHintImpost =
        "Зажмите левую кнопку мыши (ЛКМ), чтобы переместить импост";
    private const string CanvasHintDimension =
        "Нажмите, чтобы изменить значение размера конструкции";
    private const string CanvasHintSash =
        "Выберите створку, чтобы настроить фурнитуру и тип открывания";
    private const string CanvasHintEmpty =
        "Чертеж конструкции. Используйте инструменты для редактирования";

    public void UpdateCanvasHoverHint(Point screenPoint, CadPoint worldPoint)
    {
        var hint = ResolveCanvasHoverHint(screenPoint, worldPoint);
        if (string.Equals(hint, _lastCanvasHoverHint, StringComparison.Ordinal))
        {
            return;
        }

        _lastCanvasHoverHint = hint;
        CanvasHoverHintChanged?.Invoke(hint);
    }

    public void ClearCanvasHoverHint()
    {
        if (string.IsNullOrEmpty(_lastCanvasHoverHint))
        {
            return;
        }

        _lastCanvasHoverHint = string.Empty;
        CanvasHoverHintChanged?.Invoke(string.Empty);
    }

    private string ResolveCanvasHoverHint(Point screenPoint, CadPoint worldPoint)
    {
        if (ShowDimensions && HitTestDimension(screenPoint) is not null)
        {
            return CanvasHintDimension;
        }

        if (HitTestVerticalImpost(worldPoint) is not null || HitTestHorizontalImpost(worldPoint) is not null)
        {
            return CanvasHintImpost;
        }

        if (FindSashAt(worldPoint) is not null)
        {
            return CanvasHintSash;
        }

        return CanvasHintEmpty;
    }

    private static bool TryParseDimensionInput(string? text, out double valueMm)
    {
        valueMm = 0;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalized = text.Trim().Replace(',', '.');
        return double.TryParse(
            normalized,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out valueMm) && valueMm > 0;
    }

    private bool TryChangeOverallWidth(double newWidthMm)
    {
        if (FrameRoot is null || newWidthMm < MinEditableOpeningMm)
        {
            return false;
        }

        if (!FrameRoot.TryResizeOuter(newWidthMm, FrameRoot.Height))
        {
            return false;
        }

        SyncFillingsToFrameLayout();
        return true;
    }

    private bool TryChangeOverallHeight(double newHeightMm)
    {
        if (FrameRoot is null || newHeightMm < MinEditableOpeningMm)
        {
            return false;
        }

        if (!FrameRoot.TryResizeOuter(FrameRoot.Width, newHeightMm))
        {
            return false;
        }

        SyncFillingsToFrameLayout();
        return true;
    }

    private bool TryChangeSegmentWidth(DimensionHitInfo hit, double newValueMm)
    {
        if (FrameRoot is null
            || hit.BoundaryWorldX is not { Count: >= 2 } boundaries
            || newValueMm < MinEditableOpeningMm)
        {
            return false;
        }

        if (!ValidateNeighborSegmentWidth(boundaries, hit.SegmentIndex, hit.MoveRightBoundary, newValueMm))
        {
            return false;
        }

        double newAxisWorldX = hit.MoveRightBoundary
            ? boundaries[hit.SegmentIndex] + newValueMm
            : boundaries[hit.SegmentIndex + 1] - newValueMm;

        var target = hit.ImpostTarget;
        if (target is null)
        {
            return false;
        }

        return MoveVerticalImpost(target, newAxisWorldX);
    }

    private static bool ValidateNeighborSegmentWidth(
        IReadOnlyList<double> boundaries,
        int segmentIndex,
        bool moveRightBoundary,
        double newValueMm)
    {
        if (newValueMm < MinEditableOpeningMm)
        {
            return false;
        }

        double neighborWidth;
        if (moveRightBoundary)
        {
            if (segmentIndex + 2 >= boundaries.Count)
            {
                return true;
            }

            neighborWidth = boundaries[segmentIndex + 2] - (boundaries[segmentIndex] + newValueMm);
        }
        else
        {
            if (segmentIndex <= 0)
            {
                return true;
            }

            neighborWidth = (boundaries[segmentIndex + 1] - newValueMm) - boundaries[segmentIndex - 1];
        }

        return neighborWidth >= MinEditableOpeningMm;
    }

    private bool MoveVerticalImpost(VerticalImpostTarget target, double axisWorldX)
    {
        var node = target.SplitNode;
        bool moved;
        if (node.IsUniformSplit && target.UniformGapIndex is int gapIndex)
        {
            moved = node.TrySetUniformVerticalImpostGapAxisWorldX(gapIndex, axisWorldX);
        }
        else
        {
            moved = node.TrySetVerticalImpostAxisWorldX(axisWorldX);
        }

        if (moved)
        {
            SyncFillingsToFrameLayout();
        }

        return moved;
    }

    private bool MoveHorizontalImpost(VerticalImpostTarget target, double axisWorldY)
    {
        var node = target.SplitNode;
        bool moved;
        if (node.IsUniformSplit && target.UniformGapIndex is int gapIndex)
        {
            moved = node.TrySetUniformHorizontalImpostGapAxisWorldY(gapIndex, axisWorldY);
        }
        else
        {
            moved = node.TrySetHorizontalImpostAxisWorldY(axisWorldY);
        }

        if (moved)
        {
            SyncFillingsToFrameLayout();
        }

        return moved;
    }

    /// <summary>
    /// Пересчитывает координаты и размеры створок и остекления по актуальной геометрии листьев FrameRoot.
    /// </summary>
    private void SyncFillingsToFrameLayout()
    {
        if (FrameRoot is null)
        {
            return;
        }

        foreach (var element in CanvasElements)
        {
            if (element.Kind == GeometryElementKind.Sash)
            {
                SyncSashElementToLeaf(element);
            }
        }

        foreach (var element in CanvasElements)
        {
            if (element.Kind == GeometryElementKind.Glazing)
            {
                SyncGlazingElementToLayout(element);
            }
        }
    }

    private void SyncSashElementToLeaf(GeometryElement sash)
    {
        var leaf = FrameRoot!.FindLeafAt((double)sash.X, (double)sash.Y);
        if (leaf is not { IsLeaf: true })
        {
            return;
        }

        var (innerX, innerY, innerW, innerH) = SashGeometry.ResolveLeafInnerOpening(
            leaf,
            FrameRoot,
            FrameThickness);
        if (innerW <= 0 || innerH <= 0)
        {
            return;
        }

        sash.X = (decimal)(innerX + innerW / 2.0);
        sash.Y = (decimal)(innerY + innerH / 2.0);
        sash.WidthMm = (decimal)innerW;
        sash.HeightMm = (decimal)innerH;
    }

    private void SyncGlazingElementToLayout(GeometryElement glazing)
    {
        if (glazing.HostSashId is Guid sashId)
        {
            var sash = CanvasElements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
            if (sash is null)
            {
                return;
            }

            var leaf = FrameRoot!.FindLeafAt((double)sash.X, (double)sash.Y);
            var updated = CreateGlazingElementForSash(sash, leaf);
            if (updated is null)
            {
                return;
            }

            glazing.X = updated.X;
            glazing.Y = updated.Y;
            glazing.WidthMm = updated.WidthMm;
            glazing.HeightMm = updated.HeightMm;
            return;
        }

        var glazingLeaf = FrameRoot!.FindLeafAt((double)glazing.X, (double)glazing.Y);
        if (glazingLeaf is not { IsLeaf: true })
        {
            return;
        }

        var standalone = CreateGlazingElement(glazingLeaf);
        if (standalone is null)
        {
            return;
        }

        glazing.X = standalone.X;
        glazing.Y = standalone.Y;
        glazing.WidthMm = standalone.WidthMm;
        glazing.HeightMm = standalone.HeightMm;
    }

    private void SyncFrameGeometryToDocument()
    {
        if (FrameRoot is null)
        {
            return;
        }

        var bounds = new CadRect(
            FrameRoot.X,
            FrameRoot.Y + FrameRoot.Height,
            FrameRoot.Width,
            FrameRoot.Height);
        UpdatePrimaryElementGeometry(bounds);

        if (Document.Rectangles.Count > 0)
        {
            var existing = Document.Rectangles[0];
            Document.RemoveRectangle(existing);
            Document.AddRectangle(bounds);
            SelectedRectangle = bounds;
        }

        NotifyFrameSelectionProperties();
    }

    public void FitToCanvas(double itemWidthMm, double itemHeightMm)
    {
        FitToCanvas(new CadRect(0, itemHeightMm, itemWidthMm, itemHeightMm));
    }

    public void FitToCanvas(CadRect bounds)
    {
        Viewport.FitToCanvas(bounds);
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));
        RequestSceneRefresh();
    }

    public CadRect GetWorldBounds()
    {
        if (FrameRoot != null)
        {
            return new CadRect(
                FrameRoot.X,
                FrameRoot.Y + FrameRoot.Height,
                FrameRoot.Width,
                FrameRoot.Height);
        }

        var rects = Document.Rectangles;
        if (rects.Count == 0)
        {
            return new CadRect(-200, 200, 400, 400);
        }

        var minX = rects.Min(r => r.X);
        var maxX = rects.Max(r => r.X + r.Width);
        var minY = rects.Min(r => r.Y - r.Height);
        var maxY = rects.Max(r => r.Y);

        return new CadRect(minX - 100, maxY + 100, maxX - minX + 200, maxY - minY + 200);
    }

    /// <summary>Границы только установленной конструкции (без полей холста) — для миниатюр.</summary>
    public CadRect GetConstructionContentBounds()
    {
        if (FrameRoot != null)
        {
            return new CadRect(
                FrameRoot.X,
                FrameRoot.Y + FrameRoot.Height,
                FrameRoot.Width,
                FrameRoot.Height);
        }

        if (CanvasElements.Count > 0)
        {
            var minX = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var minY = double.PositiveInfinity;
            var maxY = double.NegativeInfinity;

            foreach (var element in CanvasElements)
            {
                var halfW = (double)element.WidthMm / 2.0;
                var halfH = (double)element.HeightMm / 2.0;
                var centerX = (double)element.X;
                var centerY = (double)element.Y;
                minX = Math.Min(minX, centerX - halfW);
                maxX = Math.Max(maxX, centerX + halfW);
                minY = Math.Min(minY, centerY - halfH);
                maxY = Math.Max(maxY, centerY + halfH);
            }

            if (minX < maxX && minY < maxY)
            {
                return new CadRect(minX, maxY, maxX - minX, maxY - minY);
            }
        }

        var rects = Document.Rectangles;
        if (rects.Count > 0)
        {
            var minX = rects.Min(r => r.X);
            var maxX = rects.Max(r => r.X + r.Width);
            var minY = rects.Min(r => r.Y - r.Height);
            var maxY = rects.Max(r => r.Y);
            return new CadRect(minX, maxY, maxX - minX, maxY - minY);
        }

        return GetWorldBounds();
    }

    /// <summary>Границы конструкции с запасом под выносные размерные линии (для превью).</summary>
    public CadRect GetPreviewViewportBounds()
    {
        var content = GetConstructionContentBounds();
        if (content.Width <= 0 || content.Height <= 0)
        {
            return content;
        }

        var span = Math.Max(content.Width, content.Height);
        var paddingRatio = Math.Clamp(180.0 / span, 0.18, 0.32);
        return InflateContentBounds(content, paddingRatio);
    }

    public void FitThumbnailPreview(double marginPx = 8, double boundsPaddingRatio = 0.36)
    {
        var bounds = InflateContentBounds(GetConstructionContentBounds(), boundsPaddingRatio);
        var margin = marginPx > 0
            ? marginPx
            : Math.Max(10, Math.Min(Viewport.Width, Viewport.Height) * 0.16);
        Viewport.FitToBounds(bounds, margin);
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));
    }

    public void FitPreviewViewport(double fillRatio = 0.85)
    {
        if (Viewport.Width <= CadMath.Epsilon || Viewport.Height <= CadMath.Epsilon)
        {
            return;
        }

        Viewport.FitToCanvas(GetPreviewViewportBounds(), fillRatio);
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));
        RequestSceneRefresh();
    }

    public void FitDocumentToViewport()
    {
        FitPreviewViewport();
    }

    private static CadRect InflateContentBounds(CadRect bounds, double ratio)
    {
        if (ratio <= CadMath.Epsilon || bounds.Width <= 0 || bounds.Height <= 0)
        {
            return bounds;
        }

        var padX = bounds.Width * ratio / 2.0;
        var padY = bounds.Height * ratio / 2.0;
        return new CadRect(
            bounds.X - padX,
            bounds.Y + padY,
            bounds.Width + padX * 2,
            bounds.Height + padY * 2);
    }

    public CadToolContext CreateToolContext() => new(Document, Commands, Snapping)
    {
        Viewport = Viewport,
        Invalidate = () => RequestSceneRefresh(),
        CommitPlacement = commit =>
        {
            if (!TryApplyMullionLineToFrame(commit))
            {
                OnDesignPlacementCommit?.Invoke(commit);
            }
        },
        ActiveDesignToolId = ActiveDesignToolId,
        ActiveDesignToolLabel = ActiveDesignToolLabel,
        SnapPoint = SnapWorldPoint,
        FrameProfileThickness = FrameThickness,
        SetStatus = msg => StatusText = msg,
        TryUndo = TryUndoFromTool,
        TryDeleteLastPlaced = TryDeleteLastPlacedFromTool,
        TryDeleteSelection = TryDeleteSelectionFromTool,
        TryClearConstruction = TryClearConstructionFromTool,
        TryChangeOverallDimensions = TryChangeOverallDimensionsFromTool,
        HitTestSelection = HitTestSelectionTarget,
        SelectTarget = SelectConstructionTarget,
        GetFrameRoot = () => FrameRoot,
        SnapAxis = SnapToGrid
    };

    private void ActivateCadTool(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return;
        }

        var previous = ActiveTool;
        var tool = _toolFactory.Create(id);
        var context = CreateToolContext();

        previous?.OnDeactivated(context);

        ActiveTool = tool;
        ActiveToolId = id;
        OnPropertyChanged(nameof(ActiveToolDisplayName));

        tool.OnActivated(context);

        if (tool.Interaction == CadToolInteraction.InstantAction)
        {
            var fallback = _toolFactory.Create(CadToolMetadata.GetId(CadToolKind.SelectElements));
            tool.OnDeactivated(context);
            ActiveTool = fallback;
            ActiveToolId = fallback.Id;
            fallback.OnActivated(CreateToolContext());
        }

        RequestSceneRefresh();
    }

    private bool TryUndoFromTool()
    {
        if (TryUndoConstructionSnapshot())
        {
            return true;
        }

        if (!Commands.CanUndo)
        {
            return false;
        }

        Commands.Undo();
        RefreshSelection();
        RequestSceneRefresh();
        StatusText = "Действие отменено";
        return true;
    }

    private bool TryDeleteLastPlacedFromTool() => TryUndoLastPlacement();

    private bool TryUndoLastPlacement()
    {
        if (!_placementHistory.TryUndo(ApplyConstructionMemento, out var description))
        {
            StatusText = "Нет элементов для удаления";
            return false;
        }

        RefreshAfterPlacementHistoryChange();
        StatusText = description ?? "Последний элемент удалён";
        return true;
    }

    private bool TryUndoConstructionSnapshot()
    {
        if (!_placementHistory.TryUndo(ApplyConstructionMemento, out var description))
        {
            return false;
        }

        RefreshAfterPlacementHistoryChange();
        StatusText = description ?? "Действие отменено";
        return true;
    }

    private void RecordPlacement(string description, Action applyChange)
    {
        if (_isResettingCanvas)
        {
            applyChange();
            return;
        }

        var before = CaptureConstructionMemento();
        applyChange();
        var after = CaptureConstructionMemento();
        _placementHistory.Record(before, after, description);
        NotifyPlacementHistoryChanged();
    }

    private bool RecordPlacement(string description, Func<bool> tryApply)
    {
        if (_isResettingCanvas)
        {
            return tryApply();
        }

        var before = CaptureConstructionMemento();
        if (!tryApply())
        {
            return false;
        }

        _placementHistory.Record(before, CaptureConstructionMemento(), description);
        NotifyPlacementHistoryChanged();
        return true;
    }

    private ConstructionMemento CaptureConstructionMemento() => new()
    {
        FrameRootSnapshot = FrameRoot?.CaptureTreeSnapshot(),
        Elements = CanvasElements.Select(ConstructionMemento.CloneElement).ToList(),
        DocumentRect = Document.Rectangles.Count > 0 ? Document.Rectangles[0] : null
    };

    private void ApplyConstructionMemento(ConstructionMemento memento)
    {
        FrameRoot = memento.FrameRootSnapshot is null
            ? null
            : FrameNode.RestoreTree(memento.FrameRootSnapshot);

        Document.ClearRectangles();
        if (memento.DocumentRect is CadRect rect)
        {
            Document.AddRectangle(rect);
            SelectedRectangle = rect;
        }
        else
        {
            SelectedRectangle = null;
        }

        CanvasElements.Clear();
        foreach (var element in memento.Elements)
        {
            CanvasElements.Add(ConstructionMemento.CloneElement(element));
        }
    }

    private void RefreshAfterPlacementHistoryChange()
    {
        PurgeOrphanedSashes();
        RebuildImpostSplitHistory();
        OnPropertyChanged(nameof(HasFrameOnCanvas));
        OnPropertyChanged(nameof(HasFakeFrameOnCanvas));
        OnPropertyChanged(nameof(HasRealFrameOnCanvas));
        OnPropertyChanged(nameof(FrameRoot));
        RefreshSelection();
        RequestSceneRefresh();
    }

    private void PurgeOrphanedSashes()
    {
        if (FrameRoot is null)
        {
            for (var i = CanvasElements.Count - 1; i >= 0; i--)
            {
                if (CanvasElements[i].Kind == GeometryElementKind.Sash)
                {
                    CanvasElements.RemoveAt(i);
                }
            }

            return;
        }

        for (var i = CanvasElements.Count - 1; i >= 0; i--)
        {
            var element = CanvasElements[i];
            if (element.Kind != GeometryElementKind.Sash)
            {
                continue;
            }

            if (!IsSashAnchoredInFrame(element))
            {
                CanvasElements.RemoveAt(i);
            }
        }
    }

    private bool IsSashAnchoredInFrame(GeometryElement sash)
    {
        if (FrameRoot is null)
        {
            return false;
        }

        var centerX = (double)sash.X;
        var centerY = (double)sash.Y;
        var leaf = FrameRoot.FindLeafAt(centerX, centerY);
        return leaf is { IsLeaf: true };
    }

    private void RebuildImpostSplitHistory()
    {
        _impostSplitHistory.Clear();
        if (FrameRoot is null)
        {
            return;
        }

        CollectImpostSplitNodes(FrameRoot);
    }

    private void CollectImpostSplitNodes(FrameNode node)
    {
        if (node.IsLeaf)
        {
            return;
        }

        _impostSplitHistory.Add(node);

        if (node.IsUniformSplit)
        {
            foreach (var cell in node.UniformCells)
            {
                CollectImpostSplitNodes(cell);
            }

            return;
        }

        if (node.ChildA is not null)
        {
            CollectImpostSplitNodes(node.ChildA);
        }

        if (node.ChildB is not null)
        {
            CollectImpostSplitNodes(node.ChildB);
        }
    }

    private void NotifyPlacementHistoryChanged()
    {
        OnPropertyChanged(nameof(CanUndoPlacement));
        OnPropertyChanged(nameof(CanRedoPlacement));
        OnPropertyChanged(nameof(CanUndo));
        UndoLastElementCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private bool TryDeleteSelectionFromTool()
    {
        if (SelectedRectangle != null)
        {
            Document.RemoveRectangle(SelectedRectangle.Value);
            SelectedRectangle = null;
            RefreshSelection();
            StatusText = "Выбранный объект удалён";
            return true;
        }

        if (SelectedElement is FrameNode)
        {
            ClearCommittedFrameGeometry();
            StatusText = "Конструкция удалена";
            return true;
        }

        return false;
    }

    private bool TryClearConstructionFromTool()
    {
        if (FrameRoot == null && Document.Rectangles.Count == 0 && CanvasElements.Count == 0)
        {
            return false;
        }

        ClearCommittedFrameGeometry();
        StatusText = "Конструкция очищена";
        return true;
    }

    private void TrackImpostSplit(FrameNode splitNode)
    {
        if (!splitNode.IsLeaf)
        {
            _impostSplitHistory.Add(splitNode);
        }
    }

    private double ResolveFrameSplitInset(FrameNode leaf) =>
        leaf.Parent == null
            ? FrameProfileGeometry.ResolveInnerInset(leaf.Width, leaf.Height, FrameThickness)
            : 0;

    private bool TryApplyMullionLineToFrame(DesignPlacementCommit commit)
    {
        if (FrameRoot is null || commit.Kind != DesignPlacementKind.MullionLine || !HasFrameOnCanvas)
        {
            return false;
        }

        var point = commit.Anchor;
        var leaf = FrameRoot.FindLeafAt(point.X, point.Y);
        if (leaf is null || !leaf.IsLeaf)
        {
            return false;
        }

        var impostWidth = SingleMullionPlacementWidthMm;

        if (!RecordPlacement(
                string.IsNullOrWhiteSpace(commit.Label) ? "Импост установлен" : $"{commit.Label} установлен",
                () =>
                {
                    var split = commit.ToolId switch
                    {
                        "mullion-place-h" => leaf.TrySplitHorizontalAt(
                            point.Y - leaf.Y,
                            ImpostLineStyle.Solid,
                            impostWidth),
                        "mullion-place-d" => leaf.AddSplit(
                            SplitOrientation.Vertical,
                            point.X - leaf.X,
                            ImpostLineStyle.Slanted,
                            impostWidth),
                        _ => leaf.TrySplitVerticalAt(
                            point.X - leaf.X,
                            ImpostLineStyle.Solid,
                            impostWidth)
                    };

                    if (!split)
                    {
                        return false;
                    }

                    TrackImpostSplit(leaf);
                    SelectedElement = FrameRoot;
                    OnPropertyChanged(nameof(FrameRoot));
                    return true;
                }))
        {
            StatusText = "Невозможно установить импост в этой ячейке";
            return false;
        }

        StatusText = string.IsNullOrWhiteSpace(commit.Label)
            ? "Импост установлен"
            : $"{commit.Label} установлен";
        RequestSceneRefresh();
        return true;
    }

    private bool TryChangeOverallDimensionsFromTool()
    {
        StatusText = "Изменение габаритов — в разработке";
        return false;
    }

    private CadSelectionTarget? HitTestSelectionTarget(CadPoint worldPoint, CadSelectionMode mode)
    {
        if (FrameRoot == null)
        {
            return null;
        }

        return mode switch
        {
            CadSelectionMode.Object => new CadSelectionTarget(mode, "Конструкция", worldPoint),
            CadSelectionMode.Profile or CadSelectionMode.Vertex =>
                FrameRoot.FindLeafAt(worldPoint.X, worldPoint.Y) != null
                    ? new CadSelectionTarget(mode, "Ячейка", worldPoint)
                    : null,
            _ => null
        };
    }

    private void SelectConstructionTarget(CadSelectionTarget target)
    {
        if (FrameRoot == null)
        {
            return;
        }

        if (target.Mode == CadSelectionMode.Object)
        {
            ApplyFrameSelection(FrameRoot);
            StatusText = "Выбрана конструкция целиком";
            return;
        }

        var leaf = FrameRoot.FindLeafAt(target.WorldPoint.X, target.WorldPoint.Y);
        if (leaf != null)
        {
            SelectedElement = leaf;
            StatusText = $"Выбран элемент: {target.Mode}";
        }
    }

    public void BeginShapePlacement(CadPoint worldPoint)
    {
        if (!IsDragCreateShapeMode(CurrentToolMode))
        {
            return;
        }

        ClearCommittedFrameGeometry();
        _realFrameStartWorld = RoundMm(worldPoint);
        RealFramePreview = null;
        OnPropertyChanged(nameof(RealFramePreview));
        OnPropertyChanged(nameof(IsShapePlacementActive));
        OnPropertyChanged(nameof(IsRealFramePlacementActive));
        RequestSceneRefresh();
    }

    public void BeginRealFramePlacement(CadPoint worldPoint) => BeginShapePlacement(worldPoint);

    public void UpdateShapePlacementPreview(CadPoint currentWorld)
    {
        if (_realFrameStartWorld is not CadPoint start)
        {
            return;
        }

        var current = RoundMm(currentWorld);
        var originX = RoundMm(Math.Min(start.X, current.X));
        var originY = RoundMm(Math.Min(start.Y, current.Y));
        var width = RoundMm(Math.Abs(current.X - start.X));
        var height = RoundMm(Math.Abs(current.Y - start.Y));

        RealFramePreview = new CadRect(originX, originY + height, width, height);
        _isSyncingSelection = true;
        Length = width;
        Height = height;
        CoordinateX = originX;
        CoordinateY = originY;
        _isSyncingSelection = false;
        OnPropertyChanged(nameof(RealFramePreview));
        OnPropertyChanged(nameof(Length));
        OnPropertyChanged(nameof(Height));
        OnPropertyChanged(nameof(SelectedElementLength));
        OnPropertyChanged(nameof(SelectedElementHeight));
        OnPropertyChanged(nameof(SelectedElementX));
        OnPropertyChanged(nameof(SelectedElementY));
        RequestSceneRefresh();
    }

    public void UpdateRealFramePreview(CadPoint currentWorld) => UpdateShapePlacementPreview(currentWorld);

    public bool TryCompleteShapeDrag(CadPoint endWorld)
    {
        if (!IsDragCreateShapeMode(CurrentToolMode) || _realFrameStartWorld is not CadPoint start)
        {
            CancelShapePlacement();
            return false;
        }

        try
        {
            var end = RoundMm(endWorld);
            var width = RoundMm(Math.Abs(end.X - start.X));
            var height = RoundMm(Math.Abs(end.Y - start.Y));
            var originX = RoundMm(Math.Min(start.X, end.X));
            var originY = RoundMm(Math.Min(start.Y, end.Y));

            if (!CommitShapeAt(CurrentToolMode, originX, originY, width, height))
            {
                _isCommittingRealFrame = false;
                ClearShapePlacementFlags();
                return false;
            }

            CompleteFrameCreationAndSelection();
            StatusText = $"{GetDragShapeKindLabel(CurrentToolMode).Label} создана";
            return true;
        }
        finally
        {
            OnPropertyChanged(nameof(FrameRoot));
            OnPropertyChanged(nameof(SelectedElement));
            RequestSceneRefresh();
        }
    }

    public bool TryCompleteRealFrameDrag(CadPoint endWorld) => TryCompleteShapeDrag(endWorld);

    public bool IsPropertiesPanelVisible
    {
        get => IsLeftPanelVisible;
        set => IsLeftPanelVisible = value;
    }

    public void PrepareShapeSizeEntry()
    {
        if (!IsDragCreateShapeMode(CurrentToolMode))
        {
            return;
        }

        ClearShapePlacementFlags();
        _isCommittingRealFrame = false;
        _realFrameStartWorld = null;
        ClearCommittedFrameGeometry();

        IsPropertiesPanelVisible = true;

        var defaultHeight = DefaultHeightForMode(CurrentToolMode);
        var (targetX, targetY) = ResolvePlacementOrigin(DefaultRealFrameWidth, defaultHeight);

        _isSyncingSelection = true;
        Length = DefaultRealFrameWidth;
        Height = defaultHeight;
        CoordinateX = targetX;
        CoordinateY = targetY;
        _isSyncingSelection = false;

        NotifyFrameSelectionProperties();
        RequestSceneRefresh();
    }

    public void PrepareRealFrameSizeEntry() => PrepareShapeSizeEntry();

    public async Task BeginShapeSizeEntryFromRightClickAsync()
    {
        if (!IsDragCreateShapeMode(CurrentToolMode) || _isShapeSizeDialogOpen)
        {
            return;
        }

        _isShapeSizeDialogOpen = true;
        try
        {
            PrepareShapeSizeEntry();

            var initialHeight = DefaultHeightForMode(CurrentToolMode);
            var (defaultX, defaultY) = ResolvePlacementOrigin(DefaultRealFrameWidth, initialHeight);
            var result = await _dialogService.ShowSizeInputAsync(
                DefaultRealFrameWidth,
                initialHeight,
                defaultX,
                defaultY);
            if (result == null)
            {
                StatusText = "Ввод размеров отменён";
                return;
            }

            ClearShapePlacementFlags();
            _isCommittingRealFrame = false;

            var (width, height) = NormalizeCommittedShapeSize(CurrentToolMode, result.Width, result.Height);
            var targetX = RoundMm(result.X);
            var targetY = RoundMm(result.Y);

            if (!CommitShapeAt(CurrentToolMode, targetX, targetY, width, height))
            {
                return;
            }

            CompleteFrameCreationAndSelection();
            StatusText = $"{GetDragShapeKindLabel(CurrentToolMode).Label} создана";
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("Shape size entry failed", ex);
            StatusText = "Не удалось создать объект";
        }
        finally
        {
            _isShapeSizeDialogOpen = false;
            OnPropertyChanged(nameof(FrameRoot));
            OnPropertyChanged(nameof(SelectedElement));
            OnPropertyChanged(nameof(IsPropertiesPanelVisible));
            RequestSceneRefresh();
        }
    }

    public Task BeginRealFrameSizeEntryFromRightClickAsync() => BeginShapeSizeEntryFromRightClickAsync();

    private bool CommitShapeAt(WindowToolMode mode, double originX, double originY, double width, double height)
    {
        if (mode == WindowToolMode.RealFrame)
        {
            return RecordPlacement("Коробка (рама) создана", () => CommitRealFrameAt(originX, originY, width, height));
        }

        var (_, label) = GetDragShapeKindLabel(mode);
        return RecordPlacement($"{label} создана", () => CommitFakeShapeAt(mode, originX, originY, width, height));
    }

    private bool CommitFakeShapeAt(WindowToolMode mode, double originX, double originY, double width, double height)
    {
        (width, height) = NormalizeCommittedShapeSize(mode, width, height);
        originX = RoundMm(originX);
        originY = RoundMm(originY);

        if (width <= 0 || height <= 0)
        {
            _isCommittingRealFrame = false;
            StatusText = "Некорректные размеры";
            return false;
        }

        var (kind, label) = GetDragShapeKindLabel(mode);

        _isCommittingRealFrame = true;
        ClearCommittedFrameGeometry();
        FrameRoot = FrameNode.CreateRoot(originX, originY, width, height);

        var bounds = new CadRect(originX, originY + height, width, height);
        Document.AddRectangle(bounds);
        SelectedRectangle = bounds;

        var element = new GeometryElement
        {
            Kind = kind,
            Label = label,
            X = (decimal)RoundMm(originX + width / 2.0),
            Y = (decimal)RoundMm(originY + height / 2.0),
            WidthMm = (decimal)width,
            HeightMm = (decimal)height
        };
        CanvasElements.Add(element);

        _isSyncingSelection = true;
        Length = width;
        Height = height;
        CoordinateX = originX;
        CoordinateY = originY;
        _isSyncingSelection = false;

        OnPropertyChanged(nameof(HasFakeFrameOnCanvas));
        OnPropertyChanged(nameof(HasFrameOnCanvas));
        OnPropertyChanged(nameof(HasRealFrameOnCanvas));
        OnPropertyChanged(nameof(FrameRoot));
        FitToCanvas(bounds);
        FrameCreated?.Invoke(this, new FrameCreatedEventArgs(element, replaceExisting: true));
        return true;
    }

    private void CompleteFrameCreationAndSelection()
    {
        ClearShapePlacementFlags();
        _isCommittingRealFrame = false;

        if (FrameRoot is not FrameNode frame)
        {
            return;
        }

        ApplyFrameSelection(frame);
        ScheduleFrameSelectionUiRefresh(frame);
    }

    public void FinalizeRealFramePlacement() => FinalizeShapePlacement();

    public void FinalizeShapePlacement()
    {
        ClearShapePlacementFlags();
        _isCommittingRealFrame = false;

        if (FrameRoot is FrameNode frame)
        {
            ApplyFrameSelection(frame);
            ScheduleFrameSelectionUiRefresh(frame);
        }

        RequestSceneRefresh();
    }

    private void ClearShapePlacementFlags()
    {
        _realFrameStartWorld = null;
        RealFramePreview = null;
        OnPropertyChanged(nameof(RealFramePreview));
        OnPropertyChanged(nameof(IsShapePlacementActive));
        OnPropertyChanged(nameof(IsRealFramePlacementActive));
    }

    private void ClearRealFramePlacementFlags() => ClearShapePlacementFlags();

    private void ApplyFrameSelection(FrameNode frame)
    {
        IsLeftPanelVisible = true;
        SelectedElement = frame;
        SyncPanelFromActiveFrame();
        NotifyFrameSelectionProperties();
    }

    private void ScheduleFrameSelectionUiRefresh(FrameNode frame)
    {
        var frameRef = frame;
        Dispatcher.UIThread.Post(
            () =>
            {
                if (FrameRoot is not FrameNode active || active != frameRef)
                {
                    return;
                }

                ApplyFrameSelection(active);
                OnPropertyChanged(nameof(FrameRoot));
                OnPropertyChanged(nameof(SelectedElement));
                OnPropertyChanged(nameof(CoordinateX));
                OnPropertyChanged(nameof(CoordinateY));
                OnPropertyChanged(nameof(SelectedElementX));
                OnPropertyChanged(nameof(SelectedElementY));
                OnPropertyChanged(nameof(SelectedElementLength));
                OnPropertyChanged(nameof(SelectedElementHeight));
                RequestSceneRefresh();
            },
            DispatcherPriority.Background);
    }

    partial void OnSelectedElementChanged(FrameNode? value)
    {
        if (value != null)
        {
            IsLeftPanelVisible = true;
        }

        OnPropertyChanged(nameof(SelectedElementLength));
        OnPropertyChanged(nameof(SelectedElementHeight));
        OnPropertyChanged(nameof(SelectedElementX));
        OnPropertyChanged(nameof(SelectedElementY));
    }

    private void SyncPanelFromActiveFrame()
    {
        if (FrameRoot is not FrameNode frame)
        {
            return;
        }

        _isSyncingSelection = true;
        try
        {
            var originX = RoundMm(frame.X);
            var originY = RoundMm(frame.Y);
            var width = RoundMm(frame.Width);
            var height = RoundMm(frame.Height);

            Length = width;
            Height = height;
            CoordinateX = originX;
            CoordinateY = originY;
            SelectedRectangle = new CadRect(originX, originY + height, width, height);
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private void NotifyFrameSelectionProperties()
    {
        OnPropertyChanged(nameof(FrameRoot));
        OnPropertyChanged(nameof(SelectedElement));
        OnPropertyChanged(nameof(Length));
        OnPropertyChanged(nameof(Height));
        OnPropertyChanged(nameof(CoordinateX));
        OnPropertyChanged(nameof(CoordinateY));
        OnPropertyChanged(nameof(SelectedElementLength));
        OnPropertyChanged(nameof(SelectedElementHeight));
        OnPropertyChanged(nameof(SelectedElementX));
        OnPropertyChanged(nameof(SelectedElementY));
        OnPropertyChanged(nameof(SelectedRectangle));
        OnPropertyChanged(nameof(IsLeftPanelVisible));
        OnPropertyChanged(nameof(IsPropertiesPanelVisible));
    }

    private bool CommitRealFrameAt(double originX, double originY, double width, double height)
    {
        originX = RoundMm(originX);
        originY = RoundMm(originY);
        width = RoundMm(width);
        height = RoundMm(height);

        if (width <= 0 || height <= 0)
        {
            _isCommittingRealFrame = false;
            StatusText = "Некорректные размеры";
            return false;
        }

        _isCommittingRealFrame = true;
        ClearCommittedFrameGeometry();
        FrameRoot = FrameNode.CreateRoot(originX, originY, width, height);

        var bounds = new CadRect(originX, originY + height, width, height);
        Document.AddRectangle(bounds);
        SelectedRectangle = bounds;

        var element = new GeometryElement
        {
            Kind = GeometryElementKind.Frame,
            Label = "Коробка (рама)",
            X = (decimal)RoundMm(originX + width / 2.0),
            Y = (decimal)RoundMm(originY + height / 2.0),
            WidthMm = (decimal)width,
            HeightMm = (decimal)height
        };
        CanvasElements.Add(element);

        _isSyncingSelection = true;
        Length = width;
        Height = height;
        CoordinateX = originX;
        CoordinateY = originY;
        _isSyncingSelection = false;

        OnPropertyChanged(nameof(HasFrameOnCanvas));
        OnPropertyChanged(nameof(HasRealFrameOnCanvas));
        OnPropertyChanged(nameof(FrameRoot));
        FitToCanvas(bounds);
        FrameCreated?.Invoke(this, new FrameCreatedEventArgs(element, replaceExisting: true));
        return true;
    }

    private void ClearRealFramePlacement()
    {
        ClearShapePlacementFlags();
        OnPropertyChanged(nameof(SelectedElementLength));
        OnPropertyChanged(nameof(SelectedElementHeight));
    }

    public void CancelShapePlacement()
    {
        ClearRealFramePlacement();
        RequestSceneRefresh();
    }

    public void CancelRealFramePlacement() => CancelShapePlacement();

    private void ClearCommittedFrameGeometry()
    {
        Document.ClearRectangles();
        CanvasElements.Clear();
        FrameRoot = null;
        SelectedElement = null;
        SelectedRectangle = null;
        RealFramePreview = null;
        SashDragPreview = null;
        HandlePreviewSashId = null;
        _impostSplitHistory.Clear();
        _placementHistory.Clear();
        NotifyPlacementHistoryChanged();

        _isSyncingSelection = true;
        Length = 0;
        Height = 0;
        CoordinateX = 0;
        CoordinateY = 0;
        _isSyncingSelection = false;

        OnPropertyChanged(nameof(FrameRoot));
        OnPropertyChanged(nameof(SelectedElement));
        OnPropertyChanged(nameof(HasFrameOnCanvas));
        OnPropertyChanged(nameof(HasFakeFrameOnCanvas));
        OnPropertyChanged(nameof(HasRealFrameOnCanvas));
        OnPropertyChanged(nameof(RealFramePreview));
        NotifyFrameSelectionProperties();
    }

    private void ClearExistingFrameForReplacement()
    {
        ClearCommittedFrameGeometry();
        Length = 0;
        Height = 0;
        OnPropertyChanged(nameof(SelectedElementLength));
        OnPropertyChanged(nameof(SelectedElementHeight));
    }

    public bool TryHandleCanvasClick(CadPoint worldPoint, bool splitAtHalf = false)
    {
        if (IsDragCreateShapeMode(CurrentToolMode) || IsImpostSplitMode(CurrentToolMode))
        {
            return false;
        }

        var leaf = FrameRoot?.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (!ValidateToolClick(CurrentToolMode, leaf, worldPoint))
        {
            return false;
        }

        return ExecuteToolPipeline(CurrentToolMode, worldPoint, leaf, splitAtHalf);
    }

    public async Task TryHandleImpostClickAsync(CadPoint worldPoint)
    {
        if (IsDragCreateShapeMode(CurrentToolMode) || !IsImpostSplitMode(CurrentToolMode))
        {
            return;
        }

        var leaf = FrameRoot?.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (!ValidateToolClick(CurrentToolMode, leaf, worldPoint))
        {
            return;
        }

        if (CurrentToolMode == WindowToolMode.SlantedImpost)
        {
            if (RecordPlacement("Импост установлен", () =>
                {
                    if (!ExecuteSlantedImpostSplit(worldPoint, leaf!))
                    {
                        return false;
                    }

                    TrackImpostSplit(leaf!);
                    return true;
                }))
            {
                RefreshAfterImpostAdded(CurrentToolMode);
            }

            return;
        }

        var impostCount = await _dialogService.ShowImpostCountAsync(1);
        if (impostCount is not >= 1)
        {
            return;
        }

        if (!leaf!.CanAcceptUniformSplit(
                GetImpostOrientation(CurrentToolMode),
                impostCount.Value,
                GetImpostProfileWidth(CurrentToolMode),
                ResolveFrameSplitInset(leaf)))
        {
            StatusText = "Область слишком мала для деления";
            return;
        }

        ApplyUniformImpostSplit(leaf, impostCount.Value, CurrentToolMode);
    }

    public void RefreshSelection()
    {
        OnPropertyChanged(nameof(SelectedRectangle));
        SyncCoordinatesFromSelection();
    }

    public void UpdateCursor(CadPoint point)
    {
        try
        {
            var display = IsShapePlacementActive ? RoundMm(point) : SnapPointToGrid(point);
            CursorX = display.X;
            CursorY = display.Y;
            OnPropertyChanged(nameof(SelectedElementLength));
            OnPropertyChanged(nameof(SelectedElementHeight));

            if (IsPlacementToolActive && !IsDragCreateShapeToolActive && !IsShapePlacementActive &&
                !_isShapeSizeDialogOpen && !_isSyncingSelection)
            {
                _isSyncingSelection = true;
                CoordinateX = display.X;
                CoordinateY = display.Y;
                _isSyncingSelection = false;
            }

            OnPropertyChanged(nameof(ZoomPercent));
            OnPropertyChanged(nameof(MillimetersPerPixel));
            OnPropertyChanged(nameof(CanvasHintText));
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("UpdateCursor failed", ex);
        }
    }

    partial void OnSelectedProfileSeriesChanged(string? value) => ApplySeriesThickness(value);

    public bool AreFurnitureOptionsEnabled =>
        CurrentToolMode is WindowToolMode.Hardware or WindowToolMode.Sash;

    public void SetSelectedConstructionSilently(ConstructionItemViewModel? item)
    {
        _suppressConstructionSelectionCallback = true;
        try
        {
            SelectedConstruction = item;
            _activeOrderConstructionId = item?.OrderConstructionId;
        }
        finally
        {
            _suppressConstructionSelectionCallback = false;
        }
    }

    public void PrepareForNewConstruction()
    {
        if (_isResettingCanvas)
        {
            return;
        }

        try
        {
            _isResettingCanvas = true;
            ClearDrawing();
            ConstructionQuantity = 1;
            StatusText = "Новая конструкция — начните с чистого листа";
        }
        finally
        {
            _isResettingCanvas = false;
        }
    }

    partial void OnSelectedConstructionChanged(ConstructionItemViewModel? value)
    {
        if (_suppressConstructionSelectionCallback
            || _isResettingCanvas
            || value is null
            || SelectConstructionHandler is null)
        {
            return;
        }

        if (_activeOrderConstructionId == value.OrderConstructionId)
        {
            return;
        }

        _ = SelectConstructionHandler(value.OrderConstructionId);
    }

    partial void OnShowAllMaterialsChanged(bool value) => RefreshFilteredMaterials();

    partial void OnSelectedDrawingTemplateChanged(CadDrawingTemplate? value)
    {
        if (value is null || _isApplyingDrawingTemplate)
        {
            return;
        }

        ApplyDrawingTemplate(value);
    }

    partial void OnCurrentToolModeChanged(WindowToolMode value)
    {
        OnPropertyChanged(nameof(IsDragCreateShapeToolActive));
        OnPropertyChanged(nameof(ActiveToolMode));
        OnPropertyChanged(nameof(AreFurnitureOptionsEnabled));
        RefreshFilteredMaterials();
        if (value is not WindowToolMode.OffsetImpostVertical and not WindowToolMode.OffsetImpostHorizontal)
        {
            ClearImpostOffsetInteractionState();
        }
    }

    partial void OnImpostPanelXChanged(double value)
    {
        if (CurrentToolMode == WindowToolMode.OffsetImpostVertical)
        {
            ApplyImpostPanelAxisX(value);
        }
    }

    partial void OnImpostPanelYChanged(double value)
    {
        if (CurrentToolMode == WindowToolMode.OffsetImpostHorizontal)
        {
            ApplyImpostPanelAxisY(value);
        }
    }

    partial void OnImpostPanelLChanged(double value)
    {
        if (CurrentToolMode == WindowToolMode.OffsetImpostHorizontal)
        {
            ApplyImpostPanelBottomOpening(value);
        }
        else if (CurrentToolMode == WindowToolMode.OffsetImpostVertical)
        {
            ApplyImpostPanelLeftOpening(value);
        }
    }

    partial void OnImpostPanelHChanged(double value)
    {
        if (CurrentToolMode == WindowToolMode.OffsetImpostHorizontal)
        {
            ApplyImpostPanelTopOpening(value);
        }
        else if (CurrentToolMode == WindowToolMode.OffsetImpostVertical)
        {
            ApplyImpostPanelRightOpening(value);
        }
    }

    partial void OnPlacementAnchorChanged(PlacementAnchor value)
    {
        var match = PlacementAnchorOptionList.FirstOrDefault(option => option.Anchor == value);
        if (match != null && !ReferenceEquals(SelectedPlacementAnchorOption, match))
        {
            _isSyncingSelection = true;
            SelectedPlacementAnchorOption = match;
            _isSyncingSelection = false;
        }

        UpdatePlacementPanelCoordinatesFromAnchor();
    }

    partial void OnSelectedPlacementAnchorOptionChanged(PlacementAnchorOption value)
    {
        if (PlacementAnchor != value.Anchor)
        {
            PlacementAnchor = value.Anchor;
        }
    }

    partial void OnSelectedRectangleChanged(CadRect? value) => SyncCoordinatesFromSelection();

    partial void OnCoordinateXChanged(double value) => ApplyCoordinatesToSelection();

    partial void OnCoordinateYChanged(double value) => ApplyCoordinatesToSelection();

    partial void OnLengthChanged(double value)
    {
        if (!_isSyncingSelection && TryGetPrimaryElement(out var element) &&
            element!.Kind is GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame)
        {
            _isSyncingSelection = true;
            Height = value / 2.0;
            _isSyncingSelection = false;
        }

        ApplyCoordinatesToSelection();

        if (!_isSyncingSelection && FrameRoot == null && IsDragCreateShapeMode(CurrentToolMode))
        {
            UpdatePlacementPanelCoordinatesFromAnchor();
        }
    }

    partial void OnHeightChanged(double value)
    {
        if (!_isSyncingSelection && TryGetPrimaryElement(out var element))
        {
            if (element!.Kind is GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame)
            {
                var locked = Length / 2.0;
                if (Math.Abs(value - locked) > CadMath.Epsilon)
                {
                    _isSyncingSelection = true;
                    Height = locked;
                    _isSyncingSelection = false;
                }
            }
            else if (element.Kind == GeometryElementKind.RoundDummyFrame && Math.Abs(value - Length) > CadMath.Epsilon)
            {
                _isSyncingSelection = true;
                Height = Length;
                _isSyncingSelection = false;
            }
        }

        ApplyCoordinatesToSelection();

        if (!_isSyncingSelection && FrameRoot == null && IsDragCreateShapeMode(CurrentToolMode))
        {
            UpdatePlacementPanelCoordinatesFromAnchor();
        }
    }

    [RelayCommand]
    private void DeleteSelected() =>
        ActivateCadTool(CadToolMetadata.GetId(CadToolKind.DeleteSelection));

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private void Undo()
    {
        if (TryUndoConstructionSnapshot())
        {
            return;
        }

        if (!Commands.CanUndo)
        {
            StatusText = "Нет действий для отмены";
            return;
        }

        Commands.Undo();
        RefreshSelection();
        RequestSceneRefresh();
        StatusText = "Действие отменено";
    }

    [RelayCommand]
    private void GlobalUndo() => Undo();

    [RelayCommand(CanExecute = nameof(CanUndoPlacement))]
    private void UndoLastElement()
    {
        TryUndoLastPlacement();
    }

    [RelayCommand(CanExecute = nameof(CanRedoPlacement))]
    private void Redo()
    {
        if (!_placementHistory.TryRedo(ApplyConstructionMemento, out var description))
        {
            StatusText = "Нечего повторять";
            return;
        }

        RefreshAfterPlacementHistoryChange();
        StatusText = description ?? "Действие возвращено";
        RequestSceneRefresh();
    }

    [RelayCommand]
    private void ClearConstruction() => ActivateCadTool(CadToolMetadata.GetId(CadToolKind.ClearConstruction));

    [RelayCommand]
    private void ClearAll() => ClearConstruction();

    [RelayCommand]
    private void ZoomToFit()
    {
        FitDocumentToViewport();
        StatusText = "Автомасштаб";
    }

    [RelayCommand]
    private void ChangeDimensions() =>
        ActivateCadTool(CadToolMetadata.GetId(CadToolKind.ChangeOverallDimensions));

    [RelayCommand]
    private async Task SetArchHeight()
    {
        if (!TryGetPrimaryElement(out var element) ||
            element!.Kind is not (GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame))
        {
            StatusText = "Выберите арочную конструкцию";
            return;
        }

        var currentHeight = (double)element.HeightMm;
        var width = (double)element.WidthMm;
        var result = await _dialogService.ShowSizeInputAsync(width, currentHeight, CoordinateX, CoordinateY);
        if (result == null)
        {
            StatusText = "Ввод высоты арки отменён";
            return;
        }

        var archHeight = RoundMm(result.Height);
        if (archHeight <= 0 || archHeight > width / 2.0)
        {
            StatusText = "Высота арки должна быть от 0 до половины ширины";
            return;
        }

        _isSyncingSelection = true;
        Height = archHeight;
        Length = width;
        _isSyncingSelection = false;
        ApplyCoordinatesToSelection();
        StatusText = $"Высота арки: {archHeight:F1} мм";
        RequestSceneRefresh();
    }

    [RelayCommand]
    private void SelectTool(string? toolKey)
    {
        var toolId = toolKey switch
        {
            "Selection" => CadToolMetadata.GetId(CadToolKind.SelectElements),
            "SelectFullFrame" => CadToolMetadata.GetId(CadToolKind.SelectObject),
            "SelectProfile" => CadToolMetadata.GetId(CadToolKind.SelectProfile),
            "SelectVertex" => CadToolMetadata.GetId(CadToolKind.SelectVertex),
            "MoveMullionVertical" => CadToolMetadata.GetId(CadToolKind.OffsetImpostVertical),
            "MoveMullionHorizontal" => CadToolMetadata.GetId(CadToolKind.OffsetImpostHorizontal),
            _ => null
        };

        if (toolId is not null)
        {
            ActivateCadTool(toolId);
        }
    }

    [RelayCommand]
    private void SetVertexCoordinates() =>
        ActivateCadTool(CadToolMetadata.GetId(CadToolKind.SetVertexCoordinates));

    [RelayCommand]
    private void SplitProfile() => ActivateCadTool(CadToolMetadata.GetId(CadToolKind.SplitProfile));

    [RelayCommand]
    private void AlignMullions() => ActivateCadTool(CadToolMetadata.GetId(CadToolKind.AlignImposts));

    [RelayCommand]
    private void AlignGlassUnits() => ActivateCadTool(CadToolMetadata.GetId(CadToolKind.AlignGlazingUnits));

    [RelayCommand]
    private void AlignClearOpenings() => ActivateCadTool(CadToolMetadata.GetId(CadToolKind.AlignDeadLights));

    [RelayCommand]
    private void ChangeOuterDimensions() =>
        ActivateCadTool(CadToolMetadata.GetId(CadToolKind.ChangeOverallDimensions));

    [RelayCommand]
    private void FitAll()
    {
        FitDocumentToViewport();
        StatusText = "Показать всё";
    }

    [RelayCommand]
    private async Task OpenConstructionsLookupAsync()
    {
        if (OpenConstructionsLookupHandler is not null)
        {
            await OpenConstructionsLookupHandler();
        }
    }

    [RelayCommand]
    private async Task OpenTemplatesAsync()
    {
        var template = await _dialogService.ShowTemplatePickerAsync(_templateStore, this);
        if (template == null)
        {
            return;
        }

        _isApplyingDrawingTemplate = true;
        try
        {
            SelectedDrawingTemplate = template;
            ApplyDrawingTemplate(template);
        }
        finally
        {
            _isApplyingDrawingTemplate = false;
        }
    }

    [RelayCommand]
    private void IncrementQuantity()
    {
        if (ConstructionQuantity < 9999)
        {
            ConstructionQuantity++;
        }
    }

    [RelayCommand]
    private void DecrementQuantity()
    {
        if (ConstructionQuantity > 1)
        {
            ConstructionQuantity--;
        }
    }

    private void ApplyDrawingTemplate(CadDrawingTemplate template)
    {
        _isApplyingDrawingTemplate = true;
        try
        {
            if (template.IsBlank)
            {
                ClearDrawing();
                StatusText = "Пустой чертеж";
                return;
            }

            var mode = template.IsFakeFrame ? WindowToolMode.FakeFrame : WindowToolMode.RealFrame;
            CreatePlacedShape(mode, template.WidthMm, template.HeightMm);
            StatusText = $"Применён шаблон: {template.Name}";
        }
        finally
        {
            _isApplyingDrawingTemplate = false;
        }
    }

    private void ClearDrawing()
    {
        if (_canvasResetDepth > 0)
        {
            ClearDrawingCore();
            return;
        }

        try
        {
            _canvasResetDepth++;
            _isResettingCanvas = true;
            ClearDrawingCore();
        }
        finally
        {
            _isResettingCanvas = false;
            _canvasResetDepth--;
        }
    }

    private void ClearDrawingCore()
    {
        Document.ClearRectangles();
        CanvasElements.Clear();
        FrameRoot = null;
        SelectedRectangle = null;

        Length = 0;
        Height = 0;
        CoordinateX = 0;
        CoordinateY = 0;

        Viewport.ResetToDefaultScale(15.0);
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));

        Document.Name = "Новый чертеж";
        NewTemplateRequested?.Invoke(this, EventArgs.Empty);
        RequestSceneRefresh();
    }

    private void InitializeDesignTools()
    {
        DesignTools.Clear();

        // Группа 1: управление и модификация геометрии
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.SelectElement,
            "Выделение элементов",
            DesignToolIcon.Selection));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.OffsetImpostVertical,
            "Сместить импост вертикально",
            DesignToolIcon.MoveMullionVertical));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.OffsetImpostHorizontal,
            "Сместить импост горизонтально",
            DesignToolIcon.MoveMullionHorizontal));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.Other,
            "Удалить последний установленный элемент (Ctrl+Alt+Z)",
            DesignToolIcon.UndoLast,
            command: UndoLastElementCommand));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.Other,
            "Отменить действие (Ctrl+Z)",
            DesignToolIcon.UndoGlobal,
            command: UndoCommand));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.Other,
            "Вернуть действие (Ctrl+Y)",
            DesignToolIcon.RedoGlobal,
            command: RedoCommand));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.Separator, string.Empty, isSeparator: true));

        // Группа 2: выравнивание и навигация
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.AlignImposts,
            "Выравнивание импостов",
            DesignToolIcon.AlignMullions));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.AlignGlazingUnits,
            "Выравнивание стеклопакетов",
            DesignToolIcon.AlignGlassUnits));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.AlignDeadLights,
            "Выравнивание просветов в свету",
            DesignToolIcon.AlignClearOpenings));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.ZoomView,
            "Увеличить/уменьшить вид",
            DesignToolIcon.Zoom));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.PanView,
            "Переместить вид",
            DesignToolIcon.Hand));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.Other,
            "Автомасштаб",
            DesignToolIcon.ZoomFit,
            command: ZoomToFitCommand));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.Separator, string.Empty, isSeparator: true));

        // Группа 3: фиктивные контуры
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeFrame, "Коробка (фиктивная)", DesignToolIcon.FakeFrame));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeFrameCircle, "Коробка фиктивная круглая", DesignToolIcon.FakeCircle));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeFrameTriangle, "Коробка фиктивная треугольная", DesignToolIcon.FakeTriangle));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeFrameArch, "Полукруг (фиктивная)", DesignToolIcon.FakeArch));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeVerticalImpost, "Импост верт.", DesignToolIcon.VerticalImpost));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeHorizontalImpost, "Импост гориз.", DesignToolIcon.HorizontalImpost));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.RackRigelNone, "Стойка/ригель", DesignToolIcon.RackRigel));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.Separator, "--- РЕАЛЬНЫЕ ПРОФИЛИ ---", isSeparator: true));

        // Группа 4: реальные конструктивные элементы
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.RealFrame, "Коробка (рама)", DesignToolIcon.RealFrame));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.AdapterProfile, "Переходный профиль", DesignToolIcon.AdapterProfile));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.Sash, "Створка", DesignToolIcon.Sash));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.Hardware, "Фурнитура", DesignToolIcon.Hardware));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.Handle, "Ручка", DesignToolIcon.Handle));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.RealVerticalImpost, "Импост верт.", DesignToolIcon.RealVerticalImpost));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.RealHorizontalImpost, "Импост гориз.", DesignToolIcon.RealHorizontalImpost));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.DoubleVerticalImpost, "Импост двойной верт.", DesignToolIcon.DoubleVerticalImpost));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.DoubleHorizontalImpost, "Импост двойной гориз.", DesignToolIcon.DoubleHorizontalImpost));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.SlantedImpost, "Импост наклонный", DesignToolIcon.SlantedImpost));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.Glassing, "Остекление", DesignToolIcon.Glassing));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.Separator, string.Empty, isSeparator: true));

        // Группа 5: выделение подобъектов и глобальные операции
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.Select,
            "Выделение объекта",
            DesignToolIcon.SelectObject));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.SelectProfile,
            "Выделение профиля",
            DesignToolIcon.SelectProfile));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.SelectVertex,
            "Выделение вершины",
            DesignToolIcon.SelectVertex));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.Other,
            "Задать высоту арки",
            DesignToolIcon.ArchHeight,
            command: SetArchHeightCommand));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.Other,
            "Изменить габаритные размеры",
            DesignToolIcon.Resize,
            command: ChangeDimensionsCommand));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.Other,
            "Удаление",
            DesignToolIcon.Trash,
            command: DeleteSelectedCommand));
        DesignTools.Add(new CadDesignToolButtonViewModel(
            WindowToolMode.Other,
            "Очистить всю конструкцию",
            DesignToolIcon.ClearAll,
            command: ClearAllCommand));
    }

    private void SetActiveToolVisual(WindowToolMode mode)
    {
        UpdateDesignToolSelection(mode);
        ActiveTool = _toolFactory.Create("select");
        ActiveToolId = "select";
        OnPropertyChanged(nameof(ActiveToolDisplayName));
        StatusText = DesignTools.FirstOrDefault(t => t.Mode == mode)?.Title ?? "Инструмент";
    }

    private void UpdateDesignToolSelection(WindowToolMode mode)
    {
        CurrentToolMode = mode;
        foreach (var tool in DesignTools)
        {
            tool.IsActive = !tool.IsSeparator && tool.Mode == mode;
        }

        var active = DesignTools.FirstOrDefault(t => t.Mode == mode);
        ActiveDesignToolLabel = active?.Title;
        ActiveDesignToolId = mode.ToString();
        OnPropertyChanged(nameof(CanvasHintText));
    }

    private static bool TryResolveCadToolId(WindowToolMode mode, out string toolId)
    {
        toolId = mode switch
        {
            WindowToolMode.SelectElement => CadToolMetadata.GetId(CadToolKind.SelectElements),
            WindowToolMode.Select => CadToolMetadata.GetId(CadToolKind.SelectObject),
            WindowToolMode.SelectProfile => CadToolMetadata.GetId(CadToolKind.SelectProfile),
            WindowToolMode.SelectVertex => CadToolMetadata.GetId(CadToolKind.SelectVertex),
            WindowToolMode.OffsetImpostVertical => CadToolMetadata.GetId(CadToolKind.OffsetImpostVertical),
            WindowToolMode.OffsetImpostHorizontal => CadToolMetadata.GetId(CadToolKind.OffsetImpostHorizontal),
            WindowToolMode.AlignImposts => CadToolMetadata.GetId(CadToolKind.AlignImposts),
            WindowToolMode.AlignGlazingUnits => CadToolMetadata.GetId(CadToolKind.AlignGlazingUnits),
            WindowToolMode.AlignDeadLights => CadToolMetadata.GetId(CadToolKind.AlignDeadLights),
            WindowToolMode.ClearConstruction => CadToolMetadata.GetId(CadToolKind.ClearConstruction),
            WindowToolMode.Other => string.Empty,
            _ => string.Empty
        };

        return toolId.Length > 0;
    }

    private async Task SelectToolModeAsync(WindowToolMode mode)
    {
        if (_isRunningFramePipeline)
        {
            return;
        }

        if (mode is WindowToolMode.Separator)
        {
            return;
        }

        if (mode is WindowToolMode.ZoomView or WindowToolMode.PanView)
        {
            UpdateDesignToolSelection(mode);
            ActiveTool = _toolFactory.Create(CadToolMetadata.GetId(CadToolKind.SelectElements));
            ActiveToolId = CadToolMetadata.GetId(CadToolKind.SelectElements);
            StatusText = mode switch
            {
                WindowToolMode.ZoomView => "Лупа: ЛКМ — увеличить, ПКМ — уменьшить",
                WindowToolMode.PanView => "Рука: зажмите ЛКМ для панорамирования",
                _ => "Инструмент"
            };
            await Task.CompletedTask;
            return;
        }

        if (TryResolveCadToolId(mode, out var cadToolId))
        {
            UpdateDesignToolSelection(mode);
            ActivateCadTool(cadToolId);
            StatusText = DesignTools.FirstOrDefault(t => t.Mode == mode)?.Title ?? "Инструмент";
            await Task.CompletedTask;
            return;
        }

        if (mode is WindowToolMode.Other)
        {
            return;
        }

        SetActiveToolVisual(mode);

        if (IsFramePlacementMode(mode))
        {
            var active = DesignTools.FirstOrDefault(t => t.Mode == mode);
            StatusText = IsDragCreateShapeMode(mode)
                ? $"{active?.Title}: ЛКМ — натянуть, ПКМ — ввод размеров"
                : $"{active?.Title}: кликните по полю чертежа";
        }
        else if (IsImpostSplitMode(mode))
        {
            var active = DesignTools.FirstOrDefault(t => t.Mode == mode);
            StatusText = FrameRoot == null
                ? "Сначала создайте коробку"
                : mode == WindowToolMode.SlantedImpost
                    ? $"{active?.Title}: ЛКМ — положение наклонного импоста"
                    : $"{active?.Title}: ЛКМ внутри ячейки — введите количество импостов";
        }
        else if (IsInteractiveToolMode(mode))
        {
            StatusText = mode switch
            {
                WindowToolMode.Sash => "Створка: зажмите ЛКМ и укажите направление открывания",
                WindowToolMode.Hardware => "Фурнитура: клик по створке — петли справа → слева → снизу → снять",
                WindowToolMode.Handle => "Ручка: клик по створке — установить / снять",
                _ => DesignTools.FirstOrDefault(t => t.Mode == mode)?.Title is { Length: > 0 } title
                    ? $"Выбран инструмент {title}"
                    : "Инструмент"
            };
        }
        else if (mode == WindowToolMode.Glassing)
        {
            StatusText = FrameRoot is null || !HasRealFrameOnCanvas
                ? "Сначала создайте коробку (рама)"
                : "Остекление: кликните по пустому проёму или по створке";
        }
        else
        {
            var active = DesignTools.FirstOrDefault(t => t.Mode == mode);
            StatusText = active?.Title is { Length: > 0 } title
                ? $"Выбран инструмент {title}"
                : "Инструмент";
        }

        await Task.CompletedTask;
    }

    private async Task RunShapePlacementAsync(WindowToolMode mode, CadPoint _)
    {
        if (_isRunningFramePipeline)
        {
            return;
        }

        _isRunningFramePipeline = true;
        try
        {
            var initialWidth = Length > 0 ? Length : 1000;
            var initialHeight = Height > 0 ? Height : 1000;
            var result = await _dialogService.ShowSizeInputAsync(
                initialWidth,
                initialHeight,
                CoordinateX,
                CoordinateY);
            if (result == null)
            {
                StatusText = "Ввод размеров отменён";
                return;
            }

            CreatePlacedShape(
                mode,
                result.Width,
                result.Height,
                new CadPoint(RoundMm(result.X), RoundMm(result.Y)));
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("Shape placement failed", ex);
            StatusText = "Не удалось создать объект";
        }
        finally
        {
            _isRunningFramePipeline = false;
        }
    }

    private void CreatePlacedShape(
        WindowToolMode mode,
        double width,
        double height,
        CadPoint? originBottomLeft = null,
        bool snapToGrid = true,
        CadPoint? clickCenter = null)
    {
        if (width <= 0 || height <= 0)
        {
            CadDiagnostics.LogWarning($"Invalid shape size: {width}x{height}");
            StatusText = "Некорректные размеры";
            return;
        }

        var (_, label) = mode switch
        {
            WindowToolMode.RealFrame => (GeometryElementKind.Frame, "Коробка (рама)"),
            WindowToolMode.FakeFrame => (GeometryElementKind.DummyFrame, "Коробка (фиктивная)"),
            WindowToolMode.FakeFrameCircle => (GeometryElementKind.RoundDummyFrame, "Коробка фиктивная круглая"),
            WindowToolMode.FakeFrameTriangle => (GeometryElementKind.TriangleDummyFrame, "Коробка фиктивная треугольная"),
            WindowToolMode.FakeFrameArch => (GeometryElementKind.ArchDummyFrame, "Полукруг (фиктивная)"),
            _ => (GeometryElementKind.DummyFrame, "Коробка")
        };

        RecordPlacement($"{label} создана", () =>
        {
            CreatePlacedShapeCore(mode, width, height, originBottomLeft, snapToGrid, clickCenter);
            return true;
        });
    }

    private void CreatePlacedShapeCore(
        WindowToolMode mode,
        double width,
        double height,
        CadPoint? originBottomLeft,
        bool snapToGrid,
        CadPoint? clickCenter)
    {
        Document.ClearRectangles();
        CanvasElements.Clear();

        if (mode == WindowToolMode.FakeFrameCircle)
        {
            height = width;
        }
        else if (mode == WindowToolMode.FakeFrameArch)
        {
            height = width / 2.0;
        }

        if (!snapToGrid)
        {
            width = RoundMm(width);
            height = RoundMm(height);
        }

        var (kind, label) = mode switch
        {
            WindowToolMode.RealFrame => (GeometryElementKind.Frame, "Коробка (рама)"),
            WindowToolMode.FakeFrame => (GeometryElementKind.DummyFrame, "Коробка (фиктивная)"),
            WindowToolMode.FakeFrameCircle => (GeometryElementKind.RoundDummyFrame, "Коробка фиктивная круглая"),
            WindowToolMode.FakeFrameTriangle => (GeometryElementKind.TriangleDummyFrame, "Коробка фиктивная треугольная"),
            WindowToolMode.FakeFrameArch => (GeometryElementKind.ArchDummyFrame, "Полукруг (фиктивная)"),
            _ => (GeometryElementKind.DummyFrame, "Коробка")
        };

        double originX;
        double originY;
        if (originBottomLeft is CadPoint origin)
        {
            originX = snapToGrid ? SnapToGrid(origin.X) : RoundMm(origin.X);
            originY = snapToGrid ? SnapToGrid(origin.Y) : RoundMm(origin.Y);
        }
        else
        {
            var center = GetViewportCenterWorld();
            originX = SnapToGrid(center.X - width / 2);
            originY = SnapToGrid(center.Y - height / 2);
        }

        var bounds = new CadRect(originX, originY + height, width, height);
        Document.AddRectangle(bounds);
        FrameRoot = FrameNode.CreateRoot(originX, originY, width, height);
        SelectedElement = FrameRoot;
        SelectedRectangle = bounds;

        Length = width;
        Height = height;
        CoordinateX = originX;
        CoordinateY = originY;
        if (clickCenter is CadPoint clickPoint)
        {
            CursorX = clickPoint.X;
            CursorY = clickPoint.Y;
            OnPropertyChanged(nameof(CursorX));
            OnPropertyChanged(nameof(CursorY));
        }

        var element = new GeometryElement
        {
            Kind = kind,
            Label = label,
            X = (decimal)(originX + width / 2),
            Y = (decimal)(originY + height / 2),
            WidthMm = (decimal)width,
            HeightMm = (decimal)height
        };
        CanvasElements.Add(element);

        OnPropertyChanged(nameof(HasFakeFrameOnCanvas));
        OnPropertyChanged(nameof(HasFrameOnCanvas));
        OnPropertyChanged(nameof(FrameRoot));
        OnPropertyChanged(nameof(SelectedElementLength));
        OnPropertyChanged(nameof(SelectedElementHeight));
        FitToCanvas(bounds);
        FrameCreated?.Invoke(this, new FrameCreatedEventArgs(element, replaceExisting: true));
        StatusText = $"{label} создана";
        if (FrameRoot is FrameNode frame)
        {
            ScheduleFrameSelectionUiRefresh(frame);
        }
        else
        {
            RequestSceneRefresh();
        }
    }

    private void KonAddExecute()
    {
        var nextNumber = Constructions.Count + 1;
        var item = new ConstructionItemViewModel(Guid.NewGuid(), nextNumber, $"Конструкция {nextNumber}");
        Constructions.Add(item);
        SelectedConstruction = item;
        StatusText = "Добавлена конструкция";
    }

    [RelayCommand]
    private async Task AddConstructionAsync()
    {
        if (AddConstructionHandler is not null)
        {
            await AddConstructionHandler();
            return;
        }

        KonAddExecute();
    }

    [RelayCommand]
    private async Task RemoveConstructionAsync()
    {
        if (Constructions.Count <= 1 || SelectedConstruction is null)
        {
            StatusText = "Нельзя удалить единственную конструкцию";
            return;
        }

        var name = SelectedConstruction.Name;
        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Удаление конструкции",
            $"Удалить «{name}»?");

        if (!confirmed)
        {
            return;
        }

        if (RemoveConstructionHandler is not null && SelectedConstruction is not null)
        {
            await RemoveConstructionHandler(SelectedConstruction.OrderConstructionId);
            return;
        }

        KonDelExecute();
    }

    private void KonDelExecute()
    {
        if (Constructions.Count <= 1 || SelectedConstruction is null)
        {
            StatusText = "Нельзя удалить единственную конструкцию";
            return;
        }

        var index = Constructions.IndexOf(SelectedConstruction);
        Constructions.Remove(SelectedConstruction);
        SelectedConstruction = Constructions[Math.Max(0, index - 1)];
        StatusText = "Конструкция удалена";
    }

    private void EnsureDefaultConstructions()
    {
        Constructions.Clear();
        var first = new ConstructionItemViewModel(Guid.NewGuid(), 1, "Конструкция 1");
        Constructions.Add(first);
        SelectedConstruction = first;
        Document.Name = "Новый чертеж";
    }

    private async Task LoadMaterialsAsync()
    {
        _allMaterials.Clear();

        try
        {
            if (_materialRepository is not null)
            {
                _allMaterials.AddRange(await _materialRepository.GetAllAsync());
            }
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("LoadMaterials failed", ex);
        }

        if (_allMaterials.Count == 0)
        {
            _allMaterials.AddRange(CreateSeedMaterials());
        }

        RefreshFilteredMaterials();
    }

    private static IEnumerable<MaterialModel> CreateSeedMaterials() =>
    [
        new MaterialModel { Article = "PR-70", Name = "Профиль рамы 70 мм", Category = MaterialCategory.Profile },
        new MaterialModel { Article = "PS-70", Name = "Профиль створки 70 мм", Category = MaterialCategory.Profile },
        new MaterialModel { Article = "IM-70", Name = "Импост 70 мм", Category = MaterialCategory.Profile },
        new MaterialModel { Article = "HW-01", Name = "Фурнитурный набор поворотный", Category = MaterialCategory.Hardware },
        new MaterialModel { Article = "HW-02", Name = "Фурнитурный набор откидной", Category = MaterialCategory.Hardware },
        new MaterialModel { Article = "GL-24", Name = "Стеклопакет 24 мм", Category = MaterialCategory.Glazing },
        new MaterialModel { Article = "SL-01", Name = "Уплотнитель EPDM", Category = MaterialCategory.Sealant },
        new MaterialModel { Article = "AC-01", Name = "Москитная сетка", Category = MaterialCategory.Accessory }
    ];

    private void RefreshFilteredMaterials()
    {
        var previousId = SelectedMaterial?.Id;
        var filtered = (ShowAllMaterials
                ? _allMaterials
                : _allMaterials.Where(MatchesActiveTool))
            .OrderBy(material => material.Name)
            .ToList();

        FilteredMaterials.Clear();
        foreach (var material in filtered)
        {
            FilteredMaterials.Add(material);
        }

        SelectedMaterial = previousId is Guid id
            ? FilteredMaterials.FirstOrDefault(material => material.Id == id)
            : FilteredMaterials.FirstOrDefault();
    }

    private bool MatchesActiveTool(MaterialModel material)
    {
        var category = ResolveMaterialCategoryForTool(CurrentToolMode);
        return category is not null && material.Category == category;
    }

    private static MaterialCategory? ResolveMaterialCategoryForTool(WindowToolMode mode) => mode switch
    {
        WindowToolMode.Hardware or WindowToolMode.Handle => MaterialCategory.Hardware,
        WindowToolMode.Sash or WindowToolMode.SlidingSash => MaterialCategory.Profile,
        WindowToolMode.RealFrame or WindowToolMode.FakeFrame or WindowToolMode.FakeFrameCircle
            or WindowToolMode.FakeFrameTriangle or WindowToolMode.FakeFrameArch or WindowToolMode.Frame
            or WindowToolMode.AdapterProfile or WindowToolMode.RealVerticalImpost
            or WindowToolMode.RealHorizontalImpost or WindowToolMode.DoubleVerticalImpost
            or WindowToolMode.DoubleHorizontalImpost or WindowToolMode.SlantedImpost
            or WindowToolMode.VerticalImpost or WindowToolMode.HorizontalImpost
            or WindowToolMode.DiagonalImpost or WindowToolMode.MullionVertical
            or WindowToolMode.MullionHorizontal or WindowToolMode.Post => MaterialCategory.Profile,
        WindowToolMode.Glass or WindowToolMode.GlassWithBead or WindowToolMode.BeadSystem
            or WindowToolMode.Glassing => MaterialCategory.Glazing,
        WindowToolMode.AdditionalElement or WindowToolMode.Corrugation => MaterialCategory.Accessory,
        _ => null
    };

    private void ApplySeriesThickness(string? series)
    {
        (FrameThickness, ImpostThickness) = series switch
        {
            "ПВХ 60мм" => (60, 60),
            "ПВХ 70мм" => (70, 70),
            "Алюминий 50мм" => (50, 50),
            "Алюминий 70мм" => (70, 70),
            _ => (70, 70)
        };
        StatusText = $"Серия: {series}";
        RequestSceneRefresh();
    }

    private bool IsFakeFrameDrawing() => HasFakeFrameOnCanvas;

    private bool ValidateToolClick(WindowToolMode mode, FrameNode? leaf, CadPoint point)
    {
        if (IsFramePlacementMode(mode))
        {
            return false;
        }

        if (IsImpostSplitMode(mode) && FrameRoot is null)
        {
            StatusText = "Сначала создайте коробку";
            return false;
        }

        if (IsFakeImpostMode(mode) && !IsFakeFrameDrawing())
        {
            StatusText = "Импост доступен только для фиктивной коробки";
            return false;
        }

        if (IsImpostSplitMode(mode))
        {
            if (leaf == null)
            {
                StatusText = "Кликните внутри коробки";
                return false;
            }

            if (mode == WindowToolMode.SlantedImpost)
            {
                if (!leaf.CanAccept(mode, GetImpostProfileWidth(mode)))
                {
                    StatusText = "Область слишком мала для деления";
                    return false;
                }
            }
            else if (!leaf.CanAcceptUniformSplit(
                    GetImpostOrientation(mode),
                    impostCount: 1,
                    GetImpostProfileWidth(mode),
                    ResolveFrameSplitInset(leaf)))
            {
                StatusText = "Область слишком мала для деления";
                return false;
            }

            return true;
        }

        if (mode == WindowToolMode.RackRigelNone)
        {
            return true;
        }

        if (mode == WindowToolMode.Glassing)
        {
            if (FrameRoot is null || !HasRealFrameOnCanvas)
            {
                StatusText = "Сначала создайте коробку (рама)";
                return false;
            }

            if (leaf is null || !leaf.IsLeaf)
            {
                StatusText = "Кликните внутри проёма или по створке";
                return false;
            }

            if (!CanApplyGlazingInLeaf(leaf))
            {
                StatusText = FindGlazingInLeaf(leaf) is not null
                    ? "Остекление уже установлено в этом проёме"
                    : "Проём содержит панель — остекление недоступно";
                return false;
            }

            return true;
        }

        return false;
    }

    private bool ExecuteToolPipeline(WindowToolMode mode, CadPoint worldPoint, FrameNode? leaf, bool splitAtHalf = false)
    {
        if (mode == WindowToolMode.RackRigelNone)
        {
            StatusText = "Стойка/ригель в крайнем положении: нет";
            return true;
        }

        if (mode == WindowToolMode.Glassing && leaf is not null)
        {
            return TryPlaceGlazingInLeaf(leaf);
        }

        return false;
    }

    private void ApplyUniformImpostSplit(FrameNode leaf, int impostCount, WindowToolMode mode)
    {
        var orientation = GetImpostOrientation(mode);
        var impostWidth = GetImpostProfileWidth(mode);
        var frameInset = ResolveFrameSplitInset(leaf);
        var lineStyle = GetImpostLineStyle(mode);
        var active = DesignTools.FirstOrDefault(t => t.Mode == mode);
        var description = active?.Title ?? "Импост";

        if (!RecordPlacement(description, () =>
                leaf.TrySplitUniformWithVolume(orientation, impostCount, impostWidth, frameInset, lineStyle)))
        {
            StatusText = "Невозможно разделить область";
            return;
        }

        TrackImpostSplit(leaf);
        RefreshAfterImpostAdded(mode);
    }

    private bool ExecuteSlantedImpostSplit(CadPoint worldPoint, FrameNode leaf)
    {
        var impostWidth = GetImpostProfileWidth(WindowToolMode.SlantedImpost);
        var splitLocal = worldPoint.X - leaf.X;
        if (!leaf.AddSplit(SplitOrientation.Vertical, splitLocal, ImpostLineStyle.Slanted, impostWidth))
        {
            StatusText = "Невозможно разделить область";
            return false;
        }

        return true;
    }

    private static SplitOrientation GetImpostOrientation(WindowToolMode mode) =>
        IsVerticalImpostMode(mode) ? SplitOrientation.Vertical : SplitOrientation.Horizontal;

    private static bool IsFramePlacementMode(WindowToolMode mode) =>
        mode is WindowToolMode.RealFrame or WindowToolMode.FakeFrame
            or WindowToolMode.FakeFrameCircle or WindowToolMode.FakeFrameTriangle
            or WindowToolMode.FakeFrameArch;

    private static bool IsImpostSplitMode(WindowToolMode mode) =>
        mode is WindowToolMode.FakeVerticalImpost or WindowToolMode.FakeHorizontalImpost
            or WindowToolMode.RealVerticalImpost or WindowToolMode.RealHorizontalImpost
            or WindowToolMode.DoubleVerticalImpost or WindowToolMode.DoubleHorizontalImpost
            or WindowToolMode.SlantedImpost;

    private static bool IsFakeImpostMode(WindowToolMode mode) =>
        mode is WindowToolMode.FakeVerticalImpost or WindowToolMode.FakeHorizontalImpost;

    private static bool IsVerticalImpostMode(WindowToolMode mode) =>
        mode is WindowToolMode.FakeVerticalImpost or WindowToolMode.RealVerticalImpost
            or WindowToolMode.DoubleVerticalImpost or WindowToolMode.SlantedImpost;

    private double GetImpostProfileWidth(WindowToolMode mode)
    {
        if (ImpostThickness > CadMath.Epsilon)
        {
            return ImpostThickness;
        }

        return CadSystemThickness.DefaultImpostProfileMm;
    }

    private void RefreshAfterImpostAdded(WindowToolMode mode)
    {
        if (FrameRoot == null)
        {
            return;
        }

        SelectedElement = FrameRoot;

        var active = DesignTools.FirstOrDefault(t => t.Mode == mode);
        StatusText = active?.Title ?? "Импост";

        OnPropertyChanged(nameof(FrameRoot));
        OnPropertyChanged(nameof(SelectedElement));
        OnPropertyChanged(nameof(SelectedElementLength));
        OnPropertyChanged(nameof(SelectedElementHeight));
        RequestSceneRefresh();
    }

    private static ImpostLineStyle GetImpostLineStyle(WindowToolMode mode) => mode switch
    {
        WindowToolMode.RealVerticalImpost or WindowToolMode.RealHorizontalImpost => ImpostLineStyle.Solid,
        WindowToolMode.DoubleVerticalImpost or WindowToolMode.DoubleHorizontalImpost => ImpostLineStyle.Double,
        WindowToolMode.SlantedImpost => ImpostLineStyle.Slanted,
        _ => ImpostLineStyle.Dashed
    };

    private void SyncCoordinatesFromSelection()
    {
        if (_isSyncingSelection)
        {
            return;
        }

        try
        {
            _isSyncingSelection = true;
            if (SelectedRectangle is CadRect rect)
            {
                CoordinateX = RoundMm(rect.X);
                CoordinateY = RoundMm(rect.Y - rect.Height);
                Length = RoundMm(rect.Width);
                Height = RoundMm(rect.Height);
                OnPropertyChanged(nameof(SelectedElementX));
                OnPropertyChanged(nameof(SelectedElementY));
                OnPropertyChanged(nameof(SelectedElementLength));
                OnPropertyChanged(nameof(SelectedElementHeight));
            }
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("SyncCoordinatesFromSelection failed", ex);
        }
        finally
        {
            _isSyncingSelection = false;
        }
    }

    private void ApplyCoordinatesToSelection()
    {
        if (_isSyncingSelection || _isCommittingRealFrame || IsShapePlacementActive ||
            _isShapeSizeDialogOpen || SelectedRectangle is not CadRect rect)
        {
            return;
        }

        if (FrameRoot is { IsLeaf: false })
        {
            return;
        }

        try
        {
            var width = Length;
            var height = Height;
            if (TryGetPrimaryElement(out var element))
            {
                if (element!.Kind is GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame)
                {
                    height = width / 2.0;
                    if (!_isSyncingSelection)
                    {
                        _isSyncingSelection = true;
                        Height = height;
                        _isSyncingSelection = false;
                    }
                }
                else if (element.Kind == GeometryElementKind.RoundDummyFrame)
                {
                    height = width;
                    if (!_isSyncingSelection)
                    {
                        _isSyncingSelection = true;
                        Height = height;
                        _isSyncingSelection = false;
                    }
                }
            }

            if (width <= 0 || height <= 0)
            {
                CadDiagnostics.LogWarning("Skip coordinate apply: invalid dimensions");
                return;
            }

            var originX = SnapToGrid(CoordinateX);
            var originY = SnapToGrid(CoordinateY);
            var updated = new CadRect(originX, originY + height, width, height);
            Document.RemoveRectangle(rect);
            Document.AddRectangle(updated);
            SelectedRectangle = updated;
            FrameRoot = FrameNode.CreateRoot(originX, originY, width, height);
            SelectedElement = FrameRoot;
            UpdatePrimaryElementGeometry(updated);
            NotifyFrameSelectionProperties();
            RequestSceneRefresh();
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("ApplyCoordinatesToSelection failed", ex);
        }
    }

    private bool TryGetPrimaryElement(out GeometryElement? element)
    {
        element = CanvasElements.FirstOrDefault();
        return element != null;
    }

    private void UpdatePrimaryElementGeometry(CadRect bounds)
    {
        if (CanvasElements.Count == 0)
        {
            return;
        }

        var primary = CanvasElements[0];
        primary.X = (decimal)(bounds.X + bounds.Width / 2);
        primary.Y = (decimal)(bounds.Y - bounds.Height / 2);
        primary.WidthMm = (decimal)bounds.Width;
        primary.HeightMm = (decimal)bounds.Height;
    }

    public CadPoint GetViewportCenterWorld() =>
        Viewport.ScreenToWorld(new ScreenPoint(Viewport.Width / 2, Viewport.Height / 2));

    public double SnapToGrid(double value)
    {
        var step = GridSnapStep;
        if (step <= CadMath.Epsilon)
        {
            return value;
        }

        return Math.Round(value / step) * step;
    }

    public CadPoint SnapPointToGrid(CadPoint point) =>
        new(SnapToGrid(point.X), SnapToGrid(point.Y));

    public bool TryPressVerticalImpostOffset(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.OffsetImpostVertical || FrameRoot is null)
        {
            return false;
        }

        var hit = HitTestVerticalImpost(worldPoint);
        if (hit is null)
        {
            StatusText = "Кликните по вертикальному импосту";
            return false;
        }

        _selectedMullion = hit;
        _isVerticalImpostDragging = true;
        _draggingMullionWorldX = ResolveSelectedMullionAxisX();
        SyncImpostPanelFromGhostAxis(_draggingMullionWorldX ?? worldPoint.X);
        OnPropertyChanged(nameof(SelectedMullion));
        OnPropertyChanged(nameof(DraggingMullionWorldX));
        OnPropertyChanged(nameof(IsImpostPanelActive));
        OnPropertyChanged(nameof(IsVerticalImpostDragging));
        StatusText = "Импост выделен — перетащите или измените L/H";
        RequestSceneRefresh();
        return true;
    }

    public void UpdateVerticalImpostOffsetDrag(CadPoint worldPoint)
    {
        if (!_isVerticalImpostDragging || _selectedMullion is null)
        {
            return;
        }

        if (!TryResolveVerticalImpostDragAxisRange(_selectedMullion, out var minX, out var maxX))
        {
            return;
        }

        var axisX = RoundMm(Math.Clamp(worldPoint.X, minX, maxX));
        _draggingMullionWorldX = axisX;
        TryMoveSelectedVerticalImpost(axisX);
        OnPropertyChanged(nameof(DraggingMullionWorldX));
        SyncImpostPanelFromGhostAxis(axisX);
        StatusText = $"Свет слева {ImpostPanelL:F1} · справа {ImpostPanelH:F1} мм";
        RequestSceneRefresh();
    }

    public void ReleaseVerticalImpostOffset()
    {
        if (!_isVerticalImpostDragging)
        {
            return;
        }

        if (_selectedMullion is not null && _draggingMullionWorldX is double axisX)
        {
            TryMoveSelectedVerticalImpost(axisX);
            SyncImpostPanelFromSelection();
        }

        _isVerticalImpostDragging = false;
        _draggingMullionWorldX = null;
        OnPropertyChanged(nameof(DraggingMullionWorldX));
        OnPropertyChanged(nameof(IsVerticalImpostDragging));
        if (_selectedMullion is not null)
        {
            StatusText = $"Импост: L={ImpostPanelL:F0} · H={ImpostPanelH:F0} мм";
        }

        RequestSceneRefresh();
    }

    public bool TryPressHorizontalImpostOffset(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.OffsetImpostHorizontal || FrameRoot is null)
        {
            return false;
        }

        var hit = HitTestHorizontalImpost(worldPoint);
        if (hit is null)
        {
            StatusText = "Кликните по горизонтальному импосту";
            return false;
        }

        _selectedMullion = hit;
        _isHorizontalImpostDragging = true;
        _draggingMullionWorldY = ResolveSelectedMullionAxisY();
        SyncImpostPanelFromGhostAxisY(_draggingMullionWorldY ?? worldPoint.Y);
        OnPropertyChanged(nameof(SelectedMullion));
        OnPropertyChanged(nameof(DraggingMullionWorldY));
        OnPropertyChanged(nameof(IsImpostPanelActive));
        OnPropertyChanged(nameof(IsHorizontalImpostDragging));
        StatusText = "Импост выделен — перетащите или измените L/H";
        RequestSceneRefresh();
        return true;
    }

    public void UpdateHorizontalImpostOffsetDrag(CadPoint worldPoint)
    {
        if (!_isHorizontalImpostDragging || _selectedMullion is null)
        {
            return;
        }

        if (!TryResolveHorizontalImpostDragAxisRange(_selectedMullion, out var minY, out var maxY))
        {
            return;
        }

        var axisY = RoundMm(Math.Clamp(worldPoint.Y, minY, maxY));
        _draggingMullionWorldY = axisY;
        TryMoveSelectedHorizontalImpost(axisY);
        OnPropertyChanged(nameof(DraggingMullionWorldY));
        SyncImpostPanelFromGhostAxisY(axisY);
        StatusText = $"Свет снизу {ImpostPanelL:F1} · сверху {ImpostPanelH:F1} мм";
        RequestSceneRefresh();
    }

    public void ReleaseHorizontalImpostOffset()
    {
        if (!_isHorizontalImpostDragging)
        {
            return;
        }

        if (_selectedMullion is not null && _draggingMullionWorldY is double axisY)
        {
            TryMoveSelectedHorizontalImpost(axisY);
            SyncImpostPanelFromSelection();
        }

        _isHorizontalImpostDragging = false;
        _draggingMullionWorldY = null;
        OnPropertyChanged(nameof(DraggingMullionWorldY));
        OnPropertyChanged(nameof(IsHorizontalImpostDragging));
        if (_selectedMullion is not null)
        {
            StatusText = $"Импост: L={ImpostPanelL:F0} · H={ImpostPanelH:F0} мм";
        }

        RequestSceneRefresh();
    }

    private double? ResolveSelectedMullionAxisX()
    {
        if (_selectedMullion is null)
        {
            return null;
        }

        var node = _selectedMullion.SplitNode;
        if (node.IsUniformSplit && _selectedMullion.UniformGapIndex is int gapIndex
            && node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, gapIndex, out var uniformAxis))
        {
            return RoundMm(uniformAxis);
        }

        return node.TryGetVerticalImpostAxisWorldX(out var axisX) ? RoundMm(axisX) : null;
    }

    private double? ResolveSelectedMullionAxisY()
    {
        if (_selectedMullion is null)
        {
            return null;
        }

        var node = _selectedMullion.SplitNode;
        if (node.IsUniformSplit && _selectedMullion.UniformGapIndex is int gapIndex
            && node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var uniformAxis))
        {
            return RoundMm(uniformAxis);
        }

        return node.TryGetHorizontalImpostAxisWorldY(out var axisY) ? RoundMm(axisY) : null;
    }

    private bool TryResolveVerticalImpostDragAxisRange(
        VerticalImpostTarget target,
        out double minAxisX,
        out double maxAxisX)
    {
        minAxisX = maxAxisX = 0;
        var node = target.SplitNode;
        var thickness = node.GetResolvedImpostThicknessMm();
        var half = thickness / 2.0;
        const double minCellMm = 40;

        var nodeMin = node.X + minCellMm + half;
        var nodeMax = node.X + node.Width - minCellMm - half;

        if (FrameRoot is null)
        {
            minAxisX = nodeMin;
            maxAxisX = nodeMax;
            return maxAxisX > minAxisX;
        }

        var inner = SashGeometry.ResolveNodeInnerOpeningBounds(node, FrameRoot, FrameThickness);
        var innerMin = inner.Left + ImpostDragInnerInsetMm + half;
        var innerMax = inner.Right - ImpostDragInnerInsetMm - half;

        minAxisX = Math.Max(nodeMin, innerMin);
        maxAxisX = Math.Min(nodeMax, innerMax);
        return maxAxisX > minAxisX;
    }

    private bool TryResolveHorizontalImpostDragAxisRange(
        VerticalImpostTarget target,
        out double minAxisY,
        out double maxAxisY)
    {
        minAxisY = maxAxisY = 0;
        var node = target.SplitNode;
        var thickness = node.GetResolvedImpostThicknessMm();
        var half = thickness / 2.0;
        const double minCellMm = 40;

        var nodeMin = node.Y + minCellMm + half;
        var nodeMax = node.Y + node.Height - minCellMm - half;

        if (FrameRoot is null)
        {
            minAxisY = nodeMin;
            maxAxisY = nodeMax;
            return maxAxisY > minAxisY;
        }

        var inner = SashGeometry.ResolveNodeInnerOpeningBounds(node, FrameRoot, FrameThickness);
        var innerMin = inner.Bottom + ImpostDragInnerInsetMm + half;
        var innerMax = inner.Top - ImpostDragInnerInsetMm - half;

        minAxisY = Math.Max(nodeMin, innerMin);
        maxAxisY = Math.Min(nodeMax, innerMax);
        return maxAxisY > minAxisY;
    }

    private void SyncImpostPanelFromGhostAxis(double axisWorldX)
    {
        if (_selectedMullion is null)
        {
            return;
        }

        var node = _selectedMullion.SplitNode;
        var half = node.GetResolvedImpostThicknessMm() / 2.0;
        _isSyncingImpostPanel = true;
        ImpostPanelX = RoundMm(axisWorldX);
        ImpostPanelY = RoundMm(node.Y);

        if (node.IsUniformSplit && _selectedMullion.UniformGapIndex is int gapIndex)
        {
            var leftCell = node.UniformCells[gapIndex];
            var rightCell = node.UniformCells[gapIndex + 1];
            var rightEdge = rightCell.X + rightCell.Width;
            ImpostPanelL = RoundMm(axisWorldX - leftCell.X - half);
            ImpostPanelH = RoundMm(rightEdge - axisWorldX - half);
        }
        else
        {
            ImpostPanelL = RoundMm((axisWorldX - node.X) - half);
            ImpostPanelH = RoundMm((node.X + node.Width - axisWorldX) - half);
        }

        _isSyncingImpostPanel = false;
    }

    private void SyncImpostPanelFromGhostAxisY(double axisWorldY)
    {
        if (_selectedMullion is null)
        {
            return;
        }

        var node = _selectedMullion.SplitNode;
        var half = node.GetResolvedImpostThicknessMm() / 2.0;
        _isSyncingImpostPanel = true;
        ImpostPanelY = RoundMm(axisWorldY);
        ImpostPanelX = RoundMm(node.X);

        if (node.IsUniformSplit && _selectedMullion.UniformGapIndex is int gapIndex)
        {
            var bottomCell = node.UniformCells[gapIndex];
            var topCell = node.UniformCells[gapIndex + 1];
            var topEdge = topCell.Y + topCell.Height;
            ImpostPanelL = RoundMm(axisWorldY - bottomCell.Y - half);
            ImpostPanelH = RoundMm(topEdge - axisWorldY - half);
        }
        else
        {
            ImpostPanelL = RoundMm((axisWorldY - node.Y) - half);
            ImpostPanelH = RoundMm((node.Y + node.Height - axisWorldY) - half);
        }

        _isSyncingImpostPanel = false;
    }

    private bool TryMoveSelectedVerticalImpost(double axisWorldX)
    {
        if (_selectedMullion is null)
        {
            return false;
        }

        return MoveVerticalImpost(_selectedMullion, axisWorldX);
    }

    private bool TryMoveSelectedHorizontalImpost(double axisWorldY)
    {
        if (_selectedMullion is null)
        {
            return false;
        }

        return MoveHorizontalImpost(_selectedMullion, axisWorldY);
    }

    private void ApplyImpostPanelAxisX(double axisX)
    {
        if (_isSyncingImpostPanel || _selectedMullion is null)
        {
            return;
        }

        if (TryMoveSelectedVerticalImpost(axisX))
        {
            SyncImpostPanelFromSelection();
            RequestSceneRefresh();
        }
    }

    private void ApplyImpostPanelAxisY(double axisY)
    {
        if (_isSyncingImpostPanel || _selectedMullion is null)
        {
            return;
        }

        if (TryMoveSelectedHorizontalImpost(axisY))
        {
            SyncImpostPanelFromSelection();
            RequestSceneRefresh();
        }
    }

    private void ApplyImpostPanelLeftOpening(double leftWidth)
    {
        if (_isSyncingImpostPanel || _selectedMullion is null)
        {
            return;
        }

        var node = _selectedMullion.SplitNode;
        var moved = node.IsUniformSplit && _selectedMullion.UniformGapIndex is int gap
            ? TrySetUniformVerticalLightOpeningLeftWidth(node, gap, leftWidth)
            : node.TrySetVerticalLightOpeningLeftWidth(leftWidth);
        if (moved)
        {
            SyncFillingsToFrameLayout();
            SyncImpostPanelFromSelection();
            RequestSceneRefresh();
        }
    }

    private void ApplyImpostPanelRightOpening(double rightWidth)
    {
        if (_isSyncingImpostPanel || _selectedMullion is null)
        {
            return;
        }

        var node = _selectedMullion.SplitNode;
        var moved = node.IsUniformSplit && _selectedMullion.UniformGapIndex is int gap
            ? TrySetUniformVerticalLightOpeningRightWidth(node, gap, rightWidth)
            : node.TrySetVerticalLightOpeningRightWidth(rightWidth);
        if (moved)
        {
            SyncFillingsToFrameLayout();
            SyncImpostPanelFromSelection();
            RequestSceneRefresh();
        }
    }

    private void ApplyImpostPanelBottomOpening(double bottomHeight)
    {
        if (_isSyncingImpostPanel || _selectedMullion is null)
        {
            return;
        }

        var node = _selectedMullion.SplitNode;
        var moved = node.IsUniformSplit && _selectedMullion.UniformGapIndex is int gap
            ? TrySetUniformHorizontalLightOpeningBottomHeight(node, gap, bottomHeight)
            : node.TrySetHorizontalLightOpeningBottomHeight(bottomHeight);
        if (moved)
        {
            SyncFillingsToFrameLayout();
            SyncImpostPanelFromSelection();
            RequestSceneRefresh();
        }
    }

    private void ApplyImpostPanelTopOpening(double topHeight)
    {
        if (_isSyncingImpostPanel || _selectedMullion is null)
        {
            return;
        }

        var node = _selectedMullion.SplitNode;
        var moved = node.IsUniformSplit && _selectedMullion.UniformGapIndex is int gap
            ? TrySetUniformHorizontalLightOpeningTopHeight(node, gap, topHeight)
            : node.TrySetHorizontalLightOpeningTopHeight(topHeight);
        if (moved)
        {
            SyncFillingsToFrameLayout();
            SyncImpostPanelFromSelection();
            RequestSceneRefresh();
        }
    }

    private void SyncImpostPanelFromSelection()
    {
        if (_selectedMullion is null)
        {
            return;
        }

        var node = _selectedMullion.SplitNode;
        _isSyncingImpostPanel = true;

        if (CurrentToolMode == WindowToolMode.OffsetImpostHorizontal)
        {
            if (node.IsUniformSplit && _selectedMullion.UniformGapIndex is int horizontalGapIndex)
            {
                if (node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, horizontalGapIndex, out var axisY))
                {
                    ImpostPanelY = RoundMm(axisY);
                }

                ImpostPanelX = RoundMm(node.X);
                if (TryGetUniformHorizontalLightOpeningHeights(node, horizontalGapIndex, out var bottom, out var top))
                {
                    ImpostPanelL = RoundMm(bottom);
                    ImpostPanelH = RoundMm(top);
                }
            }
            else
            {
                if (node.TryGetHorizontalImpostAxisWorldY(out var axisY))
                {
                    ImpostPanelY = RoundMm(axisY);
                }

                ImpostPanelX = RoundMm(node.X);
                if (node.TryGetHorizontalLightOpeningHeights(out var bottom, out var top))
                {
                    ImpostPanelL = RoundMm(bottom);
                    ImpostPanelH = RoundMm(top);
                }
            }
        }
        else if (node.IsUniformSplit && _selectedMullion.UniformGapIndex is int gapIndex)
        {
            if (node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, gapIndex, out var axisX))
            {
                ImpostPanelX = RoundMm(axisX);
            }

            ImpostPanelY = RoundMm(node.Y);
            if (TryGetUniformVerticalLightOpeningWidths(node, gapIndex, out var left, out var right))
            {
                ImpostPanelL = RoundMm(left);
                ImpostPanelH = RoundMm(right);
            }
        }
        else
        {
            if (node.TryGetVerticalImpostAxisWorldX(out var axisX))
            {
                ImpostPanelX = RoundMm(axisX);
            }

            ImpostPanelY = RoundMm(node.Y);
            if (node.TryGetVerticalLightOpeningWidths(out var left, out var right))
            {
                ImpostPanelL = RoundMm(left);
                ImpostPanelH = RoundMm(right);
            }
        }

        _isSyncingImpostPanel = false;
    }

    private void ClearImpostOffsetInteractionState()
    {
        _selectedMullion = null;
        _isVerticalImpostDragging = false;
        _isHorizontalImpostDragging = false;
        _draggingMullionWorldX = null;
        _draggingMullionWorldY = null;
        OnPropertyChanged(nameof(SelectedMullion));
        OnPropertyChanged(nameof(DraggingMullionWorldX));
        OnPropertyChanged(nameof(DraggingMullionWorldY));
        OnPropertyChanged(nameof(IsImpostPanelActive));
        OnPropertyChanged(nameof(IsVerticalImpostDragging));
        OnPropertyChanged(nameof(IsHorizontalImpostDragging));
        RequestSceneRefresh();
    }

    private double ResolveVerticalImpostHitToleranceMm() =>
        Math.Max(VerticalImpostHitToleranceMm, VerticalImpostHitScreenPx * MillimetersPerPixel);

    private VerticalImpostTarget? HitTestVerticalImpost(CadPoint worldPoint)
    {
        if (FrameRoot is null)
        {
            return null;
        }

        var tolerance = ResolveVerticalImpostHitToleranceMm();
        VerticalImpostTarget? best = null;
        var bestDistance = double.MaxValue;

        foreach (var node in FrameRoot.EnumerateInternalNodes())
        {
            if (node.SplitOrientation != SplitOrientation.Vertical)
            {
                continue;
            }

            if (node.IsUniformSplit)
            {
                for (var gapIndex = 0; gapIndex < node.UniformCells.Count - 1; gapIndex++)
                {
                    if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, gapIndex, out var axisX))
                    {
                        continue;
                    }

                    if (!IsPointNearVerticalImpost(node, axisX, worldPoint, tolerance, out var distance))
                    {
                        continue;
                    }

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = new VerticalImpostTarget
                        {
                            SplitNode = node,
                            UniformGapIndex = gapIndex
                        };
                    }
                }

                continue;
            }

            if (!node.TryGetVerticalImpostAxisWorldX(out var binaryAxisX))
            {
                continue;
            }

            if (!IsPointNearVerticalImpost(node, binaryAxisX, worldPoint, tolerance, out var binaryDistance))
            {
                continue;
            }

            if (binaryDistance < bestDistance)
            {
                bestDistance = binaryDistance;
                best = new VerticalImpostTarget { SplitNode = node };
            }
        }

        return best;
    }

    private VerticalImpostTarget? HitTestHorizontalImpost(CadPoint worldPoint)
    {
        if (FrameRoot is null)
        {
            return null;
        }

        var tolerance = ResolveVerticalImpostHitToleranceMm();
        VerticalImpostTarget? best = null;
        var bestDistance = double.MaxValue;

        foreach (var node in FrameRoot.EnumerateInternalNodes())
        {
            if (node.SplitOrientation != SplitOrientation.Horizontal)
            {
                continue;
            }

            if (node.IsUniformSplit)
            {
                for (var gapIndex = 0; gapIndex < node.UniformCells.Count - 1; gapIndex++)
                {
                    if (!node.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var axisY))
                    {
                        continue;
                    }

                    if (!IsPointNearHorizontalImpost(node, axisY, worldPoint, tolerance, out var distance))
                    {
                        continue;
                    }

                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = new VerticalImpostTarget
                        {
                            SplitNode = node,
                            UniformGapIndex = gapIndex
                        };
                    }
                }

                continue;
            }

            if (!node.TryGetHorizontalImpostAxisWorldY(out var binaryAxisY))
            {
                continue;
            }

            if (!IsPointNearHorizontalImpost(node, binaryAxisY, worldPoint, tolerance, out var binaryDistance))
            {
                continue;
            }

            if (binaryDistance < bestDistance)
            {
                bestDistance = binaryDistance;
                best = new VerticalImpostTarget { SplitNode = node };
            }
        }

        return best;
    }

    private static bool IsPointNearVerticalImpost(
        FrameNode node,
        double axisX,
        CadPoint point,
        double tolerance,
        out double distance)
    {
        distance = Math.Abs(point.X - axisX);
        if (distance > tolerance)
        {
            return false;
        }

        return point.Y >= node.Y - tolerance && point.Y <= node.Y + node.Height + tolerance;
    }

    private static bool IsPointNearHorizontalImpost(
        FrameNode node,
        double axisY,
        CadPoint point,
        double tolerance,
        out double distance)
    {
        distance = Math.Abs(point.Y - axisY);
        if (distance > tolerance)
        {
            return false;
        }

        return point.X >= node.X - tolerance && point.X <= node.X + node.Width + tolerance;
    }

    private static bool TryGetUniformVerticalLightOpeningWidths(
        FrameNode splitNode,
        int gapIndex,
        out double leftWidth,
        out double rightWidth)
    {
        leftWidth = rightWidth = 0;
        if (!splitNode.IsUniformSplit || gapIndex < 0 || gapIndex >= splitNode.UniformCells.Count - 1)
        {
            return false;
        }

        leftWidth = splitNode.UniformCells[gapIndex].Width;
        rightWidth = splitNode.UniformCells[gapIndex + 1].Width;
        return leftWidth > CadMath.Epsilon && rightWidth > CadMath.Epsilon;
    }

    private static bool TrySetUniformVerticalLightOpeningLeftWidth(
        FrameNode splitNode,
        int gapIndex,
        double leftWidth)
    {
        if (!splitNode.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, gapIndex, out var currentAxis))
        {
            return false;
        }

        var delta = leftWidth - splitNode.UniformCells[gapIndex].Width;
        return splitNode.TrySetUniformVerticalImpostGapAxisWorldX(
            gapIndex,
            currentAxis + delta);
    }

    private static bool TrySetUniformVerticalLightOpeningRightWidth(
        FrameNode splitNode,
        int gapIndex,
        double rightWidth)
    {
        if (!splitNode.TryGetUniformImpostAxisWorld(SplitOrientation.Vertical, gapIndex, out var currentAxis))
        {
            return false;
        }

        var delta = rightWidth - splitNode.UniformCells[gapIndex + 1].Width;
        return splitNode.TrySetUniformVerticalImpostGapAxisWorldX(
            gapIndex,
            currentAxis - delta);
    }

    private static bool TryGetUniformHorizontalLightOpeningHeights(
        FrameNode splitNode,
        int gapIndex,
        out double bottomHeight,
        out double topHeight)
    {
        bottomHeight = topHeight = 0;
        if (!splitNode.IsUniformSplit || gapIndex < 0 || gapIndex >= splitNode.UniformCells.Count - 1)
        {
            return false;
        }

        bottomHeight = splitNode.UniformCells[gapIndex].Height;
        topHeight = splitNode.UniformCells[gapIndex + 1].Height;
        return bottomHeight > CadMath.Epsilon && topHeight > CadMath.Epsilon;
    }

    private static bool TrySetUniformHorizontalLightOpeningBottomHeight(
        FrameNode splitNode,
        int gapIndex,
        double bottomHeight)
    {
        if (!splitNode.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var currentAxis))
        {
            return false;
        }

        var delta = bottomHeight - splitNode.UniformCells[gapIndex].Height;
        return splitNode.TrySetUniformHorizontalImpostGapAxisWorldY(
            gapIndex,
            currentAxis + delta);
    }

    private static bool TrySetUniformHorizontalLightOpeningTopHeight(
        FrameNode splitNode,
        int gapIndex,
        double topHeight)
    {
        if (!splitNode.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var currentAxis))
        {
            return false;
        }

        var delta = topHeight - splitNode.UniformCells[gapIndex + 1].Height;
        return splitNode.TrySetUniformHorizontalImpostGapAxisWorldY(
            gapIndex,
            currentAxis - delta);
    }

    public bool TryCycleSashHardware(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.Hardware)
        {
            return false;
        }

        var sash = FindSashAt(worldPoint);
        if (sash is null)
        {
            StatusText = "Кликните по створке";
            return false;
        }

        RecordPlacement("Фурнитура", () =>
        {
            if (!sash.HasHardware)
            {
                sash.HasHardware = true;
                sash.HingeSide = SashHingeSide.Right;
                sash.OpeningType = SashOpeningType.TiltTurn;
                StatusText = "Петли справа";
            }
            else if (sash.HingeSide == SashHingeSide.Right)
            {
                sash.HingeSide = SashHingeSide.Left;
                sash.OpeningType = SashOpeningType.TiltTurn;
                StatusText = "Петли слева";
            }
            else if (sash.HingeSide == SashHingeSide.Left)
            {
                sash.HingeSide = SashHingeSide.Bottom;
                sash.OpeningType = SashOpeningType.Turn;
                StatusText = "Петли снизу (фрамуга)";
            }
            else
            {
                sash.HasHardware = false;
                StatusText = "Петли сняты";
            }

            return true;
        });

        RequestSceneRefresh();
        return true;
    }

    public bool TryToggleSashHandle(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.Handle)
        {
            return false;
        }

        var sash = FindSashAt(worldPoint);
        if (sash is null)
        {
            StatusText = "Кликните по створке";
            return false;
        }

        RecordPlacement(sash.HasHandle ? "Ручка снята" : "Ручка установлена", () =>
        {
            sash.HasHandle = !sash.HasHandle;
            StatusText = sash.HasHandle ? "Ручка установлена" : "Ручка снята";
            return true;
        });

        RequestSceneRefresh();
        return true;
    }

    public bool TryBeginPointerInteraction(CadPoint worldPoint)
    {
        if (!IsInteractiveToolMode(CurrentToolMode))
        {
            return false;
        }

        return CurrentToolMode switch
        {
            WindowToolMode.Sash => BeginSashDrag(worldPoint),
            _ => false
        };
    }

    public void UpdatePointerInteraction(CadPoint worldPoint)
    {
        if (CurrentToolMode == WindowToolMode.Sash &&
            _isCanvasDragging &&
            _interactionStartWorld is not null &&
            _interactionLeaf is not null)
        {
            var start = _interactionStartWorld.Value;
            var deltaX = worldPoint.X - start.X;
            var deltaY = worldPoint.Y - start.Y;
            if (Math.Abs(deltaX) >= SashDragMinDeltaMm || Math.Abs(deltaY) >= SashDragMinDeltaMm)
            {
                _sashPreviewHingeSide = ResolveHingeSide(deltaX, deltaY);
            }

            UpdateSashPreview(_interactionLeaf, _sashPreviewHingeSide);
            return;
        }

        if (CurrentToolMode == WindowToolMode.Handle)
        {
            var sash = FindSashAt(worldPoint);
            HandlePreviewSashId = sash?.Id;
            OnPropertyChanged(nameof(HandlePreviewSashId));
        }
    }

    public bool TryEndPointerInteraction(CadPoint worldPoint)
    {
        try
        {
            return CurrentToolMode switch
            {
                WindowToolMode.Sash => EndSashDrag(worldPoint),
                _ => false
            };
        }
        finally
        {
            ClearInteractionState();
        }
    }

    public bool HasRealFrameOnCanvas =>
        CanvasElements.Any(e => e.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame);

    private bool BeginSashDrag(CadPoint worldPoint)
    {
        if (FrameRoot is null || !HasRealFrameOnCanvas)
        {
            StatusText = "Сначала создайте коробку (рама)";
            return false;
        }

        var leaf = FrameRoot.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (leaf is null || !leaf.IsLeaf)
        {
            StatusText = "Кликните внутри свободной ячейки";
            return false;
        }

        if (FindSashInLeaf(leaf) is not null)
        {
            StatusText = "Ячейка уже содержит створку";
            return false;
        }

        _interactionStartWorld = worldPoint;
        _interactionLeaf = leaf;
        _isCanvasDragging = true;
        _sashPreviewHingeSide = SashHingeSide.Left;
        UpdateSashPreview(leaf, _sashPreviewHingeSide);
        return true;
    }

    private bool EndSashDrag(CadPoint worldPoint)
    {
        if (!_isCanvasDragging || _interactionStartWorld is not CadPoint start || _interactionLeaf is not FrameNode leaf)
        {
            return false;
        }

        var deltaX = worldPoint.X - start.X;
        var deltaY = worldPoint.Y - start.Y;
        if (Math.Sqrt(deltaX * deltaX + deltaY * deltaY) < 15)
        {
            StatusText = "Укажите направление открывания перетаскиванием";
            return false;
        }

        var sash = CreateSashElement(leaf, ResolveHingeSide(deltaX, deltaY));
        if (sash is null)
        {
            StatusText = "Не удалось создать створку";
            return false;
        }

        RecordPlacement("Створка создана", () =>
        {
            CanvasElements.Add(sash);
            return true;
        });
        StatusText = "Створка создана";
        RequestSceneRefresh();
        return true;
    }

    private void ClearInteractionState()
    {
        _isCanvasDragging = false;
        _interactionStartWorld = null;
        _interactionLeaf = null;
        _sashPreviewHingeSide = SashHingeSide.Left;
        SashDragPreview = null;
        HandlePreviewSashId = null;
        OnPropertyChanged(nameof(SashDragPreview));
        OnPropertyChanged(nameof(HandlePreviewSashId));
    }

    private static SashHingeSide ResolveHingeSide(double deltaX, double deltaY)
    {
        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            return deltaX < 0 ? SashHingeSide.Right : SashHingeSide.Left;
        }

        return deltaY < 0 ? SashHingeSide.Top : SashHingeSide.Bottom;
    }

    private void UpdateSashPreview(FrameNode leaf, SashHingeSide hingeSide)
    {
        var (innerX, innerY, innerW, innerH) = SashGeometry.ResolveLeafInnerOpening(
            leaf,
            FrameRoot,
            FrameThickness);
        if (innerW <= 0 || innerH <= 0)
        {
            SashDragPreview = null;
            OnPropertyChanged(nameof(SashDragPreview));
            return;
        }

        SashDragPreview = new SashDragPreview
        {
            CellOriginX = leaf.X,
            CellOriginY = leaf.Y,
            CellWidth = leaf.Width,
            CellHeight = leaf.Height,
            OriginX = innerX,
            OriginY = innerY,
            Width = innerW,
            Height = innerH,
            HingeSide = hingeSide
        };
        OnPropertyChanged(nameof(SashDragPreview));
    }

    private GeometryElement? CreateSashElement(FrameNode leaf, SashHingeSide hingeSide)
    {
        var (innerX, innerY, innerW, innerH) = SashGeometry.ResolveLeafInnerOpening(
            leaf,
            FrameRoot,
            FrameThickness);
        if (innerW <= 40 || innerH <= 40)
        {
            return null;
        }

        return new GeometryElement
        {
            Kind = GeometryElementKind.Sash,
            Label = "Створка",
            X = (decimal)(innerX + innerW / 2.0),
            Y = (decimal)(innerY + innerH / 2.0),
            WidthMm = (decimal)innerW,
            HeightMm = (decimal)innerH,
            HingeSide = hingeSide,
            OpeningType = SashOpeningType.TiltTurn
        };
    }

    private GeometryElement? FindSashAt(CadPoint point) =>
        CanvasElements.FirstOrDefault(e => e.Kind == GeometryElementKind.Sash && ContainsElement(e, point));

    private GeometryElement? FindSashInLeaf(FrameNode leaf) =>
        CanvasElements.FirstOrDefault(e => e.Kind == GeometryElementKind.Sash && ContainsLeaf(e, leaf));

    private GeometryElement? FindGlazingInLeaf(FrameNode leaf) =>
        CanvasElements.FirstOrDefault(e =>
            e.Kind == GeometryElementKind.Glazing &&
            (ContainsLeaf(e, leaf) ||
             (e.HostSashId is Guid sashId && FindSashInLeaf(leaf)?.Id == sashId)));

    private bool CanApplyGlazingInLeaf(FrameNode leaf)
    {
        if (FindGlazingInLeaf(leaf) is not null)
        {
            return false;
        }

        if (CanvasElements.Any(e => e.Kind == GeometryElementKind.Corrugation && ContainsLeaf(e, leaf)))
        {
            return false;
        }

        return true;
    }

    private bool TryPlaceGlazingInLeaf(FrameNode leaf)
    {
        var glazing = CreateGlazingElement(leaf);
        if (glazing is null)
        {
            StatusText = FindSashInLeaf(leaf) is not null
                ? "Световой проём створки слишком мал для остекления"
                : "Проём слишком мал для остекления";
            return false;
        }

        if (!RecordPlacement("Остекление", () =>
            {
                CanvasElements.Add(glazing);
                return true;
            }))
        {
            return false;
        }

        StatusText = "Остекление установлено";
        RequestSceneRefresh();
        return true;
    }

    private GeometryElement? CreateGlazingElement(FrameNode leaf)
    {
        if (FrameRoot is null)
        {
            return null;
        }

        var sash = FindSashInLeaf(leaf);
        if (sash is not null)
        {
            return CreateGlazingElementForSash(sash, leaf);
        }

        var (innerX, innerY, innerW, innerH) = SashGeometry.ResolveLeafInnerOpening(
            leaf,
            FrameRoot,
            FrameThickness);
        if (innerW < 40 || innerH < 40)
        {
            return null;
        }

        return new GeometryElement
        {
            Kind = GeometryElementKind.Glazing,
            Label = "Остекление",
            X = (decimal)(innerX + innerW / 2.0),
            Y = (decimal)(innerY + innerH / 2.0),
            WidthMm = (decimal)innerW,
            HeightMm = (decimal)innerH,
            SourceToolId = "glazing"
        };
    }

    private GeometryElement? CreateGlazingElementForSash(GeometryElement sash, FrameNode? leaf)
    {
        if (FrameRoot is null)
        {
            return null;
        }

        var inner = SashGeometry.TryResolveInnerBounds(this, sash, out var liveInner)
            ? liveInner
            : leaf is not null
                ? SashGeometry.ResolveLeafInnerOpeningBounds(leaf, FrameRoot, FrameThickness)
                : SashGeometry.InnerOpeningBoundsFromElement(sash);
        var outer = SashGeometry.ExpandInnerBoundsToOuter(inner, this);
        var (glassLeft, glassBottom, glassRight, glassTop) = SashGeometry.ResolveInnerGlassFromOuter(
            outer.Left,
            outer.Bottom,
            outer.Width,
            outer.Height);
        var width = glassRight - glassLeft;
        var height = glassTop - glassBottom;
        if (width < 20 || height < 20)
        {
            return null;
        }

        return new GeometryElement
        {
            Kind = GeometryElementKind.Glazing,
            Label = "Остекление",
            HostSashId = sash.Id,
            X = (decimal)((glassLeft + glassRight) / 2.0),
            Y = (decimal)((glassBottom + glassTop) / 2.0),
            WidthMm = (decimal)width,
            HeightMm = (decimal)height,
            SourceToolId = "glazing"
        };
    }

    private static bool ContainsElement(GeometryElement element, CadPoint point)
    {
        if (element.Kind == GeometryElementKind.Sash)
        {
            return SashGeometry.ContainsOuterBounds(element, point.X, point.Y);
        }

        var halfW = (double)element.WidthMm / 2;
        var halfH = (double)element.HeightMm / 2;
        var cx = (double)element.X;
        var cy = (double)element.Y;
        return point.X >= cx - halfW && point.X <= cx + halfW
            && point.Y >= cy - halfH && point.Y <= cy + halfH;
    }

    private static bool ContainsLeaf(GeometryElement element, FrameNode leaf)
    {
        var cx = (double)element.X;
        var cy = (double)element.Y;
        return leaf.Contains(cx, cy);
    }

    private static bool IsInteractiveToolMode(WindowToolMode mode) =>
        mode is WindowToolMode.Sash;

    private CadPoint SnapWorldPoint(CadPoint point)
    {
        if (IsSnapEnabled)
        {
            point = Snapping.Snap(point, BuildSnapLines()).Point;
        }

        return SnapPointToGrid(point);
    }

    private IEnumerable<CadLine> BuildSnapLines()
    {
        foreach (var rect in Document.Rectangles)
        {
            var topLeft = new CadPoint(rect.X, rect.Y);
            var topRight = new CadPoint(rect.X + rect.Width, rect.Y);
            var bottomLeft = new CadPoint(rect.X, rect.Y - rect.Height);
            var bottomRight = new CadPoint(rect.X + rect.Width, rect.Y - rect.Height);
            yield return new CadLine(topLeft, topRight);
            yield return new CadLine(topRight, bottomRight);
            yield return new CadLine(bottomRight, bottomLeft);
            yield return new CadLine(bottomLeft, topLeft);
        }
    }

    public sealed class CadEditorSnapshot
    {
        public string DocumentName { get; init; } = "Новый чертеж";

        public double ViewportZoomFactor { get; init; } = 0.35;

        public double ViewportPanX { get; init; } = 500;

        public double ViewportPanY { get; init; } = 350;

        public FrameNodeSnapshot? FrameRoot { get; init; }
    }

    public sealed class FrameNodeSnapshot
    {
        public double X { get; init; }

        public double Y { get; init; }

        public double Width { get; init; }

        public double Height { get; init; }

        public bool IsLeaf { get; init; } = true;

        public bool IsUniformSplit { get; init; }

        public string SplitOrientation { get; init; } = "None";

        public string LineStyle { get; init; } = "Dashed";

        public double ImpostThicknessMm { get; init; }

        public int UniformImpostCount { get; init; }

        public List<FrameNodeSnapshot> UniformCells { get; init; } = [];

        public FrameNodeSnapshot? ChildA { get; init; }

        public FrameNodeSnapshot? ChildB { get; init; }
    }

    public CadEditorSnapshot CaptureSnapshot() => new()
    {
        DocumentName = Document.Name,
        ViewportZoomFactor = Viewport.ZoomFactor,
        ViewportPanX = Viewport.PanOffset.X,
        ViewportPanY = Viewport.PanOffset.Y,
        FrameRoot = FrameRoot is null ? null : CaptureFrameNode(FrameRoot)
    };

    public void ApplySnapshot(CadEditorSnapshot snapshot, IReadOnlyList<GeometryElement> elements)
    {
        if (_isResettingCanvas)
        {
            return;
        }

        try
        {
            _isResettingCanvas = true;
            _canvasResetDepth++;

            Document.Name = snapshot.DocumentName;
            Viewport.ZoomFactor = snapshot.ViewportZoomFactor;
            Viewport.PanOffset = new CadVector(snapshot.ViewportPanX, snapshot.ViewportPanY);
            OnPropertyChanged(nameof(ZoomPercent));
            OnPropertyChanged(nameof(MillimetersPerPixel));

            FrameRoot = snapshot.FrameRoot is null
                ? TryCreateFrameFromElements(elements)
                : RestoreFrameNode(snapshot.FrameRoot);

            SetDesignElements(elements);

            Commands.Clear();
            _placementHistory.Clear();
            NotifyPlacementHistoryChanged();
            RequestSceneRefresh();
        }
        finally
        {
            _canvasResetDepth--;
            _isResettingCanvas = false;
        }
    }

    public void ResetWorkspace()
    {
        Document.Reset();
        Document.Name = "Новый чертеж";
        CanvasElements.Clear();
        FrameRoot = null;
        SelectedRectangle = null;
        Length = 0;
        Height = 0;
        CoordinateX = 0;
        CoordinateY = 0;
        Viewport.ResetToDefaultScale(15.0);
        Commands.Clear();
        _placementHistory.Clear();
        NotifyPlacementHistoryChanged();
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));
        RequestSceneRefresh();
    }

    private static FrameNodeSnapshot CaptureFrameNode(FrameNode node) => new()
    {
        X = node.X,
        Y = node.Y,
        Width = node.Width,
        Height = node.Height,
        IsLeaf = node.IsLeaf,
        IsUniformSplit = node.IsUniformSplit,
        SplitOrientation = node.SplitOrientation.ToString(),
        LineStyle = node.LineStyle.ToString(),
        ImpostThicknessMm = node.ImpostThicknessMm,
        UniformImpostCount = node.UniformImpostCount,
        UniformCells = node.UniformCells.Select(CaptureFrameNode).ToList(),
        ChildA = node.ChildA is null ? null : CaptureFrameNode(node.ChildA),
        ChildB = node.ChildB is null ? null : CaptureFrameNode(node.ChildB)
    };

    private static FrameNode? RestoreFrameNode(FrameNodeSnapshot snapshot) =>
        FrameNode.RestoreTree(ToTreeSnapshot(snapshot));

    private static FrameNode.TreeSnapshot ToTreeSnapshot(FrameNodeSnapshot snapshot) => new()
    {
        X = snapshot.X,
        Y = snapshot.Y,
        Width = snapshot.Width,
        Height = snapshot.Height,
        IsLeaf = snapshot.IsLeaf,
        IsUniformSplit = snapshot.IsUniformSplit,
        SplitOrientation = ParseSplitOrientation(snapshot.SplitOrientation),
        LineStyle = ParseImpostLineStyle(snapshot.LineStyle),
        ImpostThicknessMm = snapshot.ImpostThicknessMm,
        UniformImpostCount = snapshot.UniformImpostCount,
        UniformCells = snapshot.UniformCells.Select(ToTreeSnapshot).ToList(),
        ChildA = snapshot.ChildA is null ? null : ToTreeSnapshot(snapshot.ChildA),
        ChildB = snapshot.ChildB is null ? null : ToTreeSnapshot(snapshot.ChildB)
    };

    private static SplitOrientation ParseSplitOrientation(string value) =>
        Enum.TryParse<SplitOrientation>(value, ignoreCase: true, out var parsed)
            ? parsed
            : SplitOrientation.None;

    private static ImpostLineStyle ParseImpostLineStyle(string value) =>
        Enum.TryParse<ImpostLineStyle>(value, ignoreCase: true, out var parsed)
            ? parsed
            : ImpostLineStyle.Dashed;

    private static FrameNode? TryCreateFrameFromElements(IReadOnlyList<GeometryElement> elements)
    {
        var frameElement = elements.FirstOrDefault(e => e.Kind == GeometryElementKind.Frame);
        if (frameElement is null)
        {
            return null;
        }

        var width = (double)frameElement.WidthMm;
        var height = (double)frameElement.HeightMm;
        return FrameNode.CreateRoot(
            (double)frameElement.X - width / 2.0,
            (double)frameElement.Y - height / 2.0,
            width,
            height);
    }
}
