using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Models.Dictionaries;

/// <summary>App-совместимый алиас канонической модели <see cref="Customer"/>.</summary>
public sealed class CustomerItem : Customer
{
    public static new CustomerItem CreateDefault() => new()
    {
        FullName = string.Empty,
        Phone = "+996",
        Type = CustomerType.Individual,
        CreatedAt = DateTime.Now,
        ModifiedAt = DateTime.Now
    };

    public new CustomerItem Clone() => new()
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
