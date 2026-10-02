namespace NurSapat.Core.Models.Orders;

public sealed class Dealer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string CompanyName { get; set; } = string.Empty;

    public string ContactPerson { get; set; } = string.Empty;

    public decimal DiscountPercentage { get; set; }

    public decimal CreditLimit { get; set; }

    public decimal CurrentDebt { get; set; }

    public string DiscountCategory { get; set; } = "B";

    public decimal AvailableCredit => Math.Max(0, CreditLimit - CurrentDebt);

    public bool IsWithinCreditLimit(decimal orderAmount) => CurrentDebt + orderAmount <= CreditLimit;
}
