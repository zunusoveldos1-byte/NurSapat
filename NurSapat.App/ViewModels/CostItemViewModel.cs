using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.View;

namespace NurSapat.App.ViewModels;

public sealed partial class CostItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _materialName = string.Empty;

    [ObservableProperty]
    private MaterialCostType _materialType;

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private string _unit = "шт.";

    [ObservableProperty]
    private decimal _unitPrice;

    public string MaterialTypeDisplay => MaterialType.ToDisplayName();

    public decimal TotalCost => Math.Round(Quantity * UnitPrice, 2, MidpointRounding.AwayFromZero);

    public string UnitPriceDisplay => $"{UnitPrice:N2} сом";

    public string TotalCostDisplay => $"{TotalCost:N2} сом";

    partial void OnQuantityChanged(decimal value)
    {
        NotifyCostChanged();
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        NotifyCostChanged();
    }

    partial void OnMaterialTypeChanged(MaterialCostType value)
    {
        OnPropertyChanged(nameof(MaterialTypeDisplay));
    }

    private void NotifyCostChanged()
    {
        OnPropertyChanged(nameof(TotalCost));
        OnPropertyChanged(nameof(TotalCostDisplay));
        OnPropertyChanged(nameof(UnitPriceDisplay));
    }
}
