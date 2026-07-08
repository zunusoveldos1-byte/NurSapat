using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Services.Orders;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Dialogs;

public sealed partial class PriceModeDialogViewModel : ViewModelBase
{
    private readonly Order _order;
    private bool _syncingDiscountFields;

    public PriceModeDialogViewModel(Order order)
    {
        _order = order;
        SelectedPriceMode = order.Header.CurrentPriceMode;
        DiscountPercent = order.Header.TotalDiscountPercent > 0
            ? order.Header.TotalDiscountPercent
            : order.Totals.Discount2Percent;
        DiscountSum = order.Header.TotalDiscountSum;
        if (DiscountSum <= 0 && DiscountPercent > 0)
        {
            DiscountSum = DiscountCalculator.SumFromPercent(Subtotal, DiscountPercent);
        }

        foreach (var mode in PriceModeTypeExtensions.All)
        {
            PriceModes.Add(mode);
        }

        UpdateMargin();
    }

    public ObservableCollection<PriceModeType> PriceModes { get; } = [];

    [ObservableProperty]
    private PriceModeType _selectedPriceMode;

    [ObservableProperty]
    private decimal _discountPercent;

    [ObservableProperty]
    private decimal _discountSum;

    [ObservableProperty]
    private decimal _marginAmount;

    [ObservableProperty]
    private string? _discountError;

    public decimal Subtotal => _order.Totals.ConstructionsSubtotal;

    public decimal EstimatedCost => Math.Round(Subtotal * 0.72m, 2, MidpointRounding.AwayFromZero);

    public event Action<bool>? CloseRequested;

    public string Title => "Режим цены";

    partial void OnSelectedPriceModeChanged(PriceModeType value) => ValidateDiscount();

    partial void OnDiscountPercentChanged(decimal value)
    {
        if (_syncingDiscountFields)
        {
            return;
        }

        _syncingDiscountFields = true;
        DiscountSum = DiscountCalculator.SumFromPercent(Subtotal, value);
        _syncingDiscountFields = false;
        ValidateDiscount();
        UpdateMargin();
    }

    partial void OnDiscountSumChanged(decimal value)
    {
        if (_syncingDiscountFields)
        {
            return;
        }

        _syncingDiscountFields = true;
        DiscountPercent = DiscountCalculator.PercentFromSum(Subtotal, value);
        _syncingDiscountFields = false;
        ValidateDiscount();
        UpdateMargin();
    }

    [RelayCommand]
    private void Confirm()
    {
        if (!string.IsNullOrWhiteSpace(DiscountError))
        {
            return;
        }

        _order.Header.CurrentPriceMode = SelectedPriceMode;
        _order.Header.TotalDiscountPercent = DiscountPercent;
        _order.Header.TotalDiscountSum = DiscountSum;
        _order.Totals.Discount2Percent = DiscountPercent;
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    private void ValidateDiscount()
    {
        if (SelectedPriceMode != PriceModeType.Retail)
        {
            DiscountError = null;
            return;
        }

        if (DiscountPercent > OrderBusinessRules.MaxManagerRetailDiscountPercent)
        {
            DiscountError =
                $"Максимальная скидка менеджера — {OrderBusinessRules.MaxManagerRetailDiscountPercent:N0}%.";
            _syncingDiscountFields = true;
            DiscountPercent = OrderBusinessRules.MaxManagerRetailDiscountPercent;
            DiscountSum = DiscountCalculator.SumFromPercent(Subtotal, DiscountPercent);
            _syncingDiscountFields = false;
            return;
        }

        DiscountError = null;
    }

    private void UpdateMargin()
    {
        var afterDiscount = Subtotal - DiscountSum;
        MarginAmount = DiscountCalculator.CalculateMargin(afterDiscount, EstimatedCost);
    }
}
