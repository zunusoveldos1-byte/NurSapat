namespace NurSapat.Core.Models.Orders;

public class Supplier
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string LegalAddress { get; set; } = string.Empty;

    public string ContractTerms { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public Supplier Clone() => new()
    {
        Id = Id,
        Name = Name,
        Phone = Phone,
        LegalAddress = LegalAddress,
        ContractTerms = ContractTerms,
        Note = Note
    };
}
