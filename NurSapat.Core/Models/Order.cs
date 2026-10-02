using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Models;

public sealed class Order
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public OrderHeader Header { get; set; } = new();

    public List<OrderConstruction> Constructions { get; } = [];

    public OrderCostTotals Totals { get; } = new();

    public OrderPayment Payment { get; } = new();

    public decimal ProfitabilityPercent { get; set; } = 30m;

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
