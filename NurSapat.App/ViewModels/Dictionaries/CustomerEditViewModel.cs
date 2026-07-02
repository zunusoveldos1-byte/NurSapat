using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class CustomerEditViewModel : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public string WindowTitle => _isNew ? "Новый заказчик" : "Данные заказчика";

    public IReadOnlyList<CustomerType> CustomerTypes { get; } = Enum.GetValues<CustomerType>();

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

    public event Action<bool>? RequestClose;

    public void Load(CustomerItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;

        FullName = source.FullName;
        Phone = source.Phone;
        Type = source.Type;
        Turnover = source.Turnover;
        Debt = source.Debt;
        OkpoCode = source.OkpoCode;
        BankName = source.BankName;
        CheckingAccount = source.CheckingAccount;
        Mfo = source.Mfo;
        LegalAddress = source.LegalAddress;
        Note = source.Note;

        OnPropertyChanged(nameof(WindowTitle));
    }

    public CustomerItem BuildResult() => new()
    {
        Id = _id,
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

    [RelayCommand]
    private void Save() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}
