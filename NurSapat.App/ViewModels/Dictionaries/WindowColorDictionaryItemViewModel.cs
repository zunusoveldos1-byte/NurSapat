using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class WindowColorDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _hexCode = "#FFFFFF";

    public IBrush Brush => CreateBrush(HexCode);

    partial void OnHexCodeChanged(string value) => OnPropertyChanged(nameof(Brush));

    public static WindowColorDictionaryItemViewModel FromModel(WindowColorItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        HexCode = item.HexCode
    };

    public WindowColorItem ToModel() => new()
    {
        Id = Id,
        Name = Name,
        HexCode = HexCode
    };

    private static IBrush CreateBrush(string hexCode)
    {
        try
        {
            return new SolidColorBrush(Color.Parse(hexCode));
        }
        catch
        {
            return Brushes.LightGray;
        }
    }
}
