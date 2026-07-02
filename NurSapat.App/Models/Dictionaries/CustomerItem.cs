namespace NurSapat.App.Models.Dictionaries;

public enum CustomerType
{
    Individual,
    LegalEntity,
    Dealer
}

public sealed class CustomerItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public CustomerType Type { get; set; } = CustomerType.Individual;

    public decimal Turnover { get; set; }

    public decimal Debt { get; set; }

    public string OkpoCode { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string CheckingAccount { get; set; } = string.Empty;

    public string Mfo { get; set; } = string.Empty;

    public string LegalAddress { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public static CustomerItem CreateDefault() => new()
    {
        FullName = "Новый заказчик",
        Phone = "+996",
        Type = CustomerType.Individual
    };

    public CustomerItem Clone() => new()
    {
        Id = Id,
        FullName = FullName,
        Phone = Phone,
        Type = Type,
        Turnover = Turnover,
        Debt = Debt,
        OkpoCode = OkpoCode,
        BankName = BankName,
        CheckingAccount = CheckingAccount,
        Mfo = Mfo,
        LegalAddress = LegalAddress,
        Note = Note
    };
}
