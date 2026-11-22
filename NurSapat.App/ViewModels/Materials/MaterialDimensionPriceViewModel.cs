using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Materials;

namespace NurSapat.App.ViewModels.Materials;

public sealed partial class MaterialDimensionPriceViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private decimal _widthMm;

    [ObservableProperty]
    private decimal _heightMm;

    [ObservableProperty]
    private decimal _price;

    public string PriceDisplay => $"{Price:N2} сом";

    partial void OnPriceChanged(decimal value) => OnPropertyChanged(nameof(PriceDisplay));

    public static MaterialDimensionPriceViewModel FromModel(MaterialDimensionPrice model) => new()
    {
        Id = model.Id,
        WidthMm = model.WidthMm,
        HeightMm = model.HeightMm,
        Price = model.Price
    };

    public MaterialDimensionPrice ToModel() => new()
    {
        Id = Id,
        WidthMm = WidthMm,
        HeightMm = HeightMm,
        Price = Price
    };
}
