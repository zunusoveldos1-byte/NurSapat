using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class OrderTemplateParametersViewModel : ObservableObject
{
    [ObservableProperty]
    private string _defaultOrderNote = string.Empty;

    [ObservableProperty]
    private int _defaultProductionDays = 14;

    [ObservableProperty]
    private decimal _defaultPrepaymentPercent = 30m;

    [ObservableProperty]
    private bool _applyDealerDiscount;

    [ObservableProperty]
    private decimal _dealerDiscountPercent;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(OrderTemplateItem source)
    {
        DefaultOrderNote = source.DefaultOrderNote;
        DefaultProductionDays = source.DefaultProductionDays;
        DefaultPrepaymentPercent = source.DefaultPrepaymentPercent;
        ApplyDealerDiscount = source.ApplyDealerDiscount;
        DealerDiscountPercent = source.DealerDiscountPercent;
        ValidationMessage = string.Empty;
    }

    public void ApplyTo(OrderTemplateItem target)
    {
        target.DefaultOrderNote = DefaultOrderNote.Trim();
        target.DefaultProductionDays = Math.Max(1, DefaultProductionDays);
        target.DefaultPrepaymentPercent = DefaultPrepaymentPercent;
        target.ApplyDealerDiscount = ApplyDealerDiscount;
        target.DealerDiscountPercent = DealerDiscountPercent;
    }

    [RelayCommand]
    private void Save()
    {
        if (DefaultProductionDays < 1)
        {
            ValidationMessage = "Срок производства должен быть не меньше 1 дня.";
            return;
        }

        ValidationMessage = string.Empty;
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}
