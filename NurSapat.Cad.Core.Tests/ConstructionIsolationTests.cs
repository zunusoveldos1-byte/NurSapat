using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Serialization;

namespace NurSapat.Cad.Core.Tests;

public sealed class ConstructionIsolationTests
{
    [Fact]
    public void ConstructionOrderDetails_AreIndependentAndSurviveProjectRoundTrip()
    {
        var order = new Order();
        var first = order.EnsureDefaultConstruction();
        first.OrderDetails.CustomerName = "Первый заказчик";
        first.ProfileSystemName = "Series A";
        first.SelectedMaterialId = Guid.NewGuid();
        first.Appearance.ContourColorName = "Black";

        var second = new OrderConstruction
        {
            Number = 2,
            Name = "Конструкция 2"
        };
        second.OrderDetails.CustomerName = "Второй заказчик";
        order.Constructions.Add(second);

        var restored = new Order();
        OrderProjectMapper.ApplyData(restored, OrderProjectMapper.ToData(order));

        Assert.Equal("Первый заказчик", restored.Constructions[0].OrderDetails.CustomerName);
        Assert.Equal("Второй заказчик", restored.Constructions[1].OrderDetails.CustomerName);
        Assert.Equal("Series A", restored.Constructions[0].ProfileSystemName);
        Assert.Equal(first.SelectedMaterialId, restored.Constructions[0].SelectedMaterialId);
        Assert.Equal("Black", restored.Constructions[0].Appearance.ContourColorName);
    }

    [Fact]
    public void NewDefaultConstruction_HasNoBorrowedDimensions()
    {
        var construction = new Order().EnsureDefaultConstruction();

        Assert.Empty(construction.Geometry.Elements);
        Assert.Equal(0, construction.Geometry.OuterWidthMm);
        Assert.Equal(0, construction.Geometry.OuterHeightMm);
        Assert.Equal("—", construction.DimensionsDisplay);
    }

    [Fact]
    public void Order_KeepsOneObservableConstructionCollectionForTheSession()
    {
        var order = new Order();
        var sessionCollection = order.Constructions;

        var first = order.EnsureDefaultConstruction();
        order.Constructions.Add(new OrderConstruction
        {
            Number = 2,
            Name = "Конструкция 2"
        });

        Assert.Same(sessionCollection, order.Constructions);
        Assert.Same(first, order.Constructions[0]);
        Assert.Equal(2, sessionCollection.Count);
    }
}
