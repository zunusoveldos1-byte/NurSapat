using NurSapat.App.Models.Dictionaries;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.ViewModels;

/// <summary>Элемент ComboBox заказчика на листе заказа.</summary>
public sealed class CustomerLookupItem
{
    public required Guid Id { get; init; }

    public required string FullName { get; init; }

    public string Phone { get; init; } = string.Empty;

    public string TypeDisplay { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string DisplayText => string.IsNullOrWhiteSpace(Phone)
        ? FullName
        : $"{FullName} · {Phone}";

    public string DetailText => $"{TypeDisplay} · {City}";

    public static CustomerLookupItem FromItem(CustomerItem item) => new()
    {
        Id = item.Id,
        FullName = item.FullName,
        Phone = item.Phone,
        TypeDisplay = item.Type switch
        {
            CustomerType.Individual => "Физ. лицо",
            CustomerType.LegalEntity => "Юр. лицо",
            CustomerType.Dealer => "Дилер",
            _ => item.Type.ToString()
        },
        City = item.City
    };
}

/// <summary>Элемент ComboBox менеджера на листе заказа.</summary>
public sealed class ManagerLookupItem
{
    public required Guid Id { get; init; }

    public required string FullName { get; init; }

    public string Phone { get; init; } = string.Empty;

    public string Position { get; init; } = string.Empty;

    public string DisplayText => string.IsNullOrWhiteSpace(Position)
        ? FullName
        : $"{FullName} · {Position}";

    public static ManagerLookupItem FromItem(ManagerItem item) => new()
    {
        Id = item.Id,
        FullName = item.FullName,
        Phone = item.Phone,
        Position = item.Position
    };
}
