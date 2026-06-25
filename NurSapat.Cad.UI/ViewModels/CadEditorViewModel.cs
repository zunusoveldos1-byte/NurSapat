using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
using NurSapat.Core.Models.Orders;
using System.Collections.ObjectModel;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadEditorViewModel : ObservableObject
{
    private readonly CadToolFactory _toolFactory = new();
    private readonly ICadDialogService _dialogService;
    private readonly CadTemplateStore _templateStore;
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

    [ObservableProperty]
    private string _activeToolId = "select";

    [ObservableProperty]
    private ICadTool _activeTool;

    [ObservableProperty]
    private WindowToolMode _currentToolMode = WindowToolMode.RealFrame;

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
    private CadRect? _selectedRectangle;

    [ObservableProperty]
    private string? _selectedProfileSeries = "ПВХ 70мм";

    [ObservableProperty]
    private ConstructionItemViewModel? _selectedConstruction;

    [ObservableProperty]
    private bool _showAllConstructions;

    [ObservableProperty]
    private bool _blockTilt;

    [ObservableProperty]
    private bool _microVentilation;

    [ObservableProperty]
    private bool _balconyKit;

    [ObservableProperty]
    private bool _microLift;

    [ObservableProperty]
    private int _constructionQuantity = 1;

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

    public IRelayCommand KonAddExecuteCommand { get; }

    public IRelayCommand KonDelExecuteCommand { get; }

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
        ActivateToolCommand = new RelayCommand<string>(ActivateCadTool);
        SelectToolModeCommand = new RelayCommand<WindowToolMode>(mode => _ = SelectToolModeAsync(mode));
        KonAddExecuteCommand = new RelayCommand(KonAddExecute);
        KonDelExecuteCommand = new RelayCommand(KonDelExecute);

        InitializeDesignTools();
        EnsureDefaultConstructions();
        ActiveTool = _toolFactory.Create("select");
        SetActiveToolVisual(WindowToolMode.RealFrame);
        Viewport.ResetToDefaultScale(15.0);
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

    public double GridSnapStep => Viewport.ChooseGridStep();

    public void SetDesignElements(IReadOnlyList<GeometryElement> elements)
    {
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

    public void FitDocumentToViewport()
    {
        Viewport.FitToBounds(GetWorldBounds());
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));
        RequestSceneRefresh();
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
        SelectTarget = SelectConstructionTarget
    };

    private void ActivateCadTool(string id)
    {
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
        if (!Commands.CanUndo)
        {
            return false;
        }

        Commands.Undo();
        RefreshSelection();
        StatusText = "Действие отменено";
        return true;
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

    private bool TryDeleteLastPlacedFromTool()
    {
        if (TryRemoveLastFrameImpost())
        {
            return true;
        }

        if (TryRemoveLastMullionElement())
        {
            return true;
        }

        StatusText = "Нет импостов для удаления";
        return false;
    }

    private bool TryRemoveLastFrameImpost()
    {
        if (FrameRoot is null)
        {
            return false;
        }

        while (_impostSplitHistory.Count > 0)
        {
            var node = _impostSplitHistory[^1];
            _impostSplitHistory.RemoveAt(_impostSplitHistory.Count - 1);

            if (node.IsLeaf)
            {
                continue;
            }

            if (node.TryRemoveImpostSplit())
            {
                FinishImpostRemoval();
                return true;
            }
        }

        var fallback = FrameNode.FindDeepestInternalNode(FrameRoot);
        if (fallback?.TryRemoveImpostSplit() == true)
        {
            FinishImpostRemoval();
            return true;
        }

        return false;
    }

    private bool TryRemoveLastMullionElement()
    {
        for (var i = CanvasElements.Count - 1; i >= 0; i--)
        {
            var element = CanvasElements[i];
            if (element.Kind is GeometryElementKind.Mullion
                or GeometryElementKind.DummyMullion
                or GeometryElementKind.EdgeMullion)
            {
                CanvasElements.RemoveAt(i);
                StatusText = "Последний импост удалён";
                RequestSceneRefresh();
                return true;
            }
        }

        return false;
    }

    private void FinishImpostRemoval()
    {
        SelectedElement = FrameRoot;
        OnPropertyChanged(nameof(FrameRoot));
        OnPropertyChanged(nameof(SelectedElement));
        OnPropertyChanged(nameof(SelectedElementLength));
        OnPropertyChanged(nameof(SelectedElementHeight));
        StatusText = "Последний импост удалён";
        RequestSceneRefresh();
    }

    private void TrackImpostSplit(FrameNode splitNode)
    {
        if (!splitNode.IsLeaf)
        {
            _impostSplitHistory.Add(splitNode);
        }
    }

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
            StatusText = "Невозможно установить импост в этой ячейке";
            return false;
        }

        TrackImpostSplit(leaf);
        SelectedElement = FrameRoot;
        OnPropertyChanged(nameof(FrameRoot));
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
            return CommitRealFrameAt(originX, originY, width, height);
        }

        return CommitFakeShapeAt(mode, originX, originY, width, height);
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
            if (ExecuteSlantedImpostSplit(worldPoint, leaf!))
            {
                TrackImpostSplit(leaf!);
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
                leaf.Parent == null ? FrameThickness : 0))
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

    partial void OnCurrentToolModeChanged(WindowToolMode value) =>
        OnPropertyChanged(nameof(IsDragCreateShapeToolActive));

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
    private void DeleteSelected()
    {
        if (SelectedRectangle == null)
        {
            return;
        }

        Document.RemoveRectangle(SelectedRectangle.Value);
        SelectedRectangle = null;
        RefreshSelection();
        RequestSceneRefresh();
        StatusText = "Выбранный объект удален";
    }

    [RelayCommand]
    private void Undo()
    {
        Commands.Undo();
        RefreshSelection();
        RequestSceneRefresh();
        StatusText = "Отменить";
    }

    [RelayCommand]
    private void Redo()
    {
        Commands.Redo();
        RefreshSelection();
        RequestSceneRefresh();
        StatusText = "Повторить";
    }

    [RelayCommand]
    private void FitAll()
    {
        FitDocumentToViewport();
        StatusText = "Показать всё";
    }

    [RelayCommand]
    private async Task OpenTemplatesAsync()
    {
        var template = await _dialogService.ShowTemplatePickerAsync(_templateStore, this);
        if (template == null)
        {
            return;
        }

        ApplyDrawingTemplate(template);
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

    private void ClearDrawing()
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

        // БЛОК 1: ФИКТИВНЫЕ КОНТУРЫ И ОСИ
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeFrame, "Коробка (фиктивная)", DesignToolIcon.FakeFrame));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeFrameCircle, "Коробка фиктивная круглая", DesignToolIcon.FakeCircle));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeFrameTriangle, "Коробка фиктивная треугольная", DesignToolIcon.FakeTriangle));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeFrameArch, "Полукруг (фиктивная)", DesignToolIcon.FakeArch));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeVerticalImpost, "Импост верт.", DesignToolIcon.VerticalImpost));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.FakeHorizontalImpost, "Импост гориз.", DesignToolIcon.HorizontalImpost));
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.RackRigelNone, "Стойка/ригель", DesignToolIcon.RackRigel));

        // РАЗДЕЛИТЕЛЬ ПАНЕЛИ
        DesignTools.Add(new CadDesignToolButtonViewModel(WindowToolMode.Separator, "--- РЕАЛЬНЫЕ ПРОФИЛИ ---", isSeparator: true));

        // БЛОК 2: РЕАЛЬНЫЕ КОНСТРУКТИВНЫЕ ЭЛЕМЕНТЫ
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
    }

    private void SetActiveToolVisual(WindowToolMode mode)
    {
        CurrentToolMode = mode;
        foreach (var tool in DesignTools)
        {
            tool.IsActive = !tool.IsSeparator && tool.Mode == mode;
        }

        var active = DesignTools.FirstOrDefault(t => t.Mode == mode);
        ActiveDesignToolLabel = active?.Title;
        ActiveDesignToolId = mode.ToString();
        ActiveTool = _toolFactory.Create("select");
        ActiveToolId = "select";
        OnPropertyChanged(nameof(ActiveToolDisplayName));
        OnPropertyChanged(nameof(CanvasHintText));
        StatusText = active?.Title ?? "Инструмент";
    }

    private async Task SelectToolModeAsync(WindowToolMode mode)
    {
        if (_isRunningFramePipeline)
        {
            return;
        }

        if (mode is WindowToolMode.Other or WindowToolMode.Separator)
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
                WindowToolMode.Hardware => "Фурнитура: кликните по створке",
                WindowToolMode.Handle => "Ручка: кликните по створке",
                _ => DesignTools.FirstOrDefault(t => t.Mode == mode)?.Title is { Length: > 0 } title
                    ? $"Выбран инструмент {title}"
                    : "Инструмент"
            };
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
        var nextId = Constructions.Count + 1;
        var item = new ConstructionItemViewModel(nextId, $"Конструкция {nextId}");
        Constructions.Add(item);
        SelectedConstruction = item;
        StatusText = "Добавлена конструкция";
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
        var first = new ConstructionItemViewModel(1, "Конструкция 1");
        Constructions.Add(first);
        SelectedConstruction = first;
        Document.Name = "Новый чертеж";
    }

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
                    leaf.Parent == null ? FrameThickness : 0))
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

        return false;
    }

    private bool ExecuteToolPipeline(WindowToolMode mode, CadPoint worldPoint, FrameNode? leaf, bool splitAtHalf = false)
    {
        if (mode == WindowToolMode.RackRigelNone)
        {
            StatusText = "Стойка/ригель в крайнем положении: нет";
            return true;
        }

        return false;
    }

    private void ApplyUniformImpostSplit(FrameNode leaf, int impostCount, WindowToolMode mode)
    {
        var orientation = GetImpostOrientation(mode);
        var impostWidth = GetImpostProfileWidth(mode);
        var frameInset = leaf.Parent == null ? FrameThickness : 0;
        var lineStyle = GetImpostLineStyle(mode);

        if (!leaf.TrySplitUniformWithVolume(orientation, impostCount, impostWidth, frameInset, lineStyle))
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

    public bool TryBeginPointerInteraction(CadPoint worldPoint)
    {
        if (!IsInteractiveToolMode(CurrentToolMode))
        {
            return false;
        }

        return CurrentToolMode switch
        {
            WindowToolMode.Sash => BeginSashDrag(worldPoint),
            WindowToolMode.Handle => BeginHandleHover(worldPoint),
            WindowToolMode.Hardware => FindSashAt(worldPoint) != null,
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
                WindowToolMode.Hardware => PlaceHardwareOnSash(worldPoint),
                WindowToolMode.Handle => PlaceHandleOnSash(worldPoint),
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

    private bool BeginHandleHover(CadPoint worldPoint)
    {
        var sash = FindSashAt(worldPoint);
        HandlePreviewSashId = sash?.Id;
        OnPropertyChanged(nameof(HandlePreviewSashId));
        return sash is not null;
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

        CanvasElements.Add(sash);
        StatusText = "Створка создана";
        RequestSceneRefresh();
        return true;
    }

    private bool PlaceHardwareOnSash(CadPoint worldPoint)
    {
        var sash = FindSashAt(worldPoint);
        if (sash is null)
        {
            StatusText = "Кликните по активной створке";
            return false;
        }

        if (sash.HasHardware)
        {
            StatusText = "Фурнитура уже установлена";
            return false;
        }

        sash.HasHardware = true;
        StatusText = "Фурнитура установлена";
        RequestSceneRefresh();
        return true;
    }

    private bool PlaceHandleOnSash(CadPoint worldPoint)
    {
        var sash = FindSashAt(worldPoint);
        if (sash is null)
        {
            StatusText = "Кликните по створке";
            return false;
        }

        sash.HasHandle = true;
        StatusText = "Ручка установлена";
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
        mode is WindowToolMode.Sash or WindowToolMode.Hardware or WindowToolMode.Handle;

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
        Document.Name = snapshot.DocumentName;
        Viewport.ZoomFactor = snapshot.ViewportZoomFactor;
        Viewport.PanOffset = new CadVector(snapshot.ViewportPanX, snapshot.ViewportPanY);
        OnPropertyChanged(nameof(ZoomPercent));
        OnPropertyChanged(nameof(MillimetersPerPixel));

        SetDesignElements(elements);

        FrameRoot = snapshot.FrameRoot is null
            ? TryCreateFrameFromElements(elements)
            : RestoreFrameNode(snapshot.FrameRoot);

        Commands.Clear();
        RequestSceneRefresh();
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

    private static FrameNode? RestoreFrameNode(FrameNodeSnapshot snapshot)
    {
        var root = FrameNode.CreateRoot(snapshot.X, snapshot.Y, snapshot.Width, snapshot.Height);
        return root;
    }

    private static FrameNode? TryCreateFrameFromElements(IReadOnlyList<GeometryElement> elements)
    {
        var frameElement = elements.FirstOrDefault(e => e.Kind == GeometryElementKind.Frame);
        if (frameElement is null)
        {
            return null;
        }

        return FrameNode.CreateRoot(
            (double)frameElement.X,
            (double)frameElement.Y,
            (double)frameElement.WidthMm,
            (double)frameElement.HeightMm);
    }
}
