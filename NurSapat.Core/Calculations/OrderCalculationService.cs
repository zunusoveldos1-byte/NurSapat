using NurSapat.Core.Calculations.Costing;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Interfaces.Currency;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Settings;

namespace NurSapat.Core.Calculations;

public sealed class OrderCalculationService
{
    private readonly IMaterialPriceCatalog _priceCatalog;
    private readonly ICurrencyService? _currencyService;
    private readonly ProductionOptions? _options;

    public OrderCalculationService(
        IMaterialPriceCatalog priceCatalog,
        ICurrencyService? currencyService = null,
        ProductionOptions? options = null)
    {
        _priceCatalog = priceCatalog;
        _currencyService = currencyService;
        _options = options;
    }

    public void Recalculate(Order order) => Recalculate(order, order.Header.OrderDate);

    /// <summary>
    /// Пересчёт с историческим курсом на дату заказа: профиль, стеклопакет и фурнитура
    /// переводятся в KGS по курсу НБКР, действовавшему на <paramref name="orderDate"/>.
    /// </summary>
    public void Recalculate(Order order, DateTime orderDate)
    {
        var priceCatalog = ResolvePriceCatalog(orderDate);

        var constructionCostCalculator = new ConstructionCostCalculator(priceCatalog, _options);

        foreach (var construction in order.Constructions)
        {
            ApplyConstructionCost(construction, order.ProfitabilityPercent, constructionCostCalculator);
        }

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

    private IMaterialPriceCatalog ResolvePriceCatalog(DateTime orderDate)
    {
        if (_currencyService is null)
        {
            return _priceCatalog;
        }

        return new HistoricalCurrencyMaterialPriceCatalog(_priceCatalog, _currencyService, orderDate);
    }

    private static void ApplyConstructionCost(
        OrderConstruction construction,
        decimal profitabilityPercent,
        ConstructionCostCalculator constructionCostCalculator)
    {
        var result = constructionCostCalculator.Calculate(construction);

        construction.SpecificationLines.Clear();
        construction.SpecificationLines.AddRange(result.Lines);

        construction.Metrics.ProductAreaSqM = result.Metrics.ProductAreaSqM;
        construction.Metrics.GlazingAreaSqM = result.Metrics.GlazingAreaSqM;
        construction.Metrics.WeightKg = result.Metrics.WeightKg;
        construction.Metrics.ManufacturingTime = result.Metrics.ManufacturingTime;

        var totals = result.CategoryTotals;
        construction.Cost.ProfileCost = totals.ProfileCost;
        construction.Cost.ComponentsCost = totals.ComponentsCost;
        construction.Cost.SealantCost = totals.SealantCost;
        construction.Cost.GlazingCost = totals.GlazingCost;
        construction.Cost.HardwareCost = totals.HardwareCost;
        construction.Cost.InstallationMaterialsCost = totals.InstallationMaterialsCost;

        if (totals.LaborCost > 0 || construction.Cost.LaborCost <= 0)
        {
            construction.Cost.LaborCost = totals.LaborCost;
        }

        construction.Cost.ProfitabilityPercent = profitabilityPercent;
        construction.Cost.NotifyDerived();
    }
}
