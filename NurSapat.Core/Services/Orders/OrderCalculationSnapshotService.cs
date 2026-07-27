using System.Text.Json;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Pricing;

namespace NurSapat.Core.Services.Orders;

public sealed class OrderCalculationSnapshotService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public OrderCalculationSnapshot Create(
        Order order,
        Guid priceListVersionId,
        string currencyCode,
        decimal exchangeRate)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (priceListVersionId == Guid.Empty)
        {
            throw new ArgumentException("Price list version is required.", nameof(priceListVersionId));
        }

        if (exchangeRate <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(exchangeRate));
        }

        var items = order.Constructions
            .SelectMany(construction => construction.SpecificationLines.Select(line =>
                SnapshotItem.From(construction.Id, line)))
            .ToArray();
        var costPrice = order.Constructions.Sum(x => x.Cost.CostPrice * Math.Max(1, x.Cost.Quantity));
        var totalPrice = order.Totals.GrandTotal;
        var margin = costPrice <= 0 ? 0 : (totalPrice - costPrice) / costPrice * 100m;

        return new OrderCalculationSnapshot
        {
            OrderId = order.Id,
            PriceListVersionId = priceListVersionId,
            CurrencyCode = NormalizeCurrency(currencyCode),
            ExchangeRate = exchangeRate,
            SnapshotDate = DateTime.UtcNow,
            ItemsSnapshotJson = JsonSerializer.Serialize(items, JsonOptions),
            TotalCostPrice = decimal.Round(costPrice, 2, MidpointRounding.AwayFromZero),
            TotalMarginPercent = decimal.Round(margin, 4, MidpointRounding.AwayFromZero),
            TotalDiscountPercent = order.Header.TotalDiscountPercent,
            TotalPrice = totalPrice
        };
    }

    public void Restore(Order order, OrderCalculationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.OrderId != order.Id)
        {
            throw new InvalidOperationException("Snapshot belongs to another order.");
        }

        var items = JsonSerializer.Deserialize<SnapshotItem[]>(snapshot.ItemsSnapshotJson, JsonOptions) ?? [];
        foreach (var construction in order.Constructions)
        {
            var restored = items
                .Where(x => x.ConstructionId == construction.Id)
                .Select(x => x.ToSpecificationLine())
                .ToList();

            construction.SpecificationLines.Clear();
            construction.SpecificationLines.AddRange(restored);
            ApplyCategoryTotals(construction.Cost, restored);
        }

        order.Header.CurrencyCode = snapshot.CurrencyCode;
        order.Header.TotalDiscountPercent = snapshot.TotalDiscountPercent;
        order.Totals.ConstructionsSubtotal = snapshot.TotalPrice;
        order.Totals.Discount2Percent = 0;
        order.Totals.DeliveryCost = 0;
        order.Totals.AdditionalMaterialsCost = 0;
        order.Totals.AdditionalMaterialsInstallationCost = 0;
        order.Totals.VatMode = VatDisplayMode.NotUsed;
        order.Totals.NotifyDerived();
        order.Payment.AgreedAmount = snapshot.TotalPrice;
        order.Payment.NotifyDebt();
    }

    private static void ApplyCategoryTotals(
        ConstructionCostBreakdown cost,
        IEnumerable<CostSpecificationLine> lines)
    {
        var totals = lines.GroupBy(x => x.Category).ToDictionary(x => x.Key, x => x.Sum(y => y.LineTotal));
        cost.ProfileCost = Get(CostMaterialCategory.Profile);
        cost.ComponentsCost = Get(CostMaterialCategory.Components);
        cost.SealantCost = Get(CostMaterialCategory.Sealant);
        cost.GlazingCost = Get(CostMaterialCategory.Glazing);
        cost.HardwareCost = Get(CostMaterialCategory.Hardware);
        cost.InstallationMaterialsCost = Get(CostMaterialCategory.Installation);
        cost.LaborCost = Get(CostMaterialCategory.Labor);
        cost.NotifyDerived();
        return;

        decimal Get(CostMaterialCategory category) =>
            totals.TryGetValue(category, out var value) ? value : 0m;
    }

    private static string NormalizeCurrency(string value) =>
        string.IsNullOrWhiteSpace(value) ? "KGS" : value.Trim().ToUpperInvariant();

    private sealed record SnapshotItem(
        Guid ConstructionId,
        Guid MaterialId,
        Guid? ConstructionMaterialId,
        CostMaterialCategory Category,
        string MaterialName,
        string Article,
        decimal Quantity,
        string Unit,
        decimal UnitPrice,
        decimal WastePercent)
    {
        public static SnapshotItem From(Guid constructionId, CostSpecificationLine line) => new(
            constructionId,
            line.MaterialId,
            line.ConstructionMaterialId,
            line.Category,
            line.MaterialName,
            line.Article,
            line.Quantity,
            line.Unit,
            line.UnitPrice,
            line.WastePercent);

        public CostSpecificationLine ToSpecificationLine() => new()
        {
            MaterialId = MaterialId,
            ConstructionMaterialId = ConstructionMaterialId,
            Category = Category,
            MaterialName = MaterialName,
            Article = Article,
            Quantity = Quantity,
            Unit = Unit,
            UnitPrice = UnitPrice,
            WastePercent = WastePercent
        };
    }
}
