namespace NurSapat.Core.Models.Pricing;

public sealed class PriceListVersion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public List<PriceListItem> Items { get; } = [];
}

public sealed class PriceListItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PriceListVersionId { get; set; }
    public string MaterialArticle { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal BaseRetailPrice { get; set; }
    public decimal DealerPrice { get; set; }
}
