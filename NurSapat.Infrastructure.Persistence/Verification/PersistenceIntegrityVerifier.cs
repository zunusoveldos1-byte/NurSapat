using NurSapat.Core.Interfaces.Persistence;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Materials;
using NurSapat.Infrastructure.Persistence.Abstractions;
using NurSapat.Infrastructure.Persistence.Models;

namespace NurSapat.Infrastructure.Persistence.Verification;

public sealed class PersistenceIntegrityVerifier
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IErpMaterialRepository _materialRepository;
    private readonly IWarehouseRepository _warehouseRepository;

    public PersistenceIntegrityVerifier(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IErpMaterialRepository materialRepository,
        IWarehouseRepository warehouseRepository)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _materialRepository = materialRepository;
        _warehouseRepository = warehouseRepository;
    }

    public async Task<IReadOnlyList<string>> VerifyAsync(CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        await VerifyCustomerRoundTripAsync(errors, cancellationToken);
        await VerifyMaterialRoundTripAsync(errors, cancellationToken);
        await VerifyOrderRoundTripAsync(errors, cancellationToken);
        await VerifyWarehouseRoundTripAsync(errors, cancellationToken);

        return errors;
    }

    private async Task VerifyCustomerRoundTripAsync(List<string> errors, CancellationToken cancellationToken)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            FullName = "Тест заказчик",
            Phone = "+996 555 000 111",
            Type = CustomerType.Individual,
            Turnover = 1000m,
            Debt = 50m
        };

        await _customerRepository.SaveAsync(customer, cancellationToken);
        var loaded = await _customerRepository.GetByIdAsync(customer.Id, cancellationToken);
        if (loaded is null || loaded.FullName != customer.FullName || loaded.Debt != customer.Debt)
        {
            errors.Add("Customer round-trip failed.");
        }

        await _customerRepository.DeleteAsync(customer.Id, cancellationToken);
    }

    private async Task VerifyMaterialRoundTripAsync(List<string> errors, CancellationToken cancellationToken)
    {
        var material = new ErpMaterialDetail
        {
            Id = Guid.NewGuid(),
            Article = "TEST-ART-001",
            Name = "Тестовый материал",
            Section = ErpMaterialSection.Components,
            MainUnit = ErpMaterialMainUnit.Piece,
            BasePrice = 99.5m,
            ColorPrices =
            [
                new ErpMaterialColorPrice
                {
                    Id = Guid.NewGuid(),
                    ColorName = "Белый",
                    Price = 99.5m,
                    IsDefault = true
                }
            ]
        };

        await _materialRepository.SaveAsync(material, cancellationToken);
        var loaded = await _materialRepository.GetByArticleAsync(material.Article, cancellationToken);
        if (loaded is null || loaded.Name != material.Name || loaded.BasePrice != material.BasePrice)
        {
            errors.Add("Material round-trip failed.");
        }

        await _materialRepository.DeleteAsync(material.Id, cancellationToken);
    }

    private async Task VerifyOrderRoundTripAsync(List<string> errors, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ProfitabilityPercent = 25m
        };
        order.Header.OrderNumber = $"VERIFY-{order.Id:N}";
        order.Header.OrderDate = DateTime.Today;
        order.Header.Status = OrderStatus.Draft;

        var construction = new OrderConstruction
        {
            Id = Guid.NewGuid(),
            Number = 1,
            Name = "Тестовая конструкция"
        };
        construction.Geometry.OuterWidthMm = 1200m;
        construction.Geometry.OuterHeightMm = 1400m;
        construction.Geometry.Elements.Add(new GeometryElement
        {
            Id = Guid.NewGuid(),
            Kind = GeometryElementKind.Frame,
            Label = "Рама",
            WidthMm = 1200m,
            HeightMm = 1400m,
            MaterialId = CatalogMaterialIds.ProfileRehau70
        });
        construction.SpecificationLines.Add(new CostSpecificationLine
        {
            MaterialId = CatalogMaterialIds.ProfileRehau70,
            Category = CostMaterialCategory.Profile,
            MaterialName = "Профиль Rehau 70",
            Article = CatalogMaterialArticles.ProfileRehau70,
            Quantity = 12.5m,
            Unit = "м/п",
            UnitPrice = 1250m,
            WastePercent = 5m,
            PriceSource = new MaterialPriceSource("Каталог", 15m, "USD", 83.3333m, new DateTime(2026, 11, 21))
        });
        construction.Cost.ProfileCost = 15625m;
        construction.Cost.Quantity = 1;
        order.Constructions.Add(construction);
        order.Totals.ConstructionsSubtotal = construction.Cost.ConstructionSumTotal;
        order.Payment.AgreedAmount = order.Totals.GrandTotal;

        await _orderRepository.SaveAsync(order, cancellationToken);
        try
        {
            var loaded = await _orderRepository.GetByIdAsync(order.Id, cancellationToken);
            if (loaded is null
                || loaded.Header.OrderNumber != order.Header.OrderNumber
                || loaded.Constructions.Count != 1
                || loaded.Constructions[0].Geometry.Elements.Count != 1
                || loaded.Constructions[0].SpecificationLines.Count != 1
                || loaded.Constructions[0].SpecificationLines[0].Article != order.Constructions[0].SpecificationLines[0].Article
                || loaded.Constructions[0].SpecificationLines[0].PriceSource != construction.SpecificationLines[0].PriceSource)
            {
                errors.Add("Order round-trip failed.");
                return;
            }

            // Saving a reopened order reuses the existing construction and geometry IDs.
            loaded.Constructions[0].Geometry.Elements[0].WidthMm = 1300m;
            await _orderRepository.UpdateAsync(loaded, cancellationToken);
            var updated = await _orderRepository.GetByIdAsync(order.Id, cancellationToken);
            if (updated?.Constructions.Single().Geometry.Elements.Single().WidthMm != 1300m
                || updated.Constructions.Single().SpecificationLines.Single().UnitPrice != 1250m)
            {
                errors.Add("Order update round-trip failed.");
            }
        }
        finally
        {
            await _orderRepository.DeleteAsync(order.Id, cancellationToken);
        }
    }

    private async Task VerifyWarehouseRoundTripAsync(List<string> errors, CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        var order = new Order
        {
            Id = orderId,
            ProfitabilityPercent = 30m
        };
        order.Header.OrderNumber = "WH-ORD-001";
        await _orderRepository.SaveAsync(order, cancellationToken);

        var document = new StockDocumentModel
        {
            Id = Guid.NewGuid(),
            Number = "WH-DOC-001",
            Date = DateTime.Today,
            Type = StockDocumentTypeModel.ReceiptAct,
            Lines =
            [
                new StockDocumentLineModel
                {
                    Id = Guid.NewGuid(),
                    MaterialId = CatalogMaterialIds.ProfileRehau70,
                    Article = CatalogMaterialArticles.ProfileRehau70,
                    Name = "Профиль Rehau 70",
                    Unit = "м/п",
                    Quantity = 10m,
                    Price = 1250m
                }
            ]
        };

        await _warehouseRepository.SaveDocumentAsync(document, cancellationToken);
        var loadedDocument = await _warehouseRepository.GetDocumentByIdAsync(document.Id, cancellationToken);
        if (loadedDocument is null || loadedDocument.Lines.Count != 1 || loadedDocument.Lines[0].Quantity != 10m)
        {
            errors.Add("Stock document round-trip failed.");
        }

        var finishedGood = new FinishedGoodModel
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            OrderNumber = order.Header.OrderNumber,
            ProductCode = "FG-001",
            CellAddress = "A-01",
            State = FinishedGoodStateModel.OnWarehouse,
            DateIncoming = DateTime.Today,
            UserName = "tester"
        };

        await _warehouseRepository.SaveFinishedGoodAsync(finishedGood, cancellationToken);
        var loadedFinishedGood = await _warehouseRepository.GetFinishedGoodByIdAsync(finishedGood.Id, cancellationToken);
        if (loadedFinishedGood is null || loadedFinishedGood.ProductCode != finishedGood.ProductCode)
        {
            errors.Add("Finished good round-trip failed.");
        }

        await _warehouseRepository.DeleteFinishedGoodAsync(finishedGood.Id, cancellationToken);
        await _warehouseRepository.DeleteDocumentAsync(document.Id, cancellationToken);
        await _orderRepository.DeleteAsync(orderId, cancellationToken);
    }
}
