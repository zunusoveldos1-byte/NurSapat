using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Models;

public sealed class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public OrderHeader Header { get; set; } = new();

    public OrderStatus Status
    {
        get => Header.Status;
        set => Header.Status = value;
    }

    public List<OrderConstruction> Constructions { get; } = [];

    public OrderCostTotals Totals { get; } = new();

    public OrderPayment Payment { get; } = new();

    public decimal ProfitabilityPercent { get; set; } = 30m;

    /// <summary>Optional path to the last .nsp export/backup file (not the system of record).</summary>
    public string? ExportFilePath { get; set; }

    public OrderConstruction? SelectedConstruction { get; set; }

    public OrderConstruction EnsureDefaultConstruction()
    {
        if (Constructions.Count == 0)
        {
            var construction = new OrderConstruction
            {
                Number = 1,
                Name = "Конструкция 1",
                Geometry =
                {
                    OuterWidthMm = 1500m,
                    OuterHeightMm = 1400m
                }
            };
            Constructions.Add(construction);
            SelectedConstruction = construction;
        }

        SelectedConstruction ??= Constructions[0];
        return SelectedConstruction;
    }
}
