using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class SupplierEditViewModel : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public string WindowTitle => _isNew ? "Новый поставщик" : "Редактирование поставщика";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _legalAddress = string.Empty;

    [ObservableProperty]
    private string _contractTerms = string.Empty;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(SupplierItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;
        Name = source.Name;
        Phone = source.Phone;
        LegalAddress = source.LegalAddress;
        ContractTerms = source.ContractTerms;
        Note = source.Note;
        ValidationMessage = string.Empty;
        OnPropertyChanged(nameof(WindowTitle));
    }

    public SupplierItem BuildResult() => new()
    {
        Id = _id,
        Name = Name.Trim(),
        Phone = Phone.Trim(),
        LegalAddress = LegalAddress.Trim(),
        ContractTerms = ContractTerms.Trim(),
        Note = Note.Trim()
    };

    [RelayCommand]
    private void Save()
    {
        ValidationMessage = Validate();
        if (!string.IsNullOrEmpty(ValidationMessage))
        {
            return;
        }

        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    private string Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Укажите наименование поставщика.";
        }

        return string.Empty;
    }
}
