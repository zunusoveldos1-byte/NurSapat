using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Models.Dictionaries;

/// <summary>App-совместимый алиас <see cref="OrderManager"/>.</summary>
public sealed class ManagerItem : OrderManager
{
    public static new ManagerItem CreateDefault() => new()
    {
        FullName = string.Empty,
        IsActive = true,
        Role = "Менеджер",
        CreatedAt = DateTime.Now,
        ModifiedAt = DateTime.Now
    };

    public new ManagerItem Clone() => new()
    {
        Id = Id,
        FullName = FullName,
        Phone = Phone,
        Email = Email,
        Position = Position,
        Department = Department,
        HireDate = HireDate,
        IsActive = IsActive,
        Note = Note,
        Login = Login,
        Role = Role,
        PhotoPath = PhotoPath,
        IsDeleted = IsDeleted,
        CreatedAt = CreatedAt,
        ModifiedAt = ModifiedAt
    };
}
