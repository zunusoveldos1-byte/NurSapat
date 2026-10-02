namespace NurSapat.Infrastructure.Persistence.Entities;

public sealed class CustomerEntity
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public int Type { get; set; }

    public decimal Turnover { get; set; }

    public decimal Debt { get; set; }

    public string OkpoCode { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string CheckingAccount { get; set; } = string.Empty;

    public string Mfo { get; set; } = string.Empty;

    public string LegalAddress { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string ActualAddress { get; set; } = string.Empty;

    public string DealerCode { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public bool IsBlocked { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;
}
