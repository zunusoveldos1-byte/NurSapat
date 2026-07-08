namespace NurSapat.Core.Models.Orders;

public sealed class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    /// <summary>Телефон в формате +996 (XXX) XX-XX-XX.</summary>
    public string Phone { get; set; } = "+996";

    /// <summary>Отрицательное значение — долг, положительное — предоплата.</summary>
    public decimal CurrentBalance { get; set; }

    public bool IsActive { get; set; } = true;

    public bool HasDebt => CurrentBalance < 0;

    public decimal DebtAmount => HasDebt ? Math.Abs(CurrentBalance) : 0;
}
