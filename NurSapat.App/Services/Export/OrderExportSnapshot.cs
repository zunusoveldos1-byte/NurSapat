using NurSapat.Core.Models;

namespace NurSapat.App.Services.Export;

public sealed class OrderExportSnapshot
{
    public DateTime ExportedAtUtc { get; init; } = DateTime.UtcNow;

    public Guid OrderId { get; init; }

    public string OrderNumber { get; init; } = string.Empty;

    public OrderHeaderExportNode Header { get; init; } = new();

    public OrderTotalsExportNode Totals { get; init; } = new();

    public List<ConstructionExportNode> Constructions { get; init; } = [];

    public string? SketchImageBase64 { get; init; }

    public string SketchImageMimeType { get; init; } = "image/png";
}

public sealed class OrderHeaderExportNode
{
    public string OrderNumber { get; init; } = string.Empty;

    public DateTime OrderDate { get; init; }

    public string CustomerName { get; init; } = string.Empty;

    public string CustomerPhone { get; init; } = string.Empty;

    public string SiteAddress { get; init; } = string.Empty;

    public string DealerName { get; init; } = string.Empty;

    public string ContractNumber { get; init; } = string.Empty;

    public string CurrencyCode { get; init; } = string.Empty;

    public string Note { get; init; } = string.Empty;
}

public sealed class OrderTotalsExportNode
{
    public decimal ProfitabilityPercent { get; init; }

    public decimal TotalCost { get; init; }
}

public sealed class ConstructionExportNode
{
    public Guid Id { get; init; }

    public int Number { get; init; }

    public string Name { get; init; } = string.Empty;

    public string ProfileSystemName { get; init; } = string.Empty;

    public decimal WidthMm { get; init; }

    public decimal HeightMm { get; init; }

    public int Quantity { get; init; }

    public List<ElementExportNode> Elements { get; init; } = [];

    public List<MaterialLineExportNode> Materials { get; init; } = [];
}

public sealed class ElementExportNode
{
    public Guid Id { get; init; }

    public string Kind { get; init; } = string.Empty;

    public string Label { get; init; } = string.Empty;

    public double X { get; init; }

    public double Y { get; init; }

    public double WidthMm { get; init; }

    public double HeightMm { get; init; }
}

public sealed class MaterialLineExportNode
{
    public Guid MaterialId { get; init; }

    public string Article { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Unit { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal TotalCost { get; init; }

    public string MaterialType { get; init; } = string.Empty;

    public int ConstructionNumber { get; init; }
}

public sealed record ExportResult(
    bool Success,
    string Message,
    string? PrimaryFilePath = null,
    IReadOnlyList<string>? CreatedFiles = null);
