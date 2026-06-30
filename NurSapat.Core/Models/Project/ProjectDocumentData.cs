namespace NurSapat.Core.Models.Project;

/// <summary>
/// Корневой DTO файла проекта NurSapat (*.nsp).
/// </summary>
public sealed class ProjectDocumentData
{
    public int SchemaVersion { get; set; } = ProjectFormat.CurrentSchemaVersion;

    public string AppVersion { get; set; } = string.Empty;

    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;

    public OrderProjectData Order { get; set; } = new();

    public CadProjectData Cad { get; set; } = new();

    public PrintSettings PrintSettings { get; set; } = new();

    public Guid? ActiveConstructionId { get; set; }
}

public sealed class OrderProjectData
{
    public Guid Id { get; set; }

    public OrderHeaderData Header { get; set; } = new();

    public List<ConstructionProjectData> Constructions { get; set; } = [];

    public OrderCostTotalsData Totals { get; set; } = new();

    public OrderPaymentData Payment { get; set; } = new();

    public decimal ProfitabilityPercent { get; set; } = 30m;
}

public sealed class OrderHeaderData
{
    public string OrderNumber { get; set; } = "";

    public DateTime OrderDate { get; set; } = DateTime.Today;

    public string CustomerName { get; set; } = "";

    public string CustomerPhone { get; set; } = "";

    public string SiteAddress { get; set; } = "";

    public string DealerName { get; set; } = "";

    public string OrderManagerName { get; set; } = "";

    public string CustomerCity { get; set; } = "";

    public string CustomerCode { get; set; } = "";

    public string ContractNumber { get; set; } = "";

    public DateTime? ContractDate { get; set; }

    public DateTime? ProductionDate { get; set; }

    public DateTime? DeliveryDate { get; set; }

    public DateTime? InstallationDate { get; set; }

    public bool HasDelivery { get; set; }

    public bool HasInstallation { get; set; }

    public string ProfileNote { get; set; } = "";

    public string Note { get; set; } = "";

    public string CurrencyCode { get; set; } = "KGS";
}

public sealed class ConstructionProjectData
{
    public Guid Id { get; set; }

    public int Number { get; set; } = 1;

    public string Name { get; set; } = "Конструкция 1";

    public bool IsSelectedForCalculation { get; set; } = true;

    public string ProfileSystemName { get; set; } = "";

    public DateTimeOffset? ManufactureDate { get; set; }

    public DateTimeOffset? DeliveryDate { get; set; }

    public string Manager { get; set; } = "";

    public string Supervisor { get; set; } = "";

    public string Notes { get; set; } = "";

    public GeometryProjectData Geometry { get; set; } = new();

    public ConstructionCostData Cost { get; set; } = new();

    public ConstructionMetricsData Metrics { get; set; } = new();
}

public sealed class GeometryProjectData
{
    public decimal OuterWidthMm { get; set; }

    public decimal OuterHeightMm { get; set; }

    public string DrawingJson { get; set; } = "{}";

    public List<GeometryElementData> Elements { get; set; } = [];
}

public sealed class GeometryElementData
{
    public Guid Id { get; set; }

    public string Kind { get; set; } = "";

    public string Label { get; set; } = "";

    public decimal X { get; set; }

    public decimal Y { get; set; }

    public decimal WidthMm { get; set; }

    public decimal HeightMm { get; set; }

    public string? OpeningType { get; set; }

    public string? HingeSide { get; set; }

    public bool HasHardware { get; set; }

    public bool HasHandle { get; set; }

    public Guid? HostSashId { get; set; }

    public Guid? MaterialId { get; set; }

    public string? SourceToolId { get; set; }

    public decimal? EndX { get; set; }

    public decimal? EndY { get; set; }
}

public sealed class ConstructionCostData
{
    public decimal ProfileCost { get; set; }

    public decimal ComponentsCost { get; set; }

    public decimal SealantCost { get; set; }

    public decimal GlazingCost { get; set; }

    public decimal HardwareCost { get; set; }

    public decimal InstallationMaterialsCost { get; set; }

    public decimal Expenses { get; set; }

    public string ExpensesMode { get; set; } = "Fixed";

    public decimal LaborCost { get; set; }

    public decimal MaterialsWithoutMarkup { get; set; }

    public decimal ProfitabilityPercent { get; set; }

    public decimal AdditionalExpensesPercent { get; set; }

    public decimal AdditionalExpensesFixed { get; set; }

    public decimal Discount1Percent { get; set; }

    public decimal DemolitionCost { get; set; }

    public decimal InstallationCost { get; set; }

    public int Quantity { get; set; } = 1;
}

public sealed class ConstructionMetricsData
{
    public decimal GlazingAreaSqM { get; set; }

    public decimal ProductAreaSqM { get; set; }

    public decimal WeightKg { get; set; }

    public long ManufacturingTimeTicks { get; set; }
}

public sealed class OrderCostTotalsData
{
    public decimal ConstructionsSubtotal { get; set; }

    public decimal Discount2Percent { get; set; }

    public decimal DeliveryCost { get; set; }

    public decimal AdditionalMaterialsCost { get; set; }

    public decimal AdditionalMaterialsInstallationCost { get; set; }

    public decimal VatPercent { get; set; } = 12m;

    public string VatMode { get; set; } = "NotUsed";
}

public sealed class OrderPaymentData
{
    public decimal AgreedAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public string CurrencyCode { get; set; } = "KGS";
}

public sealed class CadProjectData
{
    public string DocumentName { get; set; } = "Новый чертеж";

    public double ViewportZoomFactor { get; set; } = 0.35;

    public double ViewportPanX { get; set; } = 500;

    public double ViewportPanY { get; set; } = 350;

    public FrameNodeData? FrameRoot { get; set; }
}

public sealed class FrameNodeData
{
    public double X { get; set; }

    public double Y { get; set; }

    public double Width { get; set; }

    public double Height { get; set; }

    public bool IsLeaf { get; set; } = true;

    public bool IsUniformSplit { get; set; }

    public string SplitOrientation { get; set; } = "None";

    public string LineStyle { get; set; } = "Dashed";

    public double ImpostThicknessMm { get; set; }

    public int UniformImpostCount { get; set; }

    public List<FrameNodeData> UniformCells { get; set; } = [];

    public FrameNodeData? ChildA { get; set; }

    public FrameNodeData? ChildB { get; set; }
}
