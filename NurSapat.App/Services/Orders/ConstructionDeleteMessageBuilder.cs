using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Services.Orders;

public static class ConstructionDeleteMessageBuilder
{
    public static string Build(OrderConstruction construction)
    {
        return $"Вы точно хотите удалить конструкцию \"{construction.Name}\"?";
    }

    public static string Build(int number, string name, string dimensionsDisplay, string? profileSystemName = null)
    {
        return $"Вы точно хотите удалить конструкцию \"{name}\"?";
    }
}
