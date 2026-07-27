using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Services.Orders;

public interface IDealersCatalog
{
    IReadOnlyList<Dealer> GetAll();

    Dealer? GetById(Guid id);
}

/// <summary>Раздел дилеров временно отключён — без фиктивных данных.</summary>
public sealed class DealersCatalog : IDealersCatalog
{
    public IReadOnlyList<Dealer> GetAll() => [];

    public Dealer? GetById(Guid id) => null;
}
