using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class SupplierDictionaryItemViewModel : DictionaryItemViewModelBase
{
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

    public static SupplierDictionaryItemViewModel FromModel(SupplierItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Phone = item.Phone,
        LegalAddress = item.LegalAddress,
        ContractTerms = item.ContractTerms,
        Note = item.Note
    };

    public SupplierItem ToModel() => new()
    {
        Id = Id,
        Name = Name,
        Phone = Phone,
        LegalAddress = LegalAddress,
        ContractTerms = ContractTerms,
        Note = Note
    };

    public void RefreshFrom(SupplierItem item)
    {
        Name = item.Name;
        Phone = item.Phone;
        LegalAddress = item.LegalAddress;
        ContractTerms = item.ContractTerms;
        Note = item.Note;
    }
}
