using Avalonia.Controls;
using Avalonia.Threading;
using NurSapat.App.Services;
using NurSapat.App.Services.Dictionaries;
using NurSapat.App.Services.Orders;
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
        IOrderNumberService orderNumberService,
        IDictionaryWindowService dictionaryWindowService)
    {
        _cadEditor = cadEditor;
        _orderErpDialogService = orderErpDialogService;
        _isResettingCanvas = true;

        Sheet = new OrderSheetViewModel(
            this,
            cadEditor,
            orderErpDialogService,
            calculationService,
            customersStore,
            managersStore,
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

        var construction = Order.SelectedConstruction ?? Order.EnsureDefaultConstruction();
        SyncCadToOrder();

        construction.Geometry.DrawingJson = ConstructionDrawingStore.Serialize(_cadEditor.CaptureSnapshot());
        construction.Cost.Quantity = _cadEditor.ConstructionQuantity;
        construction.ProfileSystemName = _cadEditor.SelectedProfileSeries ?? string.Empty;
        construction.Appearance.ColorName = _cadEditor.SelectedMaterialColor ?? construction.Appearance.ColorName;

        if (_cadEditor.SelectedConstruction is { } item)
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
        if (_isResettingCanvas || _isAddingConstruction)
        {
            return;
        }

        try
        {
            _isAddingConstruction = true;
            _isResettingCanvas = true;
            CommitActiveConstructionGeometry();

            var source = Order.Constructions.FirstOrDefault(c => c.Id == sourceConstructionId);
            if (source is null)
            {
                return;
            }

            var number = Order.Constructions.Count + 1;
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
                _isAddingConstruction = true;
                _isResettingCanvas = true;
                CommitActiveConstructionGeometry();

                var number = Order.Constructions.Count + 1;
                var construction = new OrderConstruction
                {
                    Number = number,
                    Name = $"Конструкция {number}"
                };
                Order.Constructions.Add(construction);
                Order.SelectedConstruction = construction;

                _cadEditor.PrepareForNewConstruction();
                _loadedConstructionId = construction.Id;
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

        try
        {
            _isResettingCanvas = true;
            CommitActiveConstructionGeometry();

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
        if (Order.Constructions.Count <= 1)
        {
            return;
        }

        var construction = Order.Constructions.FirstOrDefault(c => c.Id == constructionId);
        if (construction is null)
        {
            return;
        }

        var wasActive = Order.SelectedConstruction?.Id == constructionId;
        Order.Constructions.Remove(construction);

        if (wasActive)
        {
            var next = Order.Constructions[Math.Max(0, Order.Constructions.Count - 1)];
            LoadConstructionToCanvas(next);
            Sheet.ReloadFromOrder(next.Id);
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
        Order.SelectedConstruction = construction;

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
        if (!string.IsNullOrWhiteSpace(construction.ProfileSystemName))
        {
            _cadEditor.SelectedProfileSeries = construction.ProfileSystemName;
        }

        _cadEditor.SelectedMaterialColor = construction.Appearance.ColorName;

        _loadedConstructionId = construction.Id;
    }

    public void SyncCadToOrder()
    {
        var construction = Order.SelectedConstruction ?? Order.EnsureDefaultConstruction();
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
    }

    public IReadOnlyList<GeometryElement> GetActiveGeometryElements()
    {
        var construction = Order.SelectedConstruction ?? Order.EnsureDefaultConstruction();
        return construction.Geometry.Elements;
    }

    public void ReloadFromProject(ProjectDocumentData project)
    {
        Sheet.ReloadFromOrder(project.ActiveConstructionId);
        var construction = Order.SelectedConstruction ?? Order.EnsureDefaultConstruction();
        LoadConstructionToCanvas(construction);
        SyncConstructionListToCadEditor(construction.Id);
        Drawing.ReloadFromOrder();
        ApplyCadReadOnlyStatus();
    }

    public void ResetWorkspace()
    {
        Sheet.ReloadFromOrder(null);
        var construction = Order.EnsureDefaultConstruction();
        LoadConstructionToCanvas(construction);
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
        var items = Order.Constructions
            .OrderBy(c => c.Number)
            .Select(construction =>
            {
                var item = new ConstructionItemViewModel(construction.Id, construction.Number, construction.Name);
                item.UpdateFrom(
                    construction.Name,
                    construction.Number,
                    construction.DimensionsDisplay,
                    construction.QuantityDisplay);
                return item;
            })
            .ToList();

        _cadEditor.Constructions.Clear();
        foreach (var item in items)
        {
            _cadEditor.Constructions.Add(item);
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
            Name = $"{source.Name} (Копия)",
            ProfileSystemName = source.ProfileSystemName,
            IsSelectedForCalculation = source.IsSelectedForCalculation
        };

        copy.Geometry.OuterWidthMm = source.Geometry.OuterWidthMm;
        copy.Geometry.OuterHeightMm = source.Geometry.OuterHeightMm;
        copy.Geometry.DrawingJson = source.Geometry.DrawingJson;
        copy.Cost.Quantity = source.Cost.Quantity;

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
        SpokeArcRadius = source.SpokeArcRadius,
        HostFrameNodeId = source.HostFrameNodeId,
        HostGridLineId = source.HostGridLineId,
        SubFrameId = source.SubFrameId,
        ExtremeImpostSlotId = source.ExtremeImpostSlotId
    };
}
