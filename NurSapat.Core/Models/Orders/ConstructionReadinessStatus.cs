namespace NurSapat.Core.Models.Orders;

/// <summary>Business readiness state shown in the construction list.</summary>
public enum ConstructionReadinessStatus
{
    RequiresPaymentOrSaved = 0,
    ReadyAndPaid = 10,
    CompletedAndSold = 20,
    CancelledOrAbandoned = 99
}
