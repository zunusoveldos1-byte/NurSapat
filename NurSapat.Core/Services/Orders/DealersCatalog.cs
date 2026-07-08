using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Services.Orders;

public interface IDealersCatalog
{
    IReadOnlyList<Dealer> GetAll();

    Dealer? GetById(Guid id);
}

public sealed class DealersCatalog : IDealersCatalog
{
    private readonly List<Dealer> _dealers =
    [
        new()
        {
            CompanyName = "Дилер «СтройПлюс»",
            ContactPerson = "Асанов К.Т.",
            DiscountPercentage = 12m,
            CreditLimit = 100_000m,
            CurrentDebt = 42_000m,
            DiscountCategory = "B"
        },
        new()
        {
            CompanyName = "ОсОО «ТеплоДом»",
            ContactPerson = "Мамытов Э.С.",
            DiscountPercentage = 18m,
            CreditLimit = 250_000m,
            CurrentDebt = 85_000m,
            DiscountCategory = "A"
        },
        new()
        {
            CompanyName = "ИП «Алма-Арча»",
            ContactPerson = "Жумалиева А.К.",
            DiscountPercentage = 8m,
            CreditLimit = 50_000m,
            CurrentDebt = 12_500m,
            DiscountCategory = "C"
        }
    ];

    public IReadOnlyList<Dealer> GetAll() => _dealers;

    public Dealer? GetById(Guid id) => _dealers.FirstOrDefault(d => d.Id == id);
}
