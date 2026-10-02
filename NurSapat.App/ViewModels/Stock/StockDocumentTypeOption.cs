using NurSapat.App.Models.Stock;

namespace NurSapat.App.ViewModels.Stock;

public sealed class StockDocumentTypeOption
{
    public StockDocumentType Value { get; init; }

    public string Display { get; init; } = string.Empty;

    public override string ToString() => Display;

    public static IReadOnlyList<StockDocumentTypeOption> CreateAll() =>
        StockDocumentTypeLabels.EditableTypes
            .Select(type => new StockDocumentTypeOption
            {
                Value = type,
                Display = StockDocumentTypeLabels.GetDisplay(type)
            })
            .ToList();
}
