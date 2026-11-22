using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class StructureNameDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    public static StructureNameDictionaryItemViewModel FromModel(StructureNameItem item) => new()
    {
        Id = item.Id,
        Code = item.Code,
        Name = item.Name
    };

    public StructureNameItem ToModel() => new()
    {
        Id = Id,
        Code = Code,
        Name = Name
    };

    public void RefreshFrom(StructureNameItem item)
    {
        Code = item.Code;
        Name = item.Name;
    }
}
