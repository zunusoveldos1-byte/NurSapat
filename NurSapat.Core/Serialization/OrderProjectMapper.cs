using NurSapat.Core.Models;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;

namespace NurSapat.Core.Serialization;

public static class OrderProjectMapper
{
    public static OrderProjectData ToData(Order order)
    {
        return new OrderProjectData
        {
            Id = order.Id,
            Header = ToHeaderData(order.Header),
            Constructions = order.Constructions.Select(ToConstructionData).ToList(),
            Totals = ToTotalsData(order.Totals),
            Payment = ToPaymentData(order.Payment),
            ProfitabilityPercent = order.ProfitabilityPercent
        };
    }

    public static void ApplyData(Order order, OrderProjectData data)
    {
        order.Constructions.Clear();
        order.Header = FromHeaderData(data.Header);
        order.ProfitabilityPercent = data.ProfitabilityPercent;

        foreach (var constructionData in data.Constructions)
        {
            order.Constructions.Add(FromConstructionData(constructionData));
        }

        ApplyTotalsData(order.Totals, data.Totals);
        ApplyPaymentData(order.Payment, data.Payment);

        order.EnsureDefaultConstruction();
    }

    public static void Reset(Order order)
    {
        order.Constructions.Clear();
        order.Header = new OrderHeader();
        order.ProfitabilityPercent = 30m;
        order.Totals.ConstructionsSubtotal = 0;
        order.Totals.Discount2Percent = 0;
        order.Totals.DeliveryCost = 0;
        order.Totals.AdditionalMaterialsCost = 0;
        order.Totals.AdditionalMaterialsInstallationCost = 0;
        order.Totals.VatPercent = 12m;
        order.Totals.VatMode = VatDisplayMode.NotUsed;
        order.Totals.NotifyDerived();
        order.Payment.AgreedAmount = 0;
        order.Payment.PaidAmount = 0;
        order.Payment.CurrencyCode = "KGS";
        order.Payment.NotifyDebt();
        order.EnsureDefaultConstruction();
    }

    private static OrderHeaderData ToHeaderData(OrderHeader header) => new()
    {
        OrderNumber = header.OrderNumber,
        OrderDate = header.OrderDate,
        CustomerName = header.CustomerName,
        CustomerPhone = header.CustomerPhone,
        SiteAddress = header.SiteAddress,
        DealerName = header.DealerName,
        OrderManagerName = header.OrderManagerName,
        CustomerCity = header.CustomerCity,
        CustomerCode = header.CustomerCode,
        ContractNumber = header.ContractNumber,
        ContractDate = header.ContractDate,
        ProductionDate = header.ProductionDate,
        DeliveryDate = header.DeliveryDate,
        InstallationDate = header.InstallationDate,
        HasDelivery = header.HasDelivery,
        HasInstallation = header.HasInstallation,
        ProfileNote = header.ProfileNote,
        Note = header.Note,
        CurrencyCode = header.CurrencyCode
    };

    private static OrderHeader FromHeaderData(OrderHeaderData data) => new()
    {
        OrderNumber = data.OrderNumber,
        OrderDate = data.OrderDate,
        CustomerName = data.CustomerName,
        CustomerPhone = data.CustomerPhone,
        SiteAddress = data.SiteAddress,
        DealerName = data.DealerName,
        OrderManagerName = data.OrderManagerName,
        CustomerCity = data.CustomerCity,
        CustomerCode = data.CustomerCode,
        ContractNumber = data.ContractNumber,
        ContractDate = data.ContractDate,
        ProductionDate = data.ProductionDate,
        DeliveryDate = data.DeliveryDate,
        InstallationDate = data.InstallationDate,
        HasDelivery = data.HasDelivery,
        HasInstallation = data.HasInstallation,
        ProfileNote = data.ProfileNote,
        Note = data.Note,
        CurrencyCode = data.CurrencyCode
    };

    private static ConstructionProjectData ToConstructionData(OrderConstruction construction) => new()
    {
        Id = construction.Id,
        Number = construction.Number,
        Name = construction.Name,
        IsSelectedForCalculation = construction.IsSelectedForCalculation,
        ProfileSystemName = construction.ProfileSystemName,
        ManufactureDate = construction.ManufactureDate,
        DeliveryDate = construction.DeliveryDate,
        Manager = construction.Manager,
        Supervisor = construction.Supervisor,
        Notes = construction.Notes,
        Appearance = ToAppearanceData(construction.Appearance),
        Geometry = ToGeometryData(construction.Geometry),
        Cost = ToCostData(construction.Cost),
        Metrics = ToMetricsData(construction.Metrics)
    };

    private static OrderConstruction FromConstructionData(ConstructionProjectData data)
    {
        var construction = new OrderConstruction
        {
            Id = data.Id == Guid.Empty ? Guid.NewGuid() : data.Id,
            Number = data.Number,
            Name = data.Name,
            IsSelectedForCalculation = data.IsSelectedForCalculation,
            ProfileSystemName = data.ProfileSystemName,
            ManufactureDate = data.ManufactureDate,
            DeliveryDate = data.DeliveryDate,
            Manager = data.Manager,
            Supervisor = data.Supervisor,
            Notes = data.Notes,
            Appearance = FromAppearanceData(data.Appearance),
            Metrics = FromMetricsData(data.Metrics)
        };

        ApplyGeometryData(construction.Geometry, data.Geometry);
        ApplyCostData(construction.Cost, data.Cost);
        return construction;
    }

    private static ConstructionAppearanceData ToAppearanceData(ConstructionAppearance appearance) => new()
    {
        ColorName = appearance.ColorName,
        ColorHex = appearance.ColorHex,
        ChangeProfile = appearance.ChangeProfile,
        ChangeImposts = appearance.ChangeImposts,
        ChangeSashes = appearance.ChangeSashes,
        ChangeLayout = appearance.ChangeLayout,
        ChangeDecorativeElements = appearance.ChangeDecorativeElements
    };

    private static ConstructionAppearance FromAppearanceData(ConstructionAppearanceData? data)
    {
        if (data is null)
        {
            return new ConstructionAppearance();
        }

        return new ConstructionAppearance
        {
            ColorName = data.ColorName,
            ColorHex = data.ColorHex,
            ChangeProfile = data.ChangeProfile,
            ChangeImposts = data.ChangeImposts,
            ChangeSashes = data.ChangeSashes,
            ChangeLayout = data.ChangeLayout,
            ChangeDecorativeElements = data.ChangeDecorativeElements
        };
    }

    private static GeometryProjectData ToGeometryData(ConstructionGeometry geometry) => new()
    {
        OuterWidthMm = geometry.OuterWidthMm,
        OuterHeightMm = geometry.OuterHeightMm,
        DrawingJson = geometry.DrawingJson,
        Elements = geometry.Elements.Select(ToElementData).ToList()
    };

    private static void ApplyGeometryData(ConstructionGeometry geometry, GeometryProjectData data)
    {
        geometry.OuterWidthMm = data.OuterWidthMm;
        geometry.OuterHeightMm = data.OuterHeightMm;
        geometry.DrawingJson = data.DrawingJson;
        geometry.Elements.Clear();
        foreach (var element in data.Elements)
        {
            geometry.Elements.Add(FromElementData(element));
        }
    }

    private static GeometryElementData ToElementData(GeometryElement element) => new()
    {
        Id = element.Id,
        Kind = element.Kind.ToString(),
        Label = element.Label,
        X = element.X,
        Y = element.Y,
        WidthMm = element.WidthMm,
        HeightMm = element.HeightMm,
        OpeningType = element.OpeningType?.ToString(),
        HingeSide = element.HingeSide?.ToString(),
        HasHardware = element.HasHardware,
        HasHandle = element.HasHandle,
        HostSashId = element.HostSashId,
        MaterialId = element.MaterialId,
        SourceToolId = element.SourceToolId,
        EndX = element.EndX,
        EndY = element.EndY
    };

    private static GeometryElement FromElementData(GeometryElementData data)
    {
        Enum.TryParse<GeometryElementKind>(data.Kind, out var kind);
        SashOpeningType? opening = null;
        if (!string.IsNullOrEmpty(data.OpeningType) &&
            Enum.TryParse<SashOpeningType>(data.OpeningType, out var parsedOpening))
        {
            opening = parsedOpening;
        }

        SashHingeSide? hinge = null;
        if (!string.IsNullOrEmpty(data.HingeSide) &&
            Enum.TryParse<SashHingeSide>(data.HingeSide, out var parsedHinge))
        {
            hinge = parsedHinge;
        }

        return new GeometryElement
        {
            Id = data.Id == Guid.Empty ? Guid.NewGuid() : data.Id,
            Kind = kind,
            Label = data.Label,
            X = data.X,
            Y = data.Y,
            WidthMm = data.WidthMm,
            HeightMm = data.HeightMm,
            OpeningType = opening,
            HingeSide = hinge,
            HasHardware = data.HasHardware,
            HasHandle = data.HasHandle,
            HostSashId = data.HostSashId,
            MaterialId = data.MaterialId,
            SourceToolId = data.SourceToolId,
            EndX = data.EndX,
            EndY = data.EndY
        };
    }

    private static ConstructionCostData ToCostData(ConstructionCostBreakdown cost) => new()
    {
        ProfileCost = cost.ProfileCost,
        ComponentsCost = cost.ComponentsCost,
        SealantCost = cost.SealantCost,
        GlazingCost = cost.GlazingCost,
        HardwareCost = cost.HardwareCost,
        InstallationMaterialsCost = cost.InstallationMaterialsCost,
        Expenses = cost.Expenses,
        ExpensesMode = cost.ExpensesMode.ToString(),
        LaborCost = cost.LaborCost,
        MaterialsWithoutMarkup = cost.MaterialsWithoutMarkup,
        ProfitabilityPercent = cost.ProfitabilityPercent,
        AdditionalExpensesPercent = cost.AdditionalExpensesPercent,
        AdditionalExpensesFixed = cost.AdditionalExpensesFixed,
        Discount1Percent = cost.Discount1Percent,
        DemolitionCost = cost.DemolitionCost,
        InstallationCost = cost.InstallationCost,
        Quantity = cost.Quantity
    };

    private static void ApplyCostData(ConstructionCostBreakdown cost, ConstructionCostData data)
    {
        cost.ProfileCost = data.ProfileCost;
        cost.ComponentsCost = data.ComponentsCost;
        cost.SealantCost = data.SealantCost;
        cost.GlazingCost = data.GlazingCost;
        cost.HardwareCost = data.HardwareCost;
        cost.InstallationMaterialsCost = data.InstallationMaterialsCost;
        cost.Expenses = data.Expenses;
        if (Enum.TryParse<CostInputMode>(data.ExpensesMode, out var mode))
        {
            cost.ExpensesMode = mode;
        }

        cost.LaborCost = data.LaborCost;
        cost.MaterialsWithoutMarkup = data.MaterialsWithoutMarkup;
        cost.ProfitabilityPercent = data.ProfitabilityPercent;
        cost.AdditionalExpensesPercent = data.AdditionalExpensesPercent;
        cost.AdditionalExpensesFixed = data.AdditionalExpensesFixed;
        cost.Discount1Percent = data.Discount1Percent;
        cost.DemolitionCost = data.DemolitionCost;
        cost.InstallationCost = data.InstallationCost;
        cost.Quantity = data.Quantity;
    }

    private static ConstructionMetricsData ToMetricsData(ConstructionMetrics metrics) => new()
    {
        GlazingAreaSqM = metrics.GlazingAreaSqM,
        ProductAreaSqM = metrics.ProductAreaSqM,
        WeightKg = metrics.WeightKg,
        ManufacturingTimeTicks = metrics.ManufacturingTime.Ticks
    };

    private static ConstructionMetrics FromMetricsData(ConstructionMetricsData data) => new()
    {
        GlazingAreaSqM = data.GlazingAreaSqM,
        ProductAreaSqM = data.ProductAreaSqM,
        WeightKg = data.WeightKg,
        ManufacturingTime = TimeSpan.FromTicks(data.ManufacturingTimeTicks)
    };

    private static OrderCostTotalsData ToTotalsData(OrderCostTotals totals) => new()
    {
        ConstructionsSubtotal = totals.ConstructionsSubtotal,
        Discount2Percent = totals.Discount2Percent,
        DeliveryCost = totals.DeliveryCost,
        AdditionalMaterialsCost = totals.AdditionalMaterialsCost,
        AdditionalMaterialsInstallationCost = totals.AdditionalMaterialsInstallationCost,
        VatPercent = totals.VatPercent,
        VatMode = totals.VatMode.ToString()
    };

    private static void ApplyTotalsData(OrderCostTotals totals, OrderCostTotalsData data)
    {
        totals.ConstructionsSubtotal = data.ConstructionsSubtotal;
        totals.Discount2Percent = data.Discount2Percent;
        totals.DeliveryCost = data.DeliveryCost;
        totals.AdditionalMaterialsCost = data.AdditionalMaterialsCost;
        totals.AdditionalMaterialsInstallationCost = data.AdditionalMaterialsInstallationCost;
        totals.VatPercent = data.VatPercent;
        if (Enum.TryParse<VatDisplayMode>(data.VatMode, out var vatMode))
        {
            totals.VatMode = vatMode;
        }

        totals.NotifyDerived();
    }

    private static OrderPaymentData ToPaymentData(OrderPayment payment) => new()
    {
        AgreedAmount = payment.AgreedAmount,
        PaidAmount = payment.PaidAmount,
        CurrencyCode = payment.CurrencyCode
    };

    private static void ApplyPaymentData(OrderPayment payment, OrderPaymentData data)
    {
        payment.AgreedAmount = data.AgreedAmount;
        payment.PaidAmount = data.PaidAmount;
        payment.CurrencyCode = data.CurrencyCode;
        payment.NotifyDebt();
    }
}
