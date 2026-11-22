namespace NurSapat.App.Models.Stock;

public enum StockDocumentType
{
    ReceiptAct,
    Invoice,
    InventoryAct,
    Request,
    MinStockLimit
}

public static class StockDocumentTypeLabels
{
    public static string GetDisplay(StockDocumentType type) => type switch
    {
        StockDocumentType.ReceiptAct => "Акт приемки",
        StockDocumentType.Invoice => "Накладная на отпуск",
        StockDocumentType.InventoryAct => "Акт инвентаризации",
        StockDocumentType.Request => "Заявка",
        StockDocumentType.MinStockLimit => "Неснижаемый остаток",
        _ => type.ToString()
    };

    public static IReadOnlyList<StockDocumentType> EditableTypes { get; } =
    [
        StockDocumentType.ReceiptAct,
        StockDocumentType.Invoice,
        StockDocumentType.InventoryAct,
        StockDocumentType.Request,
        StockDocumentType.MinStockLimit
    ];
}
