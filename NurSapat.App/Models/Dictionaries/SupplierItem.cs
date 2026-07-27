using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Models.Dictionaries;

/// <summary>App-совместимый алиас канонической модели <see cref="Supplier"/>.</summary>
public sealed class SupplierItem : Supplier
{
    public new SupplierItem Clone() => new()
    {
        Id = Id,
        Name = Name,
        Phone = Phone,
        LegalAddress = LegalAddress,
        ContractTerms = ContractTerms,
        Note = Note
    };
}
