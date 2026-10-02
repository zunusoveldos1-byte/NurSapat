using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Models;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Services.Orders;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Dialogs;

public sealed partial class DealerSelectDialogViewModel : ViewModelBase
{
    private readonly Order _order;
    private readonly List<DealerRowViewModel> _allDealers = [];

    public DealerSelectDialogViewModel(Order order, IDealersCatalog dealersCatalog)
    {
        _order = order;
        foreach (var dealer in dealersCatalog.GetAll())
        {
            _allDealers.Add(new DealerRowViewModel(dealer, order.Totals.GrandTotal));
        }

        ApplyFilter();
        SelectedDealer = FilteredDealers.FirstOrDefault(d => d.Id == order.Header.DealerId)
            ?? FilteredDealers.FirstOrDefault();
    }

    public string Title => "Выбор дилера (B2B)";

    public ObservableCollection<DealerRowViewModel> FilteredDealers { get; } = [];

    [ObservableProperty]
    private DealerRowViewModel? _selectedDealer;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _creditWarning;

    [ObservableProperty]
    private bool _canConfirm = true;

    public event Action<bool>? CloseRequested;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedDealerChanged(DealerRowViewModel? value)
    {
        if (value is null)
        {
            CreditWarning = null;
            CanConfirm = false;
            return;
        }

        CreditWarning = value.IsOverCreditLimit
            ? $"Превышен кредитный лимит. Доступно: {value.AvailableCredit:N0} {_order.Header.CurrencyCode}."
            : null;
        CanConfirm = !value.IsOverCreditLimit;
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedDealer is null || !CanConfirm)
        {
            return;
        }

        var dealer = SelectedDealer.Dealer;
        _order.Header.DealerId = dealer.Id;
        _order.Header.DealerName = dealer.CompanyName;
        _order.Header.TotalDiscountPercent = dealer.DiscountPercentage;
        _order.Totals.Discount2Percent = dealer.DiscountPercentage;
        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    private void ApplyFilter()
    {
        var previousId = SelectedDealer?.Id;
        FilteredDealers.Clear();

        var query = SearchText.Trim();
        foreach (var row in _allDealers)
        {
            if (query.Length == 0
                || row.CompanyName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.ContactPerson.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                FilteredDealers.Add(row);
            }
        }

        if (previousId is Guid id)
        {
            SelectedDealer = FilteredDealers.FirstOrDefault(d => d.Id == id) ?? FilteredDealers.FirstOrDefault();
        }
        else if (SelectedDealer is null && FilteredDealers.Count > 0)
        {
            SelectedDealer = FilteredDealers[0];
        }
    }
}

public sealed class DealerRowViewModel
{
    public DealerRowViewModel(Dealer dealer, decimal currentOrderAmount)
    {
        Dealer = dealer;
        CurrentOrderAmount = currentOrderAmount;
    }

    public Dealer Dealer { get; }

    public Guid Id => Dealer.Id;

    public string CompanyName => Dealer.CompanyName;

    public string ContactPerson => Dealer.ContactPerson;

    public decimal DiscountPercentage => Dealer.DiscountPercentage;

    public decimal CreditLimit => Dealer.CreditLimit;

    public decimal CurrentDebt => Dealer.CurrentDebt;

    public decimal AvailableCredit => Dealer.AvailableCredit;

    public string DiscountCategory => Dealer.DiscountCategory;

    public decimal CurrentOrderAmount { get; }

    public decimal DealerCommissionEstimate =>
        Math.Round(CurrentOrderAmount * DiscountPercentage / 100m, 2, MidpointRounding.AwayFromZero);

    public bool IsOverCreditLimit => !Dealer.IsWithinCreditLimit(CurrentOrderAmount);
}
