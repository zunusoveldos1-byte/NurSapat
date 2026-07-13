using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class CustomerEditViewModel : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private bool _isNew;
    private DateTime _createdAt = DateTime.Now;

    public string WindowTitle => _isNew ? "Новый заказчик" : "Данные заказчика";

    public IReadOnlyList<CustomerType> CustomerTypes { get; } = Enum.GetValues<CustomerType>();

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
    [ObservableProperty] private string _createdAtText = string.Empty;
    [ObservableProperty] private string _modifiedAtText = string.Empty;

    public string TurnoverDisplay => $"{Turnover:N2}";

    public string DebtDisplay => $"{Debt:N2}";

    public event Action<bool>? RequestClose;

    public void Load(CustomerItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;
        _createdAt = isNew ? DateTime.Now : source.CreatedAt;

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
        City = source.City;
        ActualAddress = source.ActualAddress;
        DealerCode = source.DealerCode;
        Note = source.Note;
        IsBlocked = source.IsBlocked;
        CreatedAtText = _createdAt.ToString("dd.MM.yyyy HH:mm");
        ModifiedAtText = (isNew ? DateTime.Now : source.ModifiedAt).ToString("dd.MM.yyyy HH:mm");

        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(TurnoverDisplay));
        OnPropertyChanged(nameof(DebtDisplay));
    }

    public CustomerItem BuildResult() => new()
    {
        Id = _id,
        FullName = FullName.Trim(),
        Phone = Phone.Trim(),
        Type = Type,
        Turnover = Turnover,
        Debt = Debt,
        OkpoCode = OkpoCode.Trim(),
        BankName = BankName.Trim(),
        CheckingAccount = CheckingAccount.Trim(),
        Mfo = Mfo.Trim(),
        LegalAddress = LegalAddress.Trim(),
        City = City.Trim(),
        ActualAddress = ActualAddress.Trim(),
        DealerCode = DealerCode.Trim(),
        Note = Note.Trim(),
        IsBlocked = IsBlocked,
        CreatedAt = _createdAt,
        ModifiedAt = DateTime.Now
    };

    [RelayCommand]
    private void Save() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);
}
