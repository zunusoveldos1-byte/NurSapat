namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class SupplierEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string LegalAddress { get; set; } = string.Empty;

    public string ContractTerms { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}
