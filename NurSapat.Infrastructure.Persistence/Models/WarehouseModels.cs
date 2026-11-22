namespace NurSapat.Infrastructure.Persistence.Models;

public sealed class StockDocumentModel
{
    public Guid Id { get; set; }

    public string Number { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public StockDocumentTypeModel Type { get; set; }

    public string Note { get; set; } = string.Empty;

    public bool UseCustomExchangeRates { get; set; }

    public List<StockDocumentLineModel> Lines { get; set; } = [];
}

public sealed class StockDocumentLineModel
{
    public Guid Id { get; set; }

    public Guid MaterialId { get; set; }

    public string Article { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "KGS";
}

public sealed class FinishedGoodModel
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string ProductCode { get; set; } = string.Empty;

    public string CellAddress { get; set; } = string.Empty;

    public FinishedGoodStateModel State { get; set; }

    public DateTime DateIncoming { get; set; }

    public string UserName { get; set; } = string.Empty;
}

public sealed class ConstructionElementModel
{
    public Guid Id { get; set; }

    public Guid MaterialId { get; set; }

    public string Name { get; set; } = string.Empty;

    public Guid ManufacturerId { get; set; }

    public ConstructionSectionTypeModel Section { get; set; }

    public List<Guid> AssociatedSeriesIds { get; set; } = [];

    public double WidthMm { get; set; }

    public double HeightMm { get; set; }

    public double WallThicknessMm { get; set; }

    public string Article { get; set; } = string.Empty;

    public double WeightKg { get; set; }

    public string GlassFormula { get; set; } = string.Empty;

    public double GlassThicknessMm { get; set; }

    public string GlassStructure { get; set; } = string.Empty;

    public List<HardwareAutoPickRuleModel> HardwareRules { get; set; } = [];

    public string Notes { get; set; } = string.Empty;
}

public sealed class HardwareAutoPickRuleModel
{
    public Guid Id { get; set; }

    public double MinWidthFalzMm { get; set; }

    public double MaxWidthFalzMm { get; set; }

    public double MinHeightFalzMm { get; set; }

    public double MaxHeightFalzMm { get; set; }

    public int ScissorsCount { get; set; }

    public int MiddleLockCount { get; set; }
}
