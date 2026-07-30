using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class ConstructionItemViewModel : ObservableObject
{
    public ConstructionItemViewModel(Guid orderConstructionId, int number, string name)
    {
        OrderConstructionId = orderConstructionId;
        Number = number;
        _name = name;
    }

    public Guid OrderConstructionId { get; }

    public int Number { get; private set; }

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _dimensionsDisplay = "—";

    [ObservableProperty]
    private string _quantityDisplay = "1 шт.";

    public void UpdateFrom(string name, int number, string dimensionsDisplay, string quantityDisplay)
    {
        Name = name;
        Number = number;
        DimensionsDisplay = dimensionsDisplay;
        QuantityDisplay = quantityDisplay;
    }

    public void UpdateQuantity(int quantity) =>
        QuantityDisplay = $"{Math.Max(1, quantity)} шт.";

    public override string ToString() => Name;
}
