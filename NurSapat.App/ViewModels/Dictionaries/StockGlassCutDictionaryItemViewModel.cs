using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Stock;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class StockGlassCutDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _article = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = string.Empty;

    [ObservableProperty]
    private double _widthMm;

    [ObservableProperty]
    private double _heightMm;

    public string WidthDisplay => $"{WidthMm:N0}";

    public string HeightDisplay => $"{HeightMm:N0}";

    partial void OnWidthMmChanged(double value) => OnPropertyChanged(nameof(WidthDisplay));

    partial void OnHeightMmChanged(double value) => OnPropertyChanged(nameof(HeightDisplay));

    public static StockGlassCutDictionaryItemViewModel FromModel(GlassCutItem item) => new()
    {
        Id = item.Id,
        Article = item.Article,
        Name = item.Name,
        Color = item.Color,
        WidthMm = item.WidthMm,
        HeightMm = item.HeightMm
    };

    public GlassCutItem ToModel() => new()
    {
        Id = Id,
        Article = Article,
        Name = Name,
        Color = Color,
        WidthMm = WidthMm,
        HeightMm = HeightMm
    };

    public void RefreshFrom(GlassCutItem item)
    {
        Article = item.Article;
        Name = item.Name;
        Color = item.Color;
        WidthMm = item.WidthMm;
        HeightMm = item.HeightMm;
    }
}
