using System.Collections.ObjectModel;

namespace NurSapat.App.Models.Stock;

public sealed class StockDocumentHeader
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; } = DateTime.Today;

    public StockDocumentType Type { get; set; } = StockDocumentType.ReceiptAct;

    public string Note { get; set; } = string.Empty;

    public bool UseCustomExchangeRates { get; set; }

    public ObservableCollection<StockDocumentLine> Lines { get; set; } = [];

    public decimal DocumentTotalSum => Lines.Sum(line => line.TotalSum);

    public StockDocumentHeader Clone()
    {
        var clone = new StockDocumentHeader
        {
            Id = Id,
            Number = Number,
            Date = Date,
            Type = Type,
            Note = Note,
            UseCustomExchangeRates = UseCustomExchangeRates
        };

        foreach (var line in Lines)
        {
            clone.Lines.Add(line.Clone());
        }

        return clone;
    }

    public static StockDocumentHeader CreateDefault(StockDocumentType type, int sequence) => new()
    {
        Number = string.Empty,
        Date = DateTime.Today,
        Type = type,
        Note = string.Empty
    };
}
