namespace NurSapat.App.Models.Dictionaries;

public sealed class SupplierItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string LegalAddress { get; set; } = string.Empty;

    public string ContractTerms { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public SupplierItem Clone() => new()
    {
        Id = Id,
        Name = Name,
        Phone = Phone,
        LegalAddress = LegalAddress,
        ContractTerms = ContractTerms,
        Note = Note
    };
}
