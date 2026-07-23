using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.Core.Models.Calculation;

public sealed partial class OrderCostTotals : ObservableObject
{
    [ObservableProperty]
    private decimal _constructionsSubtotal;

    [ObservableProperty]
    private decimal _discount2Percent;

    [ObservableProperty]
    private decimal _deliveryCost;

    [ObservableProperty]
    private decimal _additionalMaterialsCost;

    [ObservableProperty]
    private decimal _additionalMaterialsInstallationCost;

    [ObservableProperty]
    private decimal _vatPercent = 12m;

    [ObservableProperty]
    private VatDisplayMode _vatMode = VatDisplayMode.NotUsed;

    public decimal AfterDiscount2 => Round(ConstructionsSubtotal * (1m - Discount2Percent / 100m));

    public decimal OrderSubtotal => Round(AfterDiscount2 + DeliveryCost);

    public decimal BeforeVat => Round(
        OrderSubtotal + AdditionalMaterialsCost + AdditionalMaterialsInstallationCost);

    public decimal VatAmount => VatMode switch
    {
        VatDisplayMode.IncludedInPrice => Round(BeforeVat * VatPercent / (100m + VatPercent)),
        VatDisplayMode.ExcludedFromPrice => Round(BeforeVat * VatPercent / 100m),
        _ => 0m
    };

    public decimal GrandTotal => VatMode switch
    {
        VatDisplayMode.ExcludedFromPrice => Round(BeforeVat + VatAmount),
        _ => BeforeVat
    };

    partial void OnConstructionsSubtotalChanged(decimal value) => NotifyDerived();

    partial void OnDiscount2PercentChanged(decimal value) => NotifyDerived();

    partial void OnDeliveryCostChanged(decimal value) => NotifyDerived();

    partial void OnAdditionalMaterialsCostChanged(decimal value) => NotifyDerived();

    partial void OnAdditionalMaterialsInstallationCostChanged(decimal value) => NotifyDerived();

    partial void OnVatPercentChanged(decimal value) => NotifyDerived();

    partial void OnVatModeChanged(VatDisplayMode value) => NotifyDerived();

    public void NotifyDerived()
    {
        OnPropertyChanged(nameof(AfterDiscount2));
        OnPropertyChanged(nameof(OrderSubtotal));
        OnPropertyChanged(nameof(BeforeVat));
        OnPropertyChanged(nameof(VatAmount));
        OnPropertyChanged(nameof(GrandTotal));
    }

    private static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
