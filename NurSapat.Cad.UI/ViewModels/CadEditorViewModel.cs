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
using System.ComponentModel;
using System.Linq;

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
    private readonly HashSet<CadSelectableItem> _constructionSelection = new();
    private bool _isVerticalImpostDragging;
    private bool _isHorizontalImpostDragging;
    private double? _draggingMullionWorldX;
    private double? _draggingMullionWorldY;
    private bool _isSyncingImpostPanel;
    private bool _isCommittingHorizontalImpostRelease;
    private bool _isTopProfileHovered;
    private bool _isSyncingTopProfileUi;
    private ProfileSegment? _watchedTopProfile;
    private bool _isArcVertexDragging;
    private ArcVertexDragTarget _arcVertexDragTarget;
    private double _arcHeightDragStartWorldY;
    private double _arcHeightDragStartHeight;
    private ConstructionMemento? _constructionDragBeforeMemento;

    private const double VerticalImpostHitToleranceMm = 18;
    private const double VerticalImpostHitScreenPx = 14;
    private const double TopProfileHitScreenPx = 10;
    private const double ArcVertexHandleScreenPx = 12;
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

    public ObservableCollection<CadDesignToolButtonViewModel> CreationDesignTools { get; } = [];

    public ObservableCollection<CadDesignToolButtonViewModel> EditingDesignTools { get; } = [];

    private IEnumerable<CadDesignToolButtonViewModel> AllDesignTools =>
        CreationDesignTools.Concat(EditingDesignTools);

    public event EventHandler? SceneInvalidated;

    public event EventHandler? NewTemplateRequested;

    public event EventHandler<FrameCreatedEventArgs>? FrameCreated;

    public Func<CadPoint, bool>? DesignPlacement { get; set; }

    public Action<DesignPlacementCommit>? OnDesignPlacementCommit { get; set; }

    public bool CanUndoPlacement => _placementHistory.CanUndo;

    public bool CanRedoPlacement => _placementHistory.CanRedo;

    public bool CanRedo => CanRedoPlacement || Commands.CanRedo;

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

    private string _activeToolKey = "select-elements";

    [ObservableProperty]
    private WindowToolMode _currentToolMode = WindowToolMode.SelectElement;

    public IRelayCommand<string> ActivateDesignToolCommand { get; }

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

    public CanvasTool CurrentTool
    {
        get => CurrentToolMode switch
        {
            WindowToolMode.RadiusVertex => CanvasTool.RadiusVertex,
            WindowToolMode.SelectElement => CanvasTool.SelectElement,
            _ => CanvasTool.None
        };
        set
        {
            var mode = value switch
            {
                CanvasTool.RadiusVertex => WindowToolMode.RadiusVertex,
                CanvasTool.SelectElement => WindowToolMode.SelectElement,
                _ => WindowToolMode.SelectElement
            };

            ActiveToolMode = mode;
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

    public Func<Task>? OpenStandardStructuresHandler { get; set; }

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
        ActiveTool = _toolFactory.Create(CadToolMetadata.GetId(CadToolKind.SelectElements));
        ActiveToolId = CadToolMetadata.GetId(CadToolKind.SelectElements);
        ActivateToolCommand = new RelayCommand<string>(ActivateCadTool);
        SelectToolModeCommand = new RelayCommand<WindowToolMode>(mode => _ = SelectToolModeAsync(mode));
        ActivateDesignToolCommand = new RelayCommand<string>(toolKey => _ = ActivateDesignToolAsync(toolKey));

        InitializeDesignTools();
        EnsureDefaultConstructions();
        _isApplyingDrawingTemplate = true;
        SelectedDrawingTemplate = DrawingTemplates.FirstOrDefault();
        _isApplyingDrawingTemplate = false;
        _ = LoadMaterialsAsync();
        UpdateDesignToolSelection(WindowToolMode.SelectElement, "select-elements");
        ActiveTool.OnActivated(CreateToolContext());
        Viewport.ResetToDefaultScale(15.0);
        _placementHistory.Changed += (_, _) => NotifyPlacementHistoryChanged();
        Commands.HistoryChanged += (_, _) => NotifyPlacementHistoryChanged();
    }

    public double MillimetersPerPixel =>
        Viewport.ZoomFactor > CadMath.Epsilon ? 1.0 / Viewport.ZoomFactor : 15.0;

    public string ZoomPercent => $"{Viewport.ZoomFactor * 100:0}%";

    public string ActiveToolDisplayName => ActiveDesignToolLabel ?? ActiveTool?.DisplayName ?? "Инструмент";

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

    public bool ShowStatusBarDimensions => IsShapePlacementActive;

    public double StatusBarLength => ShowStatusBarDimensions ? SelectedElementLength : 0;

    public double StatusBarHeight => ShowStatusBarDimensions ? SelectedElementHeight : 0;

    public bool IsImpostPanelActive =>
        _selectedMullion != null &&
        CurrentToolMode is WindowToolMode.OffsetImpostVertical or WindowToolMode.OffsetImpostHorizontal;

    public VerticalImpostTarget? SelectedMullion => _selectedMullion;

    public IReadOnlyCollection<CadSelectableItem> ConstructionSelection => _constructionSelection;

    public bool IsConstructionItemSelected(CadSelectableItem item) => _constructionSelection.Contains(item);

    public bool IsGeometryElementSelected(Guid elementId) =>
        _constructionSelection.Any(item =>
            item.ElementId == elementId &&
            item.Kind is CadSelectableKind.Sash
                or CadSelectableKind.Glazing
                or CadSelectableKind.Bead);

    public bool IsFrameConstructionSelected =>
        _constructionSelection.Any(item => item.Kind == CadSelectableKind.Frame);

    public bool IsImpostConstructionSelected(FrameNode node, int uniformGapIndex = 0) =>
        _constructionSelection.Any(item =>
            item.Kind == CadSelectableKind.Impost &&
            ReferenceEquals(item.ImpostNode, node) &&
            item.ImpostGapIndex == uniformGapIndex);

    private bool HasCommittedConstruction => FrameRoot != null || CanvasElements.Count > 0;

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
        IsImpostConstructionSelected(node, uniformGapIndex) ||
        (CurrentToolMode == WindowToolMode.OffsetImpostVertical &&
         !_draggingMullionWorldX.HasValue &&
         IsSelectedForOffset(node, uniformGapIndex));

    public bool IsHorizontalImpostHighlighted(FrameNode node, int uniformGapIndex = 0) =>
        IsImpostConstructionSelected(node, uniformGapIndex) ||
        (CurrentToolMode == WindowToolMode.OffsetImpostHorizontal &&
         !_draggingMullionWorldY.HasValue &&
         IsSelectedForOffset(node, uniformGapIndex));

    public bool IsTopProfileHovered =>
        _isTopProfileHovered &&
        CurrentToolMode == WindowToolMode.RadiusVertex;

    public ProfileSegment? HoveredTopProfile =>
        IsTopProfileHovered ? FrameRoot?.TopProfile : null;

    public bool HasArcTopProfile =>
        FrameRoot?.TopProfile?.SegmentType == SegmentType.Arc;

    public bool IsArcVertexDragging => _isArcVertexDragging;

    public bool ShowArcVertexHandles =>
        HasArcTopProfile && CurrentToolMode == WindowToolMode.RadiusVertex;

    public double TopProfileArcRadius
    {
        get => HasArcTopProfile && FrameRoot?.TopProfile is { } profile
            ? RoundMm(profile.Radius)
            : 0;
        set
        {
            if (!HasArcTopProfile)
            {
                return;
            }

            if (!RecordPlacement("Изменение радиуса", () => TryApplyTopProfileRadius(value)))
            {
                return;
            }

            NotifyTopProfileUiProperties();
            RequestSceneRefresh();
        }
    }

    public double TopProfileArcHeight
    {
        get => HasArcTopProfile && FrameRoot?.TopProfile is { } profile
            ? RoundMm(profile.ArcHeight)
            : 0;
        set
        {
            if (!HasArcTopProfile)
            {
                return;
            }

            if (!RecordPlacement("Изменение стрелы прогиба", () => TryApplyTopProfileArcHeight(value)))
            {
                return;
            }

            NotifyTopProfileUiProperties();
            RequestSceneRefresh();
        }
    }

    partial void OnFrameRootChanged(FrameNode? value)
    {
        AttachTopProfileWatcher();
    }

    public double GridSnapStep => Viewport.ChooseGridStep();

    public void SetDesignElements(IReadOnlyList<GeometryElement> elements)
    {
        if (_isResettingCanvas && _canvasResetDepth == 0)
        {
            return;
        }

        var snapshot = elements.ToList();
        CanvasElements.Clear();
        foreach (var element in snapshot)
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
        var extent = FrameNode.ResolveHorizontalSplitExtentLocal(node, FrameRoot ?? node.GetRoot());
        TraceHorizontalImpost(
            "move-enter",
            ghostAxisY: _draggingMullionWorldY,
            moveInputAxisY: axisWorldY,
            modelAxisY: HorizontalImpostDragDiagnostics.TryReadModelAxisY(node, target.UniformGapIndex),
            renderAxisY: HorizontalImpostDragDiagnostics.TryReadRenderAxisY(node, target.UniformGapIndex),
            detail: $"extent={extent:F1} uniform={node.IsUniformSplit} gap={target.UniformGapIndex?.ToString() ?? "-"} inTree={IsSplitNodeInFrameTree(node)} split#{node.GetHashCode()} root#{FrameRoot?.GetHashCode().ToString() ?? "null"}");

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

        TraceHorizontalImpost(
            "move-exit",
            ghostAxisY: _draggingMullionWorldY,
            moveInputAxisY: axisWorldY,
            modelAxisY: HorizontalImpostDragDiagnostics.TryReadModelAxisY(node, target.UniformGapIndex),
            renderAxisY: HorizontalImpostDragDiagnostics.TryReadRenderAxisY(node, target.UniformGapIndex),
            moveSucceeded: moved,
            detail: moved ? null : "TrySet returned false");

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

        SyncSashTopology();

        foreach (var element in CanvasElements.ToList())
        {
            if (element.Kind == GeometryElementKind.Sash)
            {
                SyncSashElementToLeaf(element);
            }
        }

        SyncGlazingTopology();

        foreach (var element in CanvasElements.ToList())
        {
            if (element.Kind == GeometryElementKind.Glazing && element.HostSashId is not null)
            {
                SyncGlazingElementToLayout(element);
            }
        }
    }

    /// <summary>
    /// Синхронизирует створки с деревом FrameNode: одна створка на каждый лист проёма.
    /// </summary>
    private void SyncSashTopology()
    {
        if (FrameRoot is null)
        {
            return;
        }

        foreach (var sash in CanvasElements
                     .Where(e => e.Kind == GeometryElementKind.Sash)
                     .ToList())
        {
            var leaves = SashGeometry.FindLeavesHostingSash(this, sash);
            if (leaves.Count <= 1)
            {
                continue;
            }

            var hingeSide = sash.HingeSide ?? SashHingeSide.Left;
            var openingType = sash.OpeningType ?? SashOpeningType.TiltTurn;
            var hasHardware = sash.HasHardware;
            var hasHandle = sash.HasHandle;
            var hadHostedGlazing = CanvasElements.Any(
                e => e.Kind == GeometryElementKind.Glazing && e.HostSashId == sash.Id);

            CanvasElements.Remove(sash);
            if (hadHostedGlazing)
            {
                RemoveHostedGlazingForSash(sash.Id);
            }

            foreach (var leaf in leaves)
            {
                if (FindSashInLeaf(leaf) is not null)
                {
                    continue;
                }

                var created = CreateSashElement(leaf, hingeSide);
                if (created is null)
                {
                    continue;
                }

                created.OpeningType = openingType;
                created.HasHardware = hasHardware;
                created.HasHandle = hasHandle;
                CanvasElements.Add(created);

                if (hadHostedGlazing)
                {
                    var glazing = CreateGlazingElementForSash(created, leaf);
                    if (glazing is not null)
                    {
                        CanvasElements.Add(glazing);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Синхронизирует остекление с деревом FrameNode: один стеклопакет на каждый лист проёма.
    /// </summary>
    private void SyncGlazingTopology()
    {
        if (FrameRoot is null)
        {
            return;
        }

        foreach (var glazing in CanvasElements
                     .Where(e => e.Kind == GeometryElementKind.Glazing && e.HostSashId is null)
                     .ToList())
        {
            var leaves = SashGeometry.FindLeavesHostingStandaloneGlazing(this, glazing)
                .Where(leaf => !LeafBlocksGlazingPlacement(leaf))
                .ToList();

            if (leaves.Count <= 1)
            {
                continue;
            }

            CanvasElements.Remove(glazing);
            foreach (var leaf in leaves)
            {
                if (FindGlazingInLeaf(leaf) is not null)
                {
                    continue;
                }

                var created = CreateGlazingElement(leaf);
                if (created is not null)
                {
                    CanvasElements.Add(created);
                }
            }
        }

        foreach (var glazing in CanvasElements
                     .Where(e => e.Kind == GeometryElementKind.Glazing && e.HostSashId is null)
                     .ToList())
        {
            SyncGlazingElementToLayout(glazing);
        }
    }

    private bool LeafBlocksGlazingPlacement(FrameNode leaf) =>
        FindSashInLeaf(leaf) is not null ||
        CanvasElements.Any(e => e.Kind == GeometryElementKind.Corrugation && ContainsLeaf(e, leaf));

    private void SyncSashElementToLeaf(GeometryElement sash)
    {
        var leaf = SashGeometry.FindLeafForSash(this, sash);
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

            var leaf = SashGeometry.FindLeafForSash(this, sash);
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

        var glazingLeaf = SashGeometry.FindLeafForGlazing(this, glazing);
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
        SnapAxis = SnapToGrid,
        HasConstructionElementAtPoint = HasConstructionElementAtPoint,
        HandleConstructionClickSelection = HandleConstructionClickSelection,
        HandleConstructionBoxSelection = HandleConstructionBoxSelection
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
            UpdateDesignToolSelection(WindowToolMode.SelectElement, "select-elements");
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
        if (CurrentToolMode == WindowToolMode.OffsetImpostHorizontal)
        {
            HorizontalImpostDragDiagnostics.Trace(
                "memento-apply",
                ghostAxisY: _draggingMullionWorldY,
                modelAxisY: HorizontalImpostDragDiagnostics.TryReadModelAxisY(
                    _selectedMullion?.SplitNode,
                    _selectedMullion?.UniformGapIndex),
                splitNode: _selectedMullion?.SplitNode,
                frameRoot: FrameRoot,
                detail: "ApplyConstructionMemento — дерево будет заменено");
        }

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
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(CanUndo));
        UndoLastElementCommand.NotifyCanExecuteChanged();
        UndoCommand.NotifyCanExecuteChanged();
        RedoCommand.NotifyCanExecuteChanged();
    }

    private void BeginConstructionDrag()
    {
        _constructionDragBeforeMemento = _isResettingCanvas ? null : CaptureConstructionMemento();
    }

    private void CommitConstructionDrag(string description)
    {
        if (_constructionDragBeforeMemento is null || _isResettingCanvas)
        {
            _constructionDragBeforeMemento = null;
            return;
        }

        TraceHorizontalImpost("commit-before-capture", ghostAxisY: _draggingMullionWorldY);

        var after = CaptureConstructionMemento();
        _placementHistory.Record(_constructionDragBeforeMemento, after, description);
        _constructionDragBeforeMemento = null;
        NotifyPlacementHistoryChanged();

        TraceHorizontalImpost("commit-after-record", ghostAxisY: _draggingMullionWorldY);

        RefreshAfterPlacementHistoryChange();

        TraceHorizontalImpost("commit-after-refresh", ghostAxisY: _draggingMullionWorldY);
    }

    private void CancelConstructionDrag() => _constructionDragBeforeMemento = null;

    private bool HasConstructionElementAtPoint(CadPoint worldPoint) =>
        HasCommittedConstruction && ConstructionSelectionHelper.HitTest(this, worldPoint) != null;

    private bool HandleConstructionClickSelection(CadPoint worldPoint, bool isCtrlDown)
    {
        if (!HasCommittedConstruction)
        {
            return false;
        }

        var hit = ConstructionSelectionHelper.HitTest(this, worldPoint);
        if (hit is null)
        {
            if (!isCtrlDown)
            {
                ClearConstructionSelection();
            }

            RequestSceneRefresh();
            return true;
        }

        if (isCtrlDown)
        {
            if (_constructionSelection.Contains(hit))
            {
                _constructionSelection.Remove(hit);
            }
            else
            {
                _constructionSelection.Add(hit);
            }
        }
        else
        {
            _constructionSelection.Clear();
            _constructionSelection.Add(hit);
        }

        SyncLegacySelectionFromConstruction();
        RequestSceneRefresh();
        return true;
    }

    private void HandleConstructionBoxSelection(CadRect bounds, bool isCtrlDown)
    {
        if (!HasCommittedConstruction)
        {
            return;
        }

        var hits = ConstructionSelectionHelper.HitTestInRect(this, bounds);
        if (!isCtrlDown)
        {
            _constructionSelection.Clear();
        }

        foreach (var hit in hits)
        {
            _constructionSelection.Add(hit);
        }

        SyncLegacySelectionFromConstruction();
        RequestSceneRefresh();
    }

    private void ClearConstructionSelection()
    {
        if (_constructionSelection.Count == 0 && _selectedMullion is null && SelectedElement is null)
        {
            return;
        }

        _constructionSelection.Clear();
        _selectedMullion = null;
        SyncGeometryElementSelectedFlags();
        SelectedElement = null;
        OnPropertyChanged(nameof(ConstructionSelection));
        OnPropertyChanged(nameof(SelectedMullion));
        NotifyFrameSelectionProperties();
        StatusText = "Выделение снято";
    }

    private void SyncLegacySelectionFromConstruction()
    {
        OnPropertyChanged(nameof(ConstructionSelection));
        SyncGeometryElementSelectedFlags();

        if (_constructionSelection.Count == 0)
        {
            _selectedMullion = null;
            SelectedElement = null;
            OnPropertyChanged(nameof(SelectedMullion));
            NotifyFrameSelectionProperties();
            StatusText = "Выделение снято";
            return;
        }

        var primary = _constructionSelection.Count == 1
            ? _constructionSelection.First()
            : _constructionSelection.FirstOrDefault(item =>
                item.Kind is CadSelectableKind.Sash
                    or CadSelectableKind.Glazing
                    or CadSelectableKind.Bead
                    or CadSelectableKind.Frame
                    or CadSelectableKind.Impost
                    or CadSelectableKind.SashHandle
                    or CadSelectableKind.SashHinge);

        if (primary != null)
        {
            ApplyPrimaryConstructionSelection(primary);
        }

        StatusText = _constructionSelection.Count > 1
            ? $"Выбрано элементов: {_constructionSelection.Count}"
            : "Элемент выбран";
    }

    private void ApplyPrimaryConstructionSelection(CadSelectableItem item)
    {
        switch (item.Kind)
        {
            case CadSelectableKind.Frame:
                if (FrameRoot is not null)
                {
                    ApplyFrameSelection(FrameRoot);
                }

                break;
            case CadSelectableKind.Impost when item.ImpostNode is not null:
                _selectedMullion = new VerticalImpostTarget
                {
                    SplitNode = item.ImpostNode,
                    UniformGapIndex = item.ImpostNode.IsUniformSplit ? item.ImpostGapIndex : null
                };
                if (FrameRoot is not null)
                {
                    SelectedElement = FrameRoot;
                }

                OnPropertyChanged(nameof(SelectedMullion));
                break;
            case CadSelectableKind.Sash or CadSelectableKind.Glazing or CadSelectableKind.Bead
                or CadSelectableKind.SashHandle or CadSelectableKind.SashHinge
                when item.ElementId is Guid elementId:
                SyncPanelFromSelectedGeometryElement(elementId);
                break;
        }
    }

    private void SyncPanelFromSelectedGeometryElement(Guid elementId)
    {
        var element = CanvasElements.FirstOrDefault(e => e.Id == elementId);
        if (element is null)
        {
            return;
        }

        if (FrameRoot is not null)
        {
            var leaf = FrameRoot.FindLeafAt((double)element.X, (double)element.Y);
            if (leaf is not null)
            {
                SelectedElement = leaf;
            }
        }

        if (ConstructionSelectionHelper.TryGetElementBounds(this, element, out var bounds))
        {
            _isSyncingSelection = true;
            try
            {
                CoordinateX = RoundMm(bounds.X);
                CoordinateY = RoundMm(bounds.Y - bounds.Height);
                Length = RoundMm(bounds.Width);
                Height = RoundMm(bounds.Height);
            }
            finally
            {
                _isSyncingSelection = false;
            }

            OnPropertyChanged(nameof(SelectedElementLength));
            OnPropertyChanged(nameof(SelectedElementHeight));
            OnPropertyChanged(nameof(SelectedElementX));
            OnPropertyChanged(nameof(SelectedElementY));
        }
    }

    private bool TryDeleteConstructionSelection()
    {
        if (_constructionSelection.Count == 0)
        {
            return false;
        }

        var items = _constructionSelection.ToList();
        var deletable = items.Where(CanDeleteConstructionItem).ToList();
        if (deletable.Count == 0)
        {
            StatusText = "Выбранные элементы нельзя удалить";
            return false;
        }

        var removed = RecordPlacement(
            deletable.Count == 1 ? "Элемент удалён" : "Элементы удалены",
            () =>
            {
                foreach (var item in deletable)
                {
                    if (!TryDeleteConstructionItem(item))
                    {
                        return false;
                    }
                }

                return true;
            });

        if (!removed)
        {
            StatusText = "Не удалось удалить выбранные элементы";
            return false;
        }

        ClearConstructionSelection();
        SyncFillingsToFrameLayout();
        RefreshAfterPlacementHistoryChange();
        RequestSceneRefresh();
        StatusText = deletable.Count == 1 ? "Элемент удалён" : $"Удалено элементов: {deletable.Count}";
        return true;
    }

    private static bool CanDeleteConstructionItem(CadSelectableItem item) =>
        item.Kind is CadSelectableKind.Sash
            or CadSelectableKind.Glazing
            or CadSelectableKind.Bead
            or CadSelectableKind.Impost
            or CadSelectableKind.SashHandle
            or CadSelectableKind.SashHinge;

    private bool TryDeleteConstructionItem(CadSelectableItem item)
    {
        switch (item.Kind)
        {
            case CadSelectableKind.Sash when item.ElementId is Guid sashId:
                RemoveHostedGlazingForSash(sashId);
                return RemoveCanvasElementById(sashId);
            case CadSelectableKind.Glazing or CadSelectableKind.Bead when item.ElementId is Guid elementId:
                return RemoveCanvasElementById(elementId);
            case CadSelectableKind.Impost when item.ImpostNode is { IsUniformSplit: false } impostNode:
                return impostNode.TryRemoveImpostSplit();
            case CadSelectableKind.SashHandle when item.ElementId is Guid handleSashId:
                return TryClearSashHandle(handleSashId);
            case CadSelectableKind.SashHinge when item.ElementId is Guid hingeSashId:
                return TryClearSashHardware(hingeSashId);
            default:
                return false;
        }
    }

    private bool TryClearSashHandle(Guid sashId)
    {
        var sash = CanvasElements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
        if (sash is null || !sash.HasHandle)
        {
            return false;
        }

        sash.HasHandle = false;
        return true;
    }

    private bool TryClearSashHardware(Guid sashId)
    {
        var sash = CanvasElements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
        if (sash is null || !sash.HasHardware)
        {
            return false;
        }

        sash.HasHardware = false;
        return true;
    }

    private bool RemoveCanvasElementById(Guid elementId)
    {
        for (var i = CanvasElements.Count - 1; i >= 0; i--)
        {
            if (CanvasElements[i].Id == elementId)
            {
                CanvasElements.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    private void RemoveHostedGlazingForSash(Guid sashId)
    {
        for (var i = CanvasElements.Count - 1; i >= 0; i--)
        {
            var element = CanvasElements[i];
            if (element.Kind == GeometryElementKind.Glazing && element.HostSashId == sashId)
            {
                CanvasElements.RemoveAt(i);
            }
        }
    }

    private void SyncGeometryElementSelectedFlags()
    {
        foreach (var element in CanvasElements)
        {
            element.IsSelected = IsGeometryElementSelected(element.Id);
        }
    }

    private bool TryDeleteSelectionFromTool()
    {
        if (_constructionSelection.Count > 0 && HasCommittedConstruction)
        {
            return TryDeleteConstructionSelection();
        }

        if (SelectedRectangle != null)
        {
            var removed = SelectedRectangle.Value;
            RecordPlacement("Объект удалён", () =>
            {
                Document.RemoveRectangle(removed);
                SelectedRectangle = null;
            });
            RefreshSelection();
            RequestSceneRefresh();
            StatusText = "Выбранный объект удалён";
            return true;
        }

        if (SelectedElement is FrameNode)
        {
            RecordPlacement("Конструкция удалена", () => ClearCommittedFrameGeometry(clearPlacementHistory: false));
            RefreshAfterPlacementHistoryChange();
            RequestSceneRefresh();
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
        OnPropertyChanged(nameof(ShowStatusBarDimensions));
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
        OnPropertyChanged(nameof(ShowStatusBarDimensions));
        OnPropertyChanged(nameof(StatusBarLength));
        OnPropertyChanged(nameof(StatusBarHeight));
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
        OnPropertyChanged(nameof(ShowStatusBarDimensions));
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

    private void ClearCommittedFrameGeometry(bool clearPlacementHistory = true)
    {
        Document.ClearRectangles();
        CanvasElements.Clear();
        FrameRoot = null;
        SelectedElement = null;
        SelectedRectangle = null;
        _constructionSelection.Clear();
        _selectedMullion = null;
        SyncGeometryElementSelectedFlags();
        RealFramePreview = null;
        SashDragPreview = null;
        HandlePreviewSashId = null;
        _impostSplitHistory.Clear();
        if (clearPlacementHistory)
        {
            _placementHistory.Clear();
            NotifyPlacementHistoryChanged();
        }

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
        OnPropertyChanged(nameof(ConstructionSelection));
        OnPropertyChanged(nameof(SelectedMullion));
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
        OnPropertyChanged(nameof(ConstructionSelection));
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
            OnPropertyChanged(nameof(ShowStatusBarDimensions));
            OnPropertyChanged(nameof(StatusBarLength));
            OnPropertyChanged(nameof(StatusBarHeight));

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
        OnPropertyChanged(nameof(IsTopProfileHovered));
        OnPropertyChanged(nameof(HoveredTopProfile));
        OnPropertyChanged(nameof(ShowArcVertexHandles));
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
        if (ShouldBlockImpostPanelSideEffects() || CurrentToolMode != WindowToolMode.OffsetImpostHorizontal)
        {
            return;
        }

        ApplyImpostPanelAxisY(value);
    }

    partial void OnImpostPanelLChanged(double value)
    {
        if (ShouldBlockImpostPanelSideEffects())
        {
            return;
        }

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
        if (ShouldBlockImpostPanelSideEffects())
        {
            return;
        }

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

    [RelayCommand(CanExecute = nameof(CanRedo))]
    private void Redo()
    {
        if (_placementHistory.TryRedo(ApplyConstructionMemento, out var description))
        {
            RefreshAfterPlacementHistoryChange();
            StatusText = description ?? "Действие возвращено";
            RequestSceneRefresh();
            return;
        }

        if (!Commands.CanRedo)
        {
            StatusText = "Нечего повторять";
            return;
        }

        Commands.Redo();
        RefreshSelection();
        RequestSceneRefresh();
        StatusText = "Действие возвращено";
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
        _constructionSelection.Clear();
        _selectedMullion = null;
        SyncGeometryElementSelectedFlags();

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
        CadToolbarLayout.Populate(CreationDesignTools, EditingDesignTools, ActivateDesignToolCommand);
    }

    private async Task ActivateDesignToolAsync(string? toolKey)
    {
        if (string.IsNullOrWhiteSpace(toolKey))
        {
            return;
        }

        var tool = AllDesignTools.FirstOrDefault(t => t.ToolKey == toolKey && t.IsTool);
        if (tool is null || !tool.IsImplemented)
        {
            return;
        }

        if (!tool.IsInstantCommand)
        {
            UpdateDesignToolSelection(tool.Mode, toolKey);
        }

        if (tool.IsInstantCommand)
        {
            await ExecuteToolbarInstantCommandAsync(toolKey);
            return;
        }

        await SelectToolModeAsync(tool.Mode, toolKey);
    }

    private async Task ExecuteToolbarInstantCommandAsync(string toolKey)
    {
        switch (toolKey)
        {
            case "set-arch-height":
                await SetArchHeight();
                break;
            case "zoom-fit":
                ZoomToFit();
                break;
            case "delete-selection":
                DeleteSelected();
                break;
            case "delete-last":
                UndoLastElement();
                break;
            case "clear-construction":
                ClearAll();
                break;
            case "undo":
                Undo();
                break;
            case "change-dimensions":
                ActivateCadTool(CadToolMetadata.GetId(CadToolKind.ChangeOverallDimensions));
                break;
            case "split-profile":
                ActivateCadTool(CadToolMetadata.GetId(CadToolKind.SplitProfile));
                break;
            case "set-vertex-coordinates":
                ActivateCadTool(CadToolMetadata.GetId(CadToolKind.SetVertexCoordinates));
                break;
            case "align-imposts":
                ActivateCadTool(CadToolMetadata.GetId(CadToolKind.AlignImposts));
                break;
            case "align-glazing":
                ActivateCadTool(CadToolMetadata.GetId(CadToolKind.AlignGlazingUnits));
                break;
            case "align-dead-lights":
                ActivateCadTool(CadToolMetadata.GetId(CadToolKind.AlignDeadLights));
                break;
        }
    }

    private void ResetToSelectionTool()
    {
        UpdateDesignToolSelection(WindowToolMode.SelectElement, "select-elements");
        var fallback = _toolFactory.Create(CadToolMetadata.GetId(CadToolKind.SelectElements));
        ActiveTool?.OnDeactivated(CreateToolContext());
        ActiveTool = fallback;
        ActiveToolId = fallback.Id;
        fallback.OnActivated(CreateToolContext());
    }

    [RelayCommand]
    private Task OpenStandardStructuresAsync()
    {
        if (OpenStandardStructuresHandler is not null)
        {
            return OpenStandardStructuresHandler();
        }

        StatusText = "Справочник типовых конструкций недоступен";
        return Task.CompletedTask;
    }

    private void SetActiveToolVisual(WindowToolMode mode, string? toolKey = null)
    {
        UpdateDesignToolSelection(mode, toolKey ?? _activeToolKey);
        var previous = ActiveTool;
        var context = CreateToolContext();
        previous?.OnDeactivated(context);
        ActiveTool = _toolFactory.Create(CadToolMetadata.GetId(CadToolKind.SelectElements));
        ActiveToolId = CadToolMetadata.GetId(CadToolKind.SelectElements);
        ActiveTool.OnActivated(context);
        OnPropertyChanged(nameof(ActiveToolDisplayName));
        StatusText = FindDesignTool(mode, toolKey)?.Title ?? "Инструмент";
    }

    private void UpdateDesignToolSelection(WindowToolMode mode, string? toolKey = null)
    {
        CurrentToolMode = mode;
        if (!string.IsNullOrWhiteSpace(toolKey))
        {
            _activeToolKey = toolKey;
        }

        foreach (var tool in AllDesignTools)
        {
            tool.IsActive = tool.IsTool && tool.ToolKey == _activeToolKey;
        }

        var active = FindDesignTool(mode, _activeToolKey);
        ActiveDesignToolLabel = active?.Title;
        ActiveDesignToolId = _activeToolKey;
        OnPropertyChanged(nameof(CanvasHintText));
    }

    private CadDesignToolButtonViewModel? FindDesignTool(WindowToolMode mode, string? toolKey = null)
    {
        if (!string.IsNullOrWhiteSpace(toolKey))
        {
            var byKey = AllDesignTools.FirstOrDefault(t => t.ToolKey == toolKey && t.IsTool);
            if (byKey is not null)
            {
                return byKey;
            }
        }

        return AllDesignTools.FirstOrDefault(t => t.IsTool && t.Mode == mode);
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
            WindowToolMode.RadiusVertex => CadToolMetadata.GetId(CadToolKind.RadiusVertex),
            WindowToolMode.ClearConstruction => CadToolMetadata.GetId(CadToolKind.ClearConstruction),
            WindowToolMode.Other => string.Empty,
            _ => string.Empty
        };

        return toolId.Length > 0;
    }

    private async Task SelectToolModeAsync(WindowToolMode mode, string? toolKey = null)
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
            UpdateDesignToolSelection(mode, toolKey);
            var previous = ActiveTool;
            var context = CreateToolContext();
            previous?.OnDeactivated(context);
            ActiveTool = _toolFactory.Create(CadToolMetadata.GetId(CadToolKind.SelectElements));
            ActiveToolId = CadToolMetadata.GetId(CadToolKind.SelectElements);
            ActiveTool.OnActivated(context);
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
            UpdateDesignToolSelection(mode, toolKey);
            ActivateCadTool(cadToolId);
            StatusText = FindDesignTool(mode, toolKey)?.Title ?? "Инструмент";
            await Task.CompletedTask;
            return;
        }

        if (mode is WindowToolMode.Other)
        {
            return;
        }

        SetActiveToolVisual(mode, toolKey);

        if (IsFramePlacementMode(mode))
        {
            var active = FindDesignTool(mode, toolKey);
            StatusText = IsDragCreateShapeMode(mode)
                ? $"{active?.Title}: ЛКМ — натянуть, ПКМ — ввод размеров"
                : $"{active?.Title}: кликните по полю чертежа";
        }
        else if (IsImpostSplitMode(mode))
        {
            var active = FindDesignTool(mode, toolKey);
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
                _ => FindDesignTool(mode, toolKey)?.Title is { Length: > 0 } title
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
            var active = FindDesignTool(mode, toolKey);
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
        var active = FindDesignTool(mode);
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
            or WindowToolMode.SlantedImpost
            or WindowToolMode.MullionVertical or WindowToolMode.MullionHorizontal;

    private static bool IsFakeImpostMode(WindowToolMode mode) =>
        mode is WindowToolMode.FakeVerticalImpost or WindowToolMode.FakeHorizontalImpost;

    private static bool IsVerticalImpostMode(WindowToolMode mode) =>
        mode is WindowToolMode.FakeVerticalImpost or WindowToolMode.RealVerticalImpost
            or WindowToolMode.DoubleVerticalImpost or WindowToolMode.SlantedImpost
            or WindowToolMode.MullionVertical;

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

        var active = FindDesignTool(mode);
        StatusText = active?.Title ?? "Импост";

        OnPropertyChanged(nameof(FrameRoot));
        OnPropertyChanged(nameof(SelectedElement));
        OnPropertyChanged(nameof(SelectedElementLength));
        OnPropertyChanged(nameof(SelectedElementHeight));
        SyncFillingsToFrameLayout();
        RequestSceneRefresh();
    }

    private static ImpostLineStyle GetImpostLineStyle(WindowToolMode mode) => mode switch
    {
        WindowToolMode.RealVerticalImpost or WindowToolMode.RealHorizontalImpost
            or WindowToolMode.MullionVertical or WindowToolMode.MullionHorizontal => ImpostLineStyle.Solid,
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
        BeginConstructionDrag();
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
            CommitConstructionDrag("Импост перемещён");
        }
        else
        {
            CancelConstructionDrag();
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
        BeginConstructionDrag();
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
        TraceHorizontalImpost("drag-update", ghostAxisY: axisY, moveInputAxisY: axisY);
        TryMoveSelectedHorizontalImpost(axisY);
        TraceHorizontalImpost("drag-after-move", ghostAxisY: axisY);
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

        _isCommittingHorizontalImpostRelease = true;
        try
        {
            if (_selectedMullion is not null && _draggingMullionWorldY is double axisY)
            {
                if (TryResolveHorizontalImpostDragAxisRange(_selectedMullion, out var minY, out var maxY))
                {
                    axisY = RoundMm(Math.Clamp(axisY, minY, maxY));
                }

                TraceHorizontalImpost("release-before-move", ghostAxisY: axisY, moveInputAxisY: axisY);

                if (TryMoveSelectedHorizontalImpost(axisY))
                {
                    TraceHorizontalImpost("release-after-move", ghostAxisY: axisY);
                    _isSyncingImpostPanel = true;
                    try
                    {
                        SyncImpostPanelFromSelection();
                    }
                    finally
                    {
                        _isSyncingImpostPanel = false;
                    }

                    CommitConstructionDrag("Импост перемещён");
                }
                else
                {
                    TraceHorizontalImpost("release-move-failed", ghostAxisY: axisY, moveInputAxisY: axisY);
                    CancelConstructionDrag();
                }
            }
            else
            {
                CancelConstructionDrag();
            }
        }
        finally
        {
            _isHorizontalImpostDragging = false;
            _draggingMullionWorldY = null;
            OnPropertyChanged(nameof(DraggingMullionWorldY));
            OnPropertyChanged(nameof(IsHorizontalImpostDragging));

            Dispatcher.UIThread.Post(
                () =>
                {
                    if (_selectedMullion is not null)
                    {
                        _isSyncingImpostPanel = true;
                        try
                        {
                            SyncImpostPanelFromSelection();
                        }
                        finally
                        {
                            _isSyncingImpostPanel = false;
                        }
                    }

                    _isCommittingHorizontalImpostRelease = false;
                    TraceHorizontalImpost(
                        "release-deferred-check",
                        modelAxisY: HorizontalImpostDragDiagnostics.TryReadModelAxisY(
                    _selectedMullion?.SplitNode,
                    _selectedMullion?.UniformGapIndex),
                        renderAxisY: HorizontalImpostDragDiagnostics.TryReadRenderAxisY(
                            _selectedMullion?.SplitNode,
                            _selectedMullion?.UniformGapIndex));
                    RequestSceneRefresh();
                },
                DispatcherPriority.Background);
        }

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

        minAxisY = Math.Max(nodeMin, innerMin);
        maxAxisY = HorizontalImpostGeometry.ResolveMaxCenterAxisY(this, node, half, ImpostDragInnerInsetMm);

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
            var topEdge = gapIndex >= node.UniformCells.Count - 2
                ? node.Y + FrameNode.ResolveHorizontalSplitExtentLocal(node, node.GetRoot())
                : node.UniformCells[gapIndex + 1].Y + node.UniformCells[gapIndex + 1].Height;
            ImpostPanelL = RoundMm(axisWorldY - bottomCell.Y - half);
            ImpostPanelH = RoundMm(topEdge - axisWorldY - half);
        }
        else
        {
            var extentTop = node.Y + FrameNode.ResolveHorizontalSplitExtentLocal(node, node.GetRoot());
            ImpostPanelL = RoundMm((axisWorldY - node.Y) - half);
            ImpostPanelH = RoundMm((extentTop - axisWorldY) - half);
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

    private bool ShouldBlockImpostPanelSideEffects() =>
        _isSyncingImpostPanel || _isHorizontalImpostDragging || _isCommittingHorizontalImpostRelease;

    private void TraceHorizontalImpost(
        string stage,
        double? ghostAxisY = null,
        double? moveInputAxisY = null,
        double? modelAxisY = null,
        double? renderAxisY = null,
        bool? moveSucceeded = null,
        string? detail = null)
    {
        if (CurrentToolMode != WindowToolMode.OffsetImpostHorizontal &&
            stage is not ("commit-before-capture" or "commit-after-record" or "commit-after-refresh" or "release-deferred-check" or "memento-apply"))
        {
            return;
        }

        var splitNode = _selectedMullion?.SplitNode;
        var uniformGapIndex = _selectedMullion?.UniformGapIndex;
        HorizontalImpostDragDiagnostics.Trace(
            stage,
            ghostAxisY ?? _draggingMullionWorldY,
            moveInputAxisY,
            modelAxisY ?? HorizontalImpostDragDiagnostics.TryReadModelAxisY(splitNode, uniformGapIndex),
            renderAxisY ?? HorizontalImpostDragDiagnostics.TryReadRenderAxisY(splitNode, uniformGapIndex),
            splitNode,
            FrameRoot,
            moveSucceeded,
            detail);
    }

    private bool IsSplitNodeInFrameTree(FrameNode splitNode)
    {
        if (FrameRoot is null)
        {
            return false;
        }

        if (ReferenceEquals(splitNode, FrameRoot))
        {
            return true;
        }

        return FrameRoot.EnumerateInternalNodes().Any(node => ReferenceEquals(node, splitNode));
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
        if (ShouldBlockImpostPanelSideEffects() || _selectedMullion is null)
        {
            return;
        }

        TraceHorizontalImpost("panel-apply-axis-y", moveInputAxisY: axisY);

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
        if (ShouldBlockImpostPanelSideEffects() || _selectedMullion is null)
        {
            return;
        }

        TraceHorizontalImpost(
            "panel-apply-bottom",
            ghostAxisY: _draggingMullionWorldY,
            detail: $"bottomHeight={bottomHeight:F1}");

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
        if (ShouldBlockImpostPanelSideEffects() || _selectedMullion is null)
        {
            return;
        }

        TraceHorizontalImpost(
            "panel-apply-top",
            ghostAxisY: _draggingMullionWorldY,
            detail: $"topHeight={topHeight:F1}");

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
        _isCommittingHorizontalImpostRelease = false;
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

                    if (!IsPointNearHorizontalImpost(node, gapIndex, axisY, worldPoint, tolerance, out var distance))
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

            if (!IsPointNearHorizontalImpost(node, 0, binaryAxisY, worldPoint, tolerance, out var binaryDistance))
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

    private bool IsPointNearHorizontalImpost(
        FrameNode node,
        int gapIndex,
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

        return ImpostProfileHitTest.ContainsPoint(this, node, gapIndex, point);
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
        if (gapIndex >= splitNode.UniformCells.Count - 2
            && splitNode.TryGetUniformImpostAxisWorld(SplitOrientation.Horizontal, gapIndex, out var axisY))
        {
            var extentTop = splitNode.Y + FrameNode.ResolveHorizontalSplitExtentLocal(splitNode, splitNode.GetRoot());
            var half = splitNode.GetResolvedImpostThicknessMm() / 2.0;
            topHeight = extentTop - axisY - half;
        }
        else
        {
            topHeight = splitNode.UniformCells[gapIndex + 1].Height;
        }
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

    private GeometryElement? FindSashInLeaf(FrameNode leaf)
    {
        GeometryElement? best = null;
        var bestOverlap = 0.0;

        foreach (var element in CanvasElements)
        {
            if (element.Kind != GeometryElementKind.Sash)
            {
                continue;
            }

            if (!SashGeometry.TryMeasureLeafSashOverlap(this, leaf, element, out var overlap))
            {
                continue;
            }

            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
                best = element;
            }
        }

        return best;
    }

    private GeometryElement? FindGlazingInLeaf(FrameNode leaf)
    {
        GeometryElement? best = null;
        var bestOverlap = 0.0;

        foreach (var element in CanvasElements)
        {
            if (element.Kind != GeometryElementKind.Glazing)
            {
                continue;
            }

            if (element.HostSashId is Guid sashId)
            {
                if (FindSashInLeaf(leaf)?.Id == sashId)
                {
                    return element;
                }

                continue;
            }

            if (!SashGeometry.TryMeasureLeafGlazingOverlap(this, leaf, element, out var overlap))
            {
                continue;
            }

            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
                best = element;
            }
        }

        return best;
    }

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

        if (SashGeometry.TryResolveFixedFrameGlassOpeningBounds(this, leaf, out var glass))
        {
            if (glass.Width < 20 || glass.Height < 20)
            {
                return null;
            }

            return new GeometryElement
            {
                Kind = GeometryElementKind.Glazing,
                Label = "Остекление",
                X = (decimal)((glass.Left + glass.Right) / 2.0),
                Y = (decimal)((glass.Bottom + glass.Top) / 2.0),
                WidthMm = (decimal)glass.Width,
                HeightMm = (decimal)glass.Height,
                SourceToolId = "glazing"
            };
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
        SashGeometry.TryResolveGlassOpeningBounds(
            this,
            inner,
            outer,
            SashGeometry.SashDrawProfileThicknessMm,
            out var glass);
        var width = glass.Width;
        var height = glass.Height;
        if (width < 20 || height < 20)
        {
            return null;
        }

        return new GeometryElement
        {
            Kind = GeometryElementKind.Glazing,
            Label = "Остекление",
            HostSashId = sash.Id,
            X = (decimal)((glass.Left + glass.Right) / 2.0),
            Y = (decimal)((glass.Bottom + glass.Top) / 2.0),
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

        public ProfileSegmentSnapshot? TopProfile { get; init; }
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

            AttachTopProfileWatcher();
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
        ChildB = node.ChildB is null ? null : CaptureFrameNode(node.ChildB),
        TopProfile = node.TopProfile?.CaptureSnapshot()
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
        ChildB = snapshot.ChildB is null ? null : ToTreeSnapshot(snapshot.ChildB),
        TopProfile = snapshot.TopProfile
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

    public void UpdateRadiusVertexHover(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.RadiusVertex)
        {
            if (_isTopProfileHovered)
            {
                _isTopProfileHovered = false;
                OnPropertyChanged(nameof(IsTopProfileHovered));
                OnPropertyChanged(nameof(HoveredTopProfile));
            }

            return;
        }

        var hit = HitTestTopProfileSegment(worldPoint);
        if (hit == _isTopProfileHovered)
        {
            return;
        }

        _isTopProfileHovered = hit;
        OnPropertyChanged(nameof(IsTopProfileHovered));
        OnPropertyChanged(nameof(HoveredTopProfile));
    }

    public bool TryPressRadiusVertex(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.RadiusVertex || FrameRoot is null || !HasRealFrameOnCanvas)
        {
            return false;
        }

        if (TryPressArcVertexDrag(worldPoint))
        {
            return true;
        }

        if (!HitTestTopProfileSegment(worldPoint))
        {
            StatusText = "Кликните по верхнему профилю коробки";
            return false;
        }

        FrameRoot.EnsureTopProfile();
        AttachTopProfileWatcher();
        var segment = FrameRoot.TopProfile;
        if (segment is null || segment.SegmentType != SegmentType.Line)
        {
            StatusText = segment?.SegmentType == SegmentType.Arc
                ? "Перетащите маркер вершины или измените R / H"
                : "Не удалось определить сегмент профиля";
            return false;
        }

        if (!RecordPlacement("Преобразование в арку", TryConvertTopProfileToArc))
        {
            return false;
        }

        segment = FrameRoot.TopProfile!;
        StatusText = $"Верхний профиль преобразован в дугу: R={segment.Radius:F0} мм, H={segment.ArcHeight:F0} мм";
        RequestSceneRefresh();
        return true;
    }

    private bool TryConvertTopProfileToArc()
    {
        if (FrameRoot is null)
        {
            return false;
        }

        FrameRoot.EnsureTopProfile();
        var segment = FrameRoot.TopProfile;
        if (segment is null || segment.SegmentType != SegmentType.Line)
        {
            return false;
        }

        segment.ConvertToArc();
        NotifyTopProfileUiProperties();
        return true;
    }

    public bool TryPressArcVertexDrag(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.RadiusVertex || !HasArcTopProfile || FrameRoot is null)
        {
            return false;
        }

        FrameRoot.EnsureTopProfile();
        var profile = FrameRoot.TopProfile;
        if (profile is null || profile.SegmentType != SegmentType.Arc)
        {
            return false;
        }

        AttachTopProfileWatcher();
        var tolerance = Math.Max(ArcVertexHandleScreenPx * MillimetersPerPixel, 8.0);
        var handlePoint = profile.GetArcSpringingHandleWorld();

        if (Distance(worldPoint, handlePoint) <= tolerance)
        {
            BeginConstructionDrag();
            _isArcVertexDragging = true;
            _arcVertexDragTarget = ArcVertexDragTarget.Springing;
            _arcHeightDragStartWorldY = worldPoint.Y;
            _arcHeightDragStartHeight = profile.ArcHeight;
            OnPropertyChanged(nameof(IsArcVertexDragging));
            StatusText = "Перетаскивание стрелы прогиба (H) с линии начала арки";
            return true;
        }

        return false;
    }

    public void UpdateArcVertexDrag(CadPoint worldPoint)
    {
        if (!_isArcVertexDragging || FrameRoot?.TopProfile is not { SegmentType: SegmentType.Arc } profile)
        {
            return;
        }

        _isSyncingTopProfileUi = true;
        try
        {
            if (_arcVertexDragTarget != ArcVertexDragTarget.Springing)
            {
                return;
            }

            var deltaY = worldPoint.Y - _arcHeightDragStartWorldY;
            var newHeight = profile.ClampArcHeight(_arcHeightDragStartHeight - deltaY);
            profile.ArcHeight = newHeight;
            StatusText = $"H={profile.ArcHeight:F0} мм, R={profile.Radius:F0} мм";
        }
        finally
        {
            _isSyncingTopProfileUi = false;
        }

        NotifyTopProfileUiProperties();
        RequestSceneRefresh();
    }

    public void ReleaseArcVertexDrag()
    {
        if (!_isArcVertexDragging)
        {
            return;
        }

        CommitConstructionDrag("Изменение стрелы прогиба");

        _isArcVertexDragging = false;
        _arcVertexDragTarget = ArcVertexDragTarget.None;
        OnPropertyChanged(nameof(IsArcVertexDragging));
    }

    private void AttachTopProfileWatcher()
    {
        if (_watchedTopProfile is not null)
        {
            _watchedTopProfile.PropertyChanged -= OnTopProfilePropertyChanged;
            _watchedTopProfile = null;
        }

        _watchedTopProfile = FrameRoot?.TopProfile;
        if (_watchedTopProfile is not null)
        {
            _watchedTopProfile.PropertyChanged += OnTopProfilePropertyChanged;
        }

        NotifyTopProfileUiProperties();
    }

    private void OnTopProfilePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncingTopProfileUi)
        {
            return;
        }

        if (e.PropertyName is nameof(ProfileSegment.Radius)
            or nameof(ProfileSegment.ArcHeight)
            or nameof(ProfileSegment.SegmentType))
        {
            NotifyTopProfileUiProperties();
            RequestSceneRefresh();
        }
    }

    private void NotifyTopProfileUiProperties()
    {
        OnPropertyChanged(nameof(HasArcTopProfile));
        OnPropertyChanged(nameof(ShowArcVertexHandles));
        OnPropertyChanged(nameof(TopProfileArcRadius));
        OnPropertyChanged(nameof(TopProfileArcHeight));
    }

    private bool TryApplyTopProfileRadius(double radiusMm)
    {
        if (FrameRoot is null)
        {
            return false;
        }

        FrameRoot.EnsureTopProfile();
        var profile = FrameRoot.TopProfile;
        if (profile is null || profile.SegmentType != SegmentType.Arc)
        {
            return false;
        }

        _isSyncingTopProfileUi = true;
        try
        {
            profile.Radius = radiusMm;
        }
        finally
        {
            _isSyncingTopProfileUi = false;
        }

        return true;
    }

    private bool TryApplyTopProfileArcHeight(double arcHeightMm)
    {
        if (FrameRoot is null)
        {
            return false;
        }

        FrameRoot.EnsureTopProfile();
        var profile = FrameRoot.TopProfile;
        if (profile is null || profile.SegmentType != SegmentType.Arc)
        {
            return false;
        }

        _isSyncingTopProfileUi = true;
        try
        {
            if (arcHeightMm <= ProfileSegment.MinFlatArcHeightMm)
            {
                profile.ConvertToLine();
            }
            else
            {
                profile.ArcHeight = arcHeightMm;
            }
        }
        finally
        {
            _isSyncingTopProfileUi = false;
        }

        return true;
    }

    private bool HitTestTopProfileSegment(CadPoint worldPoint)
    {
        if (FrameRoot is null || !HasRealFrameOnCanvas)
        {
            return false;
        }

        FrameRoot.EnsureTopProfile();
        var segment = FrameRoot.TopProfile;
        if (segment is null || segment.ChordWidth <= CadMath.Epsilon)
        {
            return false;
        }

        var tolerance = Math.Max(TopProfileHitScreenPx * MillimetersPerPixel, 5.0);
        var distance = DistancePointToSegment(worldPoint, segment.Start, segment.End);
        var minX = Math.Min(segment.StartX, segment.EndX) - tolerance;
        var maxX = Math.Max(segment.StartX, segment.EndX) + tolerance;
        return distance <= tolerance &&
               worldPoint.X >= minX &&
               worldPoint.X <= maxX;
    }

    private static double Distance(CadPoint a, CadPoint b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double DistancePointToSegment(CadPoint point, CadPoint start, CadPoint end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var lengthSquared = dx * dx + dy * dy;
        if (lengthSquared <= CadMath.Epsilon)
        {
            return Math.Sqrt(Math.Pow(point.X - start.X, 2) + Math.Pow(point.Y - start.Y, 2));
        }

        var t = Math.Clamp(
            ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / lengthSquared,
            0.0,
            1.0);
        var projectionX = start.X + t * dx;
        var projectionY = start.Y + t * dy;
        return Math.Sqrt(Math.Pow(point.X - projectionX, 2) + Math.Pow(point.Y - projectionY, 2));
    }
}

internal enum ArcVertexDragTarget
{
    None,
    Springing
}
