using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Services.Orders;

public static class ConstructionDeleteMessageBuilder
{
    public static string Build(OrderConstruction construction)
    {
        var profile = string.IsNullOrWhiteSpace(construction.ProfileSystemName)
            ? construction.Name
            : construction.ProfileSystemName;

        return
            $"Вы уверены, что хотите удалить Конструкцию №{construction.Number} ({profile}, {construction.DimensionsDisplay})? " +
            "Это действие нельзя отменить.";
    }

    public static string Build(int number, string name, string dimensionsDisplay, string? profileSystemName = null)
    {
        var profile = string.IsNullOrWhiteSpace(profileSystemName) ? name : profileSystemName;
        return
            $"Вы уверены, что хотите удалить Конструкцию №{number} ({profile}, {dimensionsDisplay})? " +
            "Это действие нельзя отменить.";
    }
}
