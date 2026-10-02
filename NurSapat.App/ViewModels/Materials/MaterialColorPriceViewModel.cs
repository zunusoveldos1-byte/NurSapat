using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Materials;

namespace NurSapat.App.ViewModels.Materials;

public sealed partial class MaterialColorPriceViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private string _colorArticle = string.Empty;

    [ObservableProperty]
    private string _colorName = "Белый";

    [ObservableProperty]
    private string _colorHex = "#FFFFFF";

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private string _linkArticle = string.Empty;

    [ObservableProperty]
    private bool _isDefault;

    [ObservableProperty]
    private bool _useCustomCutLength;

    [ObservableProperty]
    private decimal? _customBarLengthMm;

    public string PriceDisplay => $"{Price:N2} сом";

    partial void OnPriceChanged(decimal value) => OnPropertyChanged(nameof(PriceDisplay));

    public static MaterialColorPriceViewModel FromModel(MaterialColorPrice model) => new()
    {
        Id = model.Id,
        ColorArticle = model.ColorArticle,
        ColorName = model.ColorName,
        ColorHex = model.ColorHex,
        Price = model.Price,
        LinkArticle = model.LinkArticle,
        IsDefault = model.IsDefault,
        UseCustomCutLength = model.UseCustomCutLength,
        CustomBarLengthMm = model.CustomBarLengthMm
    };

    public MaterialColorPrice ToModel() => new()
    {
        Id = Id,
        ColorArticle = ColorArticle,
        ColorName = ColorName,
        ColorHex = ColorHex,
        Price = Price,
        LinkArticle = LinkArticle,
        IsDefault = IsDefault,
        UseCustomCutLength = UseCustomCutLength,
        CustomBarLengthMm = CustomBarLengthMm
    };
}
