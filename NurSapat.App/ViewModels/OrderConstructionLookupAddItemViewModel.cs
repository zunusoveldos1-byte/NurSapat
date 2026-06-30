namespace NurSapat.App.ViewModels;

/// <summary>
/// Плитка «Создать новую конструкцию» в галерее выбора.
/// </summary>
public sealed class OrderConstructionLookupAddItemViewModel
{
    private const double CardWidthValue = 220;
    private const double CardHeightValue = 340;
    public double CardWidth => CardWidthValue;

    public double CardHeight => CardHeightValue;

    public int ColumnSpan => 1;

    public int GridRow { get; private set; }

    public int GridColumn { get; private set; }

    public void SetGridPosition(int row, int column)
    {
        GridRow = row;
        GridColumn = column;
    }
}
