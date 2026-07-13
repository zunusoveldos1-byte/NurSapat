namespace NurSapat.Core.Models.Orders;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    /// <summary>Телефон в формате +996 (XXX) XX-XX-XX.</summary>
    public string Phone { get; set; } = "+996";

    public CustomerType Type { get; set; } = CustomerType.Individual;

    /// <summary>Сумма всех оплаченных заказов. Пересчитывается автоматически.</summary>
    public decimal Turnover { get; set; }

    /// <summary>Общая сумма заказов минус оплаченная сумма.</summary>
    public decimal Debt { get; set; }

    public decimal CurrentBalance => -Debt;

    public string OkpoCode { get; set; } = string.Empty;

    public string BankName { get; set; } = string.Empty;

    public string CheckingAccount { get; set; } = string.Empty;

    public string Mfo { get; set; } = string.Empty;

    public string LegalAddress { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string ActualAddress { get; set; } = string.Empty;

    public string DealerCode { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    /// <summary>Заблокированного заказчика нельзя выбирать в новых заказах.</summary>
    public bool IsBlocked { get; set; }

    /// <summary>Мягкое удаление — запись скрыта из справочника.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Совместимость: активен = не удалён и не заблокирован.</summary>
    public bool IsActive
    {
        get => !IsDeleted && !IsBlocked;
        set
        {
            if (value)
            {
                IsDeleted = false;
                IsBlocked = false;
            }
            else
            {
                IsBlocked = true;
            }
        }
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    public bool HasDebt => Debt > 0;

    public decimal DebtAmount => HasDebt ? Debt : 0;

    public bool CanSelectForOrder => !IsDeleted && !IsBlocked;

    public static Customer CreateDefault() => new()
    {
        FullName = string.Empty,
        Phone = "+996",
        Type = CustomerType.Individual,
        CreatedAt = DateTime.Now,
        ModifiedAt = DateTime.Now
    };

    public Customer Clone() => new()
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
        City = City,
        ActualAddress = ActualAddress,
        DealerCode = DealerCode,
        Note = Note,
        IsBlocked = IsBlocked,
        IsDeleted = IsDeleted,
        CreatedAt = CreatedAt,
        ModifiedAt = ModifiedAt
    };
}
