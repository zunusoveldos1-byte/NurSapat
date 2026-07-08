using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Calculations;

public sealed class OrderCalculationService
{
    private const decimal AreaPriceCoefficient = 5_000m;

    public void Recalculate(Order order)
    {
        var construction = order.EnsureDefaultConstruction();
        ApplyStubMaterialCosts(construction);
        ApplyOrderProfitability(order);

        order.Totals.ConstructionsSubtotal = order.Constructions
            .Where(item => item.IsSelectedForCalculation)
            .Sum(item => item.Cost.ConstructionSumTotal);

        order.Totals.NotifyDerived();

        if (order.Payment.AgreedAmount <= 0)
        {
            order.Payment.AgreedAmount = order.Totals.GrandTotal;
        }

        order.Payment.NotifyDebt();
    }

    private static void ApplyStubMaterialCosts(OrderConstruction construction)
    {
        var area = construction.Geometry.BillableProductAreaSqM;
        var baseCost = decimal.Round(area * AreaPriceCoefficient, 2, MidpointRounding.AwayFromZero);

        construction.Cost.ProfileCost = RoundShare(baseCost, 0.42m);
        construction.Cost.ComponentsCost = RoundShare(baseCost, 0.12m);
        construction.Cost.SealantCost = RoundShare(baseCost, 0.05m);
        construction.Cost.GlazingCost = RoundShare(baseCost, 0.28m);
        construction.Cost.HardwareCost = RoundShare(baseCost, 0.08m);
        construction.Cost.InstallationMaterialsCost = RoundShare(baseCost, 0.05m);

        construction.Metrics.ProductAreaSqM = area;
        construction.Metrics.GlazingAreaSqM = decimal.Round(area * 0.82m, 3, MidpointRounding.AwayFromZero);
        construction.Metrics.WeightKg = decimal.Round(area * 28m, 1, MidpointRounding.AwayFromZero);
        construction.Metrics.ManufacturingTime = TimeSpan.FromMinutes((double)(area * 120m));

        construction.Cost.NotifyDerived();
    }

    private static void ApplyOrderProfitability(Order order)
    {
        foreach (var construction in order.Constructions.ToList())
        {
            construction.Cost.ProfitabilityPercent = order.ProfitabilityPercent;
            construction.Cost.NotifyDerived();
        }
    }

    private static decimal RoundShare(decimal baseCost, decimal share) =>
        decimal.Round(baseCost * share, 2, MidpointRounding.AwayFromZero);
}
