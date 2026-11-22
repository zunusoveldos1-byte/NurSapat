using NurSapat.Core.Models;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Services.Orders;

namespace NurSapat.Infrastructure.Persistence.Tests;

public sealed class OrderProductionValidatorTests
{
    [Fact]
    public void Review_points_to_the_sash_with_missing_dimensions_and_hardware()
    {
        var order = new Order();
        var construction = order.EnsureDefaultConstruction();
        construction.IsSelectedForCalculation = true;
        var frameSeries = Guid.NewGuid();
        var sash = new GeometryElement
        {
            Kind = GeometryElementKind.Sash, OpeningType = SashOpeningType.TiltTurn,
            MaterialId = Guid.NewGuid(), ProfileSeriesId = Guid.NewGuid(), HeightMm = 800
        };
        construction.Geometry.Elements.Add(sash);
        construction.Geometry.Elements.Add(new GeometryElement
        {
            Kind = GeometryElementKind.Frame, MaterialId = Guid.NewGuid(), ProfileSeriesId = frameSeries,
            WidthMm = 1200, HeightMm = 1400
        });

        var issues = OrderProductionValidator.Review(order, "ORD-10");

        Assert.DoesNotContain(issues, issue => issue.ConstructionId is null);
        Assert.Contains(issues, issue => issue.ConstructionId == construction.Id && issue.ElementId == sash.Id
            && issue.BlocksProduction && issue.Message.Contains("ширину"));
        Assert.Contains(issues, issue => issue.ElementId == sash.Id && issue.BlocksProduction
            && issue.Message.Contains("фурнитура"));
        Assert.Contains(issues, issue => issue.ElementId == sash.Id && !issue.BlocksProduction
            && issue.Message.Contains("совместимость"));

        sash.WidthMm = 600;
        sash.HasHardware = true;
        sash.ProfileSeriesId = frameSeries;
        construction.SpecificationLines.Add(new CostSpecificationLine
        {
            MaterialId = sash.MaterialId.Value, Category = CostMaterialCategory.Profile,
            Article = "P-1", Quantity = 4, UnitPrice = 150
        });

        Assert.Empty(OrderProductionValidator.Review(order, "ORD-10"));
    }

    [Fact]
    public void Review_requires_an_order_number_and_selected_construction()
    {
        var order = new Order();
        order.EnsureDefaultConstruction().IsSelectedForCalculation = false;
        var issues = OrderProductionValidator.Review(order);
        Assert.Equal(2, issues.Count);
        Assert.All(issues, issue => Assert.True(issue.BlocksProduction));
    }
}
