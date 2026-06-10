using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.Core.Models.Calculation;

public sealed partial class ConstructionCostBreakdown : ObservableObject
{
    [ObservableProperty]
    private decimal _profileCost;

    [ObservableProperty]
    private decimal _componentsCost;

    [ObservableProperty]
    private decimal _sealantCost;

    [ObservableProperty]
    private decimal _glazingCost;

    [ObservableProperty]
    private decimal _hardwareCost;

    [ObservableProperty]
    private decimal _installationMaterialsCost;

    [ObservableProperty]
    private decimal _expenses;

    [ObservableProperty]
    private CostInputMode _expensesMode = CostInputMode.Fixed;

    [ObservableProperty]
    private decimal _laborCost;

    [ObservableProperty]
    private decimal _materialsWithoutMarkup;

    [ObservableProperty]
    private decimal _profitabilityPercent;

    [ObservableProperty]
    private decimal _additionalExpensesPercent;

    [ObservableProperty]
    private decimal _additionalExpensesFixed;

    [ObservableProperty]
    private decimal _discount1Percent;

    [ObservableProperty]
    private decimal _demolitionCost;

    [ObservableProperty]
    private decimal _installationCost;

    [ObservableProperty]
    private int _quantity = 1;

    public decimal MaterialsTotal => Round(
        ProfileCost + ComponentsCost + SealantCost + GlazingCost + HardwareCost + InstallationMaterialsCost);

    public decimal CostPrice => Round(MaterialsTotal + Expenses + LaborCost);

    public decimal ProfitabilityAmount => Round((CostPrice + MaterialsWithoutMarkup) * ProfitabilityPercent / 100m);

    public decimal AdditionalExpensesPercentAmount => Round(
        (CostPrice + ProfitabilityAmount + MaterialsWithoutMarkup) * AdditionalExpensesPercent / 100m);

    public decimal Price => Round(
        CostPrice + ProfitabilityAmount + MaterialsWithoutMarkup + AdditionalExpensesPercentAmount + AdditionalExpensesFixed);

    public decimal PriceWithDiscount => Round(Price * (1m - Discount1Percent / 100m));

    public decimal ConstructionSum => Round(PriceWithDiscount + DemolitionCost + InstallationCost);

    public decimal ConstructionSumTotal => Round(ConstructionSum * Math.Max(1, Quantity));

    partial void OnProfileCostChanged(decimal value) => NotifyDerived();

    partial void OnComponentsCostChanged(decimal value) => NotifyDerived();

    partial void OnSealantCostChanged(decimal value) => NotifyDerived();

    partial void OnGlazingCostChanged(decimal value) => NotifyDerived();

    partial void OnHardwareCostChanged(decimal value) => NotifyDerived();

    partial void OnInstallationMaterialsCostChanged(decimal value) => NotifyDerived();

    partial void OnExpensesChanged(decimal value) => NotifyDerived();

    partial void OnLaborCostChanged(decimal value) => NotifyDerived();

    partial void OnMaterialsWithoutMarkupChanged(decimal value) => NotifyDerived();

    partial void OnProfitabilityPercentChanged(decimal value) => NotifyDerived();

    partial void OnAdditionalExpensesPercentChanged(decimal value) => NotifyDerived();

    partial void OnAdditionalExpensesFixedChanged(decimal value) => NotifyDerived();

    partial void OnDiscount1PercentChanged(decimal value) => NotifyDerived();

    partial void OnDemolitionCostChanged(decimal value) => NotifyDerived();

    partial void OnInstallationCostChanged(decimal value) => NotifyDerived();

    partial void OnQuantityChanged(int value)
    {
        Quantity = Math.Max(1, value);
        NotifyDerived();
    }

    public void NotifyDerived()
    {
        OnPropertyChanged(nameof(MaterialsTotal));
        OnPropertyChanged(nameof(CostPrice));
        OnPropertyChanged(nameof(ProfitabilityAmount));
        OnPropertyChanged(nameof(AdditionalExpensesPercentAmount));
        OnPropertyChanged(nameof(Price));
        OnPropertyChanged(nameof(PriceWithDiscount));
        OnPropertyChanged(nameof(ConstructionSum));
        OnPropertyChanged(nameof(ConstructionSumTotal));
    }

    private static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
