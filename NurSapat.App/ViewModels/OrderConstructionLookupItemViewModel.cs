using NurSapat.App.Services;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.ViewModels;

public sealed class OrderConstructionLookupItemViewModel
{
    private const double StandardCardWidth = 220;
    private const double WideCardWidth = 456;
    private const double CardHeightValue = 340;
    private const double CardInnerPadding = 24;
    private const double StandardPreviewWidth = StandardCardWidth - CardInnerPadding;
    private const double WidePreviewWidth = WideCardWidth - CardInnerPadding;
    private const double PreviewHeight = 248;
    private const double WideAspectThreshold = 0.85;
    private const decimal WideWidthThresholdMm = 1300m;

    public OrderConstructionLookupItemViewModel(OrderConstruction construction, bool isActive)
    {
        Construction = construction;
        IsActive = isActive;

        ColumnSpan = ComputeColumnSpan(
            construction.Geometry.OuterWidthMm,
            construction.Geometry.OuterHeightMm);
        CardWidth = ColumnSpan == 2 ? WideCardWidth : StandardCardWidth;
        PreviewZoneWidth = ColumnSpan == 2 ? WidePreviewWidth : StandardPreviewWidth;

        PreviewEditor = new CadEditorViewModel();
        ConstructionDrawingStore.ApplyPreview(PreviewEditor, construction);
    }

    public OrderConstruction Construction { get; }

    public CadEditorViewModel PreviewEditor { get; }

    public IReadOnlyList<GeometryElement> DrawingElements => Construction.Geometry.Elements;

    public Guid Id => Construction.Id;

    public string Name => Construction.Name;

    public string DimensionsDisplay => Construction.DimensionsDisplay;

    public string QuantityDisplay => Construction.QuantityDisplay;

    public bool IsActive { get; }

    public int ColumnSpan { get; }

    public double CardWidth { get; }

    public double CardHeight => CardHeightValue;

    public double PreviewZoneWidth { get; }

    public double PreviewZoneHeight => PreviewHeight;

    public int GridRow { get; private set; }

    public int GridColumn { get; private set; }

    public void SetGridPosition(int row, int column)
    {
        GridRow = row;
        GridColumn = column;
    }

    private static int ComputeColumnSpan(decimal outerWidthMm, decimal outerHeightMm)
    {
        if (outerWidthMm <= 0 || outerHeightMm <= 0)
        {
            return 1;
        }

        if (outerWidthMm >= WideWidthThresholdMm)
        {
            return 2;
        }

        var aspect = (double)outerWidthMm / (double)outerHeightMm;
        return aspect >= WideAspectThreshold ? 2 : 1;
    }
}
