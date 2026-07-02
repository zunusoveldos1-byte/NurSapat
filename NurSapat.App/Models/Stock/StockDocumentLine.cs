using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.App.Models.Stock;

public sealed partial class StockDocumentLine : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [ObservableProperty]
    private string _article = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _color = string.Empty;

    [ObservableProperty]
    private string _unit = string.Empty;

    [ObservableProperty]
    private decimal _quantity;

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private string _currency = "KGS";

    public decimal TotalSum => Quantity * Price;

    partial void OnQuantityChanged(decimal value) => OnPropertyChanged(nameof(TotalSum));

    partial void OnPriceChanged(decimal value) => OnPropertyChanged(nameof(TotalSum));

    public StockDocumentLine Clone() => new()
    {
        Id = Id,
        Article = Article,
        Name = Name,
        Color = Color,
        Unit = Unit,
        Quantity = Quantity,
        Price = Price,
        Currency = Currency
    };
}
