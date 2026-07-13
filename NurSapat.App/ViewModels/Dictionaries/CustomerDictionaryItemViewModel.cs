using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class CustomerDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty] private int _rowNumber;
    [ObservableProperty] private string _fullName = string.Empty;
    [ObservableProperty] private string _phone = string.Empty;
    [ObservableProperty] private CustomerType _type = CustomerType.Individual;
    [ObservableProperty] private decimal _turnover;
    [ObservableProperty] private decimal _debt;
    [ObservableProperty] private string _okpoCode = string.Empty;
    [ObservableProperty] private string _bankName = string.Empty;
    [ObservableProperty] private string _checkingAccount = string.Empty;
    [ObservableProperty] private string _mfo = string.Empty;
    [ObservableProperty] private string _legalAddress = string.Empty;
    [ObservableProperty] private string _city = string.Empty;
    [ObservableProperty] private string _actualAddress = string.Empty;
    [ObservableProperty] private string _dealerCode = string.Empty;
    [ObservableProperty] private string _note = string.Empty;
    [ObservableProperty] private bool _isBlocked;
    [ObservableProperty] private DateTime _createdAt;
    [ObservableProperty] private DateTime _modifiedAt;

    public string TypeDisplay => Type switch
    {
        CustomerType.Individual => "Физ. лицо",
        CustomerType.LegalEntity => "Юр. лицо",
        CustomerType.Dealer => "Дилер",
        _ => Type.ToString()
    };

    public string BlockedDisplay => IsBlocked ? "Да" : "Нет";

    public string TurnoverDisplay => $"{Turnover:N2} сом";

    public string DebtDisplay => $"{Debt:N2} сом";

    public string ForegroundBrush => IsBlocked ? "#94A3B8" : "#0F172A";

    partial void OnTypeChanged(CustomerType value) => OnPropertyChanged(nameof(TypeDisplay));

    partial void OnTurnoverChanged(decimal value) => OnPropertyChanged(nameof(TurnoverDisplay));

    partial void OnDebtChanged(decimal value) => OnPropertyChanged(nameof(DebtDisplay));

    partial void OnIsBlockedChanged(bool value)
    {
        OnPropertyChanged(nameof(BlockedDisplay));
        OnPropertyChanged(nameof(ForegroundBrush));
    }

    public static CustomerDictionaryItemViewModel FromModel(CustomerItem item, int rowNumber) => new()
    {
        Id = item.Id,
        RowNumber = rowNumber,
        FullName = item.FullName,
        Phone = item.Phone,
        Type = item.Type,
        Turnover = item.Turnover,
        Debt = item.Debt,
        OkpoCode = item.OkpoCode,
        BankName = item.BankName,
        CheckingAccount = item.CheckingAccount,
        Mfo = item.Mfo,
        LegalAddress = item.LegalAddress,
        City = item.City,
        ActualAddress = item.ActualAddress,
        DealerCode = item.DealerCode,
        Note = item.Note,
        IsBlocked = item.IsBlocked,
        CreatedAt = item.CreatedAt,
        ModifiedAt = item.ModifiedAt
    };

    public CustomerItem ToModel() => new()
    {
        Id = Id,
        FullName = FullName,
        Phone = Phone,
        Type = Type,
        Turnover = Turnover,
        Debt = Debt,
        OkpoCode = OkpoCode,
        BankName = BankName,
        CheckingAccount = CheckingAccount,
        Mfo = Mfo,
        LegalAddress = LegalAddress,
        City = City,
        ActualAddress = ActualAddress,
        DealerCode = DealerCode,
        Note = Note,
        IsBlocked = IsBlocked,
        CreatedAt = CreatedAt,
        ModifiedAt = ModifiedAt
    };

    public void RefreshFrom(CustomerItem item)
    {
        FullName = item.FullName;
        Phone = item.Phone;
        Type = item.Type;
        Turnover = item.Turnover;
        Debt = item.Debt;
        OkpoCode = item.OkpoCode;
        BankName = item.BankName;
        CheckingAccount = item.CheckingAccount;
        Mfo = item.Mfo;
        LegalAddress = item.LegalAddress;
        City = item.City;
        ActualAddress = item.ActualAddress;
        DealerCode = item.DealerCode;
        Note = item.Note;
        IsBlocked = item.IsBlocked;
        CreatedAt = item.CreatedAt;
        ModifiedAt = item.ModifiedAt;
    }
}
