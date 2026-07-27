using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Pricing;
using NurSapat.Core.Services.Orders;
using NurSapat.Infrastructure.Persistence.Repositories;

namespace NurSapat.Infrastructure.Persistence.Tests;

public sealed class OrderSnapshotTests
{
    [Fact]
    public async Task Restored_order_prices_are_independent_from_changed_price_list()
    {
        var options = new DbContextOptionsBuilder<NurSapatDbContext>()
            .UseInMemoryDatabase($"snapshot-{Guid.NewGuid():N}")
            .Options;
        var factory = new PooledDbContextFactory<NurSapatDbContext>(options);
        var orderRepository = new OrderRepository(factory);
        var priceListRepository = new PriceListRepository(factory);
        var snapshotRepository = new OrderCalculationSnapshotRepository(factory);
        var snapshotService = new OrderCalculationSnapshotService();
        var snapshotCoordinator = new OrderSnapshotCoordinator(snapshotRepository, snapshotService);

        var priceList = CreatePriceList();
        await priceListRepository.SaveAsync(priceList);

        var order = CreateCalculatedOrder();
        await orderRepository.SaveAsync(order);

        var snapshot = await snapshotCoordinator.CaptureAsync(order, priceList.Id, "KGS", 1m);

        priceList.Items[0].PurchasePrice = 9_999m;
        priceList.Items[0].BaseRetailPrice = 12_999m;
        priceList.Items[0].DealerPrice = 11_999m;
        await priceListRepository.UpdateAsync(priceList);

        var restoredOrder = await orderRepository.GetByIdAsync(order.Id);
        Assert.NotNull(restoredOrder);
        var storedSnapshot = await snapshotCoordinator.RestoreLatestAsync(restoredOrder!);

        var restoredLine = Assert.Single(restoredOrder!.Constructions[0].SpecificationLines);
        Assert.Equal(100m, restoredLine.UnitPrice);
        Assert.Equal(200m, restoredOrder.Constructions[0].Cost.CostPrice);
        Assert.Equal(260m, restoredOrder.Totals.GrandTotal);
        Assert.Equal(storedSnapshot.TotalCostPrice, restoredOrder.Constructions[0].Cost.CostPrice);
        Assert.Equal(storedSnapshot.TotalPrice, restoredOrder.Totals.GrandTotal);

        var changedPriceList = await priceListRepository.GetByIdAsync(priceList.Id);
        Assert.Equal(12_999m, changedPriceList!.Items[0].BaseRetailPrice);
    }

    [Fact]
    public void Production_transition_requires_snapshot_and_sequential_statuses()
    {
        var lifecycle = new OrderLifecycleService();
        var order = new Order();

        Assert.Throws<InvalidOperationException>(
            () => lifecycle.Transition(order, Core.Models.Orders.OrderStatus.InProduction, false));

        lifecycle.Transition(order, Core.Models.Orders.OrderStatus.Calculated, false);
        lifecycle.Transition(order, Core.Models.Orders.OrderStatus.Approved, false);
        Assert.Throws<InvalidOperationException>(
            () => lifecycle.Transition(order, Core.Models.Orders.OrderStatus.InProduction, false));

        lifecycle.Transition(order, Core.Models.Orders.OrderStatus.InProduction, true);
        Assert.Throws<InvalidOperationException>(() => lifecycle.AssertCanEdit(order));
    }

    private static PriceListVersion CreatePriceList()
    {
        var priceList = new PriceListVersion
        {
            Title = "Production price list",
            EffectiveFrom = DateTime.UtcNow.Date,
            IsActive = true
        };
        priceList.Items.Add(new PriceListItem
        {
            PriceListVersionId = priceList.Id,
            MaterialArticle = "PROFILE-001",
            PurchasePrice = 80m,
            BaseRetailPrice = 130m,
            DealerPrice = 115m
        });
        return priceList;
    }

    private static Order CreateCalculatedOrder()
    {
        var order = new Order();
        order.Header.OrderNumber = $"SNAP-{Guid.NewGuid():N}";
        order.Header.CurrencyCode = "KGS";
        order.ProfitabilityPercent = 30m;
        var construction = order.EnsureDefaultConstruction();
        construction.SpecificationLines.Add(new CostSpecificationLine
        {
            MaterialId = Guid.NewGuid(),
            Category = CostMaterialCategory.Profile,
            MaterialName = "Test profile",
            Article = "PROFILE-001",
            Quantity = 2m,
            Unit = "m",
            UnitPrice = 100m
        });
        construction.Cost.ProfileCost = 200m;
        construction.Cost.ProfitabilityPercent = 30m;
        construction.Cost.NotifyDerived();
        order.Totals.ConstructionsSubtotal = construction.Cost.ConstructionSumTotal;
        order.Totals.NotifyDerived();
        return order;
    }
}
