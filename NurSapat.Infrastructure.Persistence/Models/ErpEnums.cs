namespace NurSapat.Infrastructure.Persistence.Models;

/// <summary>Значения совпадают с <c>NurSapat.App.Models.Materials.MaterialSection</c>.</summary>
public enum ErpMaterialSection
{
    Profile,
    Components,
    Sealants,
    Glazing,
    Hardware,
    Labor,
    MaterialWithoutMarkup,
    AdditionalMaterial
}

/// <summary>Значения совпадают с <c>NurSapat.App.Models.Materials.MaterialMainUnit</c>.</summary>
public enum ErpMaterialMainUnit
{
    LinearMeter,
    SquareMeter,
    Piece,
    Kilogram,
    Set
}

/// <summary>Значения совпадают с <c>NurSapat.App.Models.Materials.MaterialLinearGroup</c>.</summary>
public enum ErpMaterialLinearGroup
{
    Profile,
    Reinforcement,
    Bead,
    Spacer
}

/// <summary>Значения совпадают с <c>NurSapat.App.Models.Materials.MaterialSquareGroup</c>.</summary>
public enum ErpMaterialSquareGroup
{
    Glass,
    Panel
}

/// <summary>Значения совпадают с <c>NurSapat.App.Models.Materials.MaterialCurrency</c>.</summary>
public enum ErpMaterialCurrency
{
    Kgs,
    Usd,
    Eur,
    Cny
}

/// <summary>Значения совпадают с <c>NurSapat.App.Models.Stock.StockDocumentType</c>.</summary>
public enum StockDocumentTypeModel
{
    ReceiptAct,
    Invoice,
    InventoryAct,
    Request,
    MinStockLimit
}

/// <summary>Значения совпадают с <c>NurSapat.App.Models.Stock.FinishedGoodState</c>.</summary>
public enum FinishedGoodStateModel
{
    OnWarehouse,
    Shipped,
    QualityControl,
    Defect
}

/// <summary>Значения совпадают с <c>NurSapat.App.Models.Config.ConstructionSectionType</c>.</summary>
public enum ConstructionSectionTypeModel
{
    Frame,
    Sash,
    SlidingSash,
    Impost,
    Glazing,
    Corrugation,
    Bead,
    Blinds,
    MosquitoNet,
    Hardware,
    Profile,
    Glass,
    Reinforcement,
    Accessories,
    Mounting,
    Handle,
    Lock,
    Limiter,
    DoorCloser,
    Latch
}
