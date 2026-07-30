using Avalonia.Controls;
using Avalonia.Threading;
using NurSapat.App.Services;
using NurSapat.App.Services.Dictionaries;
using NurSapat.App.Services.Orders;
using NurSapat.App.Services.Users;
using NurSapat.App.Views.Dialogs;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;
using NurSapat.Core.Calculations;
using NurSapat.Core.Services.Orders;
using System.Linq;

namespace NurSapat.App.ViewModels;

/// <summary>
/// Общий контекст заказа: связывает чертёж, лист заказа и пересчёт.
/// </summary>
public sealed class OrderWorkspaceViewModel
{
    private readonly CadEditorViewModel _cadEditor;
    private readonly IOrderErpDialogService _orderErpDialogService;
    private readonly OrderLifecycleService _orderLifecycleService;
    private bool _isResettingCanvas;
    private bool _isAddingConstruction;
    private Guid? _loadedConstructionId;

    public Order Order { get; } = new();

    public OrderSheetViewModel Sheet { get; }

    public OrderDrawingViewModel Drawing { get; }

    public event EventHandler? ProjectModified;

    public OrderWorkspaceViewModel(
        CadEditorViewModel cadEditor,
        IOrderErpDialogService orderErpDialogService,
        OrderCalculationService calculationService,
        ICustomersStore customersStore,
        IManagersStore managersStore,
        IUserSessionService userSessionService,
        IOrderNumberService orderNumberService,
        IDictionaryWindowService dictionaryWindowService,
        OrderLifecycleService orderLifecycleService)
    {
        _cadEditor = cadEditor;
        _orderErpDialogService = orderErpDialogService;
        _orderLifecycleService = orderLifecycleService;
        _isResettingCanvas = true;

        Sheet = new OrderSheetViewModel(
            this,
            cadEditor,
            orderErpDialogService,
            calculationService,
            customersStore,
            managersStore,
            userSessionService,
            orderNumberService,
            dictionaryWindowService);
        Drawing = new OrderDrawingViewModel(this, cadEditor);
        Sheet.ProjectModified += (_, _) => ProjectModified?.Invoke(this, EventArgs.Empty);
        Drawing.ProjectModified += (_, _) => ProjectModified?.Invoke(this, EventArgs.Empty);

        cadEditor.CanvasHoverHintChanged = Sheet.UpdateCanvasHoverHint;

        WireConstructionHandlers();
        InitializeCadConstructions();
        _isResettingCanvas = false;
    }

    public void ApplyCadReadOnlyStatus()
    {
        _cadEditor.IsViewOnlyMode = OrderBusinessRules.IsCadReadOnly(Order.Header.Status);
    }

    public void RecalculateAll()
    {
        _orderLifecycleService.AssertCanEdit(Order);
        Sheet.RecalculateAll();
        if (!_isResettingCanvas && !_cadEditor.IsResettingCanvas)
        {
            RefreshConstructionListDisplay();
        }
    }

    public void NotifyModified() => ProjectModified?.Invoke(this, EventArgs.Empty);

    public void CommitActiveConstructionGeometry()
    {
        if (_isResettingCanvas || _cadEditor.IsResettingCanvas)
        {
            return;
        }

        _orderLifecycleService.AssertCanEdit(Order);

        var construction = GetCanvasConstruction();
        CaptureConstructionState(construction);

        if (_cadEditor.SelectedConstruction is { } item
            && item.OrderConstructionId == construction.Id)
        {
            construction.Name = item.Name;
            item.UpdateFrom(
                construction.Name,
                construction.Number,
                construction.DimensionsDisplay,
                construction.QuantityDisplay);
        }
    }

    public void RefreshConstructionListDisplay(Guid? activeConstructionId = null)
    {
        SyncConstructionListToCadEditor(activeConstructionId);
    }

    public Func<Task>? NavigateToCadTabHandler { get; set; }

    public async Task EditConstructionInDrawingAsync(Guid constructionId)
    {
        await SwitchToConstructionAsync(constructionId);
        if (NavigateToCadTabHandler is not null)
        {
            await NavigateToCadTabHandler();
        }
    }

    public bool IsAddingConstruction => _isAddingConstruction;

    public async Task DuplicateConstructionAsync(Guid sourceConstructionId)
    {
        _orderLifecycleService.AssertCanEdit(Order);
        if (_isResettingCanvas || _isAddingConstruction)
        {
            return;
        }

        if (!_cadEditor.IsViewOnlyMode)
        {
            CommitActiveConstructionGeometry();
        }

        try
        {
            _isAddingConstruction = true;
            _isResettingCanvas = true;

            var source = Order.Constructions.FirstOrDefault(c => c.Id == sourceConstructionId);
            if (source is null)
            {
                return;
            }

            var number = GetNextConstructionNumber();
            var copy = CloneConstruction(source, number);
            Order.Constructions.Add(copy);
            Order.SelectedConstruction = copy;

            LoadConstructionToCanvas(copy);
            ScheduleSyncConstructionListToCadEditor(copy.Id);
            Sheet.ReloadFromOrder(copy.Id);
            Sheet.RecalculateAll();
            NotifyModified();
        }
        finally
        {
            _isResettingCanvas = false;
            _isAddingConstruction = false;
        }

        await Task.CompletedTask;
    }

    public async Task AddNewConstructionAsync()
    {
        _orderLifecycleService.AssertCanEdit(Order);
        if (_isResettingCanvas || _isAddingConstruction)
        {
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            if (_isResettingCanvas || _isAddingConstruction)
            {
                return;
            }

            try
            {
                CommitActiveConstructionGeometry();
                _isAddingConstruction = true;
                _isResettingCanvas = true;

                var number = GetNextConstructionNumber();
                var construction = new OrderConstruction
                {
                    Number = number,
                    Name = $"Конструкция {number}"
                };
                Order.Constructions.Add(construction);
                LoadConstructionToCanvas(construction);
                ScheduleSyncConstructionListToCadEditor(construction.Id);
                Sheet.ReloadFromOrder(construction.Id);
                Sheet.RecalculateAll();
                NotifyModified();
            }
            finally
            {
                _isResettingCanvas = false;
                _isAddingConstruction = false;
            }
        });
    }

    public async Task SwitchToConstructionAsync(Guid constructionId)
    {
        if (_isResettingCanvas)
        {
            return;
        }

        if (Order.SelectedConstruction?.Id == constructionId && _loadedConstructionId == constructionId)
        {
            return;
        }

        if (!_cadEditor.IsViewOnlyMode)
        {
            CommitActiveConstructionGeometry();
        }

        try
        {
            _isResettingCanvas = true;

            var construction = Order.Constructions.FirstOrDefault(c => c.Id == constructionId);
            if (construction is null)
            {
                return;
            }

            LoadConstructionToCanvas(construction);
            SyncConstructionListToCadEditor(construction.Id);
            Sheet.ReloadFromOrder(construction.Id);
            Sheet.RecalculateAll();
            NotifyModified();
        }
        finally
        {
            _isResettingCanvas = false;
        }

        await Task.CompletedTask;
    }

    public async Task RemoveConstructionAsync(Guid constructionId)
    {
        _orderLifecycleService.AssertCanEdit(Order);
        if (Order.Constructions.Count <= 1)
        {
            return;
        }

        var construction = Order.Constructions.FirstOrDefault(c => c.Id == constructionId);
        if (construction is null)
        {
            return;
        }

        var removedIndex = Order.Constructions.IndexOf(construction);
        var wasActive = Order.SelectedConstruction?.Id == constructionId;
        Order.Constructions.Remove(construction);

        if (wasActive)
        {
            var neighborIndex = Math.Min(removedIndex, Order.Constructions.Count - 1);
            var neighbor = Order.Constructions[Math.Max(0, neighborIndex)];
            LoadConstructionToCanvas(neighbor);
            SyncConstructionListToCadEditor(neighbor.Id);
            Sheet.ReloadFromOrder(neighbor.Id);
        }
        else
        {
            SyncConstructionListToCadEditor(Order.SelectedConstruction?.Id);
            Sheet.ReloadFromOrder(Order.SelectedConstruction?.Id);
        }

        RecalculateAll();
        NotifyModified();

        await Task.CompletedTask;
    }

    public void LoadConstructionToCanvas(OrderConstruction construction)
    {
        var wasResetting = _isResettingCanvas;
        _isResettingCanvas = true;
        try
        {
            Order.SelectedConstruction = construction;
            construction.OrderDetails.ApplyTo(Order.Header);
            _cadEditor.ResetConstructionScopedSettings();
            _cadEditor.SetSelectedContourColor(construction.Appearance.ContourColorName);

            var elements = construction.Geometry.Elements.ToList();
            var snapshot = ConstructionDrawingStore.TryDeserialize(construction.Geometry.DrawingJson);

            if (snapshot is not null)
            {
                _cadEditor.ApplySnapshot(snapshot, elements);
            }
            else if (elements.Count > 0)
            {
                _cadEditor.SetDesignElements(elements);
                _cadEditor.ScheduleInitialViewportFocus();
            }
            else
            {
                _cadEditor.PrepareForNewConstruction();
            }

            _cadEditor.ConstructionQuantity = Math.Max(1, construction.Cost.Quantity);
            _cadEditor.SelectedProfileSeries = construction.ProfileSystemName;
            _cadEditor.SetSelectedConstructionMaterial(construction.SelectedMaterialId);
            _cadEditor.SelectedMaterialColor = construction.Appearance.ColorName;

            _loadedConstructionId = construction.Id;
        }
        finally
        {
            _isResettingCanvas = wasResetting;
        }
    }

    public void SyncCadToOrder()
    {
        CaptureConstructionState(GetCanvasConstruction());
    }

    private void CaptureConstructionState(OrderConstruction construction)
    {
        var geometry = construction.Geometry;
        geometry.Elements.Clear();
        foreach (var element in _cadEditor.CanvasElements.ToList())
        {
            geometry.Elements.Add(CloneElement(element));
        }

        var frame = _cadEditor.FrameRoot;
        if (frame is not null)
        {
            geometry.OuterWidthMm = (decimal)frame.Width;
            geometry.OuterHeightMm = (decimal)frame.Height;
        }
        else
        {
            geometry.OuterWidthMm = 0;
            geometry.OuterHeightMm = 0;
        }

        construction.Geometry.DrawingJson =
            ConstructionDrawingStore.Serialize(_cadEditor.CaptureSnapshot());
        construction.Cost.Quantity = _cadEditor.ConstructionQuantity;
        construction.ProfileSystemName = _cadEditor.SelectedProfileSeries ?? string.Empty;
        construction.SelectedMaterialId = _cadEditor.SelectedConstructionMaterialId;
        construction.Appearance.ColorName =
            _cadEditor.SelectedMaterialColor ?? construction.Appearance.ColorName;
        construction.Appearance.ContourColorName = _cadEditor.SelectedContourColorName;
        construction.OrderDetails.CaptureFrom(Order.Header);
    }

    private OrderConstruction GetCanvasConstruction()
    {
        if (_loadedConstructionId is Guid loadedId)
        {
            var loaded = Order.Constructions.FirstOrDefault(item => item.Id == loadedId);
            if (loaded is not null)
            {
                return loaded;
            }
        }

        return Order.SelectedConstruction ?? Order.EnsureDefaultConstruction();
    }

    private int GetNextConstructionNumber() =>
        Order.Constructions.Count == 0
            ? 1
            : Order.Constructions.Max(item => item.Number) + 1;

    public IReadOnlyList<GeometryElement> GetActiveGeometryElements()
    {
        var construction = GetCanvasConstruction();
        return construction.Geometry.Elements;
    }

    public void ReloadFromProject(ProjectDocumentData project)
    {
        var construction = project.ActiveConstructionId is Guid id
            ? Order.Constructions.FirstOrDefault(item => item.Id == id) ?? Order.EnsureDefaultConstruction()
            : Order.EnsureDefaultConstruction();
        LoadConstructionToCanvas(construction);
        Sheet.ReloadFromOrder(construction.Id);
        SyncConstructionListToCadEditor(construction.Id);
        Drawing.ReloadFromOrder();
        ApplyCadReadOnlyStatus();
    }

    public void ResetWorkspace()
    {
        var construction = Order.EnsureDefaultConstruction();
        LoadConstructionToCanvas(construction);
        Sheet.ReloadFromOrder(construction.Id);
        SyncConstructionListToCadEditor(construction.Id);
        Drawing.ReloadFromOrder();
        ApplyCadReadOnlyStatus();
    }

    private void WireConstructionHandlers()
    {
        _cadEditor.AddConstructionHandler = AddNewConstructionAsync;
        _cadEditor.SelectConstructionHandler = SwitchToConstructionAsync;
        _cadEditor.RemoveConstructionHandler = RemoveConstructionAsync;
        _cadEditor.ConfirmRemoveConstructionHandler = message =>
            _orderErpDialogService.ShowConfirmDeleteAsync(message);
        _cadEditor.OpenConstructionsLookupHandler = OpenConstructionsLookupAsync;
        _cadEditor.ConstructionQuantityChangedHandler = UpdateActiveConstructionQuantity;
    }

    private void UpdateActiveConstructionQuantity(int quantity)
    {
        if (_isResettingCanvas || _cadEditor.IsResettingCanvas)
        {
            return;
        }

        var construction = Order.SelectedConstruction ?? Order.EnsureDefaultConstruction();
        construction.Cost.Quantity = Math.Max(1, quantity);
        _cadEditor.SelectedConstruction?.UpdateQuantity(construction.Cost.Quantity);
        Sheet.RecalculateAll();
        NotifyModified();
    }

    public async Task OpenConstructionsLookupAsync()
    {
        CommitActiveConstructionGeometry();

        var owner = GetOwnerWindow();
        if (owner is null)
        {
            return;
        }

        var viewModel = new OrderConstructionsLookupViewModel(this, Order);
        var window = new OrderConstructionsLookupWindow
        {
            DataContext = viewModel
        };
        viewModel.CloseRequested += () => window.Close();
        await window.ShowDialog(owner);
    }

    private static Window? GetOwnerWindow()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        return null;
    }

    private void InitializeCadConstructions()
    {
        var construction = Order.EnsureDefaultConstruction();
        LoadConstructionToCanvas(construction);
        SyncConstructionListToCadEditor(construction.Id);
        ApplyCadReadOnlyStatus();
    }

    private void ScheduleSyncConstructionListToCadEditor(Guid? activeConstructionId = null)
    {
        Dispatcher.UIThread.Post(
            () => SyncConstructionListToCadEditor(activeConstructionId),
            DispatcherPriority.Background);
    }

    private void SyncConstructionListToCadEditor(Guid? activeConstructionId = null)
    {
        activeConstructionId ??= Order.SelectedConstruction?.Id;

        var previousId = _cadEditor.SelectedConstruction?.OrderConstructionId;
        var ordered = Order.Constructions.OrderBy(item => item.Number).ToList();
        var activeIds = ordered.Select(item => item.Id).ToHashSet();
        for (var index = _cadEditor.Constructions.Count - 1; index >= 0; index--)
        {
            if (!activeIds.Contains(_cadEditor.Constructions[index].OrderConstructionId))
            {
                _cadEditor.Constructions.RemoveAt(index);
            }
        }

        for (var targetIndex = 0; targetIndex < ordered.Count; targetIndex++)
        {
            var construction = ordered[targetIndex];
            var item = _cadEditor.Constructions.FirstOrDefault(
                candidate => candidate.OrderConstructionId == construction.Id);
            if (item is null)
            {
                item = new ConstructionItemViewModel(
                    construction.Id,
                    construction.Number,
                    construction.Name);
                _cadEditor.Constructions.Insert(targetIndex, item);
            }
            else
            {
                var currentIndex = _cadEditor.Constructions.IndexOf(item);
                if (currentIndex != targetIndex)
                {
                    _cadEditor.Constructions.Move(currentIndex, targetIndex);
                }
            }

            item.UpdateFrom(
                construction.Name,
                construction.Number,
                construction.DimensionsDisplay,
                construction.QuantityDisplay);
        }

        var selectedId = activeConstructionId ?? previousId;
        var selected = selectedId is Guid id
            ? _cadEditor.Constructions.FirstOrDefault(c => c.OrderConstructionId == id)
            : _cadEditor.Constructions.FirstOrDefault();

        _cadEditor.SetSelectedConstructionSilently(selected);
        _loadedConstructionId = selected?.OrderConstructionId;
    }

    private static OrderConstruction CloneConstruction(OrderConstruction source, int number)
    {
        var idMap = new Dictionary<Guid, Guid>();
        var copy = new OrderConstruction
        {
            Number = number,
            Name = $"{source.Name} (дубл)",
            ProfileSystemName = source.ProfileSystemName,
            SelectedMaterialId = source.SelectedMaterialId,
            IsSelectedForCalculation = source.IsSelectedForCalculation,
            Status = source.Status,
            ManufactureDate = source.ManufactureDate,
            DeliveryDate = source.DeliveryDate,
            Manager = source.Manager,
            Supervisor = source.Supervisor,
            Notes = source.Notes,
            OrderDetails = source.OrderDetails.Clone(),
            Appearance = new ConstructionAppearance
            {
                ColorName = source.Appearance.ColorName,
                ColorHex = source.Appearance.ColorHex,
                ContourColorName = source.Appearance.ContourColorName,
                ChangeProfile = source.Appearance.ChangeProfile,
                ChangeImposts = source.Appearance.ChangeImposts,
                ChangeSashes = source.Appearance.ChangeSashes,
                ChangeLayout = source.Appearance.ChangeLayout,
                ChangeDecorativeElements = source.Appearance.ChangeDecorativeElements
            }
        };

        copy.Geometry.OuterWidthMm = source.Geometry.OuterWidthMm;
        copy.Geometry.OuterHeightMm = source.Geometry.OuterHeightMm;
        copy.Geometry.DrawingJson = source.Geometry.DrawingJson;
        copy.Cost.Quantity = source.Cost.Quantity;
        CopyCost(source.Cost, copy.Cost);

        foreach (var element in source.Geometry.Elements)
        {
            var clone = CloneElement(element);
            var oldId = clone.Id;
            clone.Id = Guid.NewGuid();
            idMap[oldId] = clone.Id;
            copy.Geometry.Elements.Add(clone);
        }

        foreach (var element in copy.Geometry.Elements)
        {
            if (element.HostSashId is Guid hostId && idMap.TryGetValue(hostId, out var newHostId))
            {
                element.HostSashId = newHostId;
            }
        }

        return copy;
    }

    private static void CopyCost(
        NurSapat.Core.Models.Calculation.ConstructionCostBreakdown source,
        NurSapat.Core.Models.Calculation.ConstructionCostBreakdown target)
    {
        target.ProfileCost = source.ProfileCost;
        target.ComponentsCost = source.ComponentsCost;
        target.SealantCost = source.SealantCost;
        target.GlazingCost = source.GlazingCost;
        target.HardwareCost = source.HardwareCost;
        target.InstallationMaterialsCost = source.InstallationMaterialsCost;
        target.Expenses = source.Expenses;
        target.ExpensesMode = source.ExpensesMode;
        target.LaborCost = source.LaborCost;
        target.MaterialsWithoutMarkup = source.MaterialsWithoutMarkup;
        target.ProfitabilityPercent = source.ProfitabilityPercent;
        target.AdditionalExpensesPercent = source.AdditionalExpensesPercent;
        target.AdditionalExpensesFixed = source.AdditionalExpensesFixed;
        target.Discount1Percent = source.Discount1Percent;
        target.DemolitionCost = source.DemolitionCost;
        target.InstallationCost = source.InstallationCost;
        target.Quantity = source.Quantity;
    }

    private static GeometryElement CloneElement(GeometryElement source) => new()
    {
        Id = source.Id,
        Kind = source.Kind,
        Label = source.Label,
        X = source.X,
        Y = source.Y,
        WidthMm = source.WidthMm,
        HeightMm = source.HeightMm,
        OpeningType = source.OpeningType,
        HingeSide = source.HingeSide,
        HasHardware = source.HasHardware,
        HasHandle = source.HasHandle,
        HostSashId = source.HostSashId,
        ProfileSeriesId = source.ProfileSeriesId,
        MaterialId = source.MaterialId,
        SourceToolId = source.SourceToolId,
        EndX = source.EndX,
        EndY = source.EndY,
        ParentGlazingId = source.ParentGlazingId,
        SpokeType = source.SpokeType,
        SpokeThicknessMm = source.SpokeThicknessMm,
        SpokeColor = source.SpokeColor,
        SpokeRowsCount = source.SpokeRowsCount,
        SpokeColumnsCount = source.SpokeColumnsCount,
        SpokeRayCount = source.SpokeRayCount,
        SpokeIsArched = source.SpokeIsArched,
        SpokeArcCenterX = source.SpokeArcCenterX,
        SpokeArcCenterY = source.SpokeArcCenterY,
        SpokeArcRadius = source.SpokeArcRadius
    };
}
