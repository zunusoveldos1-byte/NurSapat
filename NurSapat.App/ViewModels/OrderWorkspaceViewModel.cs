using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;

namespace NurSapat.App.ViewModels;

/// <summary>
/// Общий контекст заказа: связывает чертёж, лист заказа и пересчёт.
/// </summary>
public sealed class OrderWorkspaceViewModel
{
    public Order Order { get; } = new();

    public OrderSheetViewModel Sheet { get; }

    public OrderDrawingViewModel Drawing { get; }

    public event EventHandler? ProjectModified;

    public OrderWorkspaceViewModel(CadEditorViewModel cadEditor)
    {
        Sheet = new OrderSheetViewModel(Order, cadEditor);
        Drawing = new OrderDrawingViewModel(Order, Sheet, cadEditor);
        Sheet.ProjectModified += (_, _) => ProjectModified?.Invoke(this, EventArgs.Empty);
        Drawing.ProjectModified += (_, _) => ProjectModified?.Invoke(this, EventArgs.Empty);
    }

    public void RecalculateAll() => Sheet.RecalculateAll();

    public void NotifyModified() => ProjectModified?.Invoke(this, EventArgs.Empty);

    public void SyncCadToOrder()
    {
        var construction = Order.SelectedConstruction ?? Order.EnsureDefaultConstruction();
        var geometry = construction.Geometry;
        geometry.Elements.Clear();
        foreach (var element in Drawing.CadEditor.CanvasElements)
        {
            geometry.Elements.Add(CloneElement(element));
        }

        var frame = Drawing.CadEditor.FrameRoot;
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
        Drawing.ReloadFromOrder();
    }

    public void ResetWorkspace()
    {
        Sheet.ReloadFromOrder(null);
        Drawing.ReloadFromOrder();
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
        MaterialId = source.MaterialId,
        SourceToolId = source.SourceToolId,
        EndX = source.EndX,
        EndY = source.EndY
    };
}
