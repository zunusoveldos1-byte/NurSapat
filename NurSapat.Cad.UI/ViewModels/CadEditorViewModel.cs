using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia;
using Avalonia.Threading;
using NurSapat.Cad.Core.Commands;
using NurSapat.Cad.Core.Document;
using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Core.Models.Grid;
using NurSapat.Cad.Core.Snapping;
using NurSapat.Cad.Core.Viewport;
using NurSapat.Cad.Tools;
using NurSapat.Cad.UI.Diagnostics;
using NurSapat.Cad.UI.Helpers;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.Services;
using NurSapat.Core.Calculations;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel : ObservableObject
{
    private readonly CadToolFactory _toolFactory = new();
    private readonly ICadDialogService _dialogService;
    private readonly CadTemplateStore _templateStore;
    private bool _isApplyingDrawingTemplate;
    private bool _isSyncingSelection;
    private bool _isUpdatingSelection;
    private bool _isRunningFramePipeline;
    private CadPoint? _interactionStartWorld;
    private FrameNode? _interactionLeaf;
    private bool _isCanvasDragging;
    /// <summary>
    /// Только жест перетаскивания (hint для Resolve). Не источник истины для петель —
    /// сторона всегда из <see cref="SashOpeningState.Resolve"/>.
    /// </summary>
    private SashHingeSide _sashDragGestureHingeHint = SashHingeSide.Right;

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

    private ImpostTarget? _selectedMullion;
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
        if (mode == WindowToolMode.FakeFrameCircle)
        {
            height = width;
        }

        return (width, height);
    }

    private static double DefaultHeightForMode(WindowToolMode mode) => mode switch
    {
        WindowToolMode.FakeFrameCircle => DefaultRealFrameWidth,
        _ => DefaultRealFrameHeight
    };

    private static readonly PlacementAnchorOption[] PlacementAnchorOptionList =
    [
        new() { Anchor = PlacementAnchor.BottomLeft, Label = "Левый нижний" },
        new() { Anchor = PlacementAnchor.TopLeft, Label = "Левый верхний" },
        new() { Anchor = PlacementAnchor.BottomRight, Label = "Правый нижний" },
        new() { Anchor = PlacementAnchor.TopRight, Label = "Правый верхний" }
    ];

    public IReadOnlyList<PlacementAnchorOption> PlacementAnchorOptions => PlacementAnchorOptionList;

    private readonly NewConstructionOptions _newConstructionOptions = new();

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

        var width = _newConstructionOptions.Width;
        var height = _newConstructionOptions.Height;
        var (originX, originY) = ResolvePlacementOrigin(width, height);

        _isSyncingSelection = true;
        CoordinateX = originX;
        CoordinateY = originY;
        _isSyncingSelection = false;

        OnPropertyChanged(nameof(SelectedElementX));
        OnPropertyChanged(nameof(SelectedElementY));
    }

    private void ApplyNewConstructionOptionsToPanel()
    {
        if (!IsDragCreateShapeMode(CurrentToolMode))
        {
            return;
        }

        _isSyncingSelection = true;
        Length = _newConstructionOptions.Width;
        Height = _newConstructionOptions.Height;
        CoordinateX = _newConstructionOptions.AnchorX;
        CoordinateY = _newConstructionOptions.AnchorY;
        PlacementAnchor = _newConstructionOptions.Anchor;
        _isSyncingSelection = false;

        NotifyFrameSelectionProperties();
    }

    private void CaptureNewConstructionOptionsFromPanel()
    {
        _newConstructionOptions.Width = Length > CadMath.Epsilon
            ? Length
            : NewConstructionOptions.DefaultWidth;
        _newConstructionOptions.Height = Height > CadMath.Epsilon
            ? Height
            : NewConstructionOptions.DefaultHeight;
        _newConstructionOptions.AnchorX = CoordinateX;
        _newConstructionOptions.AnchorY = CoordinateY;
        _newConstructionOptions.Anchor = PlacementAnchor;
    }

    private void CaptureNewConstructionOptionsFromCommit(
        double width,
        double height,
        double originX,
        double originY,
        PlacementAnchor anchor)
    {
        _newConstructionOptions.Width = width;
        _newConstructionOptions.Height = height;
        _newConstructionOptions.Anchor = anchor;
        var (anchorX, anchorY) = PlacementAnchorGeometry.ResolveAnchorPoint(anchor, originX, originY, width, height);
        _newConstructionOptions.AnchorX = anchorX;
        _newConstructionOptions.AnchorY = anchorY;
    }

    private void ApplyNewConstructionSizeInputResult(NewConstructionSizeInputResult result)
    {
        _newConstructionOptions.Width = result.Width;
        _newConstructionOptions.Height = result.Height;
        _newConstructionOptions.AnchorX = result.AnchorX;
        _newConstructionOptions.AnchorY = result.AnchorY;
        _newConstructionOptions.Anchor = result.Anchor;
    }

    public CadDocument Document { get; } = new();

    public CadViewport Viewport { get; } = new();

    public CadSnappingEngine Snapping { get; } = new();

    public CadCommandManager Commands { get; }

    public ObservableCollection<GeometryElement> CanvasElements { get; } = [];

    public ObservableCollection<CadDesignToolButtonViewModel> CreationDesignTools { get; } = [];

    public ObservableCollection<CadDesignToolButtonViewModel> EditingDesignTools { get; } = [];

    public ObservableCollection<CadDesignToolButtonViewModel> DecorationDesignTools { get; } = [];

    [ObservableProperty]
    private CadEditorWorkspaceMode _workspaceMode = CadEditorWorkspaceMode.Creation;

    public bool IsCreationWorkspaceActive => WorkspaceMode == CadEditorWorkspaceMode.Creation;

    public bool IsEditingWorkspaceActive => WorkspaceMode == CadEditorWorkspaceMode.Editing;

    public bool IsDecorationWorkspaceActive => WorkspaceMode == CadEditorWorkspaceMode.Decoration;

    /// <summary>ComboBox материалов активен только для инструментов создания с выбором материала.</summary>
    public bool IsConstructionMaterialComboEnabled =>
        WorkspaceMode == CadEditorWorkspaceMode.Creation
        && UsesPerToolMaterialCache(CurrentToolMode);

    private IEnumerable<CadDesignToolButtonViewModel> AllDesignTools =>
        CreationDesignTools.Concat(EditingDesignTools).Concat(DecorationDesignTools);

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

    public ObservableCollection<ProfileSeries> AvailableProfileSeries { get; } =
        new(ProfileSeriesCatalog.All);

    public ObservableCollection<ConstructionMaterialOption> FilteredConstructionMaterialOptions { get; } = [];

    public ObservableCollection<ConstructionItemViewModel> Constructions { get; } = [];

    public ObservableCollection<string> MaterialColors { get; } =
    [
        "Белый",
        "Коричневый",
        "Антрацит",
        "Золотой дуб",
        "Махагон",
        "Серый"
    ];

    private static readonly ContourColorOption[] ContourColorOptionList =
    [
        new() { Option = CadContourColorOption.Black, Label = "Черный" },
        new() { Option = CadContourColorOption.Gray, Label = "Серый" }
    ];

    /// <summary>Варианты глобального цвета контура чертежа (рама, импосты, створки, размеры и т.д.).</summary>
    public IReadOnlyList<ContourColorOption> ContourColorOptions => ContourColorOptionList;

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
    private ProfileSeries? _selectedProfileSeriesItem = ProfileSeriesCatalog.All.FirstOrDefault(s => s.Id == ProfileSeriesCatalog.Pvc70Id);

    private ConstructionMaterialOption? _selectedConstructionMaterialOption = ConstructionMaterialOption.None;
    private readonly Dictionary<WindowToolMode, Guid?> _constructionMaterialByTool = new();
    private WindowToolMode _materialCacheSourceTool = WindowToolMode.Other;
    private bool _pendingMaterialFilterRefresh;
    private bool _materialFilterRefreshQueued;
    private int _selectionUpdateGeneration;

    /// <summary>Пункт ComboBox «Материалы» — ручной сеттер защищён от асинхронных откатов Avalonia.</summary>
    public ConstructionMaterialOption? SelectedConstructionMaterialOption
    {
        get => _selectedConstructionMaterialOption;
        set
        {
            if (ReferenceEquals(_selectedConstructionMaterialOption, value))
            {
                return;
            }

            if (_isUpdatingSelection)
            {
                return;
            }

            if (_selectedConstructionMaterialOption?.MaterialId == value?.MaterialId)
            {
                return;
            }

            _selectedConstructionMaterialOption = value;
            OnPropertyChanged();
            if (UsesPerToolMaterialCache(CurrentToolMode))
            {
                _constructionMaterialByTool[CurrentToolMode] = value?.MaterialId;
            }

            RefreshConstructionVolume();
        }
    }

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
    private string? _selectedMaterialColor = "Белый";

    [ObservableProperty]
    private ContourColorOption _selectedContourColorOption =
        ContourColorOptionList.First(option => option.Option == CadContourColorOption.Gray);

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

    private GridLineCollection _gridLines = GridLineCollection.Empty;

    /// <summary>
    /// Абсолютные оси сетки, спроецированные из FrameRoot.
    /// Единый источник для размеров, HitTest и переходных профилей.
    /// </summary>
    public GridLineCollection GridLines => _gridLines;

    [ObservableProperty]
    private FrameNode? _selectedElement;

    [ObservableProperty]
    private SashDragPreview? _sashDragPreview;

    [ObservableProperty]
    private SashHardwareDragPreview? _sashHardwarePreview;

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

    public Func<string, Task<bool>>? ConfirmRemoveConstructionHandler { get; set; }

    public Func<Task>? OpenConstructionsLookupHandler { get; set; }

    public Func<Task>? OpenStandardStructuresHandler { get; set; }

    private bool _suppressConstructionSelectionCallback;
    private bool _isResettingCanvas;
    private int _canvasResetDepth;
    private Guid? _activeOrderConstructionId;

    private double _savedZoom;
    private double _savedPanX;
    private double _savedPanY;
    private bool _hasSavedDrawingViewport;
    private bool _restoreViewportOnNextLayout;
    private bool _pendingInitialViewportFit = true;
    private bool _hasAppliedInitialViewport;
    private bool _viewportLayoutScheduled;

    public const double DefaultViewportPaddingMm = 100;
    public const double DefaultViewportMarginPx = 80;

    public bool IsResettingCanvas => _isResettingCanvas || _canvasResetDepth > 0;

    public IRelayCommand ToggleLeftPanelCommand => new RelayCommand(() => IsLeftPanelVisible = !IsLeftPanelVisible);
    public IRelayCommand ToggleRightPanelCommand => new RelayCommand(() => IsRightPanelVisible = !IsRightPanelVisible);
    public IRelayCommand ToggleBottomPanelCommand => new RelayCommand(() => IsBottomPanelVisible = !IsBottomPanelVisible);
    public IRelayCommand ToggleTopPanelCommand => new RelayCommand(() => IsTopPanelVisible = !IsTopPanelVisible);

    public CadEditorViewModel()
        : this(new CadDialogService(), new CadTemplateStore())
    {
    }

    public CadEditorViewModel(ICadDialogService dialogService, CadTemplateStore templateStore)
    {
        _dialogService = dialogService;
        _templateStore = templateStore;
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
        ApplyWorkspaceDefaultTool(WorkspaceMode, force: true);
        ActiveTool.OnActivated(CreateToolContext());
        _pendingInitialViewportFit = true;
        _hasAppliedInitialViewport = false;
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
            return kind is GeometryElementKind.FictitiousFrame
                or GeometryElementKind.DummyFrame
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

    public ImpostTarget? SelectedMullion => _selectedMullion;

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
        CurrentToolMode is WindowToolMode.RadiusVertex or WindowToolMode.ArcVertex;

    public ProfileSegment? HoveredTopProfile =>
        IsTopProfileHovered ? FrameRoot?.TopProfile : null;

    public bool HasArcTopProfile =>
        FrameRoot?.TopProfile?.SegmentType == SegmentType.Arc;

    public bool IsArcVertexDragging => _isArcVertexDragging;

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
        RebuildGridLines();
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

        RefreshConstructionVolume();

        if (FrameRoot != null && CanvasElements.Count > 0)
        {
            ApplyFrameSelection(FrameRoot);
        }

        RequestSceneRefresh();
    }

    public void SetViewportSize(double width, double height) =>
        UpdateViewportSizeCore(width, height, requestRefresh: true);

    private void UpdateViewportSizeCore(double width, double height, bool requestRefresh)
    {
        if (Math.Abs(Viewport.Width - Math.Max(1, width)) < CadMath.Epsilon &&
            Math.Abs(Viewport.Height - Math.Max(1, height)) < CadMath.Epsilon)
        {
            return;
        }

        Viewport.Resize(width, height);
        OnPropertyChanged(nameof(MillimetersPerPixel));
        if (requestRefresh)
        {
            RequestSceneRefresh();
        }
    }

    public void NotifyDrawingCanvasSized(double width, double height)
    {
        UpdateViewportSizeCore(width, height, requestRefresh: false);
        ScheduleDrawingViewportLayout();
    }

    public void ScheduleInitialViewportFocus()
    {
        _pendingInitialViewportFit = true;
        _hasAppliedInitialViewport = false;
        ScheduleDrawingViewportLayout();
    }

    public void ScheduleDrawingViewportLayout()
    {
        if (_viewportLayoutScheduled)
        {
            return;
        }

        _viewportLayoutScheduled = true;
        Dispatcher.UIThread.Post(
            () =>
            {
                _viewportLayoutScheduled = false;
                TryApplyDrawingViewportLayout();
            },
            DispatcherPriority.Background);
    }

    public void SaveDrawingViewportState()
    {
        _savedZoom = Viewport.ZoomFactor;
        _savedPanX = Viewport.PanOffset.X;
        _savedPanY = Viewport.PanOffset.Y;
        _hasSavedDrawingViewport = true;
    }

    public void OnDrawingTabDeactivated() => SaveDrawingViewportState();

    public void OnDrawingTabActivated()
    {
        _restoreViewportOnNextLayout = true;
        ScheduleDrawingViewportLayout();
    }

    public void RestoreDrawingViewportState()
    {
        if (!_hasSavedDrawingViewport)
        {
            return;
        }

        Viewport.ZoomFactor = _savedZoom;
        Viewport.PanOffset = new CadVector(_savedPanX, _savedPanY);
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));
        RequestSceneRefresh();
    }

    private void TryApplyDrawingViewportLayout()
    {
        if (Viewport.Width <= CadMath.Epsilon || Viewport.Height <= CadMath.Epsilon)
        {
            return;
        }

        if (_restoreViewportOnNextLayout && _hasSavedDrawingViewport)
        {
            RestoreDrawingViewportState();
            _restoreViewportOnNextLayout = false;
            return;
        }

        if (_pendingInitialViewportFit && !_hasAppliedInitialViewport)
        {
            ApplyInitialViewportFocus();
        }
    }

    public void ApplyInitialViewportFocus()
    {
        if (Viewport.Width <= CadMath.Epsilon || Viewport.Height <= CadMath.Epsilon)
        {
            _pendingInitialViewportFit = true;
            return;
        }

        if (HasConstructionGeometry())
        {
            FitDocumentToViewport();
        }
        else
        {
            CenterViewportOnOrigin();
        }

        _pendingInitialViewportFit = false;
        _hasAppliedInitialViewport = true;
    }

    public void CenterViewportOnOrigin(double paddingMm = DefaultViewportPaddingMm, double marginPx = DefaultViewportMarginPx)
    {
        if (Viewport.Width <= CadMath.Epsilon || Viewport.Height <= CadMath.Epsilon)
        {
            _pendingInitialViewportFit = true;
            return;
        }

        Viewport.FitToBounds(
            new CadRect(-paddingMm, paddingMm, paddingMm * 2, paddingMm * 2),
            marginPx);
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));
        RequestSceneRefresh();
    }

    public bool HasConstructionGeometry() =>
        FrameRoot is not null || CanvasElements.Count > 0 || Document.Rectangles.Count > 0;

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
            DimensionHitKind.SegmentHeight => TryChangeSegmentHeight(hit, newValueMm),
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

        if (FrameRoot.IsFictitiousContainer && HasNestedBillableFramesInContainer())
        {
            StatusText = "Размер контейнера изменён; вложенные коробки сохраняют прежний размер";
        }

        if (!FrameRoot.TryResizeOuter(newWidthMm, FrameRoot.Height, FrameThickness))
        {
            return false;
        }

        RecalculateFrameGeometry();
        return true;
    }

    private bool TryChangeOverallHeight(double newHeightMm)
    {
        if (FrameRoot is null || newHeightMm < MinEditableOpeningMm)
        {
            return false;
        }

        if (FrameRoot.IsFictitiousContainer && HasNestedBillableFramesInContainer())
        {
            StatusText = "Размер контейнера изменён; вложенные коробки сохраняют прежний размер";
        }

        if (!FrameRoot.TryResizeOuter(FrameRoot.Width, newHeightMm, FrameThickness))
        {
            return false;
        }

        RecalculateFrameGeometry();
        return true;
    }

    /// <summary>Каскадный пересчёт геометрии конструкции после изменения габаритов.</summary>
    private void RecalculateFrameGeometry()
    {
        if (FrameRoot is null)
        {
            return;
        }

        try
        {
            switch (CurrentConstructionShape)
            {
                case ConstructionShape.Rectangle:
                default:
                    SyncFillingsToFrameLayout();
                    break;
            }

            RefreshConstructionVolume();
            RequestSceneRefresh();
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("RecalculateFrameGeometry failed", ex);
        }
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

    private bool TryChangeSegmentHeight(DimensionHitInfo hit, double newValueMm)
    {
        if (FrameRoot is null
            || hit.BoundaryWorldY is not { Count: >= 2 } boundaries
            || newValueMm < MinEditableOpeningMm)
        {
            return false;
        }

        if (!ValidateNeighborSegmentHeight(boundaries, hit.SegmentIndex, hit.MoveUpperBoundary, newValueMm))
        {
            return false;
        }

        var newAxisWorldY = hit.MoveUpperBoundary
            ? boundaries[hit.SegmentIndex] + newValueMm
            : boundaries[hit.SegmentIndex + 1] - newValueMm;

        var target = hit.ImpostTarget;
        if (target is null)
        {
            return false;
        }

        return MoveHorizontalImpost(target, newAxisWorldY);
    }

    private static bool ValidateNeighborSegmentHeight(
        IReadOnlyList<double> boundaries,
        int segmentIndex,
        bool moveUpperBoundary,
        double newValueMm)
    {
        if (newValueMm < MinEditableOpeningMm)
        {
            return false;
        }

        double neighborHeight;
        if (moveUpperBoundary)
        {
            if (segmentIndex + 2 >= boundaries.Count)
            {
                return true;
            }

            neighborHeight = boundaries[segmentIndex + 2] - (boundaries[segmentIndex] + newValueMm);
        }
        else
        {
            if (segmentIndex <= 0)
            {
                return true;
            }

            neighborHeight = (boundaries[segmentIndex + 1] - newValueMm) - boundaries[segmentIndex - 1];
        }

        return neighborHeight >= MinEditableOpeningMm;
    }

    private bool MoveVerticalImpost(ImpostTarget target, double axisWorldX)
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

    private bool MoveHorizontalImpost(ImpostTarget target, double axisWorldY)
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

        SyncExtremeImpostCanvasElements();
        SyncSashTopology();

        foreach (var element in CanvasElements.ToList())
        {
            if (element.Kind == GeometryElementKind.Sash)
            {
                SyncSashElementToLeaf(element);
            }
        }

        SyncGlazingTopology();

        SyncGlazingBeadTopology();

        foreach (var element in CanvasElements.ToList())
        {
            if (element.Kind == GeometryElementKind.Glazing)
            {
                SyncGlazingElementToLayout(element);
            }
        }

        SyncDecorativeSpokesToGlazing();
        SyncSillToFrameWidth();
        SyncTransitionProfilesToFrameLayout();

        foreach (var element in CanvasElements.ToList())
        {
            if (element.Kind == GeometryElementKind.Corrugation)
            {
                SyncCorrugationElementToLayout(element);
            }
        }

        RebuildGridLines();
    }

    /// <summary>
    /// Пересобирает абсолютные GridLines из текущего FrameRoot (без изменения топологии).
    /// </summary>
    public void RebuildGridLines()
    {
        _gridLines = FrameNodeGridLineProjector.Project(FrameRoot);
        OnPropertyChanged(nameof(GridLines));
    }

    // === CRITICAL GUARDRAIL ===
    // Синхронизация переходных профилей с деревом FrameNode.
    // Дверные материалы (SpansFullConstructionHeight) растягиваются на всю высоту колонки;
    // обычные — по квадранту сетки под центром элемента.
    // ======================================================
    private void SyncTransitionProfilesToFrameLayout()
    {
        if (FrameRoot is null || !FrameRoot.IsFictitiousContainer)
        {
            return;
        }

        foreach (var transition in CanvasElements
                     .Where(e => e.Kind == GeometryElementKind.TransitionFrame)
                     .ToList())
        {
            var probeX = (double)transition.X;
            var probeY = (double)transition.Y;
            var hostLeaf = ResolveTransitionHostLeaf(transition, probeX, probeY);
            if (hostLeaf is null || !hostLeaf.IsLeaf)
            {
                continue;
            }

            var material = ConstructionMaterialCatalog.FindById(transition.MaterialId);
            // Для сквозных дверей probeY = середина колонки, иначе квадрант под сохранённым центром.
            if (material?.SpansFullConstructionHeight == true)
            {
                probeY = FrameRoot.Y + FrameRoot.Height / 2.0;
            }

            var (originX, originY, width, height) = ResolveTransitionPlacementBounds(
                hostLeaf,
                material,
                probeX,
                probeY);

            if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
            {
                continue;
            }

            transition.X = (decimal)RoundMm(originX + width / 2.0);
            transition.Y = (decimal)RoundMm(originY + height / 2.0);
            transition.WidthMm = (decimal)width;
            transition.HeightMm = (decimal)height;
            transition.HostFrameNodeId = hostLeaf.NodeId;
        }

        RemoveDuplicateJunctionTransitionProfiles();

        if (FrameRoot.IsFictitiousContainer)
        {
            SyncFrameGeometryToDocument();
        }
    }

    private FrameNode? ResolveTransitionHostLeaf(GeometryElement transition, double probeX, double probeY)
    {
        if (FrameRoot is null)
        {
            return null;
        }

        if (transition.HostFrameNodeId is Guid boundId)
        {
            var bound = FrameRoot.FindNodeById(boundId);
            if (bound is not null)
            {
                if (bound.IsLeaf)
                {
                    return bound;
                }

                // После горизонтального сплита колонки берём лист под центром элемента.
                return bound.FindLeafAt(probeX, probeY)
                       ?? EnumerateFrameLayoutLeaves(bound).FirstOrDefault();
            }
        }

        return FrameRoot.FindLeafAt(probeX, probeY);
    }

    /// <summary>
    /// Дедуп стыков по (Orientation, Coordinate) — симметрично для X и Y.
    /// </summary>
    private void RemoveDuplicateJunctionTransitionProfiles()
    {
        var transitions = CanvasElements
            .Where(e => e.Kind == GeometryElementKind.TransitionFrame)
            .ToList();
        if (transitions.Count <= 1)
        {
            return;
        }

        var survivors = new HashSet<Guid>();
        foreach (var group in transitions.GroupBy(t =>
                 {
                     var vertical = (double)t.WidthMm <= (double)t.HeightMm * 1.25;
                     var coord = vertical ? RoundMm((double)t.X) : RoundMm((double)t.Y);
                     return (vertical, coord);
                 }))
        {
            var best = group
                .OrderByDescending(t => (double)t.HeightMm * (double)t.WidthMm)
                .First();
            survivors.Add(best.Id);
        }

        foreach (var duplicate in transitions.Where(t => !survivors.Contains(t.Id)))
        {
            CanvasElements.Remove(duplicate);
        }
    }

    private GeometryElement? FindTransitionInLeaf(FrameNode leaf) =>
        CanvasElements.FirstOrDefault(
            element => element.Kind == GeometryElementKind.TransitionFrame && ContainsLeaf(element, leaf));
    // === END CRITICAL GUARDRAIL ===

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

                var created = CreateSashElementForTool(leaf, hingeSide, 0)
                    ?? CreateSashElement(leaf, hingeSide);
                if (created is null)
                {
                    continue;
                }

                created.OpeningType = openingType;
                created.HasHardware = hasHardware;
                created.HasHandle = hasHandle;
                created.SourceToolId = sash.SourceToolId;
                AddCanvasElement(created);

                if (hadHostedGlazing)
                {
                    var glazing = CreateGlazingElementForSash(created, leaf);
                    if (glazing is not null)
                    {
                        AddCanvasElement(glazing);
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
                    AddCanvasElement(created);
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
        var leaf = sash.HostFrameNodeId is Guid hostId
                   && FrameRoot?.FindNodeById(hostId) is FrameNode boundLeaf
                   && boundLeaf.IsTerminalLeaf
            ? boundLeaf
            : SashGeometry.ResolveSashHostLeaf(this, sash);
        if (leaf is not { IsTerminalLeaf: true })
        {
            return;
        }

        var (innerX, innerY, innerW, innerH) = ResolveSashOpeningBounds(leaf, (double)sash.X, (double)sash.Y);
        if (innerW <= 0 || innerH <= 0)
        {
            return;
        }

        sash.X = (decimal)(innerX + innerW / 2.0);
        sash.Y = (decimal)(innerY + innerH / 2.0);
        sash.WidthMm = (decimal)innerW;
        sash.HeightMm = (decimal)innerH;
        sash.HostFrameNodeId = leaf.NodeId;
        SyncHostedElementsForSash(sash);
    }

    private void SyncGlazingElementToLayout(GeometryElement glazing)
    {
        if (glazing.HostSashId is Guid sashId)
        {
            var sash = CanvasElements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
            if (sash is null)
            {
                glazing.HostSashId = null;
            }
            else
            {
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

        if (FrameRoot.Shape == ConstructionShape.Triangle)
        {
            SelectedRectangle = null;
            while (Document.Rectangles.Count > 0)
            {
                Document.RemoveRectangle(Document.Rectangles[0]);
            }

            NotifyFrameSelectionProperties();
            return;
        }

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
            return new CadRect(-DefaultViewportPaddingMm, DefaultViewportPaddingMm, DefaultViewportPaddingMm * 2, DefaultViewportPaddingMm * 2);
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

        return new CadRect(0, 0, 0, 0);
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
        if (Viewport.Width <= CadMath.Epsilon || Viewport.Height <= CadMath.Epsilon)
        {
            _pendingInitialViewportFit = true;
            return;
        }

        var content = GetConstructionContentBounds();
        if (content.Width <= CadMath.Epsilon || content.Height <= CadMath.Epsilon)
        {
            CenterViewportOnOrigin();
            return;
        }

        var padded = InflateContentBounds(
            content,
            Math.Clamp(DefaultViewportPaddingMm / Math.Max(content.Width, content.Height), 0.08, 0.18));
        Viewport.FitToBounds(padded, DefaultViewportMarginPx);
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));
        RequestSceneRefresh();
        _hasAppliedInitialViewport = true;
        _pendingInitialViewportFit = false;
    }

    /// <summary>Масштабирует вид по текущему выделению на холсте.</summary>
    public void FitSelectionToViewport()
    {
        if (Viewport.Width <= CadMath.Epsilon || Viewport.Height <= CadMath.Epsilon)
        {
            return;
        }

        var bounds = GetConstructionSelectionBounds();
        if (bounds.Width <= CadMath.Epsilon || bounds.Height <= CadMath.Epsilon)
        {
            StatusText = "Нечего масштабировать: выделение пусто";
            return;
        }

        var padded = InflateContentBounds(bounds, 0.12);
        Viewport.FitToBounds(padded, DefaultViewportMarginPx);
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));
        RequestSceneRefresh();
        StatusText = "Масштаб по выделению";
    }

    private CadRect GetConstructionSelectionBounds()
    {
        if (_constructionSelection.Count == 0 && SelectedElement is null && _selectedMullion is null)
        {
            return new CadRect(0, 0, 0, 0);
        }

        var minX = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var minY = double.PositiveInfinity;
        var maxY = double.NegativeInfinity;

        void IncludePoint(double x, double y)
        {
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y);
        }

        void IncludeRect(double x, double y, double width, double height)
        {
            IncludePoint(x, y);
            IncludePoint(x + width, y);
            IncludePoint(x + width, y + height);
            IncludePoint(x, y + height);
        }

        foreach (var item in _constructionSelection)
        {
            switch (item.Kind)
            {
                case CadSelectableKind.Frame when FrameRoot is not null:
                    IncludeRect(FrameRoot.X, FrameRoot.Y, FrameRoot.Width, FrameRoot.Height);
                    break;
                case CadSelectableKind.Impost when item.ImpostNode is { } impost:
                    IncludeRect(impost.X, impost.Y, impost.Width, impost.Height);
                    break;
                case CadSelectableKind.ExtremeImpost when item.ImpostNode is { } leaf:
                    IncludeRect(leaf.X, leaf.Y, leaf.Width, leaf.Height);
                    break;
                default:
                    if (item.ElementId is Guid id)
                    {
                        var element = CanvasElements.FirstOrDefault(e => e.Id == id);
                        if (element is not null)
                        {
                            var halfW = (double)element.WidthMm / 2.0;
                            var halfH = (double)element.HeightMm / 2.0;
                            IncludeRect(
                                (double)element.X - halfW,
                                (double)element.Y - halfH,
                                (double)element.WidthMm,
                                (double)element.HeightMm);
                        }
                    }

                    break;
            }
        }

        if (SelectedElement is FrameNode frame)
        {
            IncludeRect(frame.X, frame.Y, frame.Width, frame.Height);
        }

        if (_selectedMullion?.SplitNode is { } mullionNode)
        {
            IncludeRect(mullionNode.X, mullionNode.Y, mullionNode.Width, mullionNode.Height);
        }

        if (minX >= maxX || minY >= maxY)
        {
            return new CadRect(0, 0, 0, 0);
        }

        return new CadRect(minX, maxY, maxX - minX, maxY - minY);
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
        TryAlignGlazingUnits = TryAlignGlazingUnitsFromTool,
        TryAlignDeadLights = TryAlignDeadLightsFromTool,
        TryAlignImposts = TryAlignImpostsFromTool,
        TryChangeOverallDimensions = TryChangeOverallDimensionsFromTool,
        TryRecalculateConstruction = TryRecalculateConstructionFromTool,
        TryShowObjectProperties = TryShowObjectPropertiesFromTool,
        TryShowConstructionDetails = TryShowConstructionDetailsFromTool,
        TrySetVertexCoordinates = TrySetVertexCoordinatesFromTool,
        TryPressArcVertex = TryPressArcVertex,
        TryRemoveArcVertex = TryRemoveArcVertexFromTool,
        TrySplitArcAtPoint = TrySplitArcAtPointFromTool,
        TrySplitProfile = TrySplitProfileFromTool,
        TrySetArcRadius = TrySetArcRadiusFromTool,
        TryCopyConstruction = TryCopyConstructionFromTool,
        TryPasteConstruction = TryPasteConstructionFromTool,
        TryAddConstructionToStandard = TryAddConstructionToStandardFromTool,
        TryInsertConstructionFromStandard = TryInsertConstructionFromStandardFromTool,
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

        var interaction = tool.Kind == CadToolKind.None
            ? CadToolInteraction.Persistent
            : tool.Interaction;

        if (interaction == CadToolInteraction.InstantAction)
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
        PurgeOrphanedHostedElements();
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
            if (element.Kind is not (GeometryElementKind.Sash or GeometryElementKind.SlidingSash))
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
        if (_constructionSelection.Count == 0 && _selectedMullion is null && SelectedElement is null && _selectedExtremeImpost is null)
        {
            return;
        }

        _constructionSelection.Clear();
        ClearExtremeImpostSelection();
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
            ClearExtremeImpostSelection();
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
                    or CadSelectableKind.ExtremeImpost
                    or CadSelectableKind.SashHandle
                    or CadSelectableKind.SashHinge
                    or CadSelectableKind.Sill);

        if (primary != null)
        {
            ApplyPrimaryConstructionSelection(primary);
        }

        StatusText = _constructionSelection.Count > 1
            ? $"Выбрано элементов: {_constructionSelection.Count}"
            : _constructionSelection.Count == 1
                && _constructionSelection.First() is { Kind: CadSelectableKind.Frame, ProfileRail: { } profileRail }
                ? $"Выбран профиль: {DescribeFrameProfileRail(profileRail)}"
                : "Элемент выбран";
        OnPropertyChanged(nameof(IsSashMosquitoNetPanelVisible));
        OnPropertyChanged(nameof(HasMosquitoNetOnSelectedSash));
        OnPropertyChanged(nameof(IsSillPropertiesPanelVisible));
        if (FrameRoot?.HasSill == true || _constructionSelection.Any(item => item.Kind == CadSelectableKind.Sill))
        {
            SyncSillPanelFromSelection();
        }
    }

    private void ApplyPrimaryConstructionSelection(CadSelectableItem item)
    {
        if (item.Kind == CadSelectableKind.ExtremeImpost)
        {
            SyncExtremeImpostSelectionFromConstruction(item);
        }
        else
        {
            ClearExtremeImpostSelection();
        }

        switch (item.Kind)
        {
            case CadSelectableKind.Frame:
                if (FrameRoot is not null)
                {
                    ApplyFrameSelection(FrameRoot);
                }

                break;
            case CadSelectableKind.Impost when item.ImpostNode is not null:
                _selectedMullion = new ImpostTarget
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
            case CadSelectableKind.ExtremeImpost:
                break;
            case CadSelectableKind.Sash or CadSelectableKind.Glazing or CadSelectableKind.Bead
                or CadSelectableKind.SashHandle or CadSelectableKind.SashHinge
                when item.ElementId is Guid elementId:
                SyncPanelFromSelectedGeometryElement(elementId);
                break;
            case CadSelectableKind.Sill:
                SyncSillPanelFromSelection();
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

    private static string DescribeFrameProfileRail(FrameProfileRail rail) =>
        rail switch
        {
            FrameProfileRail.Top => "верхний горизонтальный",
            FrameProfileRail.Bottom => "нижний горизонтальный",
            FrameProfileRail.Left => "левый вертикальный",
            FrameProfileRail.Right => "правый вертикальный",
            _ => "профиль коробки"
        };

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
        if (deletable.Count == 1
            && deletable[0].Kind == CadSelectableKind.Frame
            && deletable[0].ProfileRail == FrameProfileRail.Top
            && FrameRoot?.Shape == ConstructionShape.Triangle)
        {
            StatusText = "Коробка преобразована в треугольную";
        }
        else
        {
            StatusText = deletable.Count == 1 ? "Элемент удалён" : $"Удалено элементов: {deletable.Count}";
        }
        return true;
    }

    private bool CanDeleteConstructionItem(CadSelectableItem item) =>
        (item.Kind is CadSelectableKind.Sash
            or CadSelectableKind.Glazing
            or CadSelectableKind.Bead
            or CadSelectableKind.Impost
            or CadSelectableKind.ExtremeImpost
            or CadSelectableKind.SashHandle
            or CadSelectableKind.SashHinge)
        || (item.Kind is CadSelectableKind.Frame && FrameRoot?.IsDummy == true)
        || (item.Kind is CadSelectableKind.Frame
            && item.ProfileRail == FrameProfileRail.Top
            && FrameTriangleGeometry.IsRectangularFrameTransformableToTriangle(FrameRoot!));

    private bool TryDeleteConstructionItem(CadSelectableItem item)
    {
        switch (item.Kind)
        {
            case CadSelectableKind.Frame
                when item.ProfileRail == FrameProfileRail.Top
                     && FrameTriangleGeometry.IsRectangularFrameTransformableToTriangle(FrameRoot!):
                if (!Services.FrameProfileTransformService.TryTransformRectangleToTriangleByRemovingTopRail(this))
                {
                    return false;
                }

                SyncFrameGeometryToDocument();
                return true;
            case CadSelectableKind.Sash when item.ElementId is Guid sashId:
                OrphanHostedGlazingForSash(sashId);
                return RemoveCanvasElementById(sashId);
            case CadSelectableKind.Glazing or CadSelectableKind.Bead when item.ElementId is Guid elementId:
                if (item.Kind == CadSelectableKind.Bead)
                {
                    RemoveBeadWithLinkedGlass(elementId);
                    return true;
                }

                // Связанный со штапиком стеклопакет — удаляем оба объекта вместе.
                var linkedGlazing = CanvasElements.FirstOrDefault(
                    e => e.Id == elementId && e.Kind == GeometryElementKind.Glazing);
                if (linkedGlazing?.ParentBeadId is Guid parentBeadId)
                {
                    RemoveBeadWithLinkedGlass(parentBeadId);
                    return true;
                }

                RemoveSpokesForGlazing(elementId);
                return RemoveCanvasElementById(elementId);
            case CadSelectableKind.Impost when item.ImpostNode is { IsUniformSplit: false } impostNode:
                return impostNode.TryRemoveImpostSplit();
            case CadSelectableKind.ExtremeImpost
                when item.ImpostNode is FrameNode leaf && item.ElementId is Guid slotId:
                if (!leaf.TryRemoveExtremeImpost(slotId))
                {
                    return false;
                }

                SyncExtremeImpostCanvasElements();
                SyncFillingsToFrameLayout();
                return true;
            case CadSelectableKind.SashHandle when item.ElementId is Guid handleSashId:
                return TryClearSashHandle(handleSashId);
            case CadSelectableKind.SashHinge when item.ElementId is Guid hingeSashId:
                return TryClearSashHardware(hingeSashId);
            case CadSelectableKind.Frame when FrameRoot?.IsDummy == true:
                ClearCommittedFrameGeometry(clearPlacementHistory: false);
                return true;
            default:
                return false;
        }
    }

    private bool TryClearSashHandle(Guid sashId)
    {
        var sash = CanvasElements.FirstOrDefault(e =>
            e.Id == sashId && e.Kind is GeometryElementKind.Sash or GeometryElementKind.SlidingSash);
        if (sash is null || !sash.HasHandle)
        {
            return false;
        }

        sash.HasHandle = false;
        CadHostedElementHelper.RemoveHosted(CanvasElements, sash.Id, GeometryElementKind.Handle);
        return true;
    }

    private bool TryClearSashHardware(Guid sashId)
    {
        var sash = CanvasElements.FirstOrDefault(e =>
            e.Id == sashId && e.Kind is GeometryElementKind.Sash or GeometryElementKind.SlidingSash);
        if (sash is null || !sash.HasHardware)
        {
            return false;
        }

        ClearSashOpeningAccessories(sash);
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

    private void OrphanHostedGlazingForSash(Guid sashId)
    {
        foreach (var element in CanvasElements)
        {
            if (element.Kind != GeometryElementKind.Glazing || element.HostSashId != sashId)
            {
                continue;
            }

            element.HostSashId = null;
            SyncGlazingElementToLayout(element);
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
            if (_constructionSelection.Any(item => item.Kind == CadSelectableKind.Frame)
                && FrameRoot?.IsDummy == true)
            {
                RecordPlacement("Конструкция удалена",
                    () => ClearCommittedFrameGeometry(clearPlacementHistory: false));
                ClearConstructionSelection();
                RefreshAfterPlacementHistoryChange();
                RequestSceneRefresh();
                StatusText = "Конструкция удалена";
                return true;
            }

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
        leaf.GetRoot().IsFictitiousContainer
            ? 0
            : leaf.Parent == null
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

        EnsureArchFrameTopProfileForImpost();

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

        RefreshAfterImpostAdded(
            commit.ToolId switch
            {
                "mullion-place-h" => WindowToolMode.MullionHorizontal,
                "mullion-place-d" => WindowToolMode.SlantedImpost,
                _ => WindowToolMode.MullionVertical
            });
        StatusText = string.IsNullOrWhiteSpace(commit.Label)
            ? "Импост установлен"
            : $"{commit.Label} установлен";
        return true;
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
            CadSelectionMode.Vertex => HitTestParametricVertex(worldPoint),
            CadSelectionMode.Profile =>
                HitTestVerticalImpost(worldPoint) is not null
                    ? new CadSelectionTarget(mode, "Вертикальный импост", worldPoint, ProfileRole: "ImpostVertical")
                    : HitTestHorizontalImpost(worldPoint) is not null
                        ? new CadSelectionTarget(mode, "Горизонтальный импост", worldPoint, ProfileRole: "ImpostHorizontal")
                        : FrameRoot.FindLeafAt(worldPoint.X, worldPoint.Y) != null
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

        if (!ShouldPreserveFakeContainerForPlacement(worldPoint))
        {
            ClearCommittedFrameGeometry();
            ApplyNewConstructionOptionsToPanel();
        }

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

            CaptureNewConstructionOptionsFromCommit(
                width,
                height,
                originX,
                originY,
                PlacementAnchor.BottomLeft);
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

        ApplyNewConstructionOptionsToPanel();
        var placementCenter = new CadPoint(
            _newConstructionOptions.AnchorX,
            _newConstructionOptions.AnchorY);
        if (!ShouldPreserveFakeContainerForPlacement(placementCenter))
        {
            ClearCommittedFrameGeometry();
            ApplyNewConstructionOptionsToPanel();
        }

        IsPropertiesPanelVisible = true;
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

            var result = await _dialogService.ShowNewConstructionSizeInputAsync(_newConstructionOptions.Clone());
            if (result == null)
            {
                StatusText = "Ввод размеров отменён";
                return;
            }

            ClearShapePlacementFlags();
            _isCommittingRealFrame = false;

            var (width, height) = NormalizeCommittedShapeSize(CurrentToolMode, result.Width, result.Height);
            var (originX, originY) = PlacementAnchorGeometry.ResolveBottomLeftOrigin(
                result.Anchor,
                result.AnchorX,
                result.AnchorY,
                width,
                height);
            originX = RoundMm(originX);
            originY = RoundMm(originY);

            if (!CommitShapeAt(CurrentToolMode, originX, originY, width, height))
            {
                return;
            }

            ApplyNewConstructionSizeInputResult(result);
            ApplyNewConstructionOptionsToPanel();
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
        FrameRoot = FrameNode.CreateRoot(originX, originY, width, height, isFictitiousContainer: true);
        ApplyRootFrameInitialization(FrameRoot, mode, kind);

        var bounds = new CadRect(originX, originY + height, width, height);
        Document.AddRectangle(bounds);
        SelectedRectangle = bounds;

        var element = kind == GeometryElementKind.DummyFrame
            ? new DummyFrame
            {
                OriginX = originX,
                OriginY = originY,
                WidthMm = width,
                HeightMm = height
            }.ToElement(label)
            : new GeometryElement
            {
                Kind = kind,
                Label = label,
                X = (decimal)RoundMm(originX + width / 2.0),
                Y = (decimal)RoundMm(originY + height / 2.0),
                WidthMm = (decimal)width,
                HeightMm = (decimal)height,
                Shape = ResolveElementShapeForKind(kind) ?? FrameRoot.Shape
            };
        AddCanvasElement(element);

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
        SyncFrameRootFictitiousFlag();
        FitToCanvas(bounds);
        FrameCreated?.Invoke(this, new FrameCreatedEventArgs(element, replaceExisting: true));
        return true;
    }

    private void CompleteFrameCreationAndSelection()
    {
        ClearShapePlacementFlags();
        _isCommittingRealFrame = false;

        if (ShouldNestRealFrameInFakeContext() && SelectedElement is FrameNode nestedLeaf)
        {
            ScheduleFrameSelectionUiRefresh(nestedLeaf);
            return;
        }

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
        if (IsDragCreateShapeMode(CurrentToolMode))
        {
            ApplyNewConstructionOptionsToPanel();
            return;
        }

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

        var centerX = originX + width / 2.0;
        var centerY = originY + height / 2.0;
        if (TryCommitNestedRealFrameAt(centerX, centerY))
        {
            return true;
        }

        _isCommittingRealFrame = true;
        ClearCommittedFrameGeometry();

        var frameKind = ResolveRealFrameKind(width, height);
        FrameRoot = FrameNode.CreateRoot(originX, originY, width, height, isFictitiousContainer: false);
        ApplyRootFrameInitialization(FrameRoot, WindowToolMode.RealFrame, frameKind);

        var bounds = new CadRect(originX, originY + height, width, height);
        Document.AddRectangle(bounds);
        SelectedRectangle = bounds;

        var frameLabel = frameKind == GeometryElementKind.ArchFrame
            ? "Коробка арочная (рама)"
            : "Коробка (рама)";

        var element = new GeometryElement
        {
            Kind = frameKind,
            Label = frameLabel,
            X = (decimal)RoundMm(originX + width / 2.0),
            Y = (decimal)RoundMm(originY + height / 2.0),
            WidthMm = (decimal)width,
            HeightMm = (decimal)height,
            Shape = ConstructionShape.Rectangle
        };
        AddCanvasElement(element);

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

    private bool ShouldNestRealFrameInFakeContext() =>
        HasFakeFrameOnCanvas && FrameRoot is not null;

    private void SyncFrameRootFictitiousFlag()
    {
        if (FrameRoot is null)
        {
            return;
        }

        FrameRoot.IsFictitiousContainer = HasFakeFrameOnCanvas;
    }

    private bool TryCommitRealFrameInLeaf(FrameNode leaf, double worldX, double worldY)
    {
        if (!leaf.IsLeaf)
        {
            StatusText = "Кликните внутри свободной ячейки";
            return false;
        }

        if (!leaf.CanAccept(WindowToolMode.RealFrame))
        {
            StatusText = "Нельзя разместить коробку в этой ячейке";
            return false;
        }

        if (FindFrameInLeaf(leaf) is not null)
        {
            StatusText = "Ячейка уже содержит коробку";
            return false;
        }

        if (FindTransitionInLeaf(leaf) is not null)
        {
            StatusText = "Ячейка уже содержит переходной профиль";
            return false;
        }

        var (originX, originY, width, height) = Rendering.SashGeometry.ResolveFramePlacementBoundsAtPoint(
            this,
            leaf,
            worldX,
            worldY);

        if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
        {
            StatusText = FrameRoot?.IsFictitiousContainer == true
                ? "Недостаточно места для коробки с учётом нащельника"
                : "Проём слишком мал для коробки";
            return false;
        }

        var frameKind = ResolveRealFrameKind(width, height);
        var frameLabel = frameKind == GeometryElementKind.ArchFrame
            ? "Коробка арочная (рама)"
            : "Коробка (рама)";

        var element = new GeometryElement
        {
            Kind = frameKind,
            Label = frameLabel,
            X = (decimal)RoundMm(originX + width / 2.0),
            Y = (decimal)RoundMm(originY + height / 2.0),
            WidthMm = (decimal)width,
            HeightMm = (decimal)height,
            HostFrameNodeId = leaf.NodeId
        };
        AddCanvasElement(element);

        SelectedElement = leaf;
        _isSyncingSelection = true;
        Length = width;
        Height = height;
        CoordinateX = originX;
        CoordinateY = originY;
        _isSyncingSelection = false;

        OnPropertyChanged(nameof(HasRealFrameOnCanvas));
        OnPropertyChanged(nameof(SelectedElementLength));
        OnPropertyChanged(nameof(SelectedElementHeight));
        FrameCreated?.Invoke(this, new FrameCreatedEventArgs(element, replaceExisting: false));
        StatusText = $"{frameLabel} создана";
        RequestSceneRefresh();
        return true;
    }

    private static bool ShouldCreateArchFrame(double width, double height) =>
        height <= width / 2.0 + CadMath.Epsilon;

    private static GeometryElementKind ResolveRealFrameKind(double width, double height) =>
        ShouldCreateArchFrame(width, height)
            ? GeometryElementKind.ArchFrame
            : GeometryElementKind.Frame;

    private void ApplyArchProfileIfNeeded(GeometryElementKind kind, double width)
    {
        if (kind != GeometryElementKind.ArchFrame || FrameRoot is null)
        {
            return;
        }

        ArchFrameDrawingHelper.EnsureFrameRootArcProfile(FrameRoot, width);
    }

    public async Task TryHandleAdapterProfileClickAsync(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.AdapterProfile || !ShouldNestRealFrameInFakeContext())
        {
            return;
        }

        var leaf = FrameRoot?.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (leaf is null || !leaf.IsLeaf)
        {
            StatusText = "Кликните внутри свободного проёма";
            return;
        }

        if (!leaf.CanAccept(WindowToolMode.AdapterProfile))
        {
            StatusText = "Нельзя установить переходной профиль в этой ячейке";
            return;
        }

        if (FindTransitionInLeaf(leaf) is not null)
        {
            StatusText = "Переходной профиль уже установлен";
            return;
        }

        if (FindFrameInLeaf(leaf) is not null)
        {
            StatusText = "Проём уже содержит коробку";
            return;
        }

        if (RecordPlacement("Переходной профиль установлен", () => TryCommitTransitionProfileInLeaf(leaf, worldPoint.X, worldPoint.Y)))
        {
            StatusText = "Переходной профиль установлен";
            RequestSceneRefresh();
        }

        await Task.CompletedTask;
    }

    private bool TryCommitTransitionProfileInLeaf(FrameNode leaf, double worldX, double worldY)
    {
        var material = SelectedConstructionMaterialOption?.Material
            ?? ConstructionMaterialCatalog.ForKind(GeometryElementKind.TransitionFrame).FirstOrDefault();
        var (originX, originY, width, height) = ResolveTransitionPlacementBounds(leaf, material, worldX, worldY);

        if (width <= CadMath.Epsilon || height <= CadMath.Epsilon)
        {
            return false;
        }

        var transition = new GeometryElement
        {
            Kind = GeometryElementKind.TransitionFrame,
            Label = material?.Name ?? "Переходной профиль",
            X = (decimal)RoundMm(originX + width / 2.0),
            Y = (decimal)RoundMm(originY + height / 2.0),
            WidthMm = (decimal)width,
            HeightMm = (decimal)height,
            HostFrameNodeId = leaf.NodeId,
            MaterialId = material?.Id
        };
        AddCanvasElement(transition);

        FrameCreated?.Invoke(this, new FrameCreatedEventArgs(transition, replaceExisting: false));
        if (FrameRoot?.IsFictitiousContainer == true)
        {
            SyncFrameGeometryToDocument();
        }

        return true;
    }

    /// <summary>
    /// Опорный контур переходного профиля: дверные материалы («65 компланарная Т»)
    /// игнорируют горизонтальный сплиттер и занимают весь вертикальный пролёт колонки.
    /// </summary>
    private (double OriginX, double OriginY, double Width, double Height) ResolveTransitionPlacementBounds(
        FrameNode leaf,
        ConstructionMaterial? material,
        double worldX,
        double worldY)
    {
        _ = material;
        // Всегда ограничиваем квадрантом сетки; стыкуем с соседними Transition Outer-faces.
        return Rendering.SashGeometry.ResolveFramePlacementBoundsAtPoint(this, leaf, worldX, worldY);
    }

    private (double OriginX, double OriginY, double Width, double Height)? _transitionProfileHoverBounds;

    /// <summary>
    /// Фантом переходного профиля под курсором (мировые координаты).
    /// Для дверных материалов показывает сквозной контур на всю высоту рамы.
    /// </summary>
    public (double OriginX, double OriginY, double Width, double Height)? TransitionProfileHoverBounds =>
        _transitionProfileHoverBounds;

    public void UpdateAdapterProfileHover(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.AdapterProfile
            || FrameRoot is null
            || !ShouldNestRealFrameInFakeContext())
        {
            ClearAdapterProfileHover();
            return;
        }

        var leaf = FrameRoot.FindLeafAt(worldPoint.X, worldPoint.Y);
        if (leaf is null
            || !leaf.IsLeaf
            || !leaf.CanAccept(WindowToolMode.AdapterProfile)
            || FindTransitionInLeaf(leaf) is not null
            || FindFrameInLeaf(leaf) is not null)
        {
            ClearAdapterProfileHover();
            return;
        }

        var material = SelectedConstructionMaterialOption?.Material
            ?? ConstructionMaterialCatalog.ForKind(GeometryElementKind.TransitionFrame).FirstOrDefault();
        var bounds = ResolveTransitionPlacementBounds(leaf, material, worldPoint.X, worldPoint.Y);
        if (bounds.Width <= CadMath.Epsilon || bounds.Height <= CadMath.Epsilon)
        {
            ClearAdapterProfileHover();
            return;
        }

        if (_transitionProfileHoverBounds is { } current
            && Math.Abs(current.OriginX - bounds.OriginX) < CadMath.MmPrecision
            && Math.Abs(current.OriginY - bounds.OriginY) < CadMath.MmPrecision
            && Math.Abs(current.Width - bounds.Width) < CadMath.MmPrecision
            && Math.Abs(current.Height - bounds.Height) < CadMath.MmPrecision)
        {
            return;
        }

        _transitionProfileHoverBounds = bounds;
        RequestSceneRefresh();
    }

    public void ClearAdapterProfileHover()
    {
        if (_transitionProfileHoverBounds is null)
        {
            return;
        }

        _transitionProfileHoverBounds = null;
        RequestSceneRefresh();
    }

    private bool HasNestedBillableFramesInContainer() =>
        CanvasElements.Any(
            element => element.Kind is GeometryElementKind.Frame or GeometryElementKind.ArchFrame
                       && !DummyFrame.IsDummyElement(element));

    /// <summary>
    /// Ячейка является валидным проёмом для створки/остекления/фурнитуры,
    /// если в ней есть Host Frame (рама или переходной профиль).
    /// </summary>
    private bool IsBillableConstructionLeaf(FrameNode leaf) =>
        ConstructionHostResolver.CanHostSash(this, leaf);

    private GeometryElement? FindHostFrameInLeaf(FrameNode leaf) =>
        ConstructionHostResolver.FindHostFrameInLeaf(this, leaf);

    private GeometryElement? FindFrameInLeaf(FrameNode leaf)
    {
        GeometryElement? best = null;
        var bestScore = 0.0;

        foreach (var element in CanvasElements)
        {
            if (element.Kind is not (GeometryElementKind.Frame or GeometryElementKind.ArchFrame))
            {
                continue;
            }

            if (DummyFrame.IsDummyElement(element))
            {
                continue;
            }

            if (!ConstructionHostResolver.IsElementBoundToLeafSubtree(FrameRoot, leaf, element))
            {
                continue;
            }

            if (element.HostFrameNodeId is Guid hostId
                && FrameRoot is not null
                && FrameRoot.FindNodeById(hostId) is FrameNode boundNode
                && !boundNode.IsTerminalLeaf
                && boundNode.NodeId != leaf.NodeId)
            {
                continue;
            }

            var score = 0.0;
            if (element.HostFrameNodeId == leaf.NodeId)
            {
                score = 1_000_000;
            }
            else if (leaf.Contains((double)element.X, (double)element.Y))
            {
                score = 500_000 + (double)element.WidthMm * (double)element.HeightMm;
            }
            else if (ConstructionHostResolver.TryMeasureHostLeafOverlap(leaf, element, out var overlap)
                     && overlap > leaf.Width * leaf.Height * 0.2)
            {
                score = overlap;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = element;
            }
        }

        return best;
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
        ClearHardwarePreview();
        _impostSplitHistory.Clear();
        _isCommittingRealFrame = false;
        ClearExtremeImpostSelection();
        ClearExtremeImpostHover();
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

    private ConstructionShape CurrentConstructionShape =>
        FrameRoot?.Shape ?? ConstructionShape.Rectangle;

    private static ConstructionShape? ResolveElementShapeForKind(GeometryElementKind kind) =>
        kind switch
        {
            GeometryElementKind.RoundDummyFrame => ConstructionShape.Circle,
            GeometryElementKind.TriangleDummyFrame => ConstructionShape.Triangle,
            GeometryElementKind.Frame
                or GeometryElementKind.DummyFrame
                or GeometryElementKind.FictitiousFrame => ConstructionShape.Rectangle,
            _ => null
        };

    private static void EnsureRectangularRootProfile(FrameNode root)
    {
        root.Shape = ConstructionShape.Rectangle;
        root.AttachedSill = null;
        if (root.TopProfile is null)
        {
            return;
        }

        root.TopProfile.SyncTopRailGeometry(root.X, root.Y, root.Width, root.Height);
        if (root.TopProfile.SegmentType != SegmentType.Line)
        {
            root.TopProfile.ConvertToLine();
        }
    }

    private void ApplyRootFrameInitialization(
        FrameNode root,
        WindowToolMode mode,
        GeometryElementKind elementKind)
    {
        root.AttachedSill = null;

        if (elementKind is GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame)
        {
            root.Shape = ConstructionShape.Rectangle;
            ArchFrameDrawingHelper.EnsureFrameRootArcProfile(root, root.Width);
            return;
        }

        var shape = ResolveElementShapeForKind(elementKind) ?? ResolveShapeForMode(mode);
        root.Shape = shape;

        if (elementKind is GeometryElementKind.Frame
            or GeometryElementKind.DummyFrame
            or GeometryElementKind.FictitiousFrame)
        {
            EnsureRectangularRootProfile(root);
            return;
        }

        if (root.TopProfile is { } profile && profile.SegmentType == SegmentType.Arc)
        {
            profile.SyncTopRailGeometry(root.X, root.Y, root.Width, root.Height);
            profile.ConvertToLine();
        }
    }

    private bool TryCommitNestedRealFrameAt(double worldX, double worldY)
    {
        if (!ShouldNestRealFrameInFakeContext() || FrameRoot is null)
        {
            return false;
        }

        var leaf = FrameRoot.FindLeafAt(worldX, worldY);
        if (leaf is null || !leaf.IsLeaf)
        {
            return false;
        }

        _isCommittingRealFrame = true;
        return TryCommitRealFrameInLeaf(leaf, worldX, worldY);
    }

    private bool ShouldPreserveFakeContainerForPlacement(CadPoint worldPoint) =>
        CurrentToolMode == WindowToolMode.RealFrame
        && ShouldNestRealFrameInFakeContext()
        && FrameRoot?.FindLeafAt(worldPoint.X, worldPoint.Y) is FrameNode leaf
        && leaf.IsLeaf;

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
        if (WorkspaceMode == CadEditorWorkspaceMode.Decoration)
        {
            return false;
        }

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

    public async Task TryHandleDecorativeSpokeClickAsync(CadPoint worldPoint)
    {
        if (WorkspaceMode != CadEditorWorkspaceMode.Decoration ||
            CurrentToolMode != WindowToolMode.DecorativeSpoke)
        {
            return;
        }

        var glazing = ConstructionSelectionHelper.FindGlazingAtPoint(this, worldPoint);
        if (glazing is null)
        {
            StatusText = "Кликните по стеклопакету";
            return;
        }

        if (!SashGeometry.TryResolveGlassForGlazingElement(this, glazing, CanvasElements, out var glass))
        {
            StatusText = "Не удалось определить контур стеклопакета";
            return;
        }

        ReleaseModalDialogBlocking();

        var dialogResult = await _dialogService.ShowDecorativeSpokeDialogAsync(glass.IsArched);
        if (dialogResult is null)
        {
            return;
        }

        var spoke = BuildDecorativeSpokeElement(glazing, glass, dialogResult);
        if (spoke is null)
        {
            return;
        }

        if (!RecordPlacement("Декоративный шпрос добавлен", () =>
            {
                RemoveSpokesForGlazing(glazing.Id);
                AddCanvasElement(spoke.ToGeometryElement());
                return true;
            }))
        {
            StatusText = "Не удалось добавить декоративный шпрос";
            return;
        }

        RequestSceneRefresh();
        StatusText = "Декоративная раскладка добавлена";
    }

    private static DecorativeSpokeElement? BuildDecorativeSpokeElement(
        GeometryElement glazing,
        SashGeometry.GlassOpeningBounds glass,
        DecorativeSpokeDialogResult dialog)
    {
        if (glass.Width < 20 || glass.Height < 20)
        {
            return null;
        }

        return new DecorativeSpokeElement
        {
            ParentGlazingId = glazing.Id,
            SpokeType = dialog.SpokeType,
            Thickness = dialog.Thickness,
            Color = dialog.Color,
            RowsCount = dialog.RowsCount,
            ColumnsCount = dialog.ColumnsCount,
            RayCount = dialog.RayCount,
            IsArched = glass.IsArched,
            ArcCenterX = glass.ArcCenter.X,
            ArcCenterY = glass.ArcCenter.Y,
            ArcRadius = glass.ArcRadius,
            CenterX = (glass.Left + glass.Right) / 2.0,
            CenterY = (glass.Bottom + glass.Top) / 2.0,
            WidthMm = glass.Width,
            HeightMm = glass.Height,
            MaterialId = ResolveSpokeMaterialId(dialog.Thickness, dialog.Color)
        };
    }

    private static Guid? ResolveSpokeMaterialId(double thickness, string color)
    {
        var colorToken = color switch
        {
            DecorativeSpokeElement.Colors.Gold => "золото",
            DecorativeSpokeElement.Colors.Brown => "коричнев",
            _ => "белый"
        };

        return ConstructionMaterialCatalog.All.FirstOrDefault(material =>
                material.ApplicableKind == GeometryElementKind.DecorativeSpoke &&
                material.Name.Contains(colorToken, StringComparison.OrdinalIgnoreCase) &&
                Math.Abs(material.ProfileWidth - thickness) < 0.1)
            ?.Id;
    }

    private void RemoveSpokesForGlazing(Guid glazingId)
    {
        for (var i = CanvasElements.Count - 1; i >= 0; i--)
        {
            var element = CanvasElements[i];
            if (element.Kind == GeometryElementKind.DecorativeSpoke && element.ParentGlazingId == glazingId)
            {
                CanvasElements.RemoveAt(i);
            }
        }
    }

    private void SyncDecorativeSpokesToGlazing()
    {
        foreach (var spokeElement in CanvasElements
                     .Where(e => e.Kind == GeometryElementKind.DecorativeSpoke)
                     .ToList())
        {
            if (spokeElement.ParentGlazingId is not Guid glazingId)
            {
                continue;
            }

            var glazing = CanvasElements.FirstOrDefault(e => e.Id == glazingId && e.Kind == GeometryElementKind.Glazing);
            if (glazing is null)
            {
                CanvasElements.Remove(spokeElement);
                continue;
            }

            if (!SashGeometry.TryResolveGlassForGlazingElement(this, glazing, CanvasElements, out var glass))
            {
                continue;
            }

            spokeElement.X = (decimal)((glass.Left + glass.Right) / 2.0);
            spokeElement.Y = (decimal)((glass.Bottom + glass.Top) / 2.0);
            spokeElement.WidthMm = (decimal)glass.Width;
            spokeElement.HeightMm = (decimal)glass.Height;
            spokeElement.SpokeIsArched = glass.IsArched;
            spokeElement.SpokeArcCenterX = (decimal)glass.ArcCenter.X;
            spokeElement.SpokeArcCenterY = (decimal)glass.ArcCenter.Y;
            spokeElement.SpokeArcRadius = glass.ArcRadius;
        }
    }

    public async Task TryHandleImpostClickAsync(CadPoint worldPoint)
    {
        if (WorkspaceMode != CadEditorWorkspaceMode.Creation)
        {
            return;
        }

        if (IsDragCreateShapeMode(CurrentToolMode) || !IsImpostSplitMode(CurrentToolMode))
        {
            return;
        }

        if (IsImpostDragPlacementTool(CurrentToolMode))
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

    partial void OnSelectedProfileSeriesItemChanged(ProfileSeries? value) => ApplySeriesThickness(value);

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

    partial void OnShowAllMaterialsChanged(bool value) => QueueMaterialFilterRefresh();

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
        CacheMaterialForTool(_materialCacheSourceTool);

        // Сброс очереди материалов — старые callback'и ComboBox не должны перезаписывать новый инструмент.
        _pendingMaterialFilterRefresh = false;
        _isUpdatingSelection = false;
        _selectionUpdateGeneration++;

        FinalizeActiveToolModeSwitch(value);
        _materialCacheSourceTool = value;

        OnPropertyChanged(nameof(IsDragCreateShapeToolActive));
        OnPropertyChanged(nameof(IsPlacementToolActive));
        OnPropertyChanged(nameof(ActiveToolMode));
        OnPropertyChanged(nameof(AreFurnitureOptionsEnabled));
        OnPropertyChanged(nameof(IsTopProfileHovered));
        OnPropertyChanged(nameof(HoveredTopProfile));
        OnPropertyChanged(nameof(ShowArcVertexHandles));
        OnPropertyChanged(nameof(IsConstructionMaterialComboEnabled));

        QueueMaterialFilterRefresh();

        if (value is not WindowToolMode.OffsetImpostVertical and not WindowToolMode.OffsetImpostHorizontal)
        {
            ClearImpostOffsetInteractionState();
        }

        if (value != WindowToolMode.ExtremeImpost)
        {
            ClearExtremeImpostHover();
        }

        if (value != WindowToolMode.Hardware)
        {
            ClearHardwarePreview();
        }

        OnPropertyChanged(nameof(IsExtremeImpostToolActive));
        OnPropertyChanged(nameof(IsExtremeImpostEditPanelActive));
        OnPropertyChanged(nameof(IsSillToolActive));
    }

    /// <summary>Немедленная подготовка логики мыши/превью при смене инструмента (без ожидания ComboBox).</summary>
    private void FinalizeActiveToolModeSwitch(WindowToolMode newMode)
    {
        _isRunningFramePipeline = false;
        _isShapeSizeDialogOpen = false;
        _isCommittingRealFrame = false;

        if (_realFrameStartWorld != null || RealFramePreview != null)
        {
            ClearShapePlacementFlags();
            RealFramePreview = null;
            OnPropertyChanged(nameof(RealFramePreview));
            OnPropertyChanged(nameof(ShowStatusBarDimensions));
            RequestSceneRefresh();
        }

        if (!IsDragCreateShapeMode(newMode) && FrameRoot == null)
        {
            UpdatePlacementPanelCoordinatesFromAnchor();
        }

        if (IsDragCreateShapeMode(newMode))
        {
            ApplyNewConstructionOptionsToPanel();
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

        if (IsDragCreateShapeMode(CurrentToolMode))
        {
            _newConstructionOptions.Anchor = value;
        }

        UpdatePlacementPanelCoordinatesFromAnchor();
    }

    partial void OnSelectedPlacementAnchorOptionChanged(PlacementAnchorOption value)
    {
        if (PlacementAnchor != value.Anchor)
        {
            PlacementAnchor = value.Anchor;
        }

        if (IsDragCreateShapeMode(CurrentToolMode))
        {
            _newConstructionOptions.Anchor = value.Anchor;
        }
    }

    /// <summary>
    /// Глобальный переключатель цвета контура чертежа: применяет выбранный цвет к общей
    /// теме <see cref="CadContourTheme"/> (используется всеми рендерерами рамы, импостов,
    /// створок, штапика и размерных линий) и мгновенно перерисовывает холст.
    /// </summary>
    partial void OnSelectedContourColorOptionChanged(ContourColorOption value)
    {
        CadContourTheme.Apply(value.Option);
        RequestSceneRefresh();
    }

    partial void OnSelectedRectangleChanged(CadRect? value) => SyncCoordinatesFromSelection();

    partial void OnCoordinateXChanged(double value) => ApplyCoordinatesToSelection();

    partial void OnCoordinateYChanged(double value) => ApplyCoordinatesToSelection();

    partial void OnLengthChanged(double value)
    {
        if (!_isSyncingSelection && TryGetPrimaryElement(out var element) &&
            element is not null &&
            element.Kind is GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame &&
            FrameRoot is not null)
        {
            ArchFrameDrawingHelper.SyncFrameRootArcWidth(FrameRoot, value);
        }

        ApplyCoordinatesToSelection();

        if (!_isSyncingSelection && FrameRoot == null && IsDragCreateShapeMode(CurrentToolMode))
        {
            CaptureNewConstructionOptionsFromPanel();
            UpdatePlacementPanelCoordinatesFromAnchor();
        }
    }

    partial void OnHeightChanged(double value)
    {
        if (!_isSyncingSelection && TryGetPrimaryElement(out var element) && element is not null)
        {
            if (element.Kind == GeometryElementKind.RoundDummyFrame && Math.Abs(value - Length) > CadMath.Epsilon)
            {
                _isSyncingSelection = true;
                Height = Length;
                _isSyncingSelection = false;
            }
        }

        ApplyCoordinatesToSelection();

        if (!_isSyncingSelection && FrameRoot == null && IsDragCreateShapeMode(CurrentToolMode))
        {
            CaptureNewConstructionOptionsFromPanel();
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
            element is null ||
            element.Kind is not (GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame))
        {
            StatusText = "Выберите арочную конструкцию";
            return;
        }

        var width = (double)element.WidthMm;
        var currentArchHeight = FrameRoot?.TopProfile?.SegmentType == SegmentType.Arc
            ? FrameRoot.TopProfile.ArcHeight
            : ArchFrameDrawingHelper.DefaultArcHeightForWidth(width);
        var result = await _dialogService.ShowSizeInputAsync(width, currentArchHeight, CoordinateX, CoordinateY);
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

        if (FrameRoot is not null)
        {
            FrameRoot.EnsureTopProfile();
            if (FrameRoot.TopProfile is { } profile)
            {
                if (profile.SegmentType != SegmentType.Arc)
                {
                    profile.ConvertToArc(width / 2.0);
                }

                profile.ArcHeight = archHeight;
            }
        }

        _isSyncingSelection = true;
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

            if (template.HasFullGeometry)
            {
                var memento = ConstructionTemplateSerializer.TryDeserialize(template.GeometryJson);
                if (memento is null)
                {
                    StatusText = "Не удалось прочитать геометрию шаблона";
                    return;
                }

                ApplyImportedConstructionMemento(ConstructionCloneHelper.DeepClone(memento));
                StatusText = $"Применён шаблон: {template.Name}";
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

        if (Viewport.Width > CadMath.Epsilon && Viewport.Height > CadMath.Epsilon)
        {
            CenterViewportOnOrigin();
        }
        else
        {
            _pendingInitialViewportFit = true;
            _hasAppliedInitialViewport = false;
        }

        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));

        Document.Name = "Новый чертеж";
        NewTemplateRequested?.Invoke(this, EventArgs.Empty);
        RequestSceneRefresh();
    }

    private void InitializeDesignTools()
    {
        CadToolbarLayout.Populate(CreationDesignTools, EditingDesignTools, DecorationDesignTools, ActivateDesignToolCommand);
    }

    [RelayCommand]
    private async Task SetWorkspaceModeAsync(CadEditorWorkspaceMode mode)
    {
        var modeChanged = WorkspaceMode != mode;
        if (modeChanged)
        {
            WorkspaceMode = mode;
        }

        await ApplyWorkspaceDefaultToolAsync(mode, force: modeChanged || !IsCurrentToolValidForWorkspace(mode));

        if (modeChanged)
        {
            StatusText = mode switch
            {
                CadEditorWorkspaceMode.Creation => "Режим: создание конструкции",
                CadEditorWorkspaceMode.Editing => "Режим: редактирование",
                CadEditorWorkspaceMode.Decoration => "Режим: декорирование (шпросы)",
                _ => StatusText
            };
        }
    }

    partial void OnWorkspaceModeChanged(CadEditorWorkspaceMode value)
    {
        OnPropertyChanged(nameof(IsCreationWorkspaceActive));
        OnPropertyChanged(nameof(IsEditingWorkspaceActive));
        OnPropertyChanged(nameof(IsDecorationWorkspaceActive));
        OnPropertyChanged(nameof(IsConstructionMaterialComboEnabled));
        QueueMaterialFilterRefresh();
    }

    private bool IsCurrentToolValidForWorkspace(CadEditorWorkspaceMode mode)
    {
        var tool = FindDesignTool(CurrentToolMode, _activeToolKey);
        return tool is not null && IsToolAllowedInCurrentWorkspace(tool);
    }

    private Task ApplyWorkspaceDefaultToolAsync(CadEditorWorkspaceMode mode, bool force = false)
    {
        if (!force && IsCurrentToolValidForWorkspace(mode))
        {
            return Task.CompletedTask;
        }

        var (defaultMode, defaultToolKey) = ResolveWorkspaceDefaultTool(mode);
        return SelectToolModeAsync(defaultMode, defaultToolKey);
    }

    private void ApplyWorkspaceDefaultTool(CadEditorWorkspaceMode mode, bool force = false)
    {
        if (!force && IsCurrentToolValidForWorkspace(mode))
        {
            return;
        }

        var (defaultMode, defaultToolKey) = ResolveWorkspaceDefaultTool(mode);
        UpdateDesignToolSelection(defaultMode, defaultToolKey);
    }

    private static (WindowToolMode Mode, string ToolKey) ResolveWorkspaceDefaultTool(CadEditorWorkspaceMode mode) =>
        mode switch
        {
            CadEditorWorkspaceMode.Creation => (WindowToolMode.RealFrame, "frame-real"),
            CadEditorWorkspaceMode.Editing => (WindowToolMode.SelectElement, "select-elements"),
            CadEditorWorkspaceMode.Decoration => (WindowToolMode.DecorativeSpoke, "decorative-spoke"),
            _ => (WindowToolMode.SelectElement, "select-elements")
        };

    private bool IsToolAllowedInCurrentWorkspace(CadDesignToolButtonViewModel tool) =>
        WorkspaceMode switch
        {
            CadEditorWorkspaceMode.Creation => CreationDesignTools.Contains(tool),
            CadEditorWorkspaceMode.Editing => EditingDesignTools.Contains(tool),
            CadEditorWorkspaceMode.Decoration => DecorationDesignTools.Contains(tool),
            _ => false
        };

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

        if (!IsToolAllowedInCurrentWorkspace(tool))
        {
            StatusText = "Инструмент недоступен в текущем режиме работы";
            return;
        }

        if (tool.IsInstantCommand)
        {
            await ExecuteToolbarInstantCommandAsync(toolKey);
            return;
        }

        if (tool.CadToolKind is CadToolKind cadKind && cadKind != CadToolKind.None)
        {
            UpdateDesignToolSelection(tool.Mode, toolKey);
            ActivateCadTool(CadToolMetadata.GetId(cadKind));
            StatusText = tool.Title;
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
            case "redo":
                Redo();
                break;
            default:
            {
                var tool = AllDesignTools.FirstOrDefault(t => t.ToolKey == toolKey && t.IsTool);
                if (tool?.CadToolKind is CadToolKind kind && kind != CadToolKind.None)
                {
                    ActivateCadTool(CadToolMetadata.GetId(kind));
                }

                break;
            }
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
            WindowToolMode.ArcVertex => CadToolMetadata.GetId(CadToolKind.ArcVertex),
            WindowToolMode.SplitArc => CadToolMetadata.GetId(CadToolKind.SplitArc),
            WindowToolMode.ClearConstruction => CadToolMetadata.GetId(CadToolKind.ClearConstruction),
            WindowToolMode.Other => string.Empty,
            _ => string.Empty
        };

        return toolId.Length > 0;
    }

    private async Task SelectToolModeAsync(WindowToolMode mode, string? toolKey = null)
    {
        if (mode is WindowToolMode.Separator or WindowToolMode.Other)
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
            await Task.CompletedTask;
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
                    : IsImpostDragPlacementTool(mode)
                        ? mode == WindowToolMode.RealVerticalImpost
                            ? $"{active?.Title}: зажмите ЛКМ в ячейке и потяните вверх или вниз"
                            : $"{active?.Title}: зажмите ЛКМ в ячейке и потяните влево или вправо"
                        : $"{active?.Title}: ЛКМ внутри ячейки — введите количество импостов";
        }
        else if (IsInteractiveToolMode(mode))
        {
            StatusText = mode switch
            {
                WindowToolMode.Sash => ResolveSashToolStatus(),
                WindowToolMode.SlidingSash => "Раздвижная створка: укажите направление сдвига",
                _ => FindDesignTool(mode, toolKey)?.Title is { Length: > 0 } title
                    ? $"Выбран инструмент {title}"
                    : "Инструмент"
            };
        }
        else if (mode is WindowToolMode.Handle or WindowToolMode.Hardware)
        {
            StatusText = mode switch
            {
                WindowToolMode.Hardware => "Фурнитура: клик по створке — левое → правое → фрамуга (петли снизу) → снять",
                WindowToolMode.Handle => "Ручка: клик по створке — справа → слева → сверху → снизу → снять",
                _ => "Инструмент"
            };
        }
        else if (CadCreationToolCatalog.IsClickPlacementMode(mode))
        {
            StatusText = mode switch
            {
                WindowToolMode.Lock => "Замок: клик по створке — установить / снять",
                WindowToolMode.OpeningLimiter => "Ограничитель: клик по створке — установить / снять",
                WindowToolMode.DoorCloser => "Доводчик: клик по створке — установить / снять",
                WindowToolMode.Espagnolette => "Шпингалет: клик по створке — установить / снять",
                WindowToolMode.BeadSystem => "Штапик: клик по световому проёму; Alt — во все проёмы",
                WindowToolMode.GlassWithBead => "Остекление + штапик: клик по световому проёму; Alt — во все проёмы",
                WindowToolMode.AdditionalElement => "Доп. элементы: клик внутри проёма",
                WindowToolMode.Cornice => "Карниз: клик по верхней стороне конструкции",
                WindowToolMode.BlindVertical => "Жалюзи вертикальные: клик внутри проёма",
                WindowToolMode.BlindHorizontal => "Жалюзи горизонтальные: клик внутри проёма",
                WindowToolMode.BlindControl => "Блок управления: клик по жалюзи",
                _ => FindDesignTool(mode, toolKey)?.Title is { Length: > 0 } title
                    ? $"{title}: кликните по конструкции"
                    : "Инструмент"
            };
        }
        else if (mode == WindowToolMode.AdapterProfile)
        {
            StatusText = ShouldNestRealFrameInFakeContext()
                ? "Переходной профиль: кликните внутри свободного проёма"
                : "Переходной профиль доступен только внутри фиктивной коробки";
        }
        else if (mode == WindowToolMode.Glassing)
        {
            StatusText = FrameRoot is null || !HasConstructionHostOnCanvas
                ? "Сначала создайте коробку или переходной профиль"
                : "Остекление: кликните по пустому проёму или по створке";
        }
        else if (mode == WindowToolMode.MosquitoNet)
        {
            StatusText = FrameRoot is null || !HasConstructionHostOnCanvas
                ? "Сначала создайте коробку/переходной профиль и створку"
                : "Москитная сетка: кликните по створке";
        }
        else if (mode == WindowToolMode.Sill)
        {
            StatusText = !CanAttachSillToFrame()
                ? "Сначала создайте коробку (рама)"
                : "Подоконник: кликните по нижнему профилю рамы снаружи";
        }
        else if (mode == WindowToolMode.DecorativeSpoke)
        {
            StatusText = "Декоративный шпрос: кликните по стеклопакету";
        }
        else if (mode == WindowToolMode.ExtremeImpost)
        {
            InitializeExtremeImpostTool();
            StatusText = FrameRoot is null
                ? "Сначала создайте коробку"
                : "Стойка/ригель: наведите на край проёма и кликните";
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
            var result = await _dialogService.ShowNewConstructionSizeInputAsync(_newConstructionOptions.Clone());
            if (result == null)
            {
                StatusText = "Ввод размеров отменён";
                return;
            }

            ApplyNewConstructionSizeInputResult(result);
            var (originX, originY) = PlacementAnchorGeometry.ResolveBottomLeftOrigin(
                result.Anchor,
                result.AnchorX,
                result.AnchorY,
                result.Width,
                result.Height);

            CreatePlacedShape(
                mode,
                result.Width,
                result.Height,
                new CadPoint(RoundMm(originX), RoundMm(originY)));
            ApplyNewConstructionOptionsToPanel();
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
        if (mode == WindowToolMode.FakeFrameCircle)
        {
            height = width;
        }

        if (!snapToGrid)
        {
            width = RoundMm(width);
            height = RoundMm(height);
        }

        if (mode == WindowToolMode.RealFrame && ShouldNestRealFrameInFakeContext())
        {
            double lookupX;
            double lookupY;
            if (clickCenter is CadPoint center)
            {
                lookupX = center.X;
                lookupY = center.Y;
            }
            else if (originBottomLeft is CadPoint placementOrigin)
            {
                lookupX = placementOrigin.X + width / 2.0;
                lookupY = placementOrigin.Y + height / 2.0;
            }
            else
            {
                var viewportCenter = GetViewportCenterWorld();
                lookupX = viewportCenter.X;
                lookupY = viewportCenter.Y;
            }

            if (TryCommitNestedRealFrameAt(lookupX, lookupY))
            {
                if (clickCenter is CadPoint nestedClick)
                {
                    CursorX = nestedClick.X;
                    CursorY = nestedClick.Y;
                    OnPropertyChanged(nameof(CursorX));
                    OnPropertyChanged(nameof(CursorY));
                }

                return;
            }
        }

        ClearCommittedFrameGeometry(clearPlacementHistory: false);

        GeometryElementKind kind;
        string label;
        if (mode == WindowToolMode.RealFrame)
        {
            kind = ResolveRealFrameKind(width, height);
            label = kind == GeometryElementKind.ArchFrame
                ? "Коробка арочная (рама)"
                : "Коробка (рама)";
        }
        else
        {
            (kind, label) = mode switch
            {
                WindowToolMode.FakeFrame => (GeometryElementKind.DummyFrame, "Коробка (фиктивная)"),
                WindowToolMode.FakeFrameCircle => (GeometryElementKind.RoundDummyFrame, "Коробка фиктивная круглая"),
                WindowToolMode.FakeFrameTriangle => (GeometryElementKind.TriangleDummyFrame, "Коробка фиктивная треугольная"),
                WindowToolMode.FakeFrameArch => (GeometryElementKind.ArchDummyFrame, "Полукруг (фиктивная)"),
                _ => (GeometryElementKind.DummyFrame, "Коробка")
            };
        }

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
        FrameRoot = FrameNode.CreateRoot(
            originX,
            originY,
            width,
            height,
            isFictitiousContainer: mode != WindowToolMode.RealFrame);
        ApplyRootFrameInitialization(FrameRoot, mode, kind);

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
            HeightMm = (decimal)height,
            Shape = ResolveElementShapeForKind(kind) ?? ConstructionShape.Rectangle
        };
        AddCanvasElement(element);

        OnPropertyChanged(nameof(HasFakeFrameOnCanvas));
        OnPropertyChanged(nameof(HasFrameOnCanvas));
        OnPropertyChanged(nameof(FrameRoot));
        SyncFrameRootFictitiousFlag();
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
        var number = SelectedConstruction.Number;
        var dims = SelectedConstruction.DimensionsDisplay;
        var profile = SelectedProfileSeries ?? name;
        var message =
            $"Вы уверены, что хотите удалить Конструкцию №{number} ({profile}, {dims})? Это действие нельзя отменить.";

        var confirmed = ConfirmRemoveConstructionHandler is not null
            ? await ConfirmRemoveConstructionHandler(message)
            : await _dialogService.ShowConfirmationAsync(
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

    /// <summary>Совместимость с OrderWorkspaceViewModel (строковое имя серии).</summary>
    public string? SelectedProfileSeries
    {
        get => SelectedProfileSeriesItem?.Name;
        set => SelectedProfileSeriesItem = ProfileSeriesCatalog.FindByName(value) ?? SelectedProfileSeriesItem;
    }

    /// <summary>Суммарный объём конструкций, л (отображается в спецификации ERP).</summary>
    public double ConstructionVolumeLiters => _constructionVolumeLiters;

    private double _constructionVolumeLiters;
    private bool _isRefreshingConstructionVolume;

    public ConstructionVolumeSummary CalculateConstructionVolume() =>
        ConstructionVolumeCalculator.Calculate(CanvasElements);

    /// <summary>Сбрасывает флаги асинхронного обновления материалов перед модальным диалогом.</summary>
    public void ReleaseModalDialogBlocking()
    {
        _isUpdatingSelection = false;
        _pendingMaterialFilterRefresh = false;
        _materialFilterRefreshQueued = false;
    }

    public void StampCatalogSelection(GeometryElement element)
    {
        if (element.Kind == GeometryElementKind.DecorativeSpoke && element.MaterialId.HasValue)
        {
            if (element.ProfileSeriesId != SelectedProfileSeriesItem?.Id)
            {
                element.ProfileSeriesId = SelectedProfileSeriesItem?.Id;
            }

            return;
        }

        var seriesId = SelectedProfileSeriesItem?.Id;
        if (element.ProfileSeriesId != seriesId)
        {
            element.ProfileSeriesId = seriesId;
        }

        var materialId = SelectedConstructionMaterialOption?.MaterialId;
        if (element.MaterialId != materialId)
        {
            element.MaterialId = materialId;
        }
    }

    private void RefreshConstructionVolume()
    {
        if (_isRefreshingConstructionVolume)
        {
            return;
        }

        _isRefreshingConstructionVolume = true;
        try
        {
            _constructionVolumeLiters = ConstructionVolumeCalculator.Calculate(CanvasElements).TotalVolumeLiters;
            OnPropertyChanged(nameof(ConstructionVolumeLiters));
        }
        finally
        {
            _isRefreshingConstructionVolume = false;
        }
    }

    private void AddCanvasElement(GeometryElement element)
    {
        StampCatalogSelection(element);
        CanvasElements.Add(element);
        if (!_isUpdatingSelection)
        {
            RefreshConstructionVolume();
        }
    }

    /// <summary>Ставит обновление списка материалов в очередь UI (никогда не блокирует смену инструмента).</summary>
    private void QueueMaterialFilterRefresh()
    {
        _pendingMaterialFilterRefresh = true;
        if (_materialFilterRefreshQueued)
        {
            return;
        }

        _materialFilterRefreshQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _materialFilterRefreshQueued = false;
            TryExecuteMaterialFilterRefresh();
        }, DispatcherPriority.Loaded);
    }

    private void RefreshFilteredConstructionMaterials() => QueueMaterialFilterRefresh();

    private void TryExecuteMaterialFilterRefresh()
    {
        if (_isUpdatingSelection)
        {
            _pendingMaterialFilterRefresh = true;
            return;
        }

        if (!_pendingMaterialFilterRefresh)
        {
            return;
        }

        _pendingMaterialFilterRefresh = false;
        ExecuteMaterialFilterRefresh();
    }

    private void ExecuteMaterialFilterRefresh()
    {
        _isUpdatingSelection = true;
        var generation = ++_selectionUpdateGeneration;
        var capturedToolMode = CurrentToolMode;
        var materialComboEnabled = IsConstructionMaterialComboEnabledForMode(capturedToolMode);

        if (!materialComboEnabled)
        {
            FilteredConstructionMaterialOptions.Clear();
            FilteredConstructionMaterialOptions.Add(ConstructionMaterialOption.None);
            ApplySelectedConstructionMaterialOption(ConstructionMaterialOption.None);

            Dispatcher.UIThread.Post(() =>
            {
                if (generation != _selectionUpdateGeneration)
                {
                    return;
                }

                _isUpdatingSelection = false;
                var rerun = _pendingMaterialFilterRefresh;
                _pendingMaterialFilterRefresh = false;
                if (rerun)
                {
                    QueueMaterialFilterRefresh();
                }
            }, DispatcherPriority.Loaded);
            return;
        }

        var previousId = _selectedConstructionMaterialOption?.MaterialId;
        var kind = ResolveGeometryKindForTool(capturedToolMode);

        // «Все» выключено → только материалы текущего инструмента; без kind — пусто (не весь каталог).
        IEnumerable<ConstructionMaterial> sourceMaterials;
        if (ShowAllMaterials)
        {
            sourceMaterials = ConstructionMaterialCatalog.All;
        }
        else if (kind is GeometryElementKind resolvedKind)
        {
            sourceMaterials = ConstructionMaterialCatalog.ForKind(resolvedKind);
        }
        else
        {
            sourceMaterials = Array.Empty<ConstructionMaterial>();
        }

        var materials = sourceMaterials
            .OrderBy(material => material.Name)
            .Select(ConstructionMaterialOption.FromMaterial)
            .ToList();

        FilteredConstructionMaterialOptions.Clear();
        FilteredConstructionMaterialOptions.Add(ConstructionMaterialOption.None);
        foreach (var option in materials)
        {
            FilteredConstructionMaterialOptions.Add(option);
        }

        Guid? targetId = null;
        if (_constructionMaterialByTool.TryGetValue(capturedToolMode, out var cachedId))
        {
            targetId = cachedId;
        }
        else if (previousId is Guid id
                 && FilteredConstructionMaterialOptions.Any(option => option.MaterialId == id))
        {
            targetId = id;
        }

        var nextSelection = targetId is Guid materialId
            ? FilteredConstructionMaterialOptions.FirstOrDefault(option => option.MaterialId == materialId)
            : ConstructionMaterialOption.None;

        ApplySelectedConstructionMaterialOption(nextSelection ?? ConstructionMaterialOption.None);

        Dispatcher.UIThread.Post(() =>
        {
            if (generation != _selectionUpdateGeneration)
            {
                return;
            }

            _isUpdatingSelection = false;

            var rerun = _pendingMaterialFilterRefresh;
            _pendingMaterialFilterRefresh = false;
            if (rerun)
            {
                QueueMaterialFilterRefresh();
            }
        }, DispatcherPriority.Loaded);
    }

    /// <summary>Программная установка материала при перестройке списка (без пересчёта объёма).</summary>
    private void ApplySelectedConstructionMaterialOption(ConstructionMaterialOption? option)
    {
        if (ReferenceEquals(_selectedConstructionMaterialOption, option))
        {
            return;
        }

        if (_selectedConstructionMaterialOption?.MaterialId == option?.MaterialId)
        {
            _selectedConstructionMaterialOption = option;
            return;
        }

        _selectedConstructionMaterialOption = option;
        OnPropertyChanged(nameof(SelectedConstructionMaterialOption));
    }

    private static GeometryElementKind? ResolveGeometryKindForTool(WindowToolMode mode) => mode switch
    {
        WindowToolMode.RealFrame or WindowToolMode.Frame => GeometryElementKind.Frame,
        WindowToolMode.FakeFrame => GeometryElementKind.DummyFrame,
        WindowToolMode.FakeFrameCircle => GeometryElementKind.RoundDummyFrame,
        WindowToolMode.FakeFrameArch => GeometryElementKind.ArchDummyFrame,
        WindowToolMode.FakeFrameTriangle => GeometryElementKind.TriangleDummyFrame,
        WindowToolMode.AdapterProfile => GeometryElementKind.TransitionFrame,
        WindowToolMode.Sash or WindowToolMode.SlidingSash => GeometryElementKind.Sash,
        WindowToolMode.Hardware or WindowToolMode.Lock or WindowToolMode.OpeningLimiter
            or WindowToolMode.DoorCloser or WindowToolMode.Espagnolette => GeometryElementKind.Hardware,
        WindowToolMode.Handle => GeometryElementKind.Handle,
        WindowToolMode.MullionVertical or WindowToolMode.MullionHorizontal
            or WindowToolMode.RealVerticalImpost or WindowToolMode.RealHorizontalImpost
            or WindowToolMode.DoubleVerticalImpost or WindowToolMode.DoubleHorizontalImpost
            or WindowToolMode.SlantedImpost or WindowToolMode.VerticalImpost
            or WindowToolMode.HorizontalImpost or WindowToolMode.DiagonalImpost
            or WindowToolMode.Post => GeometryElementKind.Mullion,
        WindowToolMode.ExtremeImpost => GeometryElementKind.EdgeMullion,
        WindowToolMode.BeadSystem or WindowToolMode.GlassWithBead => GeometryElementKind.Bead,
        WindowToolMode.Glassing or WindowToolMode.Glass => GeometryElementKind.Glazing,
        WindowToolMode.DecorativeSpoke => GeometryElementKind.DecorativeSpoke,
        WindowToolMode.CorrugationVertical or WindowToolMode.CorrugationHorizontal
            or WindowToolMode.CorrugationSlanted or WindowToolMode.Corrugation => GeometryElementKind.Corrugation,
        WindowToolMode.MosquitoNet => GeometryElementKind.MosquitoNet,
        WindowToolMode.Sill => GeometryElementKind.Sill,
        _ => null
    };

    private void RefreshFilteredMaterials()
    {
        RefreshFilteredConstructionMaterials();
    }

    private static bool UsesPerToolMaterialCache(WindowToolMode mode) =>
        SupportsConstructionMaterialSelection(mode);

    private bool IsConstructionMaterialComboEnabledForMode(WindowToolMode mode) =>
        WorkspaceMode == CadEditorWorkspaceMode.Creation && UsesPerToolMaterialCache(mode);

    private void CacheMaterialForTool(WindowToolMode mode)
    {
        if (!UsesPerToolMaterialCache(mode))
        {
            return;
        }

        _constructionMaterialByTool[mode] = _selectedConstructionMaterialOption?.MaterialId;
    }

    private static bool SupportsConstructionMaterialSelection(WindowToolMode mode) => mode switch
    {
        WindowToolMode.Hardware or WindowToolMode.Handle or WindowToolMode.Lock
            or WindowToolMode.OpeningLimiter or WindowToolMode.DoorCloser or WindowToolMode.Espagnolette
            or WindowToolMode.Sash or WindowToolMode.SlidingSash
            or WindowToolMode.RealFrame or WindowToolMode.FakeFrame or WindowToolMode.FakeFrameCircle
            or WindowToolMode.FakeFrameTriangle or WindowToolMode.FakeFrameArch or WindowToolMode.Frame
            or WindowToolMode.AdapterProfile or WindowToolMode.RealVerticalImpost
            or WindowToolMode.RealHorizontalImpost or WindowToolMode.DoubleVerticalImpost
            or WindowToolMode.DoubleHorizontalImpost or WindowToolMode.SlantedImpost
            or WindowToolMode.VerticalImpost or WindowToolMode.HorizontalImpost
            or WindowToolMode.DiagonalImpost or WindowToolMode.MullionVertical
            or WindowToolMode.MullionHorizontal or WindowToolMode.Post
            or WindowToolMode.ExtremeImpost
            or WindowToolMode.Glass or WindowToolMode.GlassWithBead or WindowToolMode.BeadSystem
            or WindowToolMode.Glassing
            or WindowToolMode.AdditionalElement or WindowToolMode.Corrugation
            or WindowToolMode.CorrugationVertical or WindowToolMode.CorrugationHorizontal
            or WindowToolMode.CorrugationSlanted
            or WindowToolMode.MosquitoNet or WindowToolMode.Cornice
            or WindowToolMode.BlindVertical or WindowToolMode.BlindHorizontal
            or WindowToolMode.BlindControl or WindowToolMode.Sill or WindowToolMode.DecorativeSpoke
            => true,
        _ => false
    };

    private void ApplySeriesThickness(ProfileSeries? series)
    {
        var depth = series?.SystemDepth ?? 70.0;
        FrameThickness = depth;
        ImpostThickness = depth;
        StatusText = series is null ? "Серия не выбрана" : $"Серия: {series.Name}";
        RequestSceneRefresh();
    }

    private bool IsFakeFrameDrawing() => HasFakeFrameOnCanvas;

    private bool ValidateToolClick(WindowToolMode mode, FrameNode? leaf, CadPoint point)
    {
        if (IsFramePlacementMode(mode))
        {
            return false;
        }

        if (!ValidateTriangleOpeningClick(leaf, point))
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
            if (FrameRoot is null || !HasConstructionHostOnCanvas)
            {
                StatusText = "Сначала создайте коробку или переходной профиль";
                return false;
            }

            if (leaf is null || !leaf.IsLeaf)
            {
                StatusText = "Кликните внутри проёма или по створке";
                return false;
            }

            if (!IsBillableConstructionLeaf(leaf))
            {
                StatusText = "Остекление доступно только внутри несущей рамы или переходного профиля";
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

        if (mode == WindowToolMode.MosquitoNet)
        {
            if (FrameRoot is null || !HasConstructionHostOnCanvas)
            {
                StatusText = "Сначала создайте коробку/переходной профиль и створку";
                return false;
            }

            if (leaf is null || !leaf.IsLeaf)
            {
                StatusText = "Кликните по створке";
                return false;
            }

            if (!IsBillableConstructionLeaf(leaf))
            {
                StatusText = "Москитная сетка доступна только внутри несущей рамы или переходного профиля";
                return false;
            }

            if (FindSashInLeaf(leaf) is null)
            {
                StatusText = "Москитная сетка может быть установлена только на створку";
                return false;
            }

            if (FindMosquitoNetOnSash(FindSashInLeaf(leaf)!) is not null)
            {
                StatusText = "На этой створке уже установлена москитная сетка";
                return false;
            }

            return true;
        }

        if (mode is WindowToolMode.CorrugationVertical or WindowToolMode.CorrugationHorizontal
            or WindowToolMode.CorrugationSlanted)
        {
            if (FrameRoot is null || !HasConstructionHostOnCanvas)
            {
                StatusText = "Сначала создайте коробку или переходной профиль";
                return false;
            }

            if (leaf is null || !leaf.IsLeaf)
            {
                StatusText = "Кликните внутри светового проёма";
                return false;
            }

            if (!IsBillableConstructionLeaf(leaf))
            {
                StatusText = "Гофра доступна только внутри несущей рамы или переходного профиля";
                return false;
            }

            if (!CanApplyCorrugationInLeaf(leaf))
            {
                StatusText = FindCorrugationInLeaf(leaf) is not null
                    ? "Гофра уже установлена в этом проёме"
                    : "Проём слишком мал для гофры";
                return false;
            }

            return true;
        }

        if (mode == WindowToolMode.Sill)
        {
            if (!CanAttachSillToFrame())
            {
                StatusText = "Сначала создайте коробку (рама)";
                return false;
            }

            return SillRenderer.TryHitBottomFrameProfile(this, point);
        }

        return false;
    }

    private bool ValidateTriangleOpeningClick(FrameNode? leaf, CadPoint point)
    {
        if (FrameRoot?.Shape != ConstructionShape.Triangle || leaf?.Parent is not null)
        {
            return true;
        }

        if (FrameTriangleGeometry.ContainsPointInInnerTriangle(FrameRoot, FrameThickness, point.X, point.Y))
        {
            return true;
        }

        StatusText = "Кликните внутри проёма";
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

        if (mode == WindowToolMode.MosquitoNet && leaf is not null)
        {
            return TryPlaceMosquitoNetInLeaf(leaf);
        }

        if (mode == WindowToolMode.CorrugationVertical && leaf is not null)
        {
            return TryPlaceCorrugationInLeaf(leaf, GlazingType.VerticalCorrugation);
        }

        if (mode == WindowToolMode.CorrugationHorizontal && leaf is not null)
        {
            return TryPlaceCorrugationInLeaf(leaf, GlazingType.HorizontalCorrugation);
        }

        if (mode == WindowToolMode.CorrugationSlanted && leaf is not null)
        {
            return TryPlaceCorrugationInLeaf(leaf, GlazingType.SlantedCorrugation);
        }

        if (mode == WindowToolMode.Sill)
        {
            return false;
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
        if (IsFakeImpostMode(mode) && FrameRoot?.IsFictitiousContainer == true)
        {
            return 0;
        }

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
        if (FrameRoot.IsFictitiousContainer)
        {
            // Держим DummyFrame синхронным с FrameRoot, иначе размерные цепочки падают в fallback «только габарит».
            SyncFrameGeometryToDocument();
        }

        SyncAllNestedFrameElements();
        SyncFillingsToFrameLayout();
        RebuildGridLines();
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

        if (IsDragCreateShapeMode(CurrentToolMode))
        {
            CaptureNewConstructionOptionsFromPanel();
            return;
        }

        if (HasFakeFrameOnCanvas && FrameRoot is not null)
        {
            ApplyFictitiousContainerGeometry(rect);
            return;
        }

        if (FrameRoot is not null)
        {
            ApplyRealFrameGeometryFromPanel(rect);
            return;
        }

        try
        {
            var width = Length;
            var height = Height;
            if (TryGetPrimaryElement(out var element) &&
                element is not null &&
                element.Kind == GeometryElementKind.RoundDummyFrame)
            {
                height = width;
                if (!_isSyncingSelection)
                {
                    _isSyncingSelection = true;
                    Height = height;
                    _isSyncingSelection = false;
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
            if (TryGetPrimaryElement(out var primaryElement) &&
                primaryElement is not null &&
                FrameRoot is not null)
            {
                ApplyRootFrameInitialization(FrameRoot, WindowToolMode.RealFrame, primaryElement.Kind);
            }

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

    private void ApplyRealFrameGeometryFromPanel(CadRect rect)
    {
        if (FrameRoot is null)
        {
            return;
        }

        try
        {
            var width = Length;
            var height = Height;
            if (TryGetPrimaryElement(out var element) &&
                element is not null &&
                element.Kind == GeometryElementKind.RoundDummyFrame)
            {
                height = width;
                if (!_isSyncingSelection)
                {
                    _isSyncingSelection = true;
                    Height = height;
                    _isSyncingSelection = false;
                }
            }

            if (width <= 0 || height <= 0)
            {
                CadDiagnostics.LogWarning("Skip coordinate apply: invalid dimensions");
                return;
            }

            var originX = SnapToGrid(CoordinateX);
            var originY = SnapToGrid(CoordinateY);
            var deltaX = originX - FrameRoot.X;
            var deltaY = originY - FrameRoot.Y;
            var sizeChanged = Math.Abs(width - FrameRoot.Width) > CadMath.Epsilon
                              || Math.Abs(height - FrameRoot.Height) > CadMath.Epsilon;

            if (sizeChanged && !FrameRoot.TryResizeOuter(width, height, FrameThickness))
            {
                return;
            }

            if (Math.Abs(deltaX) > CadMath.Epsilon || Math.Abs(deltaY) > CadMath.Epsilon)
            {
                FrameRoot.TranslateSubtree(deltaX, deltaY);
            }

            var updated = new CadRect(originX, originY + height, width, height);
            Document.RemoveRectangle(rect);
            Document.AddRectangle(updated);
            SelectedRectangle = updated;
            if (TryGetPrimaryElement(out var archElement) &&
                archElement is not null &&
                archElement.Kind is GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame)
            {
                ArchFrameDrawingHelper.EnsureFrameRootArcProfile(FrameRoot, width);
            }
            else if (TryGetPrimaryElement(out var rectElement) &&
                     rectElement is not null &&
                     rectElement.Kind == GeometryElementKind.Frame)
            {
                EnsureRectangularRootProfile(FrameRoot);
            }

            SelectedElement = FrameRoot;
            UpdatePrimaryElementGeometry(updated);
            NotifyFrameSelectionProperties();
            RecalculateFrameGeometry();
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("ApplyRealFrameGeometryFromPanel failed", ex);
        }
    }

    private void ApplyFictitiousContainerGeometry(CadRect rect)
    {
        if (FrameRoot is null)
        {
            return;
        }

        try
        {
            var width = Length;
            var height = Height;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            var originX = SnapToGrid(CoordinateX);
            var originY = SnapToGrid(CoordinateY);
            var deltaX = originX - FrameRoot.X;
            var deltaY = originY - FrameRoot.Y;
            var sizeChanged = Math.Abs(width - FrameRoot.Width) > CadMath.Epsilon
                              || Math.Abs(height - FrameRoot.Height) > CadMath.Epsilon;

            if (sizeChanged)
            {
                if (HasNestedBillableFramesInContainer())
                {
                    StatusText = "Размер контейнера изменён; вложенные коробки сохраняют прежний размер";
                }

                if (!FrameRoot.TryResizeOuter(width, height, 0))
                {
                    return;
                }
            }

            if (Math.Abs(deltaX) > CadMath.Epsilon || Math.Abs(deltaY) > CadMath.Epsilon)
            {
                FrameRoot.TranslateSubtree(deltaX, deltaY);
                TranslateNestedCanvasElements(deltaX, deltaY);
            }

            var updated = new CadRect(originX, originY + height, width, height);
            Document.RemoveRectangle(rect);
            Document.AddRectangle(updated);
            SelectedRectangle = updated;
            FrameRoot.IsFictitiousContainer = true;
            SelectedElement = FrameRoot;
            UpdatePrimaryElementGeometry(updated);
            NotifyFrameSelectionProperties();
            RequestSceneRefresh();
        }
        catch (Exception ex)
        {
            CadDiagnostics.LogWarning("ApplyFictitiousContainerGeometry failed", ex);
        }
    }

    private void TranslateNestedCanvasElements(double deltaX, double deltaY)
    {
        foreach (var element in CanvasElements.Skip(1))
        {
            element.X = (decimal)((double)element.X + deltaX);
            element.Y = (decimal)((double)element.Y + deltaY);
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

        EnsureArchFrameTopProfileForImpost();

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
        ImpostTarget target,
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
        ImpostTarget target,
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
            var edgeInsets = node.ResolveEdgeLightOpeningInsets();
            var innerLeft = node.X + edgeInsets.Left;
            var innerRight = node.X + node.Width - edgeInsets.Right;
            ImpostPanelL = RoundMm((axisWorldX - half) - innerLeft);
            ImpostPanelH = RoundMm(innerRight - (axisWorldX + half));
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
            var edgeInsets = node.ResolveEdgeLightOpeningInsets();
            var innerBottom = node.Y + edgeInsets.Bottom;
            var innerTop = extentTop - edgeInsets.Top;
            ImpostPanelL = RoundMm((axisWorldY - half) - innerBottom);
            ImpostPanelH = RoundMm(innerTop - (axisWorldY + half));
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

    /// <summary>Единый HitTest по GridLine (одинаковый для вертикали и горизонтали).</summary>
    public ImpostTarget? HitTestGridLine(CadPoint worldPoint, GridLineOrientation? orientationFilter = null)
    {
        var tolerance = ResolveVerticalImpostHitToleranceMm();
        var hit = GridLineHitTest.HitTest(this, worldPoint, tolerance);
        if (hit is null)
        {
            return null;
        }

        if (orientationFilter is { } filter && hit.Orientation != filter)
        {
            // Среди кандидатов той же ориентации выбрать ближайший.
            RebuildGridLines();
            GridLine? best = null;
            var bestDistance = double.MaxValue;
            foreach (var line in GridLines.OfOrientation(filter))
            {
                var d = AxisMath.DistanceToLine(line, worldPoint.X, worldPoint.Y);
                if (d > tolerance || d >= bestDistance)
                {
                    continue;
                }

                bestDistance = d;
                best = line;
            }

            hit = best;
            if (hit is null)
            {
                return null;
            }
        }

        return GridLineHitTest.ToImpostTarget(this, hit);
    }

    private ImpostTarget? HitTestVerticalImpost(CadPoint worldPoint) =>
        HitTestGridLine(worldPoint, GridLineOrientation.Vertical);

    private ImpostTarget? HitTestHorizontalImpost(CadPoint worldPoint) =>
        HitTestGridLine(worldPoint, GridLineOrientation.Horizontal);

    private bool IsPointNearVerticalImpost(
        FrameNode node,
        int gapIndex,
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

        if (CurvedContainerImpostGeometry.IsRoundContainer(this, FrameRoot))
        {
            return ImpostProfileHitTest.ContainsPoint(this, node, gapIndex, point);
        }

        return point.Y >= node.Y - tolerance && point.Y <= node.Y + node.Height + tolerance;
    }

    private bool IsPointNearVerticalImpost(
        FrameNode node,
        double axisX,
        CadPoint point,
        double tolerance,
        out double distance) =>
        IsPointNearVerticalImpost(node, 0, axisX, point, tolerance, out distance);

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

        // Призрак сбрасываем ДО записи модели, чтобы ни один кадр не смешал
        // старый ghost.HingeSide с новым OpeningType створки.
        ClearHardwarePreview();

        RecordPlacement("Фурнитура", () =>
        {
            var next = ResolveNextHardwareState(sash);
            if (next is { } planned)
            {
                // Один атомарный шаг: OpeningType + HingeSide + HasHardware.
                var applied = ApplySashOpeningAccessories(sash, planned.Opening, planned.Hinge);
                StatusText = applied.OpeningType switch
                {
                    SashOpeningType.TiltTurn when applied.HingeSide == SashHingeSide.Left =>
                        "Поворотно-откидное: петли слева, фурнитура вверх",
                    SashOpeningType.TiltTurn =>
                        "Поворотно-откидное: петли справа, фурнитура вверх",
                    SashOpeningType.RightTurn => "Петли справа (поворотное правое)",
                    SashOpeningType.LeftTurn => "Петли слева (поворотное левое)",
                    SashOpeningType.Tilt => "Фрамуга: петли сбоку, фурнитура вверх (без ручки)",
                    _ => "Фурнитура установлена"
                };
            }
            else
            {
                ClearSashOpeningAccessories(sash);
                StatusText = "Петли сняты";
            }

            return true;
        });

        // Следующий ghost строится уже от обновлённой створки (единый Resolve).
        UpdateHardwarePreview(worldPoint);
        NotifySashOpeningVisualChanged();
        return true;
    }

    /// <summary>
    /// Уведомляет UI о смене открывания/петель на UI-потоке после полного пересчёта.
    /// </summary>
    private void NotifySashOpeningVisualChanged()
    {
        void notify()
        {
            OnPropertyChanged(nameof(SashHardwarePreview));
            OnPropertyChanged(nameof(SashDragPreview));
            OnPropertyChanged(nameof(HandlePreviewSashId));
            RequestSceneRefresh();
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            notify();
            return;
        }

        Dispatcher.UIThread.Post(notify, DispatcherPriority.Render);
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

        if (_activeToolKey == CadCreationToolCatalog.SashFixed)
        {
            return false;
        }

        return CurrentToolMode switch
        {
            WindowToolMode.Sash or WindowToolMode.SlidingSash => BeginSashDrag(worldPoint),
            _ => false
        };
    }

    public void UpdatePointerInteraction(CadPoint worldPoint)
    {
        if ((CurrentToolMode == WindowToolMode.Sash || CurrentToolMode == WindowToolMode.SlidingSash) &&
            _isCanvasDragging &&
            _interactionStartWorld is not null &&
            _interactionLeaf is not null)
        {
            ClearHardwarePreview();
            var start = _interactionStartWorld.Value;
            var deltaX = worldPoint.X - start.X;
            var deltaY = worldPoint.Y - start.Y;
            if (Math.Abs(deltaX) >= SashDragMinDeltaMm || Math.Abs(deltaY) >= SashDragMinDeltaMm)
            {
                _sashDragGestureHingeHint = ResolveHingeSide(deltaX, deltaY);
            }

            UpdateSashPreview(_interactionLeaf, _sashDragGestureHingeHint);
            return;
        }

        if (CurrentToolMode == WindowToolMode.Hardware)
        {
            UpdateHardwarePreview(worldPoint);
            return;
        }

        ClearHardwarePreview();

        if (CurrentToolMode == WindowToolMode.Handle)
        {
            var sash = FindSashAt(worldPoint);
            HandlePreviewSashId = sash?.Id;
            OnPropertyChanged(nameof(HandlePreviewSashId));
        }
    }

    private void UpdateHardwarePreview(CadPoint worldPoint)
    {
        var sash = FindSashAt(worldPoint);
        if (sash is null)
        {
            ClearHardwarePreview();
            return;
        }

        var inner = SashGeometry.TryResolveInnerBounds(this, sash, out var liveInner)
            ? liveInner
            : SashGeometry.InnerOpeningBoundsFromElement(sash);
        if (inner.Width <= 0 || inner.Height <= 0)
        {
            ClearHardwarePreview();
            return;
        }

        var next = ResolveNextHardwareState(sash);
        // Ghost не хранит свою «сторону»: HingeSide всегда из единого Resolve.
        var visual = next is { } planned
            ? SashOpeningState.Resolve(planned.Opening, planned.Hinge)
            : SashOpeningState.Resolve(sash);
        var preview = new SashHardwareDragPreview
        {
            SashId = sash.Id,
            OriginX = inner.Left,
            OriginY = inner.Bottom,
            Width = inner.Width,
            Height = inner.Height,
            OpeningType = visual.OpeningType,
            HingeSide = visual.GhostHingeSide,
            WillClear = next is null
        };

        if (SashHardwarePreview is { } current
            && current.SashId == preview.SashId
            && current.OpeningType == preview.OpeningType
            && current.HingeSide == preview.HingeSide
            && current.WillClear == preview.WillClear
            && Math.Abs(current.OriginX - preview.OriginX) < 0.01
            && Math.Abs(current.OriginY - preview.OriginY) < 0.01
            && Math.Abs(current.Width - preview.Width) < 0.01
            && Math.Abs(current.Height - preview.Height) < 0.01)
        {
            return;
        }

        SashHardwarePreview = preview;
        OnPropertyChanged(nameof(SashHardwarePreview));
        RequestSceneRefresh();
    }

    private void ClearHardwarePreview()
    {
        if (SashHardwarePreview is null)
        {
            return;
        }

        SashHardwarePreview = null;
        OnPropertyChanged(nameof(SashHardwarePreview));
    }

    /// <summary>
    /// Следующее состояние цикла фурнитуры (для ghost и клика).
    /// Возвращает planned Opening + preferred hinge; итоговая сторона — только через
    /// <see cref="SashOpeningState.Resolve"/> при Apply / ghost.
    /// </summary>
    private static (SashOpeningType Opening, SashHingeSide Hinge)? ResolveNextHardwareState(GeometryElement sash)
    {
        if (!sash.HasHardware)
        {
            // Правое открывание: основание треугольника и петли справа.
            return (SashOpeningType.TiltTurn, SashHingeSide.Right);
        }

        var current = SashOpeningState.Resolve(sash);
        var opening = current.OpeningType;
        var sideHinge = current.HingeSide;

        // Устаревшее LeftTurn → TiltTurn слева (петли у основания треугольника).
        if (opening is SashOpeningType.LeftTurn)
        {
            return (SashOpeningType.TiltTurn, SashHingeSide.Left);
        }

        // Устаревшее RightTurn → TiltTurn справа.
        if (opening is SashOpeningType.RightTurn)
        {
            return (SashOpeningType.TiltTurn, SashHingeSide.Right);
        }

        // TiltTurn справа → TiltTurn слева
        if (opening is SashOpeningType.TiltTurn && sideHinge == SashHingeSide.Right)
        {
            return (SashOpeningType.TiltTurn, SashHingeSide.Left);
        }

        // TiltTurn слева → фрамуга (петли остаются слева)
        if (opening is SashOpeningType.TiltTurn && sideHinge == SashHingeSide.Left)
        {
            return (SashOpeningType.Tilt, SashHingeSide.Left);
        }

        // Tilt / прочее → снятие
        return null;
    }

    public bool TryEndPointerInteraction(CadPoint worldPoint)
    {
        try
        {
            return CurrentToolMode switch
            {
                WindowToolMode.Sash or WindowToolMode.SlidingSash => EndSashDrag(worldPoint),
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

    /// <summary>Есть несущий хост: обычная рама или переходной профиль.</summary>
    public bool HasConstructionHostOnCanvas =>
        ConstructionHostResolver.HasConstructionHostOnCanvas(this);

    private bool BeginSashDrag(CadPoint worldPoint)
    {
        if (FrameRoot is null || !HasConstructionHostOnCanvas)
        {
            ConstructionHostResolver.LogSashPlacementDiagnostics(
                this, null, "begin", "нет Host Frame на холсте");
            StatusText = "Сначала создайте коробку или переходной профиль";
            return false;
        }

        var leaf = SashGeometry.ResolveSashPlacementLeafAtPoint(this, worldPoint.X, worldPoint.Y);
        if (leaf is null)
        {
            ConstructionHostResolver.LogSashPlacementDiagnostics(
                this, leaf, "begin", "ячейка не найдена");
            StatusText = "Кликните внутри свободной ячейки";
            return false;
        }

        if (!ValidateTriangleOpeningClick(leaf, worldPoint))
        {
            ConstructionHostResolver.LogSashPlacementDiagnostics(
                this, leaf, "begin", "точка вне треугольного проёма");
            return false;
        }

        if (!IsBillableConstructionLeaf(leaf))
        {
            ConstructionHostResolver.LogSashPlacementDiagnostics(
                this, leaf, "begin", "Host does not support sash");
            StatusText = "Створка доступна только внутри несущей рамы или переходного профиля";
            return false;
        }

        if (FindSashInLeaf(leaf) is not null)
        {
            ConstructionHostResolver.LogSashPlacementDiagnostics(
                this, leaf, "begin", "ячейка уже содержит створку");
            StatusText = "Ячейка уже содержит створку";
            return false;
        }

        ConstructionHostResolver.LogSashPlacementDiagnostics(this, leaf, "begin-ok");
        _interactionStartWorld = worldPoint;
        _interactionLeaf = leaf;
        _isCanvasDragging = true;
        _sashDragGestureHingeHint = SashHingeSide.Left;
        UpdateSashPreview(leaf, _sashDragGestureHingeHint);
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

        var sash = CreateSashElementForTool(
            leaf,
            ResolveHingeSide(deltaX, deltaY),
            deltaX,
            start.X,
            start.Y);
        if (sash is null)
        {
            ConstructionHostResolver.LogSashPlacementDiagnostics(
                this, leaf, "create", "SashBuilder вернул null (проём слишком мал?)");
            StatusText = "Не удалось создать створку";
            return false;
        }

        // Жёсткая синхронизация с ghost-превью: габариты и OpeningType+HingeSide как на экране.
        if (SashDragPreview is { } preview)
        {
            sash.X = (decimal)(preview.OriginX + preview.Width / 2.0);
            sash.Y = (decimal)(preview.OriginY + preview.Height / 2.0);
            sash.WidthMm = (decimal)preview.Width;
            sash.HeightMm = (decimal)preview.Height;
            CadCreationToolCatalog.SyncSashOpeningAndHinge(
                sash,
                preview.OpeningType,
                preview.HingeSide);
        }
        else
        {
            var hinge = ResolveHingeSide(deltaX, deltaY);
            var opening = CadCreationToolCatalog.ResolveOpeningType(_activeToolKey, hinge, deltaX);
            SashOpeningState.ApplyOpeningTypeChange(sash, opening, hinge);
        }

        sash.HostFrameNodeId = leaf.NodeId;
        ConstructionHostResolver.LogSashPlacementDiagnostics(this, leaf, "create-ok");

        RecordPlacement($"{sash.Label} создана", () =>
        {
            AddCanvasElement(sash);
            ConstructionHostResolver.LogSashPlacementDiagnostics(this, leaf, "added-to-tree");
            return true;
        });
        StatusText = $"{sash.Label} создана ({DescribeOpeningType(sash.OpeningType)})";
        NotifySashOpeningVisualChanged();
        return true;
    }

    private void ClearInteractionState()
    {
        _isCanvasDragging = false;
        _interactionStartWorld = null;
        _interactionLeaf = null;
        _sashDragGestureHingeHint = SashHingeSide.Left;
        SashDragPreview = null;
        HandlePreviewSashId = null;
        ClearHardwarePreview();
        OnPropertyChanged(nameof(SashDragPreview));
        OnPropertyChanged(nameof(HandlePreviewSashId));
    }

    private static SashHingeSide ResolveHingeSide(double deltaX, double deltaY)
    {
        if (Math.Abs(deltaX) >= Math.Abs(deltaY))
        {
            // Жест влево → левое открывание (петли слева); вправо → петли справа.
            return deltaX < 0 ? SashHingeSide.Left : SashHingeSide.Right;
        }

        return deltaY < 0 ? SashHingeSide.Bottom : SashHingeSide.Top;
    }

    private (double X, double Y, double Width, double Height) ResolveSashOpeningBounds(
        FrameNode leaf,
        double? probeX = null,
        double? probeY = null)
    {
        if (!leaf.IsTerminalLeaf)
        {
            return (0, 0, 0, 0);
        }

        var worldX = probeX ?? leaf.X + leaf.Width / 2.0;
        var worldY = probeY ?? leaf.Y + leaf.Height / 2.0;
        return SashGeometry.ResolveSashPlacementBounds(this, leaf, worldX, worldY);
    }

    private (double X, double Y, double Width, double Height) ResolveSashOpeningBounds(FrameNode leaf) =>
        ResolveSashOpeningBounds(leaf, null, null);

    private void UpdateSashPreview(FrameNode leaf, SashHingeSide gestureHingeHint)
    {
        var probeX = _interactionStartWorld?.X ?? leaf.X + leaf.Width / 2.0;
        var probeY = _interactionStartWorld?.Y ?? leaf.Y + leaf.Height / 2.0;
        var (innerX, innerY, innerW, innerH) = ResolveSashOpeningBounds(leaf, probeX, probeY);
        if (innerW <= 0 || innerH <= 0)
        {
            SashDragPreview = null;
            OnPropertyChanged(nameof(SashDragPreview));
            return;
        }

        var normalizedHinge = CadCreationToolCatalog.NormalizeHingeForTool(_activeToolKey, gestureHingeHint);
        var openingType = CadCreationToolCatalog.ResolveOpeningType(_activeToolKey, normalizedHinge, 0);
        var visual = SashOpeningState.Resolve(openingType, normalizedHinge);

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
            HingeSide = visual.HingeSide,
            OpeningType = visual.OpeningType
        };
        OnPropertyChanged(nameof(SashDragPreview));
    }

    private static string DescribeOpeningType(SashOpeningType? opening) => opening switch
    {
        SashOpeningType.LeftTurn => "поворотное левое",
        SashOpeningType.RightTurn => "поворотное правое",
        SashOpeningType.Turn => "поворотное",
        SashOpeningType.Tilt => "откидное",
        SashOpeningType.TiltTurn => "поворотно-откидное",
        SashOpeningType.Fixed => "глухое",
        SashOpeningType.Suspended => "подвесное",
        SashOpeningType.Folding => "складное",
        SashOpeningType.SlideLeft => "раздвижное влево",
        SashOpeningType.SlideRight => "раздвижное вправо",
        _ => "профиль"
    };

    private GeometryElement? CreateSashElement(FrameNode leaf, SashHingeSide hingeSide)
    {
        if (CadCreationToolCatalog.IsSashVariantTool(_activeToolKey))
        {
            return CreateSashElementForTool(leaf, hingeSide, 0);
        }

        var (innerX, innerY, innerW, innerH) = ResolveSashOpeningBounds(leaf);
        if (innerW <= 40 || innerH <= 40)
        {
            return null;
        }

        var visual = SashOpeningState.Resolve(
            CadCreationToolCatalog.ResolveSideTurnOpening(hingeSide),
            hingeSide);
        return new GeometryElement
        {
            Kind = GeometryElementKind.Sash,
            Label = "Створка",
            X = (decimal)(innerX + innerW / 2.0),
            Y = (decimal)(innerY + innerH / 2.0),
            WidthMm = (decimal)innerW,
            HeightMm = (decimal)innerH,
            OpeningType = visual.OpeningType,
            HingeSide = visual.HingeSide,
            HasHardware = false,
            HasHandle = false,
            HostFrameNodeId = leaf.NodeId
        };
    }

    private string ResolveSashToolStatus()
    {
        if (FrameRoot is null || !HasConstructionHostOnCanvas)
        {
            return "Сначала создайте коробку или переходной профиль";
        }

        if (_activeToolKey == CadCreationToolCatalog.SashFixed)
        {
            return "Створка глухая: кликните внутри свободной ячейки";
        }

        return CadCreationToolCatalog.RequiresSashDrag(_activeToolKey)
            ? $"{CadCreationToolCatalog.ResolveSashLabel(_activeToolKey)}: зажмите ЛКМ и укажите направление"
            : "Створка: зажмите ЛКМ и укажите направление открывания";
    }

    private GeometryElement? FindSashAt(CadPoint point) =>
        CanvasElements.FirstOrDefault(e =>
            e.Kind is GeometryElementKind.Sash or GeometryElementKind.SlidingSash
            && ContainsElement(e, point));

    private GeometryElement? FindSashInLeaf(FrameNode leaf)
    {
        GeometryElement? best = null;
        var bestOverlap = 0.0;

        foreach (var element in CanvasElements)
        {
            if (element.Kind is not (GeometryElementKind.Sash or GeometryElementKind.SlidingSash))
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
                AddCanvasElement(glazing);
                return true;
            }))
        {
            return false;
        }

        StatusText = "Остекление установлено";
        RequestSceneRefresh();
        return true;
    }

    private GeometryElement? FindMosquitoNetOnSash(GeometryElement sash) =>
        CanvasElements.FirstOrDefault(
            element => element.Kind == GeometryElementKind.MosquitoNet && element.HostSashId == sash.Id);

    private bool TryPlaceMosquitoNetInLeaf(FrameNode leaf)
    {
        var sash = FindSashInLeaf(leaf);
        if (sash is null)
        {
            StatusText = "Москитная сетка может быть установлена только на створку";
            return false;
        }

        if (FindMosquitoNetOnSash(sash) is not null)
        {
            StatusText = "На этой створке уже установлена москитная сетка";
            return false;
        }

        var net = CreateMosquitoNetElement(sash, leaf);
        if (net is null)
        {
            StatusText = "Не удалось разместить москитную сетку";
            return false;
        }

        if (!RecordPlacement("Москитная сетка", () =>
            {
                sash!.HasMosquitoNet = true;
                AddCanvasElement(net);
                return true;
            }))
        {
            return false;
        }

        StatusText = "Москитная сетка установлена";
        RequestSceneRefresh();
        return true;
    }

    private GeometryElement? CreateMosquitoNetElement(GeometryElement sash, FrameNode leaf)
    {
        if (FrameRoot is null)
        {
            return null;
        }

        var glazing = CreateGlazingElementForSash(sash, leaf);
        if (glazing is null)
        {
            return new GeometryElement
            {
                Kind = GeometryElementKind.MosquitoNet,
                Label = "Москитная сетка",
                X = sash.X,
                Y = sash.Y,
                WidthMm = sash.WidthMm,
                HeightMm = sash.HeightMm,
                HostSashId = sash.Id,
                SourceToolId = "mosquito-net"
            };
        }

        return new GeometryElement
        {
            Kind = GeometryElementKind.MosquitoNet,
            Label = "Москитная сетка",
            X = glazing.X,
            Y = glazing.Y,
            WidthMm = glazing.WidthMm,
            HeightMm = glazing.HeightMm,
            HostSashId = sash.Id,
            SourceToolId = "mosquito-net"
        };
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

        var (innerX, innerY, innerW, innerH) = SashGeometry.ResolveLeafInnerOpening(this, leaf);
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
                ? SashGeometry.ResolveLeafInnerOpeningBounds(this, leaf)
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
        mode is WindowToolMode.Sash or WindowToolMode.SlidingSash;

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

        public bool IsDummy { get; init; }

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
            SyncExtremeImpostCanvasElements();
            SyncSillCanvasElement();
            if (FrameRoot?.HasSill == true)
            {
                RefreshSillMaterialOptions();
            }
            SyncFrameRootFictitiousFlag();

            AttachTopProfileWatcher();
            Commands.Clear();
            _placementHistory.Clear();
            NotifyPlacementHistoryChanged();
            _hasAppliedInitialViewport = true;
            _pendingInitialViewportFit = false;
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
        _pendingInitialViewportFit = true;
        _hasAppliedInitialViewport = false;
        if (Viewport.Width > CadMath.Epsilon && Viewport.Height > CadMath.Epsilon)
        {
            CenterViewportOnOrigin();
        }

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
        IsDummy = node.IsDummy,
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
        IsDummy = snapshot.IsDummy,
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
        var frameElement = elements.FirstOrDefault(e =>
            e.Kind is GeometryElementKind.Frame
                or GeometryElementKind.FictitiousFrame
                or GeometryElementKind.DummyFrame
                or GeometryElementKind.ArchDummyFrame
                or GeometryElementKind.TriangleDummyFrame
                or GeometryElementKind.RoundDummyFrame);
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
            height,
            isFictitiousContainer: FictitiousFrame.IsFictitiousKind(frameElement.Kind));
    }

    public void UpdateRadiusVertexHover(CadPoint worldPoint)
    {
        if (CurrentToolMode is not (WindowToolMode.RadiusVertex or WindowToolMode.ArcVertex))
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

        if (FrameRoot?.TopProfile is null)
        {
            return false;
        }

        segment = FrameRoot.TopProfile;
        StatusText = $"Верхний профиль преобразован в дугу: R={segment.Radius:F0} мм, H={segment.ArcHeight:F0} мм";
        RequestSceneRefresh();
        return true;
    }

    private void EnsureArchFrameTopProfileForImpost()
    {
        if (FrameRoot is null || !CanvasElements.Any(element =>
                element.Kind is GeometryElementKind.ArchFrame or GeometryElementKind.ArchDummyFrame))
        {
            return;
        }

        ArchFrameDrawingHelper.EnsureFrameRootArcProfile(FrameRoot, FrameRoot.Width);
        NotifyTopProfileUiProperties();
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
        if (CurrentToolMode is not (WindowToolMode.RadiusVertex or WindowToolMode.ArcVertex)
            || !HasArcTopProfile
            || FrameRoot is null)
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
        var springingHandle = profile.GetArcSpringingHandleWorld();
        var apexHandle = profile.GetArcApexWorld();

        if (Distance(worldPoint, springingHandle) <= tolerance)
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

        if (CurrentToolMode == WindowToolMode.ArcVertex
            && Distance(worldPoint, apexHandle) <= tolerance)
        {
            BeginConstructionDrag();
            _isArcVertexDragging = true;
            _arcVertexDragTarget = ArcVertexDragTarget.Apex;
            _arcHeightDragStartWorldY = worldPoint.Y;
            _arcHeightDragStartHeight = profile.ArcHeight;
            OnPropertyChanged(nameof(IsArcVertexDragging));
            StatusText = "Перетаскивание вершины дуги (H)";
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
            if (_arcVertexDragTarget == ArcVertexDragTarget.Springing)
            {
                var deltaY = worldPoint.Y - _arcHeightDragStartWorldY;
                var newHeight = profile.ClampArcHeight(_arcHeightDragStartHeight - deltaY);
                profile.ArcHeight = newHeight;
                StatusText = $"H={profile.ArcHeight:F0} мм, R={profile.Radius:F0} мм";
            }
            else if (_arcVertexDragTarget == ArcVertexDragTarget.Apex)
            {
                var chordY = (profile.StartY + profile.EndY) / 2.0;
                profile.ArcHeight = profile.ClampArcHeight(worldPoint.Y - chordY);
                StatusText = $"H={profile.ArcHeight:F0} мм, R={profile.Radius:F0} мм";
            }
            else
            {
                return;
            }
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

        CommitConstructionDrag(_arcVertexDragTarget == ArcVertexDragTarget.Apex
            ? "Изменение вершины дуги"
            : "Изменение стрелы прогиба");
        SyncArchFrameElementAfterProfileChange();
        RecalculateFrameGeometry();

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
    Springing,
    Apex
}
