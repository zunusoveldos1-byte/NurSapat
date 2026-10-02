using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class GraphicSymbolDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    public static GraphicSymbolDictionaryItemViewModel FromModel(GraphicSymbolItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Code = item.Code
    };

    public GraphicSymbolItem ToModel() => new()
    {
        Id = Id,
        Name = Name,
        Code = Code
    };

    public void RefreshFrom(GraphicSymbolItem item)
    {
        Name = item.Name;
        Code = item.Code;
    }
}
