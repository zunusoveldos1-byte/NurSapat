using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Services.Dictionaries;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Services.Orders;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Dialogs;

public sealed partial class CustomerSelectDialogViewModel : ViewModelBase
{
    private readonly ICustomersStore _customersStore;
    private readonly List<CustomerRowViewModel> _allRows = [];

    public CustomerSelectDialogViewModel(ICustomersStore customersStore)
    {
        _customersStore = customersStore;
        ReloadCustomers();
    }

    public ObservableCollection<CustomerRowViewModel> Customers { get; } = [];

    [ObservableProperty]
    private CustomerRowViewModel? _selectedCustomer;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _balanceWarning;

    public event Action<bool>? CloseRequested;

    public string Title => "Выбор заказчика";

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedCustomerChanged(CustomerRowViewModel? value)
    {
        BalanceWarning = value?.HasDebt == true
            ? $"Внимание: у заказчика непогашенный долг {value.DebtAmount:N0} сом."
            : null;
    }

    [RelayCommand]
    private void Confirm()
    {
        if (SelectedCustomer is null)
        {
            return;
        }

        CloseRequested?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(false);

    public Customer? ToSelectedCustomer() => SelectedCustomer?.Model;

    public void ReloadCustomers()
    {
        _allRows.Clear();
        foreach (var item in _customersStore.GetAll().Where(c => c.CanSelectForOrder))
        {
            _allRows.Add(CustomerRowViewModel.FromItem(item));
        }

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        Customers.Clear();
        var query = SearchText.Trim();
        foreach (var row in _allRows)
        {
            if (query.Length == 0
                || row.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.Phone.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.City.Contains(query, StringComparison.OrdinalIgnoreCase)
                || row.TypeDisplay.Contains(query, StringComparison.OrdinalIgnoreCase))
            {
                Customers.Add(row);
            }
        }
    }
}

public sealed class CustomerRowViewModel
{
    public required Customer Model { get; init; }

    public Guid Id => Model.Id;

    public string FullName => Model.FullName;

    public string Phone => Model.Phone;

    public string City => Model.City;

    public string TypeDisplay => Model.Type switch
    {
        CustomerType.Individual => "Физ. лицо",
        CustomerType.LegalEntity => "Юр. лицо",
        CustomerType.Dealer => "Дилер",
        _ => Model.Type.ToString()
    };

    public decimal CurrentBalance => Model.CurrentBalance;

    public bool HasDebt => Model.HasDebt;

    public decimal DebtAmount => Model.DebtAmount;

    public static CustomerRowViewModel FromItem(Models.Dictionaries.CustomerItem item) => new()
    {
        Model = item.Clone()
    };
}
