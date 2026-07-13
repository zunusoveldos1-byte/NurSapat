namespace NurSapat.Core.Models.Orders;

/// <summary>Менеджер заказа — справочник для поля «Менеджер» в листе заказа.</summary>
public class OrderManager
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FullName { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public string Department { get; set; } = string.Empty;

    public DateTime? HireDate { get; set; }

    public bool IsActive { get; set; } = true;

    public string Note { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public string Role { get; set; } = "Менеджер";

    public string? PhotoPath { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    public bool CanSelectForOrder => IsActive && !IsDeleted;

    public static OrderManager CreateDefault() => new()
    {
        FullName = string.Empty,
        IsActive = true,
        Role = "Менеджер",
        CreatedAt = DateTime.Now,
        ModifiedAt = DateTime.Now
    };

    public OrderManager Clone() => new()
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
