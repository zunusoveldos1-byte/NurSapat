namespace NurSapat.App.Models.Users;

public sealed class UserPermissions
{
    public bool CanManageUsers { get; set; }

    public bool CanAccessMaterialsDictionary { get; set; }

    public bool CanConfigureConstructionElements { get; set; }

    public bool CanAccessEconomics { get; set; }

    public bool CanEditOrderSheetCoefficients { get; set; }

    public bool CanEditOrderSheetCostCoefficients { get; set; }

    public bool CanEditOrderSheetExtraExpenseCoefficients { get; set; }

    public bool CanPrintMaterialPrices { get; set; }

    public bool CanDeleteOrders { get; set; }

    public bool CanAccessWarehouse { get; set; }

    public bool CanEditWarehouse { get; set; }

    public bool CanEditWarehouseInvoice { get; set; }

    public bool GroupOnlyDataAccess { get; set; }

    public UserPermissions Clone() => new()
    {
        CanManageUsers = CanManageUsers,
        CanAccessMaterialsDictionary = CanAccessMaterialsDictionary,
        CanConfigureConstructionElements = CanConfigureConstructionElements,
        CanAccessEconomics = CanAccessEconomics,
        CanEditOrderSheetCoefficients = CanEditOrderSheetCoefficients,
        CanEditOrderSheetCostCoefficients = CanEditOrderSheetCostCoefficients,
        CanEditOrderSheetExtraExpenseCoefficients = CanEditOrderSheetExtraExpenseCoefficients,
        CanPrintMaterialPrices = CanPrintMaterialPrices,
        CanDeleteOrders = CanDeleteOrders,
        CanAccessWarehouse = CanAccessWarehouse,
        CanEditWarehouse = CanEditWarehouse,
        CanEditWarehouseInvoice = CanEditWarehouseInvoice,
        GroupOnlyDataAccess = GroupOnlyDataAccess
    };

    public static UserPermissions CreateAdmin() => new()
    {
        CanManageUsers = true,
        CanAccessMaterialsDictionary = true,
        CanConfigureConstructionElements = true,
        CanAccessEconomics = true,
        CanEditOrderSheetCoefficients = true,
        CanEditOrderSheetCostCoefficients = true,
        CanEditOrderSheetExtraExpenseCoefficients = true,
        CanPrintMaterialPrices = true,
        CanDeleteOrders = true,
        CanAccessWarehouse = true,
        CanEditWarehouse = true,
        CanEditWarehouseInvoice = true,
        GroupOnlyDataAccess = false
    };

    public static UserPermissions CreateOperator() => new()
    {
        CanAccessMaterialsDictionary = true,
        CanAccessWarehouse = true,
        GroupOnlyDataAccess = true
    };
}
