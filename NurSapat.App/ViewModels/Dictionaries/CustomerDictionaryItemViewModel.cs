using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class CustomerDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private CustomerType _type = CustomerType.Individual;

    [ObservableProperty]
    private decimal _turnover;

    [ObservableProperty]
    private decimal _debt;

    [ObservableProperty]
    private string _okpoCode = string.Empty;

    [ObservableProperty]
    private string _bankName = string.Empty;

    [ObservableProperty]
    private string _checkingAccount = string.Empty;

    [ObservableProperty]
    private string _mfo = string.Empty;

    [ObservableProperty]
    private string _legalAddress = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    public string TypeDisplay => Type switch
    {
        CustomerType.Individual => "Физ. лицо",
        CustomerType.LegalEntity => "Юр. лицо",
        CustomerType.Dealer => "Дилер",
        _ => Type.ToString()
    };

    public string TurnoverDisplay => $"{Turnover:N2} сом";

    public string DebtDisplay => $"{Debt:N2} сом";

    partial void OnTypeChanged(CustomerType value) => OnPropertyChanged(nameof(TypeDisplay));

    partial void OnTurnoverChanged(decimal value) => OnPropertyChanged(nameof(TurnoverDisplay));

    partial void OnDebtChanged(decimal value) => OnPropertyChanged(nameof(DebtDisplay));

    public static CustomerDictionaryItemViewModel FromModel(CustomerItem item) => new()
    {
        Id = item.Id,
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
        Note = item.Note
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
        Note = Note
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
        Note = item.Note;
    }
}
