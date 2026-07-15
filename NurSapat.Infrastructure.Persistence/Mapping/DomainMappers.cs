using NurSapat.Core.Models;
using NurSapat.Core.Models.Calculation;
using NurSapat.Core.Models.Orders;
using NurSapat.Infrastructure.Persistence.Entities;

namespace NurSapat.Infrastructure.Persistence.Mapping;

internal static class CustomerMapper
{
    public static Customer ToDomain(CustomerEntity entity) => new()
    {
        Id = entity.Id,
        FullName = entity.FullName,
        Phone = entity.Phone,
        Type = (CustomerType)entity.Type,
        Turnover = entity.Turnover,
        Debt = entity.Debt,
        OkpoCode = entity.OkpoCode,
        BankName = entity.BankName,
        CheckingAccount = entity.CheckingAccount,
        Mfo = entity.Mfo,
        LegalAddress = entity.LegalAddress,
        City = entity.City,
        ActualAddress = entity.ActualAddress,
        DealerCode = entity.DealerCode,
        Note = entity.Note,
        IsBlocked = entity.IsBlocked,
        IsDeleted = entity.IsDeleted,
        CreatedAt = entity.CreatedAt.ToLocalTime(),
        ModifiedAt = entity.ModifiedAt.ToLocalTime()
    };

    public static CustomerEntity ToEntity(Customer domain) => new()
    {
        Id = domain.Id,
        FullName = domain.FullName,
        Phone = domain.Phone,
        Type = (int)domain.Type,
        Turnover = domain.Turnover,
        Debt = domain.Debt,
        OkpoCode = domain.OkpoCode,
        BankName = domain.BankName,
        CheckingAccount = domain.CheckingAccount,
        Mfo = domain.Mfo,
        LegalAddress = domain.LegalAddress,
        City = domain.City,
        ActualAddress = domain.ActualAddress,
        DealerCode = domain.DealerCode,
        Note = domain.Note,
        IsBlocked = domain.IsBlocked,
        IsDeleted = domain.IsDeleted,
        IsActive = !domain.IsDeleted && !domain.IsBlocked,
        CreatedAt = domain.CreatedAt.ToUniversalTime(),
        ModifiedAt = domain.ModifiedAt.ToUniversalTime()
    };
}

internal static class SupplierMapper
{
    public static Supplier ToDomain(SupplierEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Phone = entity.Phone,
        LegalAddress = entity.LegalAddress,
        ContractTerms = entity.ContractTerms,
        Note = entity.Note
    };

    public static SupplierEntity ToEntity(Supplier domain) => new()
    {
        Id = domain.Id,
        Name = domain.Name,
        Phone = domain.Phone,
        LegalAddress = domain.LegalAddress,
        ContractTerms = domain.ContractTerms,
        Note = domain.Note
    };
}

internal static class OrderMapper
{
    public static Order ToDomain(OrderEntity entity)
    {
        var order = new Order
        {
            Id = entity.Id,
            ProfitabilityPercent = entity.ProfitabilityPercent,
            ExportFilePath = entity.ExportFilePath
        };

        order.Header.OrderNumber = entity.OrderNumber;
        order.Header.OrderDate = entity.OrderDate;
        order.Header.Status = (OrderStatus)entity.Status;
        order.Header.CustomerId = entity.CustomerId;
        order.Header.DealerId = entity.DealerId;
        order.Header.CurrentPriceMode = (PriceModeType)entity.CurrentPriceMode;
        order.Header.TotalDiscountPercent = entity.TotalDiscountPercent;
        order.Header.TotalDiscountSum = entity.TotalDiscountSum;
        order.Header.CustomerName = entity.CustomerName;
        order.Header.CustomerPhone = entity.CustomerPhone;
        order.Header.SiteAddress = entity.SiteAddress;
        order.Header.DealerName = entity.DealerName;
        order.Header.OrderManagerName = entity.OrderManagerName;
        order.Header.CustomerCity = entity.CustomerCity;
        order.Header.CustomerCode = entity.CustomerCode;
        order.Header.ContractNumber = entity.ContractNumber;
        order.Header.ContractDate = entity.ContractDate;
        order.Header.ProductionDate = entity.ProductionDate;
        order.Header.DeliveryDate = entity.DeliveryDate;
        order.Header.InstallationDate = entity.InstallationDate;
        order.Header.HasDelivery = entity.HasDelivery;
        order.Header.HasInstallation = entity.HasInstallation;
        order.Header.ProfileNote = entity.ProfileNote;
        order.Header.ProductionNote = entity.ProductionNote;
        order.Header.Note = entity.Note;
        order.Header.CurrencyCode = entity.CurrencyCode;

        order.Totals.ConstructionsSubtotal = entity.ConstructionsSubtotal;
        order.Totals.Discount2Percent = entity.Discount2Percent;
        order.Totals.DeliveryCost = entity.DeliveryCost;
        order.Totals.AdditionalMaterialsCost = entity.AdditionalMaterialsCost;
        order.Totals.AdditionalMaterialsInstallationCost = entity.AdditionalMaterialsInstallationCost;
        order.Totals.VatPercent = entity.VatPercent;
        order.Totals.VatMode = (VatDisplayMode)entity.VatMode;

        order.Payment.AgreedAmount = entity.AgreedAmount;
        order.Payment.PaidAmount = entity.PaidAmount;
        order.Payment.CurrencyCode = entity.PaymentCurrencyCode;

        foreach (var constructionEntity in entity.Constructions.OrderBy(c => c.Number))
        {
            order.Constructions.Add(ToDomainConstruction(constructionEntity));
        }

        if (entity.SelectedConstructionId.HasValue)
        {
            order.SelectedConstruction = order.Constructions
                .FirstOrDefault(c => c.Id == entity.SelectedConstructionId.Value);
        }

        return order;
    }

    public static OrderEntity ToEntity(Order domain)
    {
        var entity = new OrderEntity
        {
            Id = domain.Id,
            ProfitabilityPercent = domain.ProfitabilityPercent,
            ExportFilePath = domain.ExportFilePath,
            OrderNumber = domain.Header.OrderNumber,
            OrderDate = domain.Header.OrderDate,
            Status = (int)domain.Header.Status,
            CustomerId = domain.Header.CustomerId,
            DealerId = domain.Header.DealerId,
            CurrentPriceMode = (int)domain.Header.CurrentPriceMode,
            TotalDiscountPercent = domain.Header.TotalDiscountPercent,
            TotalDiscountSum = domain.Header.TotalDiscountSum,
            CustomerName = domain.Header.CustomerName,
            CustomerPhone = domain.Header.CustomerPhone,
            SiteAddress = domain.Header.SiteAddress,
            DealerName = domain.Header.DealerName,
            OrderManagerName = domain.Header.OrderManagerName,
            CustomerCity = domain.Header.CustomerCity,
            CustomerCode = domain.Header.CustomerCode,
            ContractNumber = domain.Header.ContractNumber,
            ContractDate = domain.Header.ContractDate,
            ProductionDate = domain.Header.ProductionDate,
            DeliveryDate = domain.Header.DeliveryDate,
            InstallationDate = domain.Header.InstallationDate,
            HasDelivery = domain.Header.HasDelivery,
            HasInstallation = domain.Header.HasInstallation,
            ProfileNote = domain.Header.ProfileNote,
            ProductionNote = domain.Header.ProductionNote,
            Note = domain.Header.Note,
            CurrencyCode = domain.Header.CurrencyCode,
            ConstructionsSubtotal = domain.Totals.ConstructionsSubtotal,
            Discount2Percent = domain.Totals.Discount2Percent,
            DeliveryCost = domain.Totals.DeliveryCost,
            AdditionalMaterialsCost = domain.Totals.AdditionalMaterialsCost,
            AdditionalMaterialsInstallationCost = domain.Totals.AdditionalMaterialsInstallationCost,
            VatPercent = domain.Totals.VatPercent,
            VatMode = (int)domain.Totals.VatMode,
            AgreedAmount = domain.Payment.AgreedAmount,
            PaidAmount = domain.Payment.PaidAmount,
            PaymentCurrencyCode = domain.Payment.CurrencyCode,
            SelectedConstructionId = domain.SelectedConstruction?.Id,
            Constructions = domain.Constructions.Select(ToEntityConstruction).ToList()
        };

        return entity;
    }

    private static OrderConstruction ToDomainConstruction(OrderConstructionEntity entity)
    {
        var construction = new OrderConstruction
        {
            Id = entity.Id,
            Number = entity.Number,
            Name = entity.Name,
            IsSelectedForCalculation = entity.IsSelectedForCalculation,
            ProfileSystemName = entity.ProfileSystemName,
            ManufactureDate = entity.ManufactureDate,
            DeliveryDate = entity.DeliveryDate,
            Manager = entity.Manager,
            Supervisor = entity.Supervisor,
            Notes = entity.Notes
        };

        construction.Geometry.OuterWidthMm = entity.OuterWidthMm;
        construction.Geometry.OuterHeightMm = entity.OuterHeightMm;
        construction.Geometry.DrawingJson = entity.DrawingJson;

        construction.Metrics.GlazingAreaSqM = entity.GlazingAreaSqM;
        construction.Metrics.ProductAreaSqM = entity.ProductAreaSqM;
        construction.Metrics.WeightKg = entity.WeightKg;
        construction.Metrics.ManufacturingTime = TimeSpan.FromTicks(entity.ManufacturingTimeTicks);

        construction.Cost.ProfileCost = entity.ProfileCost;
        construction.Cost.ComponentsCost = entity.ComponentsCost;
        construction.Cost.SealantCost = entity.SealantCost;
        construction.Cost.GlazingCost = entity.GlazingCost;
        construction.Cost.HardwareCost = entity.HardwareCost;
        construction.Cost.InstallationMaterialsCost = entity.InstallationMaterialsCost;
        construction.Cost.Expenses = entity.Expenses;
        construction.Cost.ExpensesMode = (CostInputMode)entity.ExpensesMode;
        construction.Cost.LaborCost = entity.LaborCost;
        construction.Cost.MaterialsWithoutMarkup = entity.MaterialsWithoutMarkup;
        construction.Cost.ProfitabilityPercent = entity.ProfitabilityPercent;
        construction.Cost.AdditionalExpensesPercent = entity.AdditionalExpensesPercent;
        construction.Cost.AdditionalExpensesFixed = entity.AdditionalExpensesFixed;
        construction.Cost.Discount1Percent = entity.Discount1Percent;
        construction.Cost.DemolitionCost = entity.DemolitionCost;
        construction.Cost.InstallationCost = entity.InstallationCost;
        construction.Cost.Quantity = entity.Quantity;

        construction.Appearance.ColorName = entity.ColorName;
        construction.Appearance.ColorHex = entity.ColorHex;
        construction.Appearance.ChangeProfile = entity.ChangeProfile;
        construction.Appearance.ChangeImposts = entity.ChangeImposts;
        construction.Appearance.ChangeSashes = entity.ChangeSashes;
        construction.Appearance.ChangeLayout = entity.ChangeLayout;
        construction.Appearance.ChangeDecorativeElements = entity.ChangeDecorativeElements;

        foreach (var element in entity.GeometryElements)
        {
            construction.Geometry.Elements.Add(ToDomainGeometry(element));
        }

        foreach (var line in entity.SpecificationLines)
        {
            construction.SpecificationLines.Add(ToDomainSpecLine(line));
        }

        return construction;
    }

    private static OrderConstructionEntity ToEntityConstruction(OrderConstruction domain)
    {
        return new OrderConstructionEntity
        {
            Id = domain.Id,
            Number = domain.Number,
            Name = domain.Name,
            IsSelectedForCalculation = domain.IsSelectedForCalculation,
            OuterWidthMm = domain.Geometry.OuterWidthMm,
            OuterHeightMm = domain.Geometry.OuterHeightMm,
            DrawingJson = domain.Geometry.DrawingJson,
            GlazingAreaSqM = domain.Metrics.GlazingAreaSqM,
            ProductAreaSqM = domain.Metrics.ProductAreaSqM,
            WeightKg = domain.Metrics.WeightKg,
            ManufacturingTimeTicks = domain.Metrics.ManufacturingTime.Ticks,
            ProfileCost = domain.Cost.ProfileCost,
            ComponentsCost = domain.Cost.ComponentsCost,
            SealantCost = domain.Cost.SealantCost,
            GlazingCost = domain.Cost.GlazingCost,
            HardwareCost = domain.Cost.HardwareCost,
            InstallationMaterialsCost = domain.Cost.InstallationMaterialsCost,
            Expenses = domain.Cost.Expenses,
            ExpensesMode = (int)domain.Cost.ExpensesMode,
            LaborCost = domain.Cost.LaborCost,
            MaterialsWithoutMarkup = domain.Cost.MaterialsWithoutMarkup,
            ProfitabilityPercent = domain.Cost.ProfitabilityPercent,
            AdditionalExpensesPercent = domain.Cost.AdditionalExpensesPercent,
            AdditionalExpensesFixed = domain.Cost.AdditionalExpensesFixed,
            Discount1Percent = domain.Cost.Discount1Percent,
            DemolitionCost = domain.Cost.DemolitionCost,
            InstallationCost = domain.Cost.InstallationCost,
            Quantity = domain.Cost.Quantity,
            ProfileSystemName = domain.ProfileSystemName,
            ColorName = domain.Appearance.ColorName,
            ColorHex = domain.Appearance.ColorHex,
            ChangeProfile = domain.Appearance.ChangeProfile,
            ChangeImposts = domain.Appearance.ChangeImposts,
            ChangeSashes = domain.Appearance.ChangeSashes,
            ChangeLayout = domain.Appearance.ChangeLayout,
            ChangeDecorativeElements = domain.Appearance.ChangeDecorativeElements,
            ManufactureDate = domain.ManufactureDate,
            DeliveryDate = domain.DeliveryDate,
            Manager = domain.Manager,
            Supervisor = domain.Supervisor,
            Notes = domain.Notes,
            GeometryElements = domain.Geometry.Elements.Select(ToEntityGeometry).ToList(),
            SpecificationLines = domain.SpecificationLines.Select(ToEntitySpecLine).ToList()
        };
    }

    private static GeometryElement ToDomainGeometry(GeometryElementEntity entity) => new()
    {
        Id = entity.Id,
        Kind = (GeometryElementKind)entity.Kind,
        Label = entity.Label,
        X = entity.X,
        Y = entity.Y,
        WidthMm = entity.WidthMm,
        HeightMm = entity.HeightMm,
        OpeningType = entity.OpeningType.HasValue ? (SashOpeningType)entity.OpeningType.Value : null,
        HingeSide = entity.HingeSide.HasValue ? (SashHingeSide)entity.HingeSide.Value : null,
        HasHardware = entity.HasHardware,
        HasHandle = entity.HasHandle,
        HasMosquitoNet = entity.HasMosquitoNet,
        HostSashId = entity.HostSashId,
        ParentGlazingId = entity.ParentGlazingId,
        SpokeType = entity.SpokeType.HasValue ? (DecorativeSpokeType)entity.SpokeType.Value : null,
        SpokeThicknessMm = entity.SpokeThicknessMm,
        SpokeColor = entity.SpokeColor,
        SpokeRowsCount = entity.SpokeRowsCount,
        SpokeColumnsCount = entity.SpokeColumnsCount,
        SpokeRayCount = entity.SpokeRayCount,
        SpokeIsArched = entity.SpokeIsArched,
        SpokeArcCenterX = entity.SpokeArcCenterX,
        SpokeArcCenterY = entity.SpokeArcCenterY,
        SpokeArcRadius = entity.SpokeArcRadius,
        ProfileSeriesId = entity.ProfileSeriesId,
        MaterialId = entity.MaterialId,
        SourceToolId = entity.SourceToolId,
        EndX = entity.EndX,
        EndY = entity.EndY,
        IsExtremeImpost = entity.IsExtremeImpost,
        EdgeAlignment = entity.EdgeAlignment.HasValue ? (ImpostEdgeAlignment)entity.EdgeAlignment.Value : null,
        ExtremeImpostOffsetMm = entity.ExtremeImpostOffsetMm,
        HostFrameNodeId = entity.HostFrameNodeId,
        ExtremeImpostSlotId = entity.ExtremeImpostSlotId,
        FillType = entity.FillType.HasValue ? (GlazingType)entity.FillType.Value : null,
        Shape = entity.Shape.HasValue ? (ConstructionShape)entity.Shape.Value : null
    };

    private static GeometryElementEntity ToEntityGeometry(GeometryElement domain) => new()
    {
        Id = domain.Id,
        Kind = (int)domain.Kind,
        Label = domain.Label,
        X = domain.X,
        Y = domain.Y,
        WidthMm = domain.WidthMm,
        HeightMm = domain.HeightMm,
        OpeningType = domain.OpeningType.HasValue ? (int)domain.OpeningType.Value : null,
        HingeSide = domain.HingeSide.HasValue ? (int)domain.HingeSide.Value : null,
        HasHardware = domain.HasHardware,
        HasHandle = domain.HasHandle,
        HasMosquitoNet = domain.HasMosquitoNet,
        HostSashId = domain.HostSashId,
        ParentGlazingId = domain.ParentGlazingId,
        SpokeType = domain.SpokeType.HasValue ? (int)domain.SpokeType.Value : null,
        SpokeThicknessMm = domain.SpokeThicknessMm,
        SpokeColor = domain.SpokeColor,
        SpokeRowsCount = domain.SpokeRowsCount,
        SpokeColumnsCount = domain.SpokeColumnsCount,
        SpokeRayCount = domain.SpokeRayCount,
        SpokeIsArched = domain.SpokeIsArched,
        SpokeArcCenterX = domain.SpokeArcCenterX,
        SpokeArcCenterY = domain.SpokeArcCenterY,
        SpokeArcRadius = domain.SpokeArcRadius,
        ProfileSeriesId = domain.ProfileSeriesId,
        MaterialId = domain.MaterialId,
        SourceToolId = domain.SourceToolId,
        EndX = domain.EndX,
        EndY = domain.EndY,
        IsExtremeImpost = domain.IsExtremeImpost,
        EdgeAlignment = domain.EdgeAlignment.HasValue ? (int)domain.EdgeAlignment.Value : null,
        ExtremeImpostOffsetMm = domain.ExtremeImpostOffsetMm,
        HostFrameNodeId = domain.HostFrameNodeId,
        ExtremeImpostSlotId = domain.ExtremeImpostSlotId,
        FillType = domain.FillType.HasValue ? (int)domain.FillType.Value : null,
        Shape = domain.Shape.HasValue ? (int)domain.Shape.Value : null
    };

    private static CostSpecificationLine ToDomainSpecLine(CostSpecificationLineEntity entity) => new()
    {
        MaterialId = entity.MaterialId,
        ConstructionMaterialId = entity.ConstructionMaterialId,
        Category = (CostMaterialCategory)entity.Category,
        MaterialName = entity.MaterialName,
        Article = entity.Article,
        Quantity = entity.Quantity,
        Unit = entity.Unit,
        UnitPrice = entity.UnitPrice,
        WastePercent = entity.WastePercent
    };

    private static CostSpecificationLineEntity ToEntitySpecLine(CostSpecificationLine domain) => new()
    {
        Id = Guid.NewGuid(),
        MaterialId = domain.MaterialId,
        ConstructionMaterialId = domain.ConstructionMaterialId,
        Category = (int)domain.Category,
        MaterialName = domain.MaterialName,
        Article = domain.Article,
        Quantity = domain.Quantity,
        Unit = domain.Unit,
        UnitPrice = domain.UnitPrice,
        WastePercent = domain.WastePercent
    };
}
