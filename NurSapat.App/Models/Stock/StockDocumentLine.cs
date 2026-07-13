using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Core.Validation;

namespace NurSapat.App.Models.Stock;

public sealed partial class StockDocumentLine : ObservableObject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid MaterialId { get; set; }

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

    public void Validate()
    {
        DomainInvariants.RequireNonEmptyGuid(MaterialId, nameof(MaterialId));
        DomainInvariants.RequireNonEmpty(Article, nameof(Article));
        DomainInvariants.RequireNonEmpty(Name, nameof(Name));
        DomainInvariants.RequireNonNegative(Quantity, nameof(Quantity));
        DomainInvariants.RequireNonNegative(Price, nameof(Price));
    }

    public StockDocumentLine Clone() => new()
    {
        Id = Id,
        MaterialId = MaterialId,
        Article = Article,
        Name = Name,
        Color = Color,
        Unit = Unit,
        Quantity = Quantity,
        Price = Price,
        Currency = Currency
    };
}
