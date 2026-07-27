using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Stock;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class StockProfileCutDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _article = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = string.Empty;

    [ObservableProperty]
    private double _lengthMm;

    public string LengthDisplay => $"{LengthMm:N0}";

    partial void OnLengthMmChanged(double value) => OnPropertyChanged(nameof(LengthDisplay));

    public static StockProfileCutDictionaryItemViewModel FromModel(ProfileCutItem item) => new()
    {
        Id = item.Id,
        Article = item.Article,
        Name = item.Name,
        Color = item.Color,
        LengthMm = item.LengthMm
    };

    public ProfileCutItem ToModel() => new()
    {
        Id = Id,
        Article = Article,
        Name = Name,
        Color = Color,
        LengthMm = LengthMm
    };

    public void RefreshFrom(ProfileCutItem item)
    {
        Article = item.Article;
        Name = item.Name;
        Color = item.Color;
        LengthMm = item.LengthMm;
    }
}
