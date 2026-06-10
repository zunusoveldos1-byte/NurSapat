using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.Core.Models.Orders;

public sealed partial class OrderPayment : ObservableObject
{
    [ObservableProperty]
    private decimal _agreedAmount;

    [ObservableProperty]
    private decimal _paidAmount;

    [ObservableProperty]
    private string _currencyCode = "KGS";

    public decimal Debt => Math.Max(0, AgreedAmount - PaidAmount);

    public string PaymentStatus => Debt <= 0 && AgreedAmount > 0
        ? "Оплачено"
        : PaidAmount > 0
            ? "Частично оплачено"
            : "Оплата ожидается";

    partial void OnAgreedAmountChanged(decimal value) => NotifyDebt();

    partial void OnPaidAmountChanged(decimal value) => NotifyDebt();

    public void NotifyDebt()
    {
        OnPropertyChanged(nameof(Debt));
        OnPropertyChanged(nameof(PaymentStatus));
    }
}
