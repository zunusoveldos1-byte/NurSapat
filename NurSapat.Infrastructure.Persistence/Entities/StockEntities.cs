namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class StockDocumentEntity
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public int Type { get; set; }

    public string Note { get; set; } = string.Empty;

    public bool UseCustomExchangeRates { get; set; }

    public ICollection<StockDocumentLineEntity> Lines { get; set; } = [];
}

public sealed class StockDocumentLineEntity
{
    public Guid Id { get; set; }

    public Guid StockDocumentId { get; set; }

    public Guid MaterialId { get; set; }

    public string Article { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "KGS";

    public StockDocumentEntity StockDocument { get; set; } = null!;

    public MaterialEntity Material { get; set; } = null!;
}

public sealed class FinishedGoodEntity
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string CellAddress { get; set; } = string.Empty;

    public int State { get; set; }

    public DateTime DateIncoming { get; set; }

    public string UserName { get; set; } = string.Empty;

    public OrderEntity Order { get; set; } = null!;
}
